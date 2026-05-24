using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class PagosController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<pagos> lista = new List<pagos>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();

                    using (var cmdVto = conexion.CreateCommand())
                    {
                        cmdVto.CommandText = "UPDATE pagos SET VENCIDO = 1 WHERE DATE(FECHA_VEN) <= DATE(NOW()) AND (VENCIDO = 0 OR VENCIDO IS NULL)";
                        cmdVto.ExecuteNonQuery();
                    }

                    List<fpagcob> formasPago = new List<fpagcob>();
                    using (var cmdFp = conexion.CreateCommand())
                    {
                        cmdFp.CommandText = "SELECT CODIGO, FORMACOBPAG FROM fpagcob ORDER BY CODIGO ASC";
                        using (var readerFp = cmdFp.ExecuteReader())
                        {
                            while (readerFp.Read())
                            {
                                formasPago.Add(new fpagcob
                                {
                                    CODIGO = readerFp["CODIGO"].ToString().Trim(),
                                    FORMACOBPAG = readerFp["FORMACOBPAG"].ToString().Trim()
                                });
                            }
                        }
                    }
                    ViewBag.FormasPago = formasPago;

                    List<KeyValuePair<string, string>> bancosLista = new List<KeyValuePair<string, string>>();
                    using (var cmdBco = conexion.CreateCommand())
                    {
                        cmdBco.CommandText = "SELECT CUENTA, NOMBRE_CUENTA FROM cuentas WHERE cuenta LIKE '57%' ORDER BY cuenta ASC";
                        using (var readerBco = cmdBco.ExecuteReader())
                        {
                            while (readerBco.Read())
                            {
                                bancosLista.Add(new KeyValuePair<string, string>(
                                    readerBco["CUENTA"].ToString().Trim(),
                                    readerBco["NOMBRE_CUENTA"].ToString().Trim()
                                ));
                            }
                        }
                    }
                    ViewBag.Bancos = bancosLista;

                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT p.CLAVE, p.SFRA, p.NUMFAC, p.CUENTA, pr.NOMBRE_FISCAL as NombreProveedor, 
                                   p.FPAGO, fp.FORMACOBPAG as NombreFormaPago, p.IMPORTE, 
                                   p.FECHA_VEN, p.FECH_FAC, p.OBSERVACION, p.ESTADO, p.BANCO
                            FROM pagos p
                            LEFT JOIN proveedores pr ON p.CUENTA = pr.CUENTA COLLATE utf8mb4_spanish_ci
                            LEFT JOIN fpagcob fp ON p.FPAGO = fp.CODIGO
                            ORDER BY p.FECHA_VEN DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new pagos
                                {
                                    CLAVE = Convert.ToInt64(reader["CLAVE"]),
                                    SFRA = reader["SFRA"].ToString().Trim(),
                                    NUMFAC = reader["NUMFAC"].ToString().Trim(),
                                    CUENTA = reader["CUENTA"].ToString().Trim(),
                                    NombreProveedor = reader["NombreProveedor"] != DBNull.Value ? reader["NombreProveedor"].ToString().Trim() : "Sin Proveedor",
                                    FPAGO = reader["FPAGO"].ToString().Trim(),
                                    NombreFormaPago = reader["NombreFormaPago"] != DBNull.Value ? reader["NombreFormaPago"].ToString().Trim() : "Desconocida",
                                    IMPORTE = reader["IMPORTE"] != DBNull.Value ? Convert.ToDecimal(reader["IMPORTE"]) : 0.00m,
                                    FECHA_VEN = reader["FECHA_VEN"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_VEN"]) : (DateTime?)null,
                                    FECH_FAC = reader["FECH_FAC"] != DBNull.Value ? Convert.ToDateTime(reader["FECH_FAC"]) : (DateTime?)null,
                                    OBSERVACION = reader["OBSERVACION"].ToString(),
                                    ESTADO = reader["ESTADO"].ToString().Trim(),
                                    BANCO = reader["BANCO"].ToString().Trim()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al compilar el panel de pagos: " + ex.Message;
                }
            }
            return View(lista);
        }

        [HttpPost]
        public JsonResult BuscarProveedores(string term)
        {
            List<object> resultado = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(term))
                return Json(resultado);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT CUENTA, NOMBRE_FISCAL, CIF, DIRECCION, CP, POBLACION, PROVINCIA, TELEFONO, EMAIL 
                                            FROM proveedores 
                                            WHERE CUENTA LIKE @term OR NOMBRE_FISCAL LIKE @term 
                                            ORDER BY CUENTA ASC LIMIT 15";
                        cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultado.Add(new
                                {
                                    CUENTA = reader["CUENTA"].ToString().Trim(),
                                    NOMBRE_FISCAL = reader["NOMBRE_FISCAL"].ToString().Trim(),
                                    CIF = reader["CIF"].ToString().Trim(),
                                    DIRECCION = reader["DIRECCION"].ToString().Trim(),
                                    CP = reader["CP"].ToString().Trim(),
                                    POBLACION = reader["POBLACION"].ToString().Trim(),
                                    PROVINCIA = reader["PROVINCIA"].ToString().Trim(),
                                    TELEFONO = reader["TELEFONO"].ToString().Trim(),
                                    EMAIL = reader["EMAIL"].ToString().Trim()
                                });
                            }
                        }
                    }
                }
                catch { }
            }
            return Json(resultado);
        }

        [HttpPost]
        public JsonResult ObtenerDetallePago(long clave)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr))
                return Json(null);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT p.*, pr.NOMBRE_FISCAL, pr.CIF, pr.DIRECCION, pr.CP, pr.POBLACION, pr.PROVINCIA, pr.TELEFONO, pr.EMAIL
                            FROM pagos p
                            LEFT JOIN proveedores pr ON p.CUENTA = pr.CUENTA COLLATE utf8mb4_spanish_ci
                            WHERE p.CLAVE = @clave";

                        cmd.Parameters.AddWithValue("@clave", clave);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Json(new
                                {
                                    CLAVE = Convert.ToInt64(reader["CLAVE"]),
                                    SFRA = reader["SFRA"].ToString().Trim(),
                                    NUMFAC = reader["NUMFAC"].ToString().Trim(),
                                    FECHA_VEN = reader["FECHA_VEN"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_VEN"]).ToString("yyyy-MM-dd") : "",
                                    FECH_FAC = reader["FECH_FAC"] != DBNull.Value ? Convert.ToDateTime(reader["FECH_FAC"]).ToString("yyyy-MM-dd") : "",
                                    IMPORTE = reader["IMPORTE"] != DBNull.Value ? Convert.ToDecimal(reader["IMPORTE"]) : 0.00m,
                                    CUENTA = reader["CUENTA"].ToString().Trim(),
                                    FPAGO = reader["FPAGO"].ToString().Trim(),
                                    OBSERVACION = reader["OBSERVACION"].ToString(),
                                    ESTADO = reader["ESTADO"].ToString().Trim(),
                                    BANCO = reader["BANCO"].ToString().Trim(),
                                    NOMBRE_FISCAL = reader["NOMBRE_FISCAL"] != DBNull.Value ? reader["NOMBRE_FISCAL"].ToString().Trim() : "",
                                    CIF = reader["CIF"].ToString().Trim(),
                                    DIRECCION = reader["DIRECCION"].ToString().Trim(),
                                    CP = reader["CP"].ToString().Trim(),
                                    POBLACION = reader["POBLACION"].ToString().Trim(),
                                    PROVINCIA = reader["PROVINCIA"].ToString().Trim(),
                                    TELEFONO = reader["TELEFONO"].ToString().Trim(),
                                    EMAIL = reader["EMAIL"].ToString().Trim()
                                });
                            }
                        }
                    }
                }
                catch { }
            }
            return Json(null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(FormCollection form)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string sfra = form["SFRA"].Trim();
            string numfac = form["NUMFAC"].Trim().PadLeft(9);
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conexion.CreateCommand())
                        {
                            cmd.Transaction = transaccion;
                            cmd.CommandText = @"INSERT INTO pagos (
                                FPAGO, NUMFAC, CUENTA, IMPORTE, FECHA_VEN, FECH_FAC, BANCO, SFRA, OBSERVACION, ESTADO
                            ) VALUES (
                                @fpago, @numfac, @cuenta, @importe, @fecha_ven, @fech_fac, @banco, @sfra, @observacion, '101'
                            )";

                            cmd.Parameters.AddWithValue("@fpago", form["FPAGO"]);
                            cmd.Parameters.AddWithValue("@numfac", numfac);
                            cmd.Parameters.AddWithValue("@cuenta", form["CUENTA"].Trim().PadLeft(9));
                            cmd.Parameters.AddWithValue("@importe", Convert.ToDecimal(form["IMPORTE"]));
                            cmd.Parameters.AddWithValue("@fecha_ven", string.IsNullOrEmpty(form["FECHA_VEN"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECHA_VEN"]));
                            cmd.Parameters.AddWithValue("@fech_fac", string.IsNullOrEmpty(form["FECH_FAC"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECH_FAC"]));
                            cmd.Parameters.AddWithValue("@banco", form["BANCO"] ?? "");
                            cmd.Parameters.AddWithValue("@sfra", sfra);
                            cmd.Parameters.AddWithValue("@observacion", form["OBSERVACION"] ?? "");

                            cmd.ExecuteNonQuery();
                        }
                        transaccion.Commit();
                        TempData["MensajeExito"] = "Documento de pago registrado con éxito.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al insertar el pago: " + ex.Message;
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(FormCollection form)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            long clave = Convert.ToInt64(form["CLAVE"]);
            string sfra = form["SFRA"].Trim();
            string numfac = form["NUMFAC"].Trim().PadLeft(9);

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conexion.CreateCommand())
                        {
                            cmd.Transaction = transaccion;
                            cmd.CommandText = @"UPDATE pagos SET 
                                NUMFAC = @numfac, 
                                CUENTA = @cuenta, 
                                FPAGO = @fpago, 
                                IMPORTE = @importe, 
                                BANCO = @banco,
                                SFRA = @sfra,
                                FECHA_VEN = @fecha_ven, 
                                FECH_FAC = @fech_fac, 
                                OBSERVACION = @observacion
                                WHERE CLAVE = @clave";

                            cmd.Parameters.AddWithValue("@clave", clave);
                            cmd.Parameters.AddWithValue("@numfac", numfac);
                            cmd.Parameters.AddWithValue("@cuenta", form["CUENTA"].Trim().PadLeft(9));
                            cmd.Parameters.AddWithValue("@fpago", form["FPAGO"]);
                            cmd.Parameters.AddWithValue("@importe", Convert.ToDecimal(form["IMPORTE"]));
                            cmd.Parameters.AddWithValue("@banco", form["BANCO"] ?? "");
                            cmd.Parameters.AddWithValue("@sfra", sfra);
                            cmd.Parameters.AddWithValue("@fecha_ven", string.IsNullOrEmpty(form["FECHA_VEN"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECHA_VEN"]));
                            cmd.Parameters.AddWithValue("@fech_fac", string.IsNullOrEmpty(form["FECH_FAC"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECH_FAC"]));
                            cmd.Parameters.AddWithValue("@observacion", form["OBSERVACION"] ?? "");

                            cmd.ExecuteNonQuery();
                        }
                        transaccion.Commit();
                        TempData["MensajeExito"] = "Pago modificado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error crítico al actualizar el pago: " + ex.Message;
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Aceptar(long id)
        {
            return CambiarEstadoAccionRapida(id, "102", "Pago liquidado / aceptado con éxito.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Rechazar(long id)
        {
            return CambiarEstadoAccionRapida(id, "103", "Pago marcado como rechazado / devuelto.");
        }

        private ActionResult CambiarEstadoAccionRapida(long id, string nuevoEstado, string mensajeExito)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE pagos SET ESTADO = @estado, FECHA_LIB = NOW() WHERE CLAVE = @clave";
                        cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                        cmd.Parameters.AddWithValue("@clave", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = mensajeExito;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al procesar el cambio de estado en tesorería: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(long id)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM pagos WHERE CLAVE = @clave";
                        cmd.Parameters.AddWithValue("@clave", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "El documento de pago ha sido eliminado.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "No se pudo eliminar el pago: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
    }
}