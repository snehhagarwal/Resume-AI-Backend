using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResumeAI.AI.API.Clients;
using ResumeAI.AI.API.Data;
using ResumeAI.AI.API.Repositories;
using ResumeAI.AI.API.Services;
using ResumeAI.AI.API.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────
builder.Services.AddDbContext<AiDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("AiDb")));

// ─── Repositories & services ─────────────────────────────────────
builder.Services.AddScoped<IAiRequestRepository, AiRequestRepository>();
builder.Services.AddScoped<IAiService, AiService>();

// ─── Resume context client ────────────────────────────────────────
// IHttpContextAccessor lets the client forward the caller's JWT to
// internal microservices so they can authorise the request.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IResumeContextClient, ResumeContextClient>();

builder.Services.AddHttpClient("ResumeApiClient", client =>
{
    var baseUrl = builder.Configuration["InternalServices:ResumeApiBaseUrl"]
                  ?? "http://localhost:5002";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("SectionApiClient", client =>
{
    var baseUrl = builder.Configuration["InternalServices:SectionApiBaseUrl"]
                  ?? "http://localhost:5003";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ─── Redis quota tracking ─────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opts =>
{
    opts.Configuration = builder.Configuration["Redis:ConnectionString"]
        ?? "localhost:6379";
});

// ─── Background Services ──────────────────────────────────────────
builder.Services.AddHostedService<QuotaResetService>();

// ─── JWT authentication ───────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("PremiumOnly", p => p.RequireClaim("plan", "PREMIUM"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ResumeAI.AI.API", Version = "v1" });

    // Add JWT bearer definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token directly in the text input below."
    });

    // Require token for all endpoints
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
    db.Database.Migrate();
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