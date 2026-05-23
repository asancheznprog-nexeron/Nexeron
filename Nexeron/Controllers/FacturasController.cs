using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class FacturasController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<facturas> lista = new List<facturas>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();

                    // Cargar Formas de Pago
                    List<fpagcob> formasPago = new List<fpagcob>();
                    using (var cmdFp = conexion.CreateCommand())
                    {
                        cmdFp.CommandText = "SELECT CODIGO, FORMACOBPAG, NUMCOBROS, PRIMVENCI, DIASVENCI FROM fpagcob ORDER BY CODIGO ASC";
                        using (var readerFp = cmdFp.ExecuteReader())
                        {
                            while (readerFp.Read())
                            {
                                formasPago.Add(new fpagcob
                                {
                                    CODIGO = readerFp["CODIGO"].ToString().Trim(),
                                    FORMACOBPAG = readerFp["FORMACOBPAG"].ToString().Trim(),
                                    NUMCOBROS = Convert.ToInt32(readerFp["NUMCOBROS"]),
                                    PRIMVENCI = Convert.ToInt32(readerFp["PRIMVENCI"]),
                                    DIASVENCI = Convert.ToInt32(readerFp["DIASVENCI"])
                                });
                            }
                        }
                    }
                    ViewBag.FormasPago = formasPago;

                    // Cargar Estados
                    List<KeyValuePair<string, string>> estadosLista = new List<KeyValuePair<string, string>>();
                    using (var cmdEst = conexion.CreateCommand())
                    {
                        cmdEst.CommandText = "SELECT codigo, estado FROM estados ORDER BY codigo ASC";
                        using (var readerEst = cmdEst.ExecuteReader())
                        {
                            while (readerEst.Read())
                            {
                                estadosLista.Add(new KeyValuePair<string, string>(
                                    readerEst["codigo"].ToString().Trim(),
                                    readerEst["estado"].ToString().Trim()
                                ));
                            }
                        }
                    }
                    ViewBag.Estados = estadosLista;

                    // Consulta principal de Facturas
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT t.*, IFNULL(e.estado, 'Desconocido') as NombreEstado
                            FROM (
                                SELECT f.NUMFACTURA, f.FECHFAC, f.CUENTA, c.NOMBRE_FISCAL as NombreCliente, 
                                       f.FCOBRO, f.OBSERVACIONES,
                                       IF(COUNT(DISTINCT f.ESTADOLIN) > 1, '104', MAX(f.ESTADO)) as ESTADO,
                                       SUM(ROUND(f.CANTI * f.EUROS * (1 - (f.DTOARTI / 100)), 2)) as BaseTotal,
                                       SUM(ROUND(ROUND(f.CANTI * f.EUROS * (1 - (f.DTOARTI / 100)), 2) * (f.IVARTI / 100), 2)) as IvaTotal
                                FROM facturas f
                                LEFT JOIN clientes c ON f.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                GROUP BY f.NUMFACTURA, f.FECHFAC, f.CUENTA, c.NOMBRE_FISCAL, f.FCOBRO, f.OBSERVACIONES
                            ) t
                            LEFT JOIN estados e ON t.ESTADO = e.codigo
                            ORDER BY t.NUMFACTURA DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal baseTotal = Convert.ToDecimal(reader["BaseTotal"]);
                                decimal ivaTotal = Convert.ToDecimal(reader["IvaTotal"]);

                                lista.Add(new facturas
                                {
                                    NUMFACTURA = reader["NUMFACTURA"].ToString(),
                                    FECHFAC = Convert.ToDateTime(reader["FECHFAC"]),
                                    CUENTA = reader["CUENTA"].ToString(),
                                    NombreCliente = reader["NombreCliente"].ToString(),
                                    ESTADO = reader["ESTADO"].ToString(),
                                    NombreEstado = reader["NombreEstado"].ToString(),
                                    FCOBRO = reader["FCOBRO"].ToString(),
                                    OBSERVACIONES = reader["OBSERVACIONES"].ToString(),
                                    BaseTotal = baseTotal,
                                    ImporteTotal = baseTotal + ivaTotal
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al compilar el panel de facturas: " + ex.Message;
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
        public JsonResult BuscarArticulos(string term)
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
                        cmd.CommandText = @"SELECT articulo, descripcion, unidad_medida, iva 
                                            FROM articulo 
                                            WHERE (articulo LIKE @term OR descripcion LIKE @term) AND activo = 1 
                                            ORDER BY articulo ASC LIMIT 15";
                        cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultado.Add(new
                                {
                                    ARTI = reader["articulo"].ToString().Trim(),
                                    DESARTI = reader["descripcion"].ToString().Trim(),
                                    UNIDAD = reader["unidad_medida"].ToString().Trim(),
                                    IVARTI = reader["iva"] != DBNull.Value ? Convert.ToDecimal(reader["iva"]) : 0.00m
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
        public JsonResult ObtenerDetalleFactura(string numFactura)
        {
            List<object> lineas = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numFactura))
                return Json(lineas);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT f.*, 
                                   (SELECT IF(COUNT(DISTINCT sub.ESTADOLIN) > 1, '104', f.ESTADO) 
                                    FROM facturas sub WHERE sub.NUMFACTURA = f.NUMFACTURA) as ESTADO_CALCULADO,
                                   c.NOMBRE_FISCAL, c.CIF, c.DIRECCION, c.CP, c.POBLACION, c.PROVINCIA, c.TELEFONO, c.EMAIL
                            FROM facturas f
                            LEFT JOIN clientes c ON f.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                            WHERE f.NUMFACTURA = @num 
                            ORDER BY f.NUMLINEA ASC";

                        cmd.Parameters.AddWithValue("@num", numFactura.PadLeft(9));
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lineas.Add(new
                                {
                                    ARTI = reader["ARTI"].ToString().Trim(),
                                    DESARTI = reader["DESARTI"].ToString().Trim(),
                                    CANTI = Convert.ToDecimal(reader["CANTI"]),
                                    UNIDAD = reader["UNIDAD"].ToString().Trim(),
                                    EUROS = Convert.ToDecimal(reader["EUROS"]),
                                    DTOARTI = Convert.ToDecimal(reader["DTOARTI"]),
                                    IVARTI = Convert.ToDecimal(reader["IVARTI"]),
                                    CUENTA = reader["CUENTA"].ToString().Trim(),
                                    FCOBRO = reader["FCOBRO"].ToString().Trim(),
                                    ESTADO = reader["ESTADO_CALCULADO"].ToString().Trim(),
                                    ESTADOLIN = reader["ESTADOLIN"].ToString().Trim(),
                                    FECHA = Convert.ToDateTime(reader["FECHFAC"]).ToString("yyyy-MM-dd"),
                                    OBSERVACIONES = reader["OBSERVACIONES"].ToString(),
                                    NUMLINEA = Convert.ToInt32(reader["NUMLINEA"]),
                                    NOMBRE_FISCAL = reader["NOMBRE_FISCAL"].ToString().Trim(),
                                    CIF = reader["CIF"].ToString().Trim(),
                                    DIRECCION = reader["DIRECCION"].ToString().Trim()
                                });
                            }
                        }
                    }
                }
                catch { }
            }
            return Json(lineas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(FormCollection form, string lineasJson)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(lineasJson))
            {
                TempData["Error"] = "La factura debe contener al menos una línea de artículo.";
                return RedirectToAction("Index");
            }

            List<facturas> lineasArticulos = new List<facturas>();
            try
            {
                var serializer = new JavaScriptSerializer();
                lineasArticulos = serializer.Deserialize<List<facturas>>(lineasJson);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error en formato de líneas: " + ex.Message;
                return RedirectToAction("Index");
            }

            string numFactura = form["NUMFACTURA"].PadLeft(9);
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();

                using (var cmdCheck = conexion.CreateCommand())
                {
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM facturas WHERE NUMFACTURA = @numCheck";
                    cmdCheck.Parameters.AddWithValue("@numCheck", numFactura);
                    if (Convert.ToInt64(cmdCheck.ExecuteScalar()) > 0)
                    {
                        TempData["Error"] = "El número de factura '" + numFactura.Trim() + "' ya existe en el sistema.";
                        return RedirectToAction("Index");
                    }
                }

                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        DateTime fechFac = Convert.ToDateTime(form["FECHFAC"]);
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string estadoGlobal = form["ESTADO"];
                        string observaciones = form["OBSERVACIONES"];

                        int contadorLinea = 10;

                        foreach (var linea in lineasArticulos)
                        {
                            using (var cmd = conexion.CreateCommand())
                            {
                                cmd.Transaction = transaccion;
                                cmd.CommandText = @"INSERT INTO facturas (
                                    NUMFACTURA, FECHFAC, NUMOFERTA, NUMPEDIDO, NUMALBARAN, FECHALB, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES, RECTIFICATIVA
                                ) VALUES (
                                    @numfactura, @fechfac, '', '', '', @fechalb, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, @estadolin, @observaciones, ''
                                )";

                                cmd.Parameters.AddWithValue("@numfactura", numFactura);
                                cmd.Parameters.AddWithValue("@fechfac", fechFac);
                                cmd.Parameters.AddWithValue("@fechalb", fechFac); // FECHALB es NOT NULL, enviamos fecha de factura por defecto
                                cmd.Parameters.AddWithValue("@cuenta", cuenta);
                                cmd.Parameters.AddWithValue("@fcobro", fcobro);
                                cmd.Parameters.AddWithValue("@numlinea", contadorLinea);
                                cmd.Parameters.AddWithValue("@arti", linea.ARTI ?? "");
                                cmd.Parameters.AddWithValue("@desarti", linea.DESARTI ?? "");
                                cmd.Parameters.AddWithValue("@unidad", linea.UNIDAD ?? "UD");
                                cmd.Parameters.AddWithValue("@canti", linea.CANTI);
                                cmd.Parameters.AddWithValue("@euros", linea.EUROS);
                                cmd.Parameters.AddWithValue("@ivarti", linea.IVARTI);
                                cmd.Parameters.AddWithValue("@dtoarti", linea.DTOARTI);
                                cmd.Parameters.AddWithValue("@estado", estadoGlobal);
                                cmd.Parameters.AddWithValue("@estadolin", string.IsNullOrEmpty(linea.ESTADOLIN) ? estadoGlobal : linea.ESTADOLIN);
                                cmd.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmd.ExecuteNonQuery();
                            }
                            contadorLinea += 10;
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Factura registrada de forma exitosa.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al insertar la factura: " + ex.Message;
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(FormCollection form, string lineasJsonModificadas)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(lineasJsonModificadas))
            {
                TempData["Error"] = "La factura debe contener al menos una línea.";
                return RedirectToAction("Index");
            }

            List<facturas> nuevasLineas = new List<facturas>();
            try
            {
                var serializer = new JavaScriptSerializer();
                nuevasLineas = serializer.Deserialize<List<facturas>>(lineasJsonModificadas);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error en análisis de datos comerciales: " + ex.Message;
                return RedirectToAction("Index");
            }

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        string numFactura = form["NUMFACTURA"].PadLeft(9);
                        DateTime fechFac = Convert.ToDateTime(form["FECHFAC"]);
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string estadoGlobal = form["ESTADO"];
                        string observaciones = form["OBSERVACIONES"];

                        if (nuevasLineas.Count > 0)
                        {
                            string primerEstadoLin = nuevasLineas[0].ESTADOLIN;
                            bool todasLasLineasSonIguales = true;
                            foreach (var linea in nuevasLineas)
                            {
                                if (linea.ESTADOLIN != primerEstadoLin)
                                {
                                    todasLasLineasSonIguales = false;
                                    break;
                                }
                            }
                            estadoGlobal = todasLasLineasSonIguales ? primerEstadoLin : "104";
                        }

                        using (var cmdDelete = conexion.CreateCommand())
                        {
                            cmdDelete.Transaction = transaccion;
                            cmdDelete.CommandText = "DELETE FROM facturas WHERE NUMFACTURA = @numfactura";
                            cmdDelete.Parameters.AddWithValue("@numfactura", numFactura);
                            cmdDelete.ExecuteNonQuery();
                        }

                        int contadorLinea = 10;
                        foreach (var linea in nuevasLineas)
                        {
                            using (var cmdInsert = conexion.CreateCommand())
                            {
                                cmdInsert.Transaction = transaccion;
                                cmdInsert.CommandText = @"INSERT INTO facturas (
                                    NUMFACTURA, FECHFAC, NUMOFERTA, NUMPEDIDO, NUMALBARAN, FECHALB, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES, RECTIFICATIVA
                                ) VALUES (
                                    @numfactura, @fechfac, '', '', '', @fechalb, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, @estadolin, @observaciones, ''
                                )";

                                cmdInsert.Parameters.AddWithValue("@numfactura", numFactura);
                                cmdInsert.Parameters.AddWithValue("@fechfac", fechFac);
                                cmdInsert.Parameters.AddWithValue("@fechalb", fechFac);
                                cmdInsert.Parameters.AddWithValue("@cuenta", cuenta);
                                cmdInsert.Parameters.AddWithValue("@fcobro", fcobro);
                                cmdInsert.Parameters.AddWithValue("@numlinea", contadorLinea);
                                cmdInsert.Parameters.AddWithValue("@arti", linea.ARTI ?? "");
                                cmdInsert.Parameters.AddWithValue("@desarti", linea.DESARTI ?? "");
                                cmdInsert.Parameters.AddWithValue("@unidad", linea.UNIDAD ?? "UD");
                                cmdInsert.Parameters.AddWithValue("@canti", linea.CANTI);
                                cmdInsert.Parameters.AddWithValue("@euros", linea.EUROS);
                                cmdInsert.Parameters.AddWithValue("@ivarti", linea.IVARTI);
                                cmdInsert.Parameters.AddWithValue("@dtoarti", linea.DTOARTI);
                                cmdInsert.Parameters.AddWithValue("@estado", estadoGlobal);
                                cmdInsert.Parameters.AddWithValue("@estadolin", string.IsNullOrEmpty(linea.ESTADOLIN) ? estadoGlobal : linea.ESTADOLIN);
                                cmdInsert.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmdInsert.ExecuteNonQuery();
                            }
                            contadorLinea += 10;
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Factura modificada con éxito.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error crítico al guardar cambios: " + ex.Message;
                    }
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
                        cmd.CommandText = "DELETE FROM facturas WHERE NUMFACTURA = @numfactura";
                        cmd.Parameters.AddWithValue("@numfactura", id.PadLeft(9));
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "La factura ha sido eliminada por completo.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "No se pudo eliminar el documento: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult ExisteNumeroFactura(string numFactura)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numFactura))
                return Json(new { existe = false });

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM facturas WHERE NUMFACTURA = @numCheck";
                        cmd.Parameters.AddWithValue("@numCheck", numFactura.PadLeft(9));
                        long conteo = Convert.ToInt64(cmd.ExecuteScalar());
                        return Json(new { existe = conteo > 0 });
                    }
                }
                catch
                {
                    return Json(new { existe = false });
                }
            }
        }
    }
}