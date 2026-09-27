using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SportsCenterAPI.Data;
using SportsCenterAPI.Middleware;
using SportsCenterAPI.Services.Implement;
using SportsCenterAPI.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAPI: tạo tài liệu API và cấu hình nút Authorize.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Info.Title = "Sports Center API";
            document.Info.Version = "v1";

            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes ??=
                new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes["Bearer"] =
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Dán JWT token, không cần nhập chữ Bearer."
                };

            return Task.CompletedTask;
        });

    options.AddOperationTransformer(
        (operation, context, cancellationToken) =>
        {
            var metadata =
                context.Description.ActionDescriptor.EndpointMetadata;

            var requiresAuthorization =
                metadata.OfType<IAuthorizeData>().Any();

            var allowsAnonymous =
                metadata.OfType<IAllowAnonymous>().Any();

            if (requiresAuthorization && !allowsAnonymous)
            {
                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                context.Document)
                        ] = []
                    }
                ];
            }

            return Task.CompletedTask;
        });
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Invalid account identifier.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                var account = await db.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new
                    {
                        user.IsActive,
                        user.Role
                    })
                    .SingleOrDefaultAsync(
                        context.HttpContext.RequestAborted);

                var tokenRole = context.Principal?
                    .FindFirstValue(ClaimTypes.Role);

                if (account == null ||
                    !account.IsActive ||
                    account.Role != tokenRole)
                {
                    context.Fail(
                        "Account is inactive or permissions have changed. " +
                        "Please sign in again.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IMembershipPackageService, MembershipPackageService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IClassCatalogService, ClassCatalogService>();

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Middleware xử lý lỗi
app.UseMiddleware<ExceptionMiddleware>();

// Swagger UI
if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Sports Center API v1");

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Sports Center API";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Tự áp dụng migration khi khởi động, như bản bạn gửi.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    db.Database.Migrate();
}

app.Run();

// Cho phép integration test khởi tạo ứng dụng.
public partial class Program { }