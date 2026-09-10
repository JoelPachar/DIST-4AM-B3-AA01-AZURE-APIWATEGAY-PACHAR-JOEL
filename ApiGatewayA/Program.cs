using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// 1. Configurar Autenticación JWT leyendo las variables de entorno
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Útil para pruebas locales en Docker
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty))
        };
    });

// 2. Configurar Autorización y definir la política
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("JwtPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
});

// 3. Registrar YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 4. Activar los middlewares (el orden es obligatorio)
app.UseAuthentication();
app.UseAuthorization();

// 5. Mapear YARP y exigir la política de seguridad
app.MapReverseProxy()
   .RequireAuthorization("JwtPolicy");

app.Run();