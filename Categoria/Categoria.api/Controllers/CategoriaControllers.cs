using Categoria.Api.Data;
using Categoria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Categoria.Api.Controllers
{
    // Ruta base: http://localhost:7114/api/categorias
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly CategoriaDBContext _dbContext;
        private readonly RabbitMQPublisher _publisher;
        private readonly ILogger<CategoriasController> _logger;

        // Constructor con RabbitMQPublisher inyectado
        public CategoriasController(CategoriaDBContext dbContext, RabbitMQPublisher publisher, ILogger<CategoriasController> logger)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _logger = logger;
        }

        // Permitido para cualquier usuario autenticado (solo listar)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categoria.Api.Models.Categoria>>> GetAllCategorias()
        {
            var categorias = await _dbContext.Categorias
                .AsNoTracking()
                .ToListAsync();

            return Ok(categorias);
        }

        // Permitido para cualquier usuario autenticado (solo listar por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Categoria.Api.Models.Categoria>> GetCategoriaById(int id)
        {
            var categoria = await _dbContext.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCategoria == id);

            if (categoria == null)
            {
                return NotFound();
            }

            return Ok(categoria);
        }

        // Solo el Administrador puede crear
        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<ActionResult<Categoria.Api.Models.Categoria>> CreateCategoria(Categoria.Api.Models.Categoria categoria)
        {
            _dbContext.Categorias.Add(categoria);
            await _dbContext.SaveChangesAsync();

            // Publicar evento a RabbitMQ en background
            try
            {
                await _publisher.PublicarCategoriaCreadaAsync(categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al publicar categoria en RabbitMQ");
                // No bloquear la respuesta si falla la publicación
            }

            // Retorna un 201 Created y la URL para consultar el nuevo recurso
            return CreatedAtAction(nameof(GetCategoriaById), new { id = categoria.IdCategoria }, categoria);
        }

        // Solo el Administrador puede actualizar
        [Authorize(Roles = "Administrador")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategoria(int id, Categoria.Api.Models.Categoria categoria)
        {
            // Verificamos usando IdCategoria
            if (id != categoria.IdCategoria)
            {
                return BadRequest();
            }

            _dbContext.Entry(categoria).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Bloqueado para el rol Usuario: Solo el Administrador puede eliminar (y se añade validación extra de rol por seguridad)
        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            // Validación extra: Si por alguna razón un usuario común llega aquí, se le niega el permiso explícitamente
            if (User.IsInRole("Usuario"))
            {
                return Forbid();
            }

            var categoria = await _dbContext.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }

            _dbContext.Categorias.Remove(categoria);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}