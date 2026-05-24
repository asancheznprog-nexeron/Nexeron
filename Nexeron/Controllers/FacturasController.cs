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
                        cmdRec.CommandText = "SELECT DISTINCT TRIM(RECTIFICATIVA) FROM facturas WHERE RECTIFICATIVA IS NOT NULL AND RECTIFICATIVA <> ''";
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
                                SELECT f.NUMFACTURA, f.FECHFAC, f.NUMALBARAN, f.CUENTA, c.NOMBRE_FISCAL as NombreCliente, 
                                       f.FCOBRO, f.OBSERVACIONES,
                                       IF(COUNT(DISTINCT f.ESTADOLIN) > 1, '104', MAX(f.ESTADO)) as ESTADO,
                                       SUM(ROUND(f.CANTI * f.EUROS * (1 - (f.DTOARTI / 100)), 2)) as BaseTotal,
                                       SUM(ROUND(ROUND(f.CANTI * f.EUROS * (1 - (f.DTOARTI / 100)), 2) * (f.IVARTI / 100), 2)) as IvaTotal
                                FROM facturas f
                                LEFT JOIN clientes c ON f.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
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

                                lista.Add(new facturas
                                {
                                    NUMFACTURA = reader["NUMFACTURA"].ToString(),
                                    FECHFAC = Convert.ToDateTime(reader["FECHFAC"]),
                                    NUMALBARAN = reader["NUMALBARAN"].ToString(),
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
                                    DIRECCION = reader["DIRECCION"].ToString().Trim(),
                                    NUMOFERTA = reader["NUMOFERTA"].ToString().Trim(),
                                    NUMPEDIDO = reader["NUMPEDIDO"].ToString().Trim(),
                                    NUMALBARAN = reader["NUMALBARAN"].ToString().Trim()
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
                        string numAlbaran = form["NUMALBARAN"];
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string observaciones = form["OBSERVACIONES"];
                        string estadoGlobal = "102";

                        string nombreCliente = "CLIENTE DESCONOCIDO";
                        using (var cmdCliente = conexion.CreateCommand())
                        {
                            cmdCliente.Transaction = transaccion;
                            cmdCliente.CommandText = "SELECT NOMBRE_FISCAL FROM clientes WHERE CUENTA = @cuentaCliente";
                            cmdCliente.Parameters.AddWithValue("@cuentaCliente", cuenta);
                            var resCliente = cmdCliente.ExecuteScalar();
                            if (resCliente != null && resCliente != DBNull.Value)
                            {
                                nombreCliente = resCliente.ToString().Trim();
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
                                cmd.CommandText = @"INSERT INTO facturas (
                                    NUMFACTURA, FECHFAC, NUMOFERTA, NUMPEDIDO, NUMALBARAN, FECHALB, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES, RECTIFICATIVA
                                ) VALUES (
                                    @numfactura, @fechfac, '', '', @numalbaran, @fechalb, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, @estadolin, @observaciones, ''
                                )";

                                cmd.Parameters.AddWithValue("@numfactura", numFactura);
                                cmd.Parameters.AddWithValue("@fechfac", fechFac);
                                cmd.Parameters.AddWithValue("@numalbaran", numAlbaran ?? "");
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

                        string textoObservacion = "FACTURA " + numFactura.Trim() + " " + nombreCliente;
                        if (textoObservacion.Length > 50) textoObservacion = textoObservacion.Substring(0, 50);

                        using (var cmdDebeCliente = conexion.CreateCommand())
                        {
                            cmdDebeCliente.Transaction = transaccion;
                            cmdDebeCliente.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FV', @asiento, @fechaAs, @obs, @cuenta, 'D', @euros)";
                            cmdDebeCliente.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                            cmdDebeCliente.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                            cmdDebeCliente.Parameters.AddWithValue("@obs", textoObservacion);
                            cmdDebeCliente.Parameters.AddWithValue("@cuenta", cuenta);
                            cmdDebeCliente.Parameters.AddWithValue("@euros", totalFacturaDocumento);
                            cmdDebeCliente.ExecuteNonQuery();
                        }

                        using (var cmdHaberVentas = conexion.CreateCommand())
                        {
                            cmdHaberVentas.Transaction = transaccion;
                            cmdHaberVentas.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                           VALUES ('FV', @asiento, @fechaAs, @obs, '7000000', 'H', @euros)";
                            cmdHaberVentas.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                            cmdHaberVentas.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                            cmdHaberVentas.Parameters.AddWithValue("@obs", textoObservacion);
                            cmdHaberVentas.Parameters.AddWithValue("@euros", totalBaseImponible);
                            cmdHaberVentas.ExecuteNonQuery();
                        }

                        foreach (var tasaIva in desgloseIva)
                        {
                            string cuentaIvaFormateada = "477" + tasaIva.Key.ToString().PadLeft(6, '0');

                            using (var cmdHaberIva = conexion.CreateCommand())
                            {
                                cmdHaberIva.Transaction = transaccion;
                                cmdHaberIva.CommandText = @"INSERT INTO asientos (CONCEPTO, ASIENTO, FECHA_ASIENTO, OBSERVACION, CUENTA, DH, EUROS) 
                                                               VALUES ('FV', @asiento, @fechaAs, @obs, @cuentaIva, 'H', @euros)";
                                cmdHaberIva.Parameters.AddWithValue("@asiento", numeroAsientoGenerado);
                                cmdHaberIva.Parameters.AddWithValue("@fechaAs", DateTime.Now);
                                cmdHaberIva.Parameters.AddWithValue("@obs", textoObservacion);
                                cmdHaberIva.Parameters.AddWithValue("@cuentaIva", cuentaIvaFormateada);
                                cmdHaberIva.Parameters.AddWithValue("@euros", tasaIva.Value);
                                cmdHaberIva.ExecuteNonQuery();
                            }
                        }

                        string numEfec = numFactura.Trim() + "/1/1";
                        using (var cmdCobro = conexion.CreateCommand())
                        {
                            cmdCobro.Transaction = transaccion;
                            cmdCobro.CommandText = @"INSERT INTO cobros (
                                FCOBRO, NUMFAC, FECHA_VEN, IMPORTE, CUENTA, GASTOS, BANCO, FECHA_LIB, FECH_FAC, 
                                NUMEFEC, FESTADO, FRECEP, OBSERVACION, EMITIDO, VENCIDO, NPAGARE, ESTADO
                            ) VALUES (
                                @fcobro, @numfac, @fecha_ven, @importe, @cuenta, @gastos, @banco, @fecha_lib, @fech_fac, 
                                @numefec, @festado, @frecep, @observacion, @emitido, @vencido, @npagare, @estado
                            )";

                            cmdCobro.Parameters.AddWithValue("@fcobro", fcobro);
                            cmdCobro.Parameters.AddWithValue("@numfac", numFactura);
                            cmdCobro.Parameters.AddWithValue("@fecha_ven", fechaVencimiento);
                            cmdCobro.Parameters.AddWithValue("@importe", totalFacturaDocumento);
                            cmdCobro.Parameters.AddWithValue("@cuenta", cuenta);
                            cmdCobro.Parameters.AddWithValue("@gastos", 0.000m);
                            cmdCobro.Parameters.AddWithValue("@banco", "");
                            cmdCobro.Parameters.AddWithValue("@fecha_lib", DBNull.Value);
                            cmdCobro.Parameters.AddWithValue("@fech_fac", fechFac);
                            cmdCobro.Parameters.AddWithValue("@numefec", numEfec);
                            cmdCobro.Parameters.AddWithValue("@festado", fechFac);
                            cmdCobro.Parameters.AddWithValue("@estado", "101");
                            cmdCobro.Parameters.AddWithValue("@frecep", DBNull.Value);
                            cmdCobro.Parameters.AddWithValue("@observacion", "");
                            cmdCobro.Parameters.AddWithValue("@emitido", 0);
                            cmdCobro.Parameters.AddWithValue("@vencido", 0);
                            cmdCobro.Parameters.AddWithValue("@npagare", 0);

                            cmdCobro.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Factura registrada, contabilizada (Asiento " + numeroAsientoGenerado + ") y enviada a cartera de cobros.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al procesar la factura, asiento y cobro: " + ex.Message;
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
                            cmdMax.CommandText = "SELECT IFNULL(MAX(CAST(NUMFACTURA AS UNSIGNED)), 0) + 1 FROM facturas";
                            object result = cmdMax.ExecuteScalar();
                            if (result != null) nuevoNumFactura = result.ToString().PadLeft(9);
                        }

                        using (var cmdClone = conexion.CreateCommand())
                        {
                            cmdClone.Transaction = transaccion;
                            cmdClone.CommandText = @"
                                INSERT INTO facturas (
                                    NUMFACTURA, FECHFAC, NUMOFERTA, NUMPEDIDO, NUMALBARAN, FECHALB, 
                                    CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, UNIDAD, CANTI, EUROS, 
                                    IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES, RECTIFICATIVA
                                )
                                SELECT 
                                    @nuevoNum, NOW(), NUMOFERTA, NUMPEDIDO, NUMALBARAN, FECHALB, 
                                    CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, UNIDAD, (CANTI * -1), EUROS, 
                                    IVARTI, DTOARTI, '105', '105', CONCAT('Rectificación de factura ', @original), @original
                                FROM facturas 
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
                            cmdUpdateOriginal.CommandText = "UPDATE facturas SET ESTADO = '103', ESTADOLIN = '103' WHERE NUMFACTURA = @original";
                            cmdUpdateOriginal.Parameters.AddWithValue("@original", numFacturaOriginal.PadLeft(9));
                            cmdUpdateOriginal.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Factura rectificativa " + nuevoNumFactura.Trim() + " generada. Documento de origen revocado.";
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
    }
}