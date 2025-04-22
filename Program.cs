using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Portfolio_Backend.Data;

var builder = WebApplication.CreateBuilder(args);

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
          ValidateIssuer           = true,
          ValidateAudience         = true,
          ValidateLifetime         = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer              = builder.Configuration["Jwt:Issuer"],
          ValidAudience            = builder.Configuration["Jwt:Audience"],
          IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
      };
  });

// 3) Controllers + EF Core
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

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
        [ new OpenApiSecurityScheme 
            { Reference = new OpenApiReference 
                { Type = ReferenceType.SecurityScheme, Id = "cookieAuth" } 
            }
        ] = Array.Empty<string>()
    });
});

var app = builder.Build();

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

app.MapGet("/", () => Results.Text("Portfolio API is running!"));

app.Run();
