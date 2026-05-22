using System;
using System.ComponentModel.DataAnnotations;

namespace Nexeron.Models
{
    public class pedidos
    {
        public int clave { get; set; }

        [Required]
        [StringLength(9)]
        public string NUMPEDIDO { get; set; }

        public DateTime FECHPED { get; set; }

        public DateTime? FECHENT { get; set; }

        [StringLength(9)]
        public string NUMOFERTA { get; set; }

        [Required]
        [StringLength(9)]
        public string CUENTA { get; set; }

        [StringLength(3)]
        public string FCOBRO { get; set; }

        public int NUMLINEA { get; set; }

        [StringLength(13)]
        public string ARTI { get; set; }

        [StringLength(75)]
        public string DESARTI { get; set; }

        [StringLength(2)]
        public string UNIDAD { get; set; }

        public decimal CANTI { get; set; }

        public decimal EUROS { get; set; }

        public decimal IVARTI { get; set; }

        public decimal DTOARTI { get; set; }

        [StringLength(3)]
        public string ESTADO { get; set; }

        [StringLength(3)]
        public string ESTADOLIN { get; set; }

        public string OBSERVACIONES { get; set; }

        // Propiedad extendida para la vista (no está en BBDD)
        public string NombreCliente { get; set; }
    }
}