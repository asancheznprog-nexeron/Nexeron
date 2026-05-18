using System;
using System.ComponentModel.DataAnnotations;

namespace Nexeron.Models
{
    public class ArticuloModel
    {
        public int codigo { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        public string articulo { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string desarti { get; set; }

        public string descripcion { get; set; }
        public decimal longitud { get; set; }
        public decimal altura { get; set; }
        public decimal anchura { get; set; }
        public string tipo { get; set; }
        public string unidad_medida { get; set; }
        public string pais_origen { get; set; }
        public decimal iva { get; set; }
        public bool es_reutilizable { get; set; }
        public decimal huella_carbono { get; set; }
        public decimal dto_base { get; set; }
        public DateTime? fecha_alta { get; set; }
        public DateTime? fecha_baja { get; set; }
        public bool activo { get; set; }
        public string observaciones { get; set; }
    }
}