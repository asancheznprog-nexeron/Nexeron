using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class ComprasFacturasController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<compras_facturas> lista = new List<compras_facturas>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();

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

                    List<KeyValuePair<string, string>> unidadesLista = new List<KeyValuePair<string, string>>();
                    using (var cmdUni = conexion.CreateCommand())
                    {
                        cmdUni.CommandText = "SELECT codigo, descripcion FROM unidades ORDER BY codigo ASC";
                        using (var readerUni = cmdUni.ExecuteReader())
                        {
                            while (readerUni.Read())
                            {
                                unidadesLista.Add(new KeyValuePair<string, string>(
                                    readerUni["codigo"].ToString().Trim(),
                                    readerUni["descripcion"].ToString().Trim()
                                ));
                            }
                        }
                    }
                    ViewBag.Unidades = unidadesLista;

                    HashSet<string> facturasRectificadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using (var cmdRec = conexion.CreateCommand())
                    {
                        cmdRec.CommandText = "SELECT DISTINCT TRIM(RECTIFICATIVA) FROM compras_facturas WHERE RECTIFICATIVA IS NOT NULL AND RECTIFICATIVA <> ''";
                        using (var readerRec = cmdRec.ExecuteReader())
                        {
                            while (readerRec.Read())
                            {
                                facturasRectificadas.Add(readerRec[0].ToString().Trim());
                            }
                        }
                    }
                    ViewBag.FacturasRectificadas = facturasRectificadas;

                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT t.*, IFNULL(e.estado, 'Desconocido') as NombreEstado
                            FROM (
                                SELECT f.NUMFACTURA, f.FECHFAC, f.NUMALBARAN, f.CUENTA, c.NOMBRE_FISCAL as NombreProveedor, 
                                       f.FCOBRO, f.OBSERVACIONES,
                                       IF(COUNT(DISTINCT f.ESTADOLIN) > 1, '104', MAX(f.ESTADO)) as ESTADO,
                                       SUM(ROUND(f.CANTI * f.EUROS * (1 - (f.DTOARTI / 100)), 2)) as BaseTotal,
                                       SUM(ROUND(ROUND(f.CANTI * f.EUROS * (1 - (f.DTOARTI / 100)), 2) * (f.IVARTI / 100), 2)) as IvaTotal
                                FROM compras_facturas f
                                LEFT JOIN proveedores c ON f.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                GROUP BY f.NUMFACTURA, f.FECHFAC, f.NUMALBARAN, f.CUENTA, c.NOMBRE_FISCAL, f.FCOBRO, f.OBSERVACIONES
                            ) t
                            LEFT JOIN estados e ON t.ESTADO = e.codigo
                            ORDER BY t.NUMFACTURA DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal baseTotal = Convert.ToDecimal(reader["BaseTotal"]);
                                decimal ivaTotal = Convert.ToDecimal(reader["IvaTotal"]);

                                lista.Add(new compras_facturas
                                {
                                    NUMFACTURA = reader["NUMFACTURA"].ToString(),
                                    FECHFAC = Convert.ToDateTime(reader["FECHFAC"]),
                                    NUMALBARAN = reader["NUMALBARAN"].ToString(),
                                    CUENTA = reader["CUENTA"].ToString(),
                                    NombreProveedor = reader["NombreProveedor"].ToString(),
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
                    ViewBag.Error = "Error al compilar el panel de facturas de compra: " + ex.Message;
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
        public JsonResult BuscarArticulos(string term, string proveedorCodigo)
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
                        cmd.CommandText = @"
                            SELECT a.articulo, 
                                   IFNULL(ap.descripcion_proveedor, a.descripcion) AS descripcion, 
                                   IFNULL(ap.unidad, a.unidad_medida) AS unidad_medida, 
                                   a.iva,
                                   IFNULL(ap.tarifa, 0.0000) AS tarifa,
                                   IFNULL(ap.descuento, 0.00) AS descuento
                            FROM articulo a
                            LEFT JOIN articulo_proveedor ap ON a.codigo = ap.articulo_codigo 
                                 AND ap.proveedor_codigo = @proveedorCodigo
                            WHERE (a.articulo LIKE @term OR a.descripcion LIKE @term) AND a.activo = 1 
                            ORDER BY a.articulo ASC LIMIT 15";

                        cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                        cmd.Parameters.AddWithValue("@proveedorCodigo", (proveedorCodigo ?? "").PadLeft(9));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultado.Add(new
                                {
                                    ARTI = reader["articulo"].ToString().Trim(),
                                    DESARTI = reader["descripcion"].ToString().Trim(),
                                    UNIDAD = reader["unidad_medida"].ToString().Trim(),
                                    IVARTI = reader["iva"] != DBNull.Value ? Convert.ToDecimal(reader["iva"]) : 0.00m,
                                    EUROS = reader["tarifa"] != DBNull.Value ? Convert.ToDecimal(reader["tarifa"]) : 0.0000m,
                                    DTOARTI = reader["descuento"] != DBNull.Value ? Convert.ToDecimal(reader["descuento"]) : 0.00m
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
                                    FROM compras_facturas sub WHERE sub.NUMFACTURA = f.NUMFACTURA) as ESTADO_CALCULADO,
                                   c.NOMBRE_FISCAL, c.CIF, c.DIRECCION, c.CP, c.POBLACION, c.PROVINCIA, c.TELEFONO, c.EMAIL
                            FROM compras_facturas f
                            LEFT JOIN proveedores c ON f.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
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
                                    DIRECCION = reader["DIRECCION"].ToString().Trim(),
                                    NUMPEDIDO = reader["NUMPEDIDO"].ToString().Trim(),
                                    NUMALBARAN = reader["NUMALBARAN"].ToString().Trim(),
                                    SFRA = reader["SFRA"] != DBNull.Value ? reader["SFRA"].ToString().Trim() : ""
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

            List<compras_facturas> lineasArticulos = new List<compras_facturas>();
            try
            {
                var serializer = new JavaScriptSerializer();
                lineasArticulos = serializer.Deserialize<List<compras_facturas>>(lineasJson);
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
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM compras_facturas WHERE NUMFACTURA = @numCheck";
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
                        string sfra = form["SFRA"] ?? "";
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string observaciones = form["OBSERVACIONES"];
                        string estadoGlobal = "102";

                        string nombreProveedor = "PROVEEDOR DESCONOCIDO";
                        using (var cmdProveedor = conexion.CreateCommand())
                        {
                            cmdProveedor.Transaction = transaccion;
                            cmdProveedor.CommandText = "SELECT NOMBRE_FISCAL FROM proveedores WHERE CUENTA = @cuentaProveedor";
                            cmdProveedor.Parameters.AddWithValue("@cuentaProveedor", cuenta);
                            var resProveedor = cmdProveedor.ExecuteScalar();
                            if (resProveedor != null && resProveedor != DBNull.Value)
                            {
                                nombreProveedor = resProveedor.ToString().Trim();
                            }
                        }

                        int diasVenci = 0;
                        using (var cmdFpRule = conexion.CreateCommand())
                        {
                            cmdFpRule.Transaction = transaccion;
                            cmdFpRule.CommandText = "SELECT IFNULL(PRIMVENCI, 0) FROM fpagcob WHERE CODIGO = @codFp";
                            cmdFpRule.Parameters.AddWithValue("@codFp", fcobro);
                            var resFp = cmdFpRule.ExecuteScalar();
                            if (resFp != null && resFp != DBNull.Value)
                            {
                                diasVenci = Convert.ToInt32(resFp);
                            }
                        }
                        DateTime fechaVencimiento = fechFac.AddDays(diasVenci);

                        decimal totalBaseImponible = 0;
                        Dictionary<int, decimal> desgloseIva = new Dictionary<int, decimal>();
                        int contadorLinea = 10;

                        foreach (var linea in lineasArticulos)
                        {
                            using (var cmd = conexion.CreateCommand())
                            {
                                cmd.Transaction = transaccion;
                                cmd.CommandText = @"INSERT INTO compras_facturas (
                                    NUMFACTURA, FECHFAC, NUMPEDIDO, SFRA, FECHALB, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES, RECTIFICATIVA
                                ) VALUES (
                                    @numfactura, @fechfac, '', @sfra, @fechalb, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, @estadolin, @observaciones, ''
                                )";

                                cmd.Parameters.AddWithValue("@numfactura", numFactura);
                                cmd.Parameters.AddWithValue("@fechfac", fechFac);
                                cmd.Parameters.AddWithValue("@sfra", sfra);
                                cmd.Parameters.AddWithValue("@fechalb", DBNull.Value);
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
                                cmd.Parameters.AddWithValue("@estadolin", estadoGlobal);
                                cmd.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmd.ExecuteNonQuery();
                            }
                            contadorLinea += 10;

                            decimal baseLinea = Math.Round((decimal)(linea.CANTI * linea.EUROS * (1 - (linea.DTOARTI / 100m))), 2);
                            decimal ivaLinea = Math.Round((decimal)(baseLinea * (linea.IVARTI / 100m)), 2);

                            totalBaseImponible += baseLinea;

                            if (linea.IVARTI > 0 && ivaLinea > 0)
                            {
                                int porcentajeIvaInt = Convert.ToInt32(linea.IVARTI);
                                if (!desgloseIva.ContainsKey(porcentajeIvaInt))
                                    desgloseIva[porcentajeIvaInt] = 0;

                                desgloseIva[porcentajeIvaInt] += ivaLinea;
                            }
                        }

                        decimal totalIvaAcumulado = 0;
                        foreach (var kvp in desgloseIva) { totalIvaAcumulado += kvp.Value; }
                        decimal totalFacturaDocumento = totalBaseImponible + totalIvaAcumulado;

                        int anioContable = fechFac.Year;
                        long rangoMinimo = (long)anioContable * 1000000 + 1;
                        long rangoMaximo = (long)anioContable * 1000000 + 999999;
                        long numeroAsientoGenerado = rangoMinimo;

                        using (var cmdAsiNum = conexion.CreateCommand())
                        {
                            cmdAsiNum.Transaction = transaccion;
                            cmdAsiNum.CommandText = "SELECT IFNULL(MAX(ASIENTO), @rangoMin - 1) + 1 FROM asientos WHERE ASIENTO BETWEEN @rangoMin AND @rangoMax";
                            cmdAsiNum.Parameters.AddWithValue("@rangoMin", rangoMinimo);
                            cmdAsiNum.Parameters.AddWithValue("@rangoMax", rangoMaximo);
                            numeroAsientoGenerado = Convert.ToInt64(cmdAsiNum.ExecuteScalar());
                        }

                        string textoObservacion = "FAC COMPRA " + numFactura.Trim() + " " + nombreProveedor;
                        if (textoObservacion.Length > 50) textoObservacion = textoObservacion.Substring(0, 50);

                        using (var cmdDebeCompras = conexion.CreateCommand())
                        {
                            cmdDebeCompras.Transaction = transaccion;
                            cmdDebeCompras.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FC', @asiento, @fechaAs, @obs, '600000000', 'D', @euros)";
                            cmdDebeCompras.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                            cmdDebeCompras.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                            cmdDebeCompras.Parameters.AddWithValue("@obs", textoObservacion);
                            cmdDebeCompras.Parameters.AddWithValue("@euros", totalBaseImponible);
                            cmdDebeCompras.ExecuteNonQuery();
                        }

                        foreach (var tasaIva in desgloseIva)
                        {
                            string cuentaIvaFormateada = "472" + tasaIva.Key.ToString().PadLeft(6, '0');

                            using (var cmdDebeIva = conexion.CreateCommand())
                            {
                                cmdDebeIva.Transaction = transaccion;
                                cmdDebeIva.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FC', @asiento, @fechaAs, @obs, @cuentaIva, 'D', @euros)";
                                cmdDebeIva.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                                cmdDebeIva.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                                cmdDebeIva.Parameters.AddWithValue("@obs", textoObservacion);
                                cmdDebeIva.Parameters.AddWithValue("@cuentaIva", cuentaIvaFormateada);
                                cmdDebeIva.Parameters.AddWithValue("@euros", tasaIva.Value);
                                cmdDebeIva.ExecuteNonQuery();
                            }
                        }

                        using (var cmdHaberProveedor = conexion.CreateCommand())
                        {
                            cmdHaberProveedor.Transaction = transaccion;
                            cmdHaberProveedor.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FC', @asiento, @fechaAs, @obs, @cuenta, 'H', @euros)";
                            cmdHaberProveedor.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                            cmdHaberProveedor.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                            cmdHaberProveedor.Parameters.AddWithValue("@obs", textoObservacion);
                            cmdHaberProveedor.Parameters.AddWithValue("@cuenta", cuenta);
                            cmdHaberProveedor.Parameters.AddWithValue("@euros", totalFacturaDocumento);
                            cmdHaberProveedor.ExecuteNonQuery();
                        }

                        using (var cmdPago = conexion.CreateCommand())
                        {
                            cmdPago.Transaction = transaccion;
                            cmdPago.CommandText = @"INSERT INTO pagos (
                                FPAGO, NUMFAC, FECHA_VEN, IMPORTE, CUENTA, FECHA_LIB, FECH_FAC, BANCO, SFRA, OBSERVACION, EMITIDO, VENCIDO, ESTADO
                            ) VALUES (
                                @fpago, @numfac, @fecha_ven, @importe, @cuenta, NULL, @fech_fac, '', @sfra, '', 'N', 0, '101'
                            )";
                            cmdPago.Parameters.AddWithValue("@fpago", fcobro);
                            cmdPago.Parameters.AddWithValue("@numfac", numFactura);
                            cmdPago.Parameters.AddWithValue("@fecha_ven", fechaVencimiento);
                            cmdPago.Parameters.AddWithValue("@importe", totalFacturaDocumento);
                            cmdPago.Parameters.AddWithValue("@cuenta", cuenta);
                            cmdPago.Parameters.AddWithValue("@fech_fac", fechFac);
                            cmdPago.Parameters.AddWithValue("@sfra", numFactura.Trim());
                            cmdPago.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Factura de compra registrada, contabilizada (Asiento " + numeroAsientoGenerado + ") y enviada a cartera de pagos.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al procesar la factura de compra, asiento y pago: " + ex.Message;
                    }
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
                        cmd.CommandText = "SELECT COUNT(*) FROM compras_facturas WHERE NUMFACTURA = @numCheck";
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

        [HttpPost]
        public JsonResult CrearRectificativa(string numFacturaOriginal)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numFacturaOriginal))
                return Json(new { success = false, message = "Parámetros inválidos." });

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        string nuevoNumFactura = "1".PadLeft(9);
                        using (var cmdMax = conexion.CreateCommand())
                        {
                            cmdMax.Transaction = transaccion;
                            cmdMax.CommandText = "SELECT IFNULL(MAX(CAST(NUMFACTURA AS UNSIGNED)), 0) + 1 FROM compras_facturas";
                            object result = cmdMax.ExecuteScalar();
                            if (result != null) nuevoNumFactura = result.ToString().PadLeft(9);
                        }

                        using (var cmdClone = conexion.CreateCommand())
                        {
                            cmdClone.Transaction = transaccion;
                            cmdClone.CommandText = @"
                                INSERT INTO compras_facturas (
                                    NUMFACTURA, FECHFAC, NUMPEDIDO, NUMALBARAN, SFRA, FECHALB, 
                                    CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, UNIDAD, CANTI, EUROS, 
                                    IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES, RECTIFICATIVA
                                )
                                SELECT 
                                    @nuevoNum, NOW(), NUMPEDIDO, NUMALBARAN, SFRA, FECHALB, 
                                    CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, UNIDAD, (CANTI * -1), EUROS, 
                                    IVARTI, DTOARTI, '105', '105', CONCAT('Rectificación de factura ', @original), @original
                                FROM compras_facturas 
                                WHERE NUMFACTURA = @original";

                            cmdClone.Parameters.AddWithValue("@nuevoNum", nuevoNumFactura);
                            cmdClone.Parameters.AddWithValue("@original", numFacturaOriginal.PadLeft(9));

                            int creadas = cmdClone.ExecuteNonQuery();
                            if (creadas == 0)
                            {
                                return Json(new { success = false, message = "No se localizaron líneas en el documento origen." });
                            }
                        }

                        using (var cmdUpdateOriginal = conexion.CreateCommand())
                        {
                            cmdUpdateOriginal.Transaction = transaccion;
                            cmdUpdateOriginal.CommandText = "UPDATE compras_facturas SET ESTADO = '103', ESTADOLIN = '103' WHERE NUMFACTURA = @original";
                            cmdUpdateOriginal.Parameters.AddWithValue("@original", numFacturaOriginal.PadLeft(9));
                            cmdUpdateOriginal.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Factura rectificativa de compra " + nuevoNumFactura.Trim() + " generada. Documento de origen revocado.";
                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        return Json(new { success = false, message = "Error de consistencia: " + ex.Message });
                    }
                }
            }
        }

        [HttpPost]
        public JsonResult CrearDesdeAlbaran(string numAlbaran, string lineasJson)
        {
            if (string.IsNullOrEmpty(numAlbaran) || string.IsNullOrEmpty(lineasJson))
                return Json(new { success = false, message = "Parámetros incompletos." });

            List<Dictionary<string, object>> lineasSeleccionadas;
            try
            {
                var serializer = new JavaScriptSerializer();
                lineasSeleccionadas = serializer.Deserialize<List<Dictionary<string, object>>>(lineasJson);
            }
            catch
            {
                return Json(new { success = false, message = "Formato de líneas incorrecto." });
            }

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        string nuevoNumFactura = "";
                        using (var cmdMax = conexion.CreateCommand())
                        {
                            cmdMax.Transaction = transaccion;
                            cmdMax.CommandText = "SELECT IFNULL(MAX(CAST(NUMFACTURA AS UNSIGNED)), 0) + 1 FROM compras_facturas";
                            object result = cmdMax.ExecuteScalar();
                            if (result != null) nuevoNumFactura = result.ToString().PadLeft(9);
                            else nuevoNumFactura = "000000001";
                        }

                        string cuenta = "";
                        string fcobro = "";
                        string observaciones = "";
                        string numPedido = "";
                        using (var cmdCab = conexion.CreateCommand())
                        {
                            cmdCab.Transaction = transaccion;
                            cmdCab.CommandText = "SELECT DISTINCT CUENTA, FCOBRO, OBSERVACIONES, NUMPEDIDO FROM compras_albaranes WHERE NUMALB = @num LIMIT 1";
                            cmdCab.Parameters.AddWithValue("@num", numAlbaran.PadLeft(9));
                            using (var reader = cmdCab.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    cuenta = reader["CUENTA"].ToString();
                                    fcobro = reader["FCOBRO"].ToString();
                                    observaciones = reader["OBSERVACIONES"]?.ToString() ?? "";
                                    numPedido = reader["NUMPEDIDO"]?.ToString() ?? "";
                                }
                                else
                                {
                                    transaccion.Rollback();
                                    return Json(new { success = false, message = "Albarán no encontrado." });
                                }
                            }
                        }

                        string nombreProveedor = "PROVEEDOR DESCONOCIDO";
                        using (var cmdCliente = conexion.CreateCommand())
                        {
                            cmdCliente.Transaction = transaccion;
                            cmdCliente.CommandText = "SELECT NOMBRE_FISCAL FROM proveedores WHERE CUENTA = @cuentaProveedor";
                            cmdCliente.Parameters.AddWithValue("@cuentaProveedor", cuenta);
                            var resCliente = cmdCliente.ExecuteScalar();
                            if (resCliente != null && resCliente != DBNull.Value)
                            {
                                nombreProveedor = resCliente.ToString().Trim();
                            }
                        }

                        int diasVenci = 0;
                        using (var cmdFpRule = conexion.CreateCommand())
                        {
                            cmdFpRule.Transaction = transaccion;
                            cmdFpRule.CommandText = "SELECT IFNULL(PRIMVENCI, 0) FROM fpagcob WHERE CODIGO = @codFp";
                            cmdFpRule.Parameters.AddWithValue("@codFp", fcobro);
                            var resFp = cmdFpRule.ExecuteScalar();
                            if (resFp != null && resFp != DBNull.Value)
                            {
                                diasVenci = Convert.ToInt32(resFp);
                            }
                        }
                        DateTime fechaVencimiento = DateTime.Now.AddDays(diasVenci);

                        decimal totalBaseImponible = 0;
                        Dictionary<int, decimal> desgloseIva = new Dictionary<int, decimal>();
                        int contadorLineaFac = 10;

                        foreach (var linea in lineasSeleccionadas)
                        {
                            int numLineaAlb = Convert.ToInt32(linea["NUMLINEA"]);
                            decimal cantidadFacturar = Convert.ToDecimal(linea["CANTI"]);

                            decimal cantidadAlbaranada = 0;
                            decimal cantidadYaFacturada = 0;
                            string arti = "", desarti = "", unidad = "";
                            decimal euros = 0, dto = 0, iva = 0;

                            using (var cmdLin = conexion.CreateCommand())
                            {
                                cmdLin.Transaction = transaccion;
                                cmdLin.CommandText = @"
                                    SELECT CANTI, ARTI, DESARTI, UNIDAD, EUROS, DTOARTI, IVARTI, FACTURADO,
                                           IFNULL((SELECT SUM(f.CANTI) FROM compras_facturas f WHERE f.NUMALBARAN = a.NUMALB AND f.NUMLINEA = a.NUMLINEA), 0) as YaFacturada
                                    FROM compras_albaranes a
                                    WHERE NUMALB = @num AND NUMLINEA = @linea";
                                cmdLin.Parameters.AddWithValue("@num", numAlbaran.PadLeft(9));
                                cmdLin.Parameters.AddWithValue("@linea", numLineaAlb);
                                using (var reader = cmdLin.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        cantidadAlbaranada = Convert.ToDecimal(reader["CANTI"]);
                                        arti = reader["ARTI"].ToString();
                                        desarti = reader["DESARTI"].ToString();
                                        unidad = reader["UNIDAD"].ToString();
                                        euros = Convert.ToDecimal(reader["EUROS"]);
                                        dto = Convert.ToDecimal(reader["DTOARTI"]);
                                        iva = Convert.ToDecimal(reader["IVARTI"]);
                                        cantidadYaFacturada = Convert.ToDecimal(reader["YaFacturada"]);
                                    }
                                    else continue;
                                }
                            }

                            decimal pendiente = cantidadAlbaranada - cantidadYaFacturada;
                            if (cantidadFacturar <= 0 || cantidadFacturar > pendiente)
                            {
                                transaccion.Rollback();
                                return Json(new { success = false, message = $"Cantidad inválida para línea {numLineaAlb}." });
                            }

                            using (var cmdIns = conexion.CreateCommand())
                            {
                                cmdIns.Transaction = transaccion;
                                cmdIns.CommandText = @"INSERT INTO compras_facturas (
                                    NUMFACTURA, FECHFAC, NUMPEDIDO, NUMALBARAN, FECHALB, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES, RECTIFICATIVA
                                ) VALUES (
                                    @numfactura, NOW(),  @numpedido, @numalbaran, @fechalb, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, '102', '102', @observaciones, ''
                                )";

                                cmdIns.Parameters.AddWithValue("@numfactura", nuevoNumFactura);
                                cmdIns.Parameters.AddWithValue("@numpedido", numPedido.PadLeft(9));
                                cmdIns.Parameters.AddWithValue("@numalbaran", numAlbaran.PadLeft(9));
                                cmdIns.Parameters.AddWithValue("@fechalb", DateTime.Now);
                                cmdIns.Parameters.AddWithValue("@cuenta", cuenta);
                                cmdIns.Parameters.AddWithValue("@fcobro", fcobro);
                                cmdIns.Parameters.AddWithValue("@numlinea", contadorLineaFac);
                                cmdIns.Parameters.AddWithValue("@arti", arti);
                                cmdIns.Parameters.AddWithValue("@desarti", desarti);
                                cmdIns.Parameters.AddWithValue("@unidad", unidad);
                                cmdIns.Parameters.AddWithValue("@canti", cantidadFacturar);
                                cmdIns.Parameters.AddWithValue("@euros", euros);
                                cmdIns.Parameters.AddWithValue("@ivarti", iva);
                                cmdIns.Parameters.AddWithValue("@dtoarti", dto);
                                cmdIns.Parameters.AddWithValue("@observaciones", observaciones);
                                cmdIns.ExecuteNonQuery();
                            }

                            decimal nuevaCantidadFacturada = cantidadYaFacturada + cantidadFacturar;
                            string nuevoEstadoFacturado = (nuevaCantidadFacturada >= cantidadAlbaranada) ? "S" : "P";

                            using (var cmdUpd = conexion.CreateCommand())
                            {
                                cmdUpd.Transaction = transaccion;
                                cmdUpd.CommandText = "UPDATE compras_albaranes SET FACTURADO = @estFact WHERE NUMALB = @num AND NUMLINEA = @linea";
                                cmdUpd.Parameters.AddWithValue("@estFact", nuevoEstadoFacturado);
                                cmdUpd.Parameters.AddWithValue("@num", numAlbaran.PadLeft(9));
                                cmdUpd.Parameters.AddWithValue("@linea", numLineaAlb);
                                cmdUpd.ExecuteNonQuery();
                            }

                            decimal baseLinea = Math.Round((decimal)(cantidadFacturar * euros * (1 - (dto / 100m))), 2);
                            decimal ivaLinea = Math.Round((decimal)(baseLinea * (iva / 100m)), 2);
                            totalBaseImponible += baseLinea;

                            if (iva > 0 && ivaLinea > 0)
                            {
                                int porcentajeIvaInt = Convert.ToInt32(iva);
                                if (!desgloseIva.ContainsKey(porcentajeIvaInt))
                                    desgloseIva[porcentajeIvaInt] = 0;

                                desgloseIva[porcentajeIvaInt] += ivaLinea;
                            }

                            contadorLineaFac += 10;
                        }

                        string estadoGlobalAlbaran = "101";
                        using (var cmdEst = conexion.CreateCommand())
                        {
                            cmdEst.Transaction = transaccion;
                            cmdEst.CommandText = @"
                                SELECT 
                                    SUM(CASE WHEN FACTURADO = 'N' THEN 1 ELSE 0 END) as Pendientes,
                                    SUM(CASE WHEN FACTURADO = 'P' THEN 1 ELSE 0 END) as Parciales,
                                    SUM(CASE WHEN FACTURADO = 'S' THEN 1 ELSE 0 END) as Completadas,
                                    COUNT(*) as Total
                                FROM compras_albaranes WHERE NUMALB = @num";
                            cmdEst.Parameters.AddWithValue("@num", numAlbaran.PadLeft(9));
                            using (var reader = cmdEst.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int pendientes = Convert.ToInt32(reader["Pendientes"]);
                                    int parciales = Convert.ToInt32(reader["Parciales"]);
                                    int completadas = Convert.ToInt32(reader["Completadas"]);
                                    int total = Convert.ToInt32(reader["Total"]);

                                    if (pendientes > 0 || parciales > 0) estadoGlobalAlbaran = "104";
                                    else if (completadas == total) estadoGlobalAlbaran = "102";
                                }
                            }
                        }

                        using (var cmdGlobal = conexion.CreateCommand())
                        {
                            cmdGlobal.Transaction = transaccion;
                            cmdGlobal.CommandText = "UPDATE compras_albaranes SET ESTADO = @est WHERE NUMALB = @num";
                            cmdGlobal.Parameters.AddWithValue("@est", estadoGlobalAlbaran);
                            cmdGlobal.Parameters.AddWithValue("@num", numAlbaran.PadLeft(9));
                            cmdGlobal.ExecuteNonQuery();
                        }

                        decimal totalIvaAcumulado = 0;
                        foreach (var kvp in desgloseIva) { totalIvaAcumulado += kvp.Value; }
                        decimal totalFacturaDocumento = totalBaseImponible + totalIvaAcumulado;

                        int anioContable = DateTime.Now.Year;
                        long rangoMinimo = (long)anioContable * 1000000 + 1;
                        long rangoMaximo = (long)anioContable * 1000000 + 999999;
                        long numeroAsientoGenerado = rangoMinimo;

                        using (var cmdAsiNum = conexion.CreateCommand())
                        {
                            cmdAsiNum.Transaction = transaccion;
                            cmdAsiNum.CommandText = "SELECT IFNULL(MAX(ASIENTO), @rangoMin - 1) + 1 FROM asientos WHERE ASIENTO BETWEEN @rangoMin AND @rangoMax";
                            cmdAsiNum.Parameters.AddWithValue("@rangoMin", rangoMinimo);
                            cmdAsiNum.Parameters.AddWithValue("@rangoMax", rangoMaximo);
                            numeroAsientoGenerado = Convert.ToInt64(cmdAsiNum.ExecuteScalar());
                        }

                        string textoObservacion = "FAC COMPRA " + nuevoNumFactura.Trim() + " " + nombreProveedor;
                        if (textoObservacion.Length > 50) textoObservacion = textoObservacion.Substring(0, 50);

                        using (var cmdDebeCompras = conexion.CreateCommand())
                        {
                            cmdDebeCompras.Transaction = transaccion;
                            cmdDebeCompras.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FC', @asiento, @fechaAs, @obs, '600000000', 'D', @euros)";
                            cmdDebeCompras.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                            cmdDebeCompras.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                            cmdDebeCompras.Parameters.AddWithValue("@obs", textoObservacion);
                            cmdDebeCompras.Parameters.AddWithValue("@euros", totalBaseImponible);
                            cmdDebeCompras.ExecuteNonQuery();
                        }

                        foreach (var tasaIva in desgloseIva)
                        {
                            string cuentaIvaFormateada = "472" + tasaIva.Key.ToString().PadLeft(6, '0');
                            using (var cmdDebeIva = conexion.CreateCommand())
                            {
                                cmdDebeIva.Transaction = transaccion;
                                cmdDebeIva.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FC', @asiento, @fechaAs, @obs, @cuentaIva, 'D', @euros)";
                                cmdDebeIva.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                                cmdDebeIva.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                                cmdDebeIva.Parameters.AddWithValue("@obs", textoObservacion);
                                cmdDebeIva.Parameters.AddWithValue("@cuentaIva", cuentaIvaFormateada);
                                cmdDebeIva.Parameters.AddWithValue("@euros", tasaIva.Value);
                                cmdDebeIva.ExecuteNonQuery();
                            }
                        }

                        using (var cmdHaberProveedor = conexion.CreateCommand())
                        {
                            cmdHaberProveedor.Transaction = transaccion;
                            cmdHaberProveedor.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FC', @asiento, @fechaAs, @obs, @cuenta, 'H', @euros)";
                            cmdHaberProveedor.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                            cmdHaberProveedor.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                            cmdHaberProveedor.Parameters.AddWithValue("@obs", textoObservacion);
                            cmdHaberProveedor.Parameters.AddWithValue("@cuenta", cuenta);
                            cmdHaberProveedor.Parameters.AddWithValue("@euros", totalFacturaDocumento);
                            cmdHaberProveedor.ExecuteNonQuery();
                        }

                        using (var cmdPago = conexion.CreateCommand())
                        {
                            cmdPago.Transaction = transaccion;
                            cmdPago.CommandText = @"INSERT INTO pagos (
                                FPAGO, NUMFAC, FECHA_VEN, IMPORTE, CUENTA, FECHA_LIB, FECH_FAC, BANCO, SFRA, OBSERVACION, EMITIDO, VENCIDO, ESTADO
                            ) VALUES (
                                @fcobro, @numfac, @fecha_ven, @importe, @cuenta, NULL, @fech_fac, '', @sfra, '', 'N', 0, '101'
                            )";
                            cmdPago.Parameters.AddWithValue("@fcobro", fcobro);
                            cmdPago.Parameters.AddWithValue("@numfac", nuevoNumFactura);
                            cmdPago.Parameters.AddWithValue("@fecha_ven", fechaVencimiento);
                            cmdPago.Parameters.AddWithValue("@importe", totalFacturaDocumento);
                            cmdPago.Parameters.AddWithValue("@cuenta", cuenta);
                            cmdPago.Parameters.AddWithValue("@fech_fac", DateTime.Now);
                            cmdPago.Parameters.AddWithValue("@sfra", numAlbaran.Trim());
                            cmdPago.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        return Json(new { success = true, mensaje = $"Factura {nuevoNumFactura.Trim()} generada correctamente a partir del albarán {numAlbaran.Trim()}." });
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }
    }
}