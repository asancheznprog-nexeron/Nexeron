using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class InventarioController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<InventarioStockModel> lista = new List<InventarioStockModel>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                con.Open();
                string sql = @"SELECT articulo, 
                                      MAX(descripcion) as descripcion,
                                      SUM(cantidad) as stock
                               FROM inventario 
                               GROUP BY articulo 
                               ORDER BY articulo ASC";
                using (var cmd = new MySqlCommand(sql, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new InventarioStockModel
                        {
                            Articulo = reader["articulo"].ToString(),
                            Descripcion = reader["descripcion"]?.ToString() ?? "",
                            Stock = Convert.ToDecimal(reader["stock"])
                        });
                    }
                }
            }

            ViewBag.Unidades = ObtenerUnidades();
            return View(lista);
        }

        [HttpGet]
        public JsonResult ObtenerMovimientosArticulo(string articulo)
        {
            List<object> movimientos = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(articulo))
                return Json(movimientos, JsonRequestBehavior.AllowGet);

            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                con.Open();
                string sql = @"SELECT id, articulo, descripcion, cantidad, tipo, origen, referencia, cuenta, fecha 
                               FROM inventario 
                               WHERE articulo = @articulo 
                               ORDER BY fecha DESC, id DESC";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@articulo", articulo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            movimientos.Add(new
                            {
                                id = Convert.ToInt32(reader["id"]),
                                articulo = reader["articulo"].ToString(),
                                descripcion = reader["descripcion"].ToString(),
                                cantidad = Convert.ToDecimal(reader["cantidad"]),
                                tipo = reader["tipo"].ToString(),
                                origen = reader["origen"].ToString(),
                                referencia = reader["referencia"].ToString(),
                                cuenta = reader["cuenta"].ToString(),
                                fecha = Convert.ToDateTime(reader["fecha"]).ToString("dd/MM/yyyy HH:mm")
                            });
                        }
                    }
                }
            }
            return Json(movimientos, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarMovimientoManual(InventarioMovimientoModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Articulo))
                return Json(new { success = false, message = "Artículo requerido." });

            string connStr = Session["cadenaConexion"]?.ToString();
            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                con.Open();
                string descripcion = "";
                using (var cmdArt = new MySqlCommand("SELECT descripcion FROM articulo WHERE articulo = @art", con))
                {
                    cmdArt.Parameters.AddWithValue("@art", model.Articulo);
                    var res = cmdArt.ExecuteScalar();
                    if (res != null) descripcion = res.ToString();
                }

                if (model.Id > 0)
                {
                    using (var cmdUpd = new MySqlCommand(
                        @"UPDATE inventario SET articulo=@articulo, descripcion=@descripcion, cantidad=@cantidad, 
                          tipo=@tipo, origen=@origen, referencia=@referencia, cuenta=@cuenta, fecha=NOW() 
                          WHERE id=@id AND origen='MANUAL'", con))
                    {
                        cmdUpd.Parameters.AddWithValue("@id", model.Id);
                        cmdUpd.Parameters.AddWithValue("@articulo", model.Articulo);
                        cmdUpd.Parameters.AddWithValue("@descripcion", model.Descripcion ?? descripcion);
                        cmdUpd.Parameters.AddWithValue("@cantidad", model.Cantidad);
                        cmdUpd.Parameters.AddWithValue("@tipo", model.Tipo ?? "M");
                        cmdUpd.Parameters.AddWithValue("@origen", "MANUAL");
                        cmdUpd.Parameters.AddWithValue("@referencia", model.Referencia ?? "");
                        cmdUpd.Parameters.AddWithValue("@cuenta", model.Cuenta ?? "");
                        cmdUpd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var cmdIns = new MySqlCommand(
                        @"INSERT INTO inventario (articulo, descripcion, cantidad, tipo, origen, referencia, cuenta, fecha) 
                          VALUES (@articulo, @descripcion, @cantidad, @tipo, 'MANUAL', @referencia, @cuenta, NOW())", con))
                    {
                        cmdIns.Parameters.AddWithValue("@articulo", model.Articulo);
                        cmdIns.Parameters.AddWithValue("@descripcion", model.Descripcion ?? descripcion);
                        cmdIns.Parameters.AddWithValue("@cantidad", model.Cantidad);
                        cmdIns.Parameters.AddWithValue("@tipo", model.Tipo ?? "M");
                        cmdIns.Parameters.AddWithValue("@referencia", model.Referencia ?? "");
                        cmdIns.Parameters.AddWithValue("@cuenta", model.Cuenta ?? "");
                        cmdIns.ExecuteNonQuery();
                    }
                }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult EliminarMovimiento(int id)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                con.Open();
                using (var cmd = new MySqlCommand("DELETE FROM inventario WHERE id = @id AND origen = 'MANUAL'", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return Json(new { success = false, message = "Solo se pueden eliminar movimientos manuales." });
                }
            }
            return Json(new { success = true });
        }

        private List<KeyValuePair<string, string>> ObtenerUnidades()
        {
            var lista = new List<KeyValuePair<string, string>>();
            string connStr = Session["cadenaConexion"].ToString();
            using (var con = new MySqlConnection(connStr))
            {
                con.Open();
                using (var cmd = new MySqlCommand("SELECT codigo, descripcion FROM unidades ORDER BY codigo ASC", con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new KeyValuePair<string, string>(reader["codigo"].ToString(), reader["descripcion"].ToString()));
                    }
                }
            }
            return lista;
        }
    }
}