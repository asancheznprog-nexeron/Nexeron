namespace Nexeron.Models
{
    public class InventarioMovimientoModel
    {
        public int Id { get; set; }
        public string Articulo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string Tipo { get; set; }
        public string Origen { get; set; }
        public string Referencia { get; set; }
        public string Cuenta { get; set; }
    }
}