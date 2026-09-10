using Microsoft.EntityFrameworkCore;
using vehiculo.api.Models;

namespace vehiculo.api.Data
{
    public class VehiculoDBContext : DbContext
    {
        public VehiculoDBContext(DbContextOptions<VehiculoDBContext> options) : base(options)
        {
        }

        public DbSet<Vehiculo> Vehiculos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Le indica a EF Core cuál es la clave primaria real
            modelBuilder.Entity<Vehiculo>()
                .HasKey(v => v.IdVehiculo);

            // Le indica a EF Core que el valor lo genera la base de datos (Identity)
            modelBuilder.Entity<Vehiculo>()
                .Property(v => v.IdVehiculo)
                .ValueGeneratedOnAdd();
        }
    }
}