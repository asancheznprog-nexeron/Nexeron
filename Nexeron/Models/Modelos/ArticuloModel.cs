using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Nexeron.Models
{
    public class ArticuloModel
    {
        public int codigo { get; set; }

        [Required(ErrorMessage = "El nombre del artículo es obligatorio")]
        public string articulo { get; set; }

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

        public decimal volumen => longitud * altura * anchura;

        public IEnumerable<SelectListItem> TiposList { get; set; }
        public IEnumerable<SelectListItem> UnidadesList { get; set; }
        public IEnumerable<SelectListItem> PaisesList { get; set; }
    }
}