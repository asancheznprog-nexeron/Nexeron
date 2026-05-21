using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Nexeron.Models
{
    public class TipoModel
    {
        public int codigo { get; set; }
        public string descripcion { get; set; }
        public string descripcion_detallada { get; set; }
    }
}