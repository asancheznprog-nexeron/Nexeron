using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexeron.Models
{
    public class LoginModel
    {
        [DataType(DataType.Text)]
        [Display(Name = "Empresa")]
        //[StringLength(15)]
        public string Empresa { get; set; }


        [DataType(DataType.Text)]
        [Display(Name = "Usuario")]
        //[StringLength(15)]
        public string Usuario { get; set; }


        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        //[StringLength(8)]
        public string Password { get; set; }


        [DataType(DataType.Text)]
        [Display(Name = "Ejercicio")]
        //[StringLength(4)]
        public string Ejercicio { get; set; }

    }
    [Table("usuarios")]
    public class usuario
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int CLAVE { get; set; }
        public string USUARIO { get; set; }
        public string PASSWORD { get; set; }
        public string PROGRAMA { get; set; }
    }
    public class LoginModel1
    {
        [Required(ErrorMessage = "Usuario no puede estar en blanco*")]
        [DataType(DataType.Text)]
        [Display(Name = "Usuario")]
        [StringLength(12)]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "Contraseña no puede estar en blanco*")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StringLength(8)]
        public string Password { get; set; }
    }
}