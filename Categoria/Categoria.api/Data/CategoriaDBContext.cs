using Microsoft.EntityFrameworkCore;

namespace Categoria.Api.Data
{
    public class CategoriaDBContext : DbContext
    {
        public CategoriaDBContext(DbContextOptions<CategoriaDBContext> options) : base(options)
        {
        }

        // DbSet para la entidad Categoria
        public DbSet<Categoria.Api.Models.Categoria> Categorias { get; set; }
    }
}
