using DotNetEnv;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Portfolio_Backend.Data;

Env.Load();  

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddEnvironmentVariables();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDev", policy =>
    {
        policy
          .WithOrigins(
              "http://localhost:5173",  
              "https://localhost:5001"   
          )
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    });
});

// === JWT‐Bearer Authentication ===
// read and validate your JWT settings from Configuration
var jwtKey = builder.Configuration["Jwt:Key"] 
             ?? throw new ArgumentException("Missing environment variable Jwt__Key");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
                ?? throw new ArgumentException("Missing environment variable Jwt__Issuer");
var jwtAudience = builder.Configuration["Jwt:Audience"] 
                  ?? throw new ArgumentException("Missing environment variable Jwt__Audience");

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.Events = new JwtBearerEvents
      {
          OnMessageReceived = ctx =>
          {
              if (ctx.Request.Cookies.TryGetValue("access_token", out var token))
                  ctx.Token = token;
              return Task.CompletedTask;
          }
      };

      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
          ValidateIssuer           = true,
          ValidIssuer              = jwtIssuer,
          ValidateAudience         = true,
          ValidAudience            = jwtAudience,
          ValidateLifetime         = true,
          ClockSkew                = TimeSpan.Zero
      };
  });

// === Controllers + EF Core ===
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(
      builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// === Swagger (with cookieAuth scheme) ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Name        = "access_token",
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Cookie,
        Description = "HttpOnly JWT stored in a cookie named access_token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [ new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id   = "cookieAuth"
            }
        } ] = new string[] { }
    });
});

var app = builder.Build();

// apply CORS before auth
app.UseCors("AllowDev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Portfolio API V1");
        c.ConfigObject.AdditionalItems["withCredentials"] = true;
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// health check
app.MapGet("/", () => Results.Text("Portfolio API is running!"));

app.Run();
