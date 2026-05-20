using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Nexeron.Models
{
    public class ArticuloProveedorModel
    {
        public int id { get; set; }
        public int articulo_codigo { get; set; }
        public string proveedor_codigo { get; set; }
        public string descripcion_proveedor { get; set; }
        public decimal tarifa { get; set; }
        public decimal descuento { get; set; }
        public string unidad { get; set; }
    }
}