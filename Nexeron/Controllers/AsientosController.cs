using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class AsientosController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<asientos> lista = new List<asientos>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT a.ASIENTO, a.FECHA_ASIENTO, a.CUENTA, IFNULL(cue.NOMBRE_CUENTA, '') as NombreCliente,
                                   IF(a.DH = 'D', a.EUROS, 0) as Debe,
                                   IF(a.DH = 'H', a.EUROS, 0) as Haber,
                                   a.CONCEPTO, a.OBSERVACION
                            FROM asientos a
                            LEFT JOIN cuentas cue ON a.CUENTA = cue.CUENTA COLLATE utf8mb4_spanish_ci
                            ORDER BY a.ASIENTO DESC, a.clave ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new asientos
                                {
                                    ASIENTO = Convert.ToDecimal(reader["ASIENTO"]),
                                    FECHA_ASIENTO = Convert.ToDateTime(reader["FECHA_ASIENTO"]),
                                    CUENTA = reader["CUENTA"].ToString(),
                                    NombreCliente = reader["NombreCliente"].ToString(),
                                    Debe = Convert.ToDecimal(reader["Debe"]),
                                    Haber = Convert.ToDecimal(reader["Haber"]),
                                    CONCEPTO = reader["CONCEPTO"].ToString(),
                                    OBSERVACION = reader["OBSERVACION"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar el libro diario: " + ex.Message;
                }
            }
            return View(lista);
        }

        [HttpPost]
        public JsonResult ObtenerDetalleAsiento(decimal numAsiento)
        {
            List<object> lineas = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr))
                return Json(lineas);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT a.CUENTA, IFNULL(cue.NOMBRE_CUENTA, 'Cuenta General / Contable') as NOMBRE_FISCAL,
                                   IF(a.DH = 'D', a.EUROS, 0) as Debe,
                                   IF(a.DH = 'H', a.EUROS, 0) as Haber,
                                   a.CONCEPTO, a.OBSERVACION, a.FECHA_ASIENTO
                            FROM asientos a
                            LEFT JOIN cuentas cue ON a.CUENTA = cue.CUENTA COLLATE utf8mb4_spanish_ci
                            WHERE a.ASIENTO = @numAsiento
                            ORDER BY a.clave ASC";

                        cmd.Parameters.AddWithValue("@numAsiento", numAsiento);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lineas.Add(new
                                {
                                    CUENTA = reader["CUENTA"].ToString().Trim(),
                                    NOMBRE_FISCAL = reader["NOMBRE_FISCAL"].ToString().Trim(),
                                    Debe = Convert.ToDecimal(reader["Debe"]),
                                    Haber = Convert.ToDecimal(reader["Haber"]),
                                    CONCEPTO = reader["CONCEPTO"].ToString().Trim(),
                                    OBSERVACION = reader["OBSERVACION"].ToString().Trim(),
                                    FECHA = Convert.ToDateTime(reader["FECHA_ASIENTO"]).ToString("yyyy-MM-dd")
                                });
                            }
                        }
                    }
                }
                catch { }
            }
            return Json(lineas);
        }
    }
}