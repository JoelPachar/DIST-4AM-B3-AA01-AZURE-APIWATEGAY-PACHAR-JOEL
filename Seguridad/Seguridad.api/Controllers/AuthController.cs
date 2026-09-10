using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Seguridad.api.Controllers // <-- Namespace actualizado al nuevo proyecto
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            string rol;

            // Usuarios de prueba
            if (request.Usuario == "admin" && request.Password == "1234")
            {
                rol = "Administrador";
            }
            else if (request.Usuario == "usuario" && request.Password == "1234")
            {
                rol = "Usuario";
            }
            else
            {
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            // Datos que se guardarán dentro del token
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.Usuario),
                new Claim(ClaimTypes.Role, rol)
            };

            // Clave secreta (leída del appsettings.json o Docker compose)
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            // Firma del token
            var credenciales = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            // Crear JWT
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:DurationInMinutes"])
                ),
                signingCredentials: credenciales
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                usuario = request.Usuario,
                rol = rol
            });
        }
    }

    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}