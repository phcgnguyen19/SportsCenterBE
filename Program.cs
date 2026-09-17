using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Scalar.AspNetCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Middleware;
using SportsCenterAPI.Services.Implement;
using SportsCenterAPI.Services.Interface;
using Microsoft.OpenApi;
using SportsCenterAPI.Models.DTOs.Class;
using SportsCenterAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// === Add Services ===

// Controllers
builder.Services.AddControllers();

// OpenAPI/Scalar
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter JWT Bearer Token."
            };

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            }
        ];

        return Task.CompletedTask;
    });
});

// Entity Framework Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };
    });
builder.Services.AddAuthorization();

// CORS (cho Frontend gọi API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Node.js frontend port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register Services (DI)

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISportClassService, SportClassService>();

// Cấu hình AutoMapper
builder.Services.AddAutoMapper(o =>
{
    o.CreateMap<SportClass, SportClassResponse>()
        .ForMember(
            dest => dest.SportName,
            opt => opt.MapFrom(src => src.Sport.Name))
        .ForMember(
            dest => dest.RegisteredCount,
            opt => opt.Ignore())
        .ForMember(
            dest => dest.AvailableSlots,
            opt => opt.Ignore());
});
var app = builder.Build();

// === Configure Middleware Pipeline ===

// Global exception handler
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate database on startup (development only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();

