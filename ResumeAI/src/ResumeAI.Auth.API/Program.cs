using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResumeAI.Auth.API.Data;
using ResumeAI.Auth.API.Entities;
using ResumeAI.Auth.API.Repositories;
using ResumeAI.Auth.API.Services;
using Microsoft.OpenApi.Models;
using ResumeAI.Shared.Enums;
using ResumeAI.Auth.API.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ─── EF Core / PostgreSQL ────────────────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));

// ─── DI registrations ────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// ─── JWT + OAuth Authentication ───────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

// API endpoints are protected by JWT. The transient "OAuthCookies" scheme
// is only used as the SignInScheme for the OAuth2 handshake — it lets ASP.NET
// Core temporarily store the external identity between the redirect and our
// callback endpoint, where we exchange it for our own JWT and then clear the cookie.
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts =>
{
    // Short-lived: only needed for the OAuth round-trip (typically a few seconds).
    opts.Cookie.HttpOnly     = true;
    // SameAsRequest = works on both HTTP (dev) and HTTPS (prod).
    // 'Always' breaks on HTTP because the browser drops the Secure cookie
    // before it can be sent back on the /signin-google callback.
    opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opts.Cookie.SameSite     = SameSiteMode.Lax;
    opts.ExpireTimeSpan      = TimeSpan.FromMinutes(5);
})
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(jwtSecret))
    };
})
// ── Google OAuth2 ────────────────────────────────────────────────
.AddGoogle(opts =>
{
    opts.ClientId     = builder.Configuration["OAuth:Google:ClientId"]
                        ?? throw new InvalidOperationException("OAuth:Google:ClientId is not configured.");
    opts.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]
                        ?? throw new InvalidOperationException("OAuth:Google:ClientSecret is not configured.");
    // /signin-google is the default — keep it to avoid any CORS confusion.
    opts.CallbackPath     = "/signin-google";
    opts.SaveTokens       = false; // We issue our own JWT; no need to persist Google tokens.
    opts.Scope.Add("profile");
    opts.Scope.Add("email");
    // The OAuth middleware sets its own correlation cookie independently of AddCookie.
    // Without this, the correlation cookie gets Secure=true even on HTTP, which
    // causes the browser to drop it and fail the CSRF correlation check.
    opts.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opts.CorrelationCookie.SameSite     = SameSiteMode.Lax;
})
// ── LinkedIn OAuth2 (community package: AspNet.Security.OAuth.LinkedIn) ─
.AddLinkedIn(opts =>
{
    opts.ClientId     = builder.Configuration["OAuth:LinkedIn:ClientId"]
                        ?? throw new InvalidOperationException("OAuth:LinkedIn:ClientId is not configured.");
    opts.ClientSecret = builder.Configuration["OAuth:LinkedIn:ClientSecret"]
                        ?? throw new InvalidOperationException("OAuth:LinkedIn:ClientSecret is not configured.");
    opts.CallbackPath     = "/signin-linkedin";
    opts.SaveTokens       = false;
    opts.Scope.Add("openid");
    opts.Scope.Add("profile");
    opts.Scope.Add("email");
    // Same fix as Google: correlation cookie must not be Secure on plain HTTP.
    opts.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opts.CorrelationCookie.SameSite     = SameSiteMode.Lax;
});

// ─── Authorization policies ───────────────────────────────────────
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("PremiumOnly", p => p.RequireClaim("plan", "PREMIUM"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ResumeAI Auth API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token directly in the text input below."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── Auto-migrate on startup ─────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();

    // ─── Data Seeding ─────────────────────────────────────────────
    if (!db.Users.Any(u => u.Role == Role.ADMIN))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User
        {
            FullName = "System Admin",
            Email = "admin@resumeai.com",
            Role = Role.ADMIN,
            IsActive = true,
            SubscriptionPlan = SubscriptionPlan.PREMIUM,
            Provider = AuthProvider.LOCAL
        };
        admin.PasswordHash = hasher.HashPassword(admin, "AdminPassword123!");
        db.Users.Add(admin);
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
