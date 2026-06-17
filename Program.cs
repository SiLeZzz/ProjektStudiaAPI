using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WebAPI.Configuration;
using WebAPI.Data;
using WebAPI.Domain.Entities;
using WebAPI.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

var writeConnectionString = builder.Configuration.GetConnectionString("WriteConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'WriteConnection' is not configured.");
var readConnectionString = builder.Configuration.GetConnectionString("ReadConnection")
    ?? writeConnectionString;

var jwtOptionsSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtOptionsSection);
builder.Services.Configure<BootstrapAdminOptions>(
    builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));

var jwtOptions = jwtOptionsSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
{
    throw new InvalidOperationException("JWT secret key must contain at least 32 characters.");
}

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var httpContext = serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
    var connectionString = UseReadConnection(httpContext?.Request.Method)
        ? readConnectionString
        : writeConnectionString;

    options.UseNpgsql(connectionString);
});
builder.Services.AddHealthChecks();

builder.Services.AddScoped<IPasswordHasher<Pracownik>, PasswordHasher<Pracownik>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<AdminBootstrapper>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Title = "Brak autoryzacji.",
                    Detail = "Token uwierzytelniajacy jest wymagany albo niepoprawny.",
                    Status = StatusCodes.Status401Unauthorized
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Title = "Brak uprawnien.",
                    Detail = "Zalogowany uzytkownik nie ma uprawnien do wykonania tej operacji.",
                    Status = StatusCodes.Status403Forbidden
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wklej token JWT bez prefiksu Bearer."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null!),
            new List<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var bootstrapper = scope.ServiceProvider.GetRequiredService<AdminBootstrapper>();
    await bootstrapper.SeedAsync();
}

// Configure the HTTP request pipeline.

app.MapOpenApi();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static bool UseReadConnection(string? method)
{
    return method is not null &&
           (HttpMethods.IsGet(method) ||
           HttpMethods.IsHead(method) ||
           HttpMethods.IsOptions(method));
}
