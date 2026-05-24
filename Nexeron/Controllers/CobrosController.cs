using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class CobrosController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<cobros> lista = new List<cobros>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();

                    using (var cmdVto = conexion.CreateCommand())
                    {
                        cmdVto.CommandText = "UPDATE cobros SET VENCIDO = 1 WHERE DATE(FECHA_VEN) <= DATE(NOW()) AND (VENCIDO = 0 OR VENCIDO IS NULL)";
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
                            SELECT c.NUMEFEC, c.CUENTA, cl.NOMBRE_FISCAL as NombreCliente, 
                                   c.FCOBRO, fp.FORMACOBPAG as NombreFormaCobro, c.IMPORTE, 
                                   c.FECHA_VEN, c.FECH_FAC, c.OBSERVACION, c.NUMFAC, c.ESTADO,
                                   c.GASTOS, c.BANCO
                            FROM cobros c
                            LEFT JOIN clientes cl ON c.CUENTA = cl.CUENTA COLLATE utf8mb4_spanish_ci
                            LEFT JOIN fpagcob fp ON c.FCOBRO = fp.CODIGO
                            ORDER BY c.FECHA_VEN DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new cobros
                                {
                                    NUMEFEC = reader["NUMEFEC"].ToString().Trim(),
                                    CUENTA = reader["CUENTA"].ToString().Trim(),
                                    NombreCliente = reader["NombreCliente"] != DBNull.Value ? reader["NombreCliente"].ToString().Trim() : "Sin Cliente",
                                    FCOBRO = reader["FCOBRO"].ToString().Trim(),
                                    NombreFormaCobro = reader["NombreFormaCobro"] != DBNull.Value ? reader["NombreFormaCobro"].ToString().Trim() : "Desconocida",
                                    IMPORTE = reader["IMPORTE"] != DBNull.Value ? Convert.ToDecimal(reader["IMPORTE"]) : 0.00m,
                                    FECHA_VEN = reader["FECHA_VEN"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_VEN"]) : (DateTime?)null,
                                    FECH_FAC = reader["FECH_FAC"] != DBNull.Value ? Convert.ToDateTime(reader["FECH_FAC"]) : (DateTime?)null,
                                    OBSERVACION = reader["OBSERVACION"].ToString(),
                                    NUMFAC = reader["NUMFAC"].ToString().Trim(),
                                    ESTADO = reader["ESTADO"].ToString().Trim(),
                                    GASTOS = reader["GASTOS"] != DBNull.Value ? Convert.ToDecimal(reader["GASTOS"]) : 0.00m,
                                    BANCO = reader["BANCO"].ToString().Trim()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al compilar el panel de cobros: " + ex.Message;
                }
            }
            return View(lista);
        }

        [HttpPost]
        public JsonResult BuscarClientes(string term)
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
                                            FROM clientes 
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
        public JsonResult ObtenerDetalleCobro(string numefec)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numefec))
                return Json(null);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT c.*, cl.NOMBRE_FISCAL, cl.CIF, cl.DIRECCION, cl.CP, cl.POBLACION, cl.PROVINCIA, cl.TELEFONO, cl.EMAIL
                            FROM cobros c
                            LEFT JOIN clientes cl ON c.CUENTA = cl.CUENTA COLLATE utf8mb4_spanish_ci
                            WHERE c.NUMEFEC = @numefec";

                        cmd.Parameters.AddWithValue("@numefec", numefec);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Json(new
                                {
                                    NUMEFEC = reader["NUMEFEC"].ToString().Trim(),
                                    NUMFAC = reader["NUMFAC"].ToString().Trim(),
                                    FECHA_VEN = reader["FECHA_VEN"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_VEN"]).ToString("yyyy-MM-dd") : "",
                                    FECH_FAC = reader["FECH_FAC"] != DBNull.Value ? Convert.ToDateTime(reader["FECH_FAC"]).ToString("yyyy-MM-dd") : "",
                                    IMPORTE = reader["IMPORTE"] != DBNull.Value ? Convert.ToDecimal(reader["IMPORTE"]) : 0.00m,
                                    CUENTA = reader["CUENTA"].ToString().Trim(),
                                    FCOBRO = reader["FCOBRO"].ToString().Trim(),
                                    OBSERVACION = reader["OBSERVACION"].ToString(),
                                    ESTADO = reader["ESTADO"].ToString().Trim(),
                                    GASTOS = reader["GASTOS"] != DBNull.Value ? Convert.ToDecimal(reader["GASTOS"]) : 0.00m,
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

            string numefec = form["NUMEFEC"].Trim();
            string numfac = form["NUMFAC"];
            if (string.IsNullOrWhiteSpace(numfac))
            {
                string[] partes = numefec.Split('/');
                numfac = partes[0];
            }
            numfac = numfac.Trim().PadLeft(9);

            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();

                using (var cmdCheck = conexion.CreateCommand())
                {
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM cobros WHERE NUMEFEC = @numCheck";
                    cmdCheck.Parameters.AddWithValue("@numCheck", numefec);
                    if (Convert.ToInt64(cmdCheck.ExecuteScalar()) > 0)
                    {
                        TempData["Error"] = "El número de documento '" + numefec + "' ya se encuentra registrado.";
                        return RedirectToAction("Index");
                    }
                }

                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conexion.CreateCommand())
                        {
                            cmd.Transaction = transaccion;
                            cmd.CommandText = @"INSERT INTO cobros (
                                NUMEFEC, NUMFAC, CUENTA, FCOBRO, IMPORTE, GASTOS, BANCO, FECHA_VEN, FECH_FAC, OBSERVACION, ESTADO
                            ) VALUES (
                                @numefec, @numfac, @cuenta, @fcobro, @importe, @gastos, @banco, @fecha_ven, @fech_fac, @observacion, '101'
                            )";

                            cmd.Parameters.AddWithValue("@numefec", numefec);
                            cmd.Parameters.AddWithValue("@numfac", numfac);
                            cmd.Parameters.AddWithValue("@cuenta", form["CUENTA"].Trim().PadLeft(9));
                            cmd.Parameters.AddWithValue("@fcobro", form["FCOBRO"]);
                            cmd.Parameters.AddWithValue("@importe", Convert.ToDecimal(form["IMPORTE"]));
                            cmd.Parameters.AddWithValue("@gastos", string.IsNullOrEmpty(form["GASTOS"]) ? 0.00m : Convert.ToDecimal(form["GASTOS"]));
                            cmd.Parameters.AddWithValue("@banco", form["BANCO"] ?? "");
                            cmd.Parameters.AddWithValue("@fecha_ven", string.IsNullOrEmpty(form["FECHA_VEN"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECHA_VEN"]));
                            cmd.Parameters.AddWithValue("@fech_fac", string.IsNullOrEmpty(form["FECH_FAC"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECH_FAC"]));
                            cmd.Parameters.AddWithValue("@observacion", form["OBSERVACION"] ?? "");

                            cmd.ExecuteNonQuery();
                        }
                        transaccion.Commit();
                        TempData["MensajeExito"] = "Documento de cobro registrado con éxito.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al insertar el cobro: " + ex.Message;
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

            string numefec = form["NUMEFEC"].Trim();
            string numfac = form["NUMFAC"];
            if (string.IsNullOrWhiteSpace(numfac))
            {
                string[] partes = numefec.Split('/');
                numfac = partes[0];
            }
            numfac = numfac.Trim().PadLeft(9);

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
                            cmd.CommandText = @"UPDATE cobros SET 
                                NUMFAC = @numfac, 
                                CUENTA = @cuenta, 
                                FCOBRO = @fcobro, 
                                IMPORTE = @importe, 
                                GASTOS = @gastos,
                                BANCO = @banco,
                                FECHA_VEN = @fecha_ven, 
                                FECH_FAC = @fech_fac, 
                                OBSERVACION = @observacion
                                WHERE NUMEFEC = @numefec";

                            cmd.Parameters.AddWithValue("@numefec", numefec);
                            cmd.Parameters.AddWithValue("@numfac", numfac);
                            cmd.Parameters.AddWithValue("@cuenta", form["CUENTA"].Trim().PadLeft(9));
                            cmd.Parameters.AddWithValue("@fcobro", form["FCOBRO"]);
                            cmd.Parameters.AddWithValue("@importe", Convert.ToDecimal(form["IMPORTE"]));
                            cmd.Parameters.AddWithValue("@gastos", string.IsNullOrEmpty(form["GASTOS"]) ? 0.00m : Convert.ToDecimal(form["GASTOS"]));
                            cmd.Parameters.AddWithValue("@banco", form["BANCO"] ?? "");
                            cmd.Parameters.AddWithValue("@fecha_ven", string.IsNullOrEmpty(form["FECHA_VEN"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECHA_VEN"]));
                            cmd.Parameters.AddWithValue("@fech_fac", string.IsNullOrEmpty(form["FECH_FAC"]) ? (object)DBNull.Value : Convert.ToDateTime(form["FECH_FAC"]));
                            cmd.Parameters.AddWithValue("@observacion", form["OBSERVACION"] ?? "");

                            cmd.ExecuteNonQuery();
                        }
                        transaccion.Commit();
                        TempData["MensajeExito"] = "Cobro modificado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error crítico al actualizar el cobro: " + ex.Message;
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Aceptar(string id)
        {
            return CambiarEstadoAccionRapida(id, "102", "Cobro liquidado / aceptado con éxito.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Rechazar(string id)
        {
            return CambiarEstadoAccionRapida(id, "103", "Cobro marcado como rechazado / devuelto.");
        }

        private ActionResult CambiarEstadoAccionRapida(string id, string nuevoEstado, string mensajeExito)
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
                        cmd.CommandText = "UPDATE cobros SET ESTADO = @estado, FESTADO = NOW() WHERE NUMEFEC = @numefec";
                        cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                        cmd.Parameters.AddWithValue("@numefec", id.Trim());
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
        public ActionResult Eliminar(string id)
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
                        cmd.CommandText = "DELETE FROM cobros WHERE NUMEFEC = @numefec";
                        cmd.Parameters.AddWithValue("@numefec", id.Trim());
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "El documento de cobro ha sido eliminado.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "No se pudo eliminar el cobro: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult ExisteNumeroEfecto(string numefec)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numefec))
                return Json(new { existe = false });

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM cobros WHERE NUMEFEC = @numCheck";
                        cmd.Parameters.AddWithValue("@numCheck", numefec.Trim());
                        long conteo = Convert.ToInt64(cmd.ExecuteScalar());
                        return Json(new { existe = conteo > 0 });
                    }
                }
                catch { return Json(new { existe = false }); }
            }
        }
    }
}