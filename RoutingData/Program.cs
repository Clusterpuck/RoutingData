using Microsoft.EntityFrameworkCore;
using RoutingData.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RoutingData.DTO;
using Microsoft.OpenApi.Models;  // Import for Swagger OpenApi

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI and configure JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token as Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// Add the singleton service for OfflineDatabase
builder.Services.AddSingleton<OfflineDatabase>();

// Set up CORS for local development
var connection = string.Empty;
Console.WriteLine($"Current Environment: {builder.Environment.EnvironmentName}");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowLocalhostFrontend", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    Console.WriteLine("Set CORS for local environment");

    // Load environment variables and development-specific settings
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
    connection = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
    Console.WriteLine("In development condition");
}
else
{
    connection = "Server=tcp:quantumsqlserver.database.windows.net,1433;Initial Catalog=quantumDelivery;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default";
}

// JWT Authentication setup
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Add DbContext with the connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connection));

// Build the app
var app = builder.Build();

// Set up CORS and Swagger in development environment
if (builder.Environment.IsDevelopment())
{
    app.UseCors("AllowLocalhostFrontend");

    // Configure Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware for authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Map the controllers
app.MapControllers();

// Run the app
app.Run();
