using System;

namespace Nexeron.Models
{
    public partial class facturas
    {
        public string NombreCliente { get; set; }
        public string NombreEstado { get; set; }
        public decimal BaseTotal { get; set; }
        public decimal ImporteTotal { get; set; }
        public decimal BaseImponible => Math.Round((decimal)(CANTI * EUROS * (1m - (DTOARTI / 100))), 2);
        public decimal ImporteIva => Math.Round((decimal)(BaseImponible * (IVARTI / 100)), 2);
        public decimal TotalLinea => BaseImponible + ImporteIva;

    }
}