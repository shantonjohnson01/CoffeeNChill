
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Azure;
using Azure.Data.Tables;
using BCrypt.Net;
using CoffeeNChill.Api.Middleware;
using CoffeeNChill.Api.Models;
using CoffeeNChill.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoffeeNChill API",
        Version = "v1",
        Description = "Secure gateway for CoffeeNChill menu, orders, and staff documents."
    });

    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Swagger sends it as: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddSingleton<UserTableService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<FunctionsProxyService>();

builder.Services.AddHttpClient("Functions", client =>
{
    string baseUrl =
        builder.Configuration["Functions:BaseUrl"]
        ?? "http://localhost:7071";

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

string signingKey =
    builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "Set Jwt__SigningKey as an environment variable.");

if (Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt__SigningKey must be at least 32 bytes.");
}

string issuer =
    builder.Configuration["Jwt:Issuer"] ?? "CoffeeNChill.Api";

string audience =
    builder.Configuration["Jwt:Audience"] ?? "CoffeeNChill.Client";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "CoffeeNChill.Api"
})).AllowAnonymous();

// Initialize the Users table and optionally seed the first Admin.
using (var scope = app.Services.CreateScope())
{
    var users = scope.ServiceProvider.GetRequiredService<UserTableService>();
    await users.InitializeAsync();

    string? adminEmail = builder.Configuration["ADMIN_EMAIL"];
    string? adminPassword = builder.Configuration["ADMIN_PASSWORD"];

    if (!string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        var existing = await users.FindByRoleAndEmailAsync(
            "Admin", adminEmail);

        if (existing is null)
        {
            var admin = new UserEntity
            {
                PartitionKey = "Admin",
                RowKey = UserTableService.NormalizeEmail(adminEmail),
                FullName = "CoffeeNChill Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                CreatedDate = DateTime.UtcNow
            };

            try
            {
                await users.CreateAsync(admin);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // Another startup instance may have seeded this account.
            }
        }
    }
}

app.Run();
