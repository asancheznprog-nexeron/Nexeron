using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class PedidosController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<pedidos> lista = new List<pedidos>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"SELECT p.NUMPEDIDO, p.FECHPED, p.CUENTA, c.NOMBRE_FISCAL as NombreCliente, 
                                        p.ESTADO, p.FCOBRO, p.OBSERVACIONES
                                        FROM pedidos p
                                        LEFT JOIN clientes c ON p.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                        GROUP BY p.NUMPEDIDO, p.FECHPED, p.CUENTA, c.NOMBRE_FISCAL, p.ESTADO, p.FCOBRO, p.OBSERVACIONES
                                        ORDER BY p.NUMPEDIDO DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new pedidos
                            {
                                NUMPEDIDO = reader["NUMPEDIDO"].ToString(),
                                FECHPED = Convert.ToDateTime(reader["FECHPED"]),
                                CUENTA = reader["CUENTA"].ToString(),
                                NombreCliente = reader["NombreCliente"].ToString(),
                                ESTADO = reader["ESTADO"].ToString(),
                                FCOBRO = reader["FCOBRO"].ToString(),
                                OBSERVACIONES = reader["OBSERVACIONES"].ToString()
                            });
                        }
                    }
                }
            }
            return View(lista);
        }

        [HttpPost]
        public JsonResult ObtenerDetallePedido(string numPedido)
        {
            List<object> lineas = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numPedido))
                return Json(lineas);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"SELECT p.*, c.NOMBRE_FISCAL, c.CIF, c.DIRECCION, c.CP, c.POBLACION, c.PROVINCIA
                                        FROM pedidos p
                                        LEFT JOIN clientes c ON p.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                        WHERE p.NUMPEDIDO = @num
                                        ORDER BY p.NUMLINEA ASC";
                    cmd.Parameters.AddWithValue("@num", numPedido.PadLeft(9));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lineas.Add(new
                            {
                                NUMLINEA = Convert.ToInt32(reader["NUMLINEA"]),
                                ARTI = reader["ARTI"].ToString().Trim(),
                                DESARTI = reader["DESARTI"].ToString().Trim(),
                                CANTI = Convert.ToDecimal(reader["CANTI"]),
                                UNIDAD = reader["UNIDAD"].ToString().Trim(),
                                EUROS = Convert.ToDecimal(reader["EUROS"]),
                                DTOARTI = Convert.ToDecimal(reader["DTOARTI"]),
                                IVARTI = Convert.ToDecimal(reader["IVARTI"]),
                                CUENTA = reader["CUENTA"].ToString().Trim(),
                                FCOBRO = reader["FCOBRO"].ToString().Trim(),
                                ESTADO = reader["ESTADO"].ToString().Trim(),
                                FECHA = Convert.ToDateTime(reader["FECHPED"]).ToString("yyyy-MM-dd"),
                                OBSERVACIONES = reader["OBSERVACIONES"].ToString(),
                                NOMBRE_FISCAL = reader["NOMBRE_FISCAL"].ToString().Trim()
                            });
                        }
                    }
                }
            }
            return Json(lineas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(string id)
        {
            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM pedidos WHERE NUMPEDIDO = @num";
                    cmd.Parameters.AddWithValue("@num", id.PadLeft(9));
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
    }
}