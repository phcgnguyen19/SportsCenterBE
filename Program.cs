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
using SportsCenterAPI.Models;
using System.Security.Claims;

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

        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any() &&
            !metadata.OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Any())
        {
            operation.Security = [new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            }];
        }
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
        // Role changes and soft deletion invalidate previously issued tokens immediately.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                {
                    context.Fail("Invalid account identifier.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var account = await db.Users.AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new { user.IsActive, user.Role })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                if (account is null || !account.IsActive || account.Role != context.Principal?.FindFirstValue(ClaimTypes.Role))
                    context.Fail("Account is inactive or permissions have changed. Please sign in again.");
            }
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
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IMembershipPackageService, MembershipPackageService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IClassService, ClassService>();

var app = builder.Build();

// === Configure Middleware Pipeline ===

// Global exception handler
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("OpenApi:Enabled"))
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
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    db.Database.Migrate();
//}


app.Run();

// Allows the integration-test host to run the actual API pipeline.
public partial class Program { }

