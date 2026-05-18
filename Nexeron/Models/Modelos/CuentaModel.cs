using System.ComponentModel.DataAnnotations;

namespace Nexeron.Models
{
    public class CuentaModel
    {

        [Required(ErrorMessage = "El código es obligatorio")]
        [Display(Name = "Código Contable")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre de la Cuenta")]
        public string Nombre { get; set; }
    }
}