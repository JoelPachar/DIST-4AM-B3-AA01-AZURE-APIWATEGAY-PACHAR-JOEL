using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Categoria.Api.Models
{
    [Table("Categoria")]
    public class Categoria
    {
        [Key]
        [Column("IdCategoria")]
        public int IdCategoria { get; set; }

        [StringLength(100)]
        [Column("Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("Descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("Estado")]
        public bool Estado { get; set; }
    }
}