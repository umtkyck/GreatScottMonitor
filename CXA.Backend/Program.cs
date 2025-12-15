using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CXA.Backend.Data;
using CXA.Backend.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Auth0
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

if (!string.IsNullOrEmpty(auth0Domain) && !string.IsNullOrEmpty(auth0Audience))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });
}

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (connectionString?.Contains("Data Source") == true && connectionString?.Contains(".db") == true)
    {
        // SQLite for development
        options.UseSqlite(connectionString);
    }
    else if (connectionString?.Contains("Host=") == true || connectionString?.Contains("postgres") == true)
    {
        // PostgreSQL
        options.UseNpgsql(connectionString);
    }
    else
    {
        // SQL Server
        options.UseSqlServer(connectionString ?? "Server=(localdb)\\mssqllocaldb;Database=CXA;Trusted_Connection=True;");
    }
});

// Application services
builder.Services.AddScoped<IFaceVerificationService, FaceVerificationService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Add HttpClient for Auth0Service
builder.Services.AddHttpClient<IAuth0Service, Auth0Service>(client =>
{
    var domain = builder.Configuration["Auth0:Domain"];
    if (!string.IsNullOrEmpty(domain))
    {
        client.BaseAddress = new Uri($"https://{domain}/");
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

