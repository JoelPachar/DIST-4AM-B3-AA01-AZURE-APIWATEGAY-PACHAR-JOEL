using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vehiculo.api.Data;
using vehiculo.api.Models;

namespace vehiculo.api.Controllers
{
    // Ruta base: http://localhost:tu-puerto/api/vehiculo
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Permite el acceso base a cualquier usuario autenticado (Usuario y Administrador)
    public class VehiculoController : ControllerBase
    {
        private readonly VehiculoDBContext _dbContext;

        public VehiculoController(VehiculoDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // VER (Todos): Accesible para User y Administrador
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehiculo>>> GetAllVehiculos()
        {
            var vehiculos = await _dbContext.Vehiculos
                .AsNoTracking()
                .ToListAsync();
            return Ok(vehiculos);
        }

        // VER (Todos): Accesible para User y Administrador
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehiculo>> GetVehiculoById(int id)
        {
            var vehiculo = await _dbContext.Vehiculos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVehiculo == id);

            if (vehiculo == null)
            {
                return NotFound();
            }
            return Ok(vehiculo);
        }

        // AGREGAR (Solo Admin): Restringido exclusivamente al Administrador
        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<ActionResult<Vehiculo>> CreateVehiculo(Vehiculo vehiculo)
        {
            _dbContext.Vehiculos.Add(vehiculo);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehiculoById), new { id = vehiculo.IdVehiculo }, vehiculo);
        }

        // NADA MÁS: Bloqueado. Se exige un rol inexistente ("Ninguno") para que ni el Admin ni el User puedan actualizar.
        [Authorize(Roles = "Ninguno")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehiculo(int id, Vehiculo vehiculo)
        {
            if (id != vehiculo.IdVehiculo)
            {
                return BadRequest();
            }

            _dbContext.Entry(vehiculo).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // NADA MÁS: Bloqueado. Se exige un rol inexistente ("Ninguno") para que ni el Admin ni el User puedan eliminar.
        [Authorize(Roles = "Ninguno")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehiculo(int id)
        {
            var vehiculo = await _dbContext.Vehiculos.FindAsync(id);
            if (vehiculo == null)
            {
                return NotFound();
            }

            _dbContext.Vehiculos.Remove(vehiculo);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}