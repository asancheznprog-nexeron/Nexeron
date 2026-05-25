using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class AlbaranesController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<albaranes> lista = new List<albaranes>();
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

                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT t.*, IFNULL(e.estado, 'Desconocido') as NombreEstado
                            FROM (
                                SELECT a.NUMALB, a.FECHALB, a.NUMPEDIDO, a.CUENTA, c.NOMBRE_FISCAL as NombreCliente,
                                       a.FCOBRO, a.OBSERVACIONES,
                                       IF(COUNT(DISTINCT a.ESTADOLIN) > 1, '104', MAX(a.ESTADO)) as ESTADO,
                                       SUM(ROUND(a.CANTI * a.EUROS * (1 - (a.DTOARTI / 100)), 2)) as BaseTotal,
                                       SUM(ROUND(ROUND(a.CANTI * a.EUROS * (1 - (a.DTOARTI / 100)), 2) * (a.IVARTI / 100), 2)) as IvaTotal
                                FROM albaranes a
                                LEFT JOIN clientes c ON a.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                GROUP BY a.NUMALB, a.FECHALB, a.NUMPEDIDO, a.CUENTA, c.NOMBRE_FISCAL, a.FCOBRO, a.OBSERVACIONES
                            ) t
                            LEFT JOIN estados e ON t.ESTADO = e.codigo
                            ORDER BY t.NUMALB DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal baseTotal = Convert.ToDecimal(reader["BaseTotal"]);
                                decimal ivaTotal = Convert.ToDecimal(reader["IvaTotal"]);

                                lista.Add(new albaranes
                                {
                                    NUMALB = reader["NUMALB"].ToString(),
                                    FECHALB = Convert.ToDateTime(reader["FECHALB"]),
                                    NUMPEDIDO = reader["NUMPEDIDO"].ToString(),
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
                    ViewBag.Error = "Error al compilar el panel de albaranes: " + ex.Message;
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
        public JsonResult ObtenerDetalleAlbaran(string numAlbaran)
        {
            List<object> lineas = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numAlbaran))
                return Json(lineas);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT a.*, 
                                   (SELECT IF(COUNT(DISTINCT sub.ESTADOLIN) > 1, '104', a.ESTADO) 
                                    FROM albaranes sub WHERE sub.NUMALB = a.NUMALB) as ESTADO_CALCULADO,
                                   c.NOMBRE_FISCAL, c.CIF, c.DIRECCION, c.CP, c.POBLACION, c.PROVINCIA, c.TELEFONO, c.EMAIL
                            FROM albaranes a
                            LEFT JOIN clientes c ON a.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                            WHERE a.NUMALB = @num 
                            ORDER BY a.NUMLINEA ASC";
                        cmd.Parameters.AddWithValue("@num", numAlbaran.PadLeft(9));
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
                                    FECHA = Convert.ToDateTime(reader["FECHALB"]).ToString("yyyy-MM-dd"),
                                    OBSERVACIONES = reader["OBSERVACIONES"].ToString(),
                                    NUMLINEA = Convert.ToInt32(reader["NUMLINEA"]),
                                    NUMPEDIDO = reader["NUMPEDIDO"].ToString().Trim(),
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
            return Json(lineas);
        }

        [HttpPost]
        public JsonResult ObtenerPedidosPendientes()
        {
            List<object> pedidosPendientes = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr)) return Json(pedidosPendientes);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.NUMPEDIDO, p.FECHPED, p.CUENTA, c.NOMBRE_FISCAL as NombreCliente, p.ESTADO
                        FROM pedidos p
                        LEFT JOIN clientes c ON p.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                        WHERE EXISTS (
                            SELECT 1 FROM pedidos sub 
                            WHERE sub.NUMPEDIDO = p.NUMPEDIDO AND sub.ALBARANADO IN ('N', 'P')
                        )
                        GROUP BY p.NUMPEDIDO, p.FECHPED, p.CUENTA, c.NOMBRE_FISCAL, p.ESTADO
                        ORDER BY p.NUMPEDIDO ASC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pedidosPendientes.Add(new
                            {
                                NUMPEDIDO = reader["NUMPEDIDO"].ToString().Trim(),
                                FECHAPED = Convert.ToDateTime(reader["FECHPED"]).ToString("dd/MM/yyyy"),
                                CUENTA = reader["CUENTA"].ToString().Trim(),
                                NombreCliente = reader["NombreCliente"].ToString().Trim(),
                                ESTADO = reader["ESTADO"].ToString().Trim()
                            });
                        }
                    }
                }
            }
            return Json(pedidosPendientes);
        }

        [HttpPost]
        public JsonResult ObtenerLineasPedidoParaAlbaran(string numPedido)
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
                    cmd.CommandText = @"
                        SELECT p.NUMLINEA, p.ARTI, p.DESARTI, p.CANTI as CantidadOriginal, 
                               p.UNIDAD, p.EUROS, p.DTOARTI, p.IVARTI, p.ALBARANADO,
                               IFNULL((SELECT SUM(a.CANTI) FROM albaranes a WHERE a.NUMPEDIDO = p.NUMPEDIDO AND a.NUMLINEAPED = p.NUMLINEA), 0) as CantidadAlbaranada,
                               (p.CANTI - IFNULL((SELECT SUM(a.CANTI) FROM albaranes a WHERE a.NUMPEDIDO = p.NUMPEDIDO AND a.NUMLINEAPED = p.NUMLINEA), 0)) as Pendiente
                        FROM pedidos p
                        WHERE p.NUMPEDIDO = @num AND p.ALBARANADO IN ('N','P')
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
                                CantidadOriginal = Convert.ToDecimal(reader["CantidadOriginal"]),
                                UNIDAD = reader["UNIDAD"].ToString().Trim(),
                                EUROS = Convert.ToDecimal(reader["EUROS"]),
                                DTOARTI = Convert.ToDecimal(reader["DTOARTI"]),
                                IVARTI = Convert.ToDecimal(reader["IVARTI"]),
                                ALBARANADO = reader["ALBARANADO"].ToString().Trim(),
                                CantidadAlbaranada = Convert.ToDecimal(reader["CantidadAlbaranada"]),
                                Pendiente = Convert.ToDecimal(reader["Pendiente"])
                            });
                        }
                    }
                }
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
                TempData["Error"] = "El albarán debe contener al menos un artículo.";
                TempData["TipoError"] = "Crear";
                return RedirectToAction("Index");
            }

            List<albaranes> lineasArticulos = new List<albaranes>();
            try
            {
                var serializer = new JavaScriptSerializer();
                lineasArticulos = serializer.Deserialize<List<albaranes>>(lineasJson);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error en formato de líneas: " + ex.Message;
                TempData["TipoError"] = "Crear";
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
                        string numAlb = form["NUMALB"].PadLeft(9);
                        DateTime fechAlb = Convert.ToDateTime(form["FECHALB"]);
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string observaciones = form["OBSERVACIONES"];
                        string numPedido = form["NUMPEDIDO"]?.PadLeft(9) ?? "";

                        int contadorLinea = 10;
                        foreach (var linea in lineasArticulos)
                        {
                            using (var cmd = conexion.CreateCommand())
                            {
                                cmd.Transaction = transaccion;
                                cmd.CommandText = @"INSERT INTO albaranes (
                                    NUMALB, FECHALB, NUMPEDIDO, NUMLINEAPED, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES
                                ) VALUES (
                                    @numalb, @fechalb, @numpedido, @numlineaped, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, '101', '101', @observaciones
                                )";

                                cmd.Parameters.AddWithValue("@numalb", numAlb);
                                cmd.Parameters.AddWithValue("@fechalb", fechAlb);
                                cmd.Parameters.AddWithValue("@numpedido", numPedido);
                                cmd.Parameters.AddWithValue("@numlineaped", 0);  
                                cmd.Parameters.AddWithValue("@cuenta", cuenta);
                                cmd.Parameters.AddWithValue("@fcobro", fcobro);
                                cmd.Parameters.AddWithValue("@numlinea", contadorLinea);
                                cmd.Parameters.AddWithValue("@arti", linea.ARTI ?? "");
                                cmd.Parameters.AddWithValue("@desarti", linea.DESARTI ?? "");
                                cmd.Parameters.AddWithValue("@unidad", linea.UNIDAD ?? "");
                                cmd.Parameters.AddWithValue("@canti", linea.CANTI);
                                cmd.Parameters.AddWithValue("@euros", linea.EUROS);
                                cmd.Parameters.AddWithValue("@ivarti", linea.IVARTI);
                                cmd.Parameters.AddWithValue("@dtoarti", linea.DTOARTI);
                                cmd.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmd.ExecuteNonQuery();
                            }
                            contadorLinea += 10;
                        }
                        foreach (var linea in lineasArticulos)
                        {
                            using (var cmdInv = conexion.CreateCommand())
                            {
                                cmdInv.Transaction = transaccion;
                                cmdInv.CommandText = @"INSERT INTO inventario (articulo, descripcion, cantidad, tipo, origen, referencia, cuenta, fecha) 
                               VALUES (@articulo, @descripcion, @cantidad, 'S', 'VENTAS', @referencia, @cuenta, @fecha)";
                                cmdInv.Parameters.AddWithValue("@articulo", linea.ARTI ?? "");
                                cmdInv.Parameters.AddWithValue("@descripcion", linea.DESARTI ?? "");
                                cmdInv.Parameters.AddWithValue("@cantidad", -linea.CANTI);
                                cmdInv.Parameters.AddWithValue("@referencia", numAlb);
                                cmdInv.Parameters.AddWithValue("@cuenta", cuenta);
                                cmdInv.Parameters.AddWithValue("@fecha", fechAlb);
                                cmdInv.ExecuteNonQuery();
                            }
                        }
                        transaccion.Commit();
                        TempData["MensajeExito"] = "Albarán creado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al crear el albarán: " + ex.Message;
                        TempData["TipoError"] = "Crear";
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
                TempData["Error"] = "El albarán debe contener al menos un artículo válido.";
                TempData["TipoError"] = "Editar";
                return RedirectToAction("Index");
            }

            List<albaranes> nuevasLineas = new List<albaranes>();
            try
            {
                var serializer = new JavaScriptSerializer();
                nuevasLineas = serializer.Deserialize<List<albaranes>>(lineasJsonModificadas);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error en análisis de datos: " + ex.Message;
                TempData["TipoError"] = "Editar";
                return RedirectToAction("Index");
            }

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();

                string numAlb = form["NUMALB"].PadLeft(9);

                using (var cmdCheck = conexion.CreateCommand())
                {
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM albaranes WHERE NUMALB = @num AND FACTURADO IN ('P','S')";
                    cmdCheck.Parameters.AddWithValue("@num", numAlb);
                    if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                    {
                        TempData["Error"] = "No se puede modificar un albarán que ya ha sido facturado total o parcialmente.";
                        TempData["TipoError"] = "Editar";
                        return RedirectToAction("Index");
                    }
                }

                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        DateTime fechAlb = Convert.ToDateTime(form["FECHALB"]);
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string observaciones = form["OBSERVACIONES"];

                        string estadoOriginal = "101";
                        using (var cmdGetEstado = conexion.CreateCommand())
                        {
                            cmdGetEstado.Transaction = transaccion;
                            cmdGetEstado.CommandText = "SELECT MAX(ESTADO) FROM albaranes WHERE NUMALB = @num";
                            cmdGetEstado.Parameters.AddWithValue("@num", numAlb);
                            var res = cmdGetEstado.ExecuteScalar();
                            if (res != null && res != DBNull.Value) estadoOriginal = res.ToString();
                        }

                        using (var cmdDelete = conexion.CreateCommand())
                        {
                            cmdDelete.Transaction = transaccion;
                            cmdDelete.CommandText = "DELETE FROM albaranes WHERE NUMALB = @numalb";
                            cmdDelete.Parameters.AddWithValue("@numalb", numAlb);
                            cmdDelete.ExecuteNonQuery();
                        }
                        using (var cmdDelInv = conexion.CreateCommand())
                        {
                            cmdDelInv.Transaction = transaccion;
                            cmdDelInv.CommandText = "DELETE FROM inventario WHERE origen = 'VENTAS' AND referencia = @ref";
                            cmdDelInv.Parameters.AddWithValue("@ref", numAlb);
                            cmdDelInv.ExecuteNonQuery();
                        }

                        int contadorLinea = 10;
                        foreach (var linea in nuevasLineas)
                        {
                            using (var cmdInsert = conexion.CreateCommand())
                            {
                                cmdInsert.Transaction = transaccion;
                                cmdInsert.CommandText = @"INSERT INTO albaranes (
                            NUMALB, FECHALB, NUMPEDIDO, NUMLINEAPED, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                            UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, FACTURADO, OBSERVACIONES
                        ) VALUES (
                            @numalb, @fechalb, @numpedido, @numlineaped, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                            @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, @estadolin, 'N', @observaciones
                        )";

                                cmdInsert.Parameters.AddWithValue("@numalb", numAlb);
                                cmdInsert.Parameters.AddWithValue("@fechalb", fechAlb);
                                cmdInsert.Parameters.AddWithValue("@numpedido", linea.NUMPEDIDO ?? "");
                                cmdInsert.Parameters.AddWithValue("@numlineaped", linea.NUMLINEAPED);
                                cmdInsert.Parameters.AddWithValue("@cuenta", cuenta);
                                cmdInsert.Parameters.AddWithValue("@fcobro", fcobro);
                                cmdInsert.Parameters.AddWithValue("@numlinea", contadorLinea);
                                cmdInsert.Parameters.AddWithValue("@arti", linea.ARTI ?? "");
                                cmdInsert.Parameters.AddWithValue("@desarti", linea.DESARTI ?? "");
                                cmdInsert.Parameters.AddWithValue("@unidad", linea.UNIDAD ?? "");
                                cmdInsert.Parameters.AddWithValue("@canti", linea.CANTI);
                                cmdInsert.Parameters.AddWithValue("@euros", linea.EUROS);
                                cmdInsert.Parameters.AddWithValue("@ivarti", linea.IVARTI);
                                cmdInsert.Parameters.AddWithValue("@dtoarti", linea.DTOARTI);
                                cmdInsert.Parameters.AddWithValue("@estado", estadoOriginal);
                                cmdInsert.Parameters.AddWithValue("@estadolin", estadoOriginal);
                                cmdInsert.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmdInsert.ExecuteNonQuery();
                            }
                            contadorLinea += 10;
                        }
                        foreach (var linea in nuevasLineas)
                        {
                            using (var cmdInv = conexion.CreateCommand())
                            {
                                cmdInv.Transaction = transaccion;
                                cmdInv.CommandText = @"INSERT INTO inventario (articulo, descripcion, cantidad, tipo, origen, referencia, cuenta, fecha) 
                               VALUES (@articulo, @descripcion, @cantidad, 'S', 'VENTAS', @referencia, @cuenta, @fecha)";
                                cmdInv.Parameters.AddWithValue("@articulo", linea.ARTI ?? "");
                                cmdInv.Parameters.AddWithValue("@descripcion", linea.DESARTI ?? "");
                                cmdInv.Parameters.AddWithValue("@cantidad", -linea.CANTI);
                                cmdInv.Parameters.AddWithValue("@referencia", numAlb);
                                cmdInv.Parameters.AddWithValue("@cuenta", cuenta);
                                cmdInv.Parameters.AddWithValue("@fecha", fechAlb);
                                cmdInv.ExecuteNonQuery();
                            }
                        }
                        transaccion.Commit();
                        TempData["MensajeExito"] = "Albarán actualizado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Fallo al actualizar el albarán: " + ex.Message;
                        TempData["TipoError"] = "Editar";
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
                        cmd.CommandText = "DELETE FROM albaranes WHERE NUMALB = @num";
                        cmd.Parameters.AddWithValue("@num", id.PadLeft(9));
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmdDelInv = conexion.CreateCommand())
                    {
                        cmdDelInv.CommandText = "DELETE FROM inventario WHERE origen = 'VENTAS' AND referencia = @ref";
                        cmdDelInv.Parameters.AddWithValue("@ref", id.PadLeft(9));
                        cmdDelInv.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Albarán eliminado correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al eliminar albarán: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult CrearDesdePedido(string numPedido, string lineasJson)
        {
            if (string.IsNullOrEmpty(numPedido) || string.IsNullOrEmpty(lineasJson))
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
                        string nuevoNumAlb = "";
                        using (var cmdMax = conexion.CreateCommand())
                        {
                            cmdMax.Transaction = transaccion;
                            cmdMax.CommandText = "SELECT IFNULL(MAX(CAST(NUMALB AS UNSIGNED)), 0) + 1 FROM albaranes";
                            object result = cmdMax.ExecuteScalar();
                            if (result != null) nuevoNumAlb = result.ToString().PadLeft(9);
                            else nuevoNumAlb = "000000001";
                        }

                        string cuenta = "";
                        string fcobro = "";
                        string observaciones = "";
                        using (var cmdCab = conexion.CreateCommand())
                        {
                            cmdCab.Transaction = transaccion;
                            cmdCab.CommandText = "SELECT DISTINCT CUENTA, FCOBRO, OBSERVACIONES FROM pedidos WHERE NUMPEDIDO = @num LIMIT 1";
                            cmdCab.Parameters.AddWithValue("@num", numPedido.PadLeft(9));
                            using (var reader = cmdCab.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    cuenta = reader["CUENTA"].ToString();
                                    fcobro = reader["FCOBRO"].ToString();
                                    observaciones = reader["OBSERVACIONES"]?.ToString() ?? "";
                                }
                                else
                                {
                                    transaccion.Rollback();
                                    return Json(new { success = false, message = "Pedido no encontrado." });
                                }
                            }
                        }

                        int contadorLineaAlb = 10;
                        foreach (var linea in lineasSeleccionadas)
                        {
                            int numLineaPed = Convert.ToInt32(linea["NUMLINEA"]);
                            decimal cantidadAlbaranar = Convert.ToDecimal(linea["CANTI"]);

                            decimal cantidadPedida = 0;
                            decimal cantidadYaAlbaranada = 0;
                            string arti = "", desarti = "", unidad = "";
                            decimal euros = 0, dto = 0, iva = 0;

                            using (var cmdLin = conexion.CreateCommand())
                            {
                                cmdLin.Transaction = transaccion;
                                cmdLin.CommandText = @"
                            SELECT CANTI, ARTI, DESARTI, UNIDAD, EUROS, DTOARTI, IVARTI, ALBARANADO,
                                   IFNULL((SELECT SUM(a.CANTI) FROM albaranes a WHERE a.NUMPEDIDO = p.NUMPEDIDO AND a.NUMLINEAPED = p.NUMLINEA), 0) as YaAlbaranada
                            FROM pedidos p
                            WHERE NUMPEDIDO = @num AND NUMLINEA = @linea";
                                cmdLin.Parameters.AddWithValue("@num", numPedido.PadLeft(9));
                                cmdLin.Parameters.AddWithValue("@linea", numLineaPed);
                                using (var reader = cmdLin.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        cantidadPedida = Convert.ToDecimal(reader["CANTI"]);
                                        arti = reader["ARTI"].ToString();
                                        desarti = reader["DESARTI"].ToString();
                                        unidad = reader["UNIDAD"].ToString();
                                        euros = Convert.ToDecimal(reader["EUROS"]);
                                        dto = Convert.ToDecimal(reader["DTOARTI"]);
                                        iva = Convert.ToDecimal(reader["IVARTI"]);
                                        cantidadYaAlbaranada = Convert.ToDecimal(reader["YaAlbaranada"]);
                                    }
                                    else continue;
                                }
                            }

                            decimal pendiente = cantidadPedida - cantidadYaAlbaranada;
                            if (cantidadAlbaranar <= 0 || cantidadAlbaranar > pendiente)
                            {
                                transaccion.Rollback();
                                return Json(new { success = false, message = $"Cantidad inválida para línea {numLineaPed}." });
                            }

                            using (var cmdIns = conexion.CreateCommand())
                            {
                                cmdIns.Transaction = transaccion;
                                cmdIns.CommandText = @"INSERT INTO albaranes (
                            NUMALB, FECHALB, NUMPEDIDO, NUMLINEAPED, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                            UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES
                        ) VALUES (
                            @numalb, NOW(), @numpedido, @numlineaped, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                            @unidad, @canti, @euros, @ivarti, @dtoarti, '101', '101', @obs
                        )";
                                cmdIns.Parameters.AddWithValue("@numalb", nuevoNumAlb);
                                cmdIns.Parameters.AddWithValue("@numpedido", numPedido.PadLeft(9));
                                cmdIns.Parameters.AddWithValue("@numlineaped", numLineaPed);
                                cmdIns.Parameters.AddWithValue("@cuenta", cuenta);
                                cmdIns.Parameters.AddWithValue("@fcobro", fcobro);
                                cmdIns.Parameters.AddWithValue("@numlinea", contadorLineaAlb);
                                cmdIns.Parameters.AddWithValue("@arti", arti);
                                cmdIns.Parameters.AddWithValue("@desarti", desarti);
                                cmdIns.Parameters.AddWithValue("@unidad", unidad);
                                cmdIns.Parameters.AddWithValue("@canti", cantidadAlbaranar);
                                cmdIns.Parameters.AddWithValue("@euros", euros);
                                cmdIns.Parameters.AddWithValue("@ivarti", iva);
                                cmdIns.Parameters.AddWithValue("@dtoarti", dto);
                                cmdIns.Parameters.AddWithValue("@obs", observaciones);
                                cmdIns.ExecuteNonQuery();
                            }

                            decimal nuevaCantidadAlbaranada = cantidadYaAlbaranada + cantidadAlbaranar;
                            string nuevoEstadoAlbaranado = (nuevaCantidadAlbaranada >= cantidadPedida) ? "S" : "P";

                            using (var cmdUpd = conexion.CreateCommand())
                            {
                                cmdUpd.Transaction = transaccion;
                                cmdUpd.CommandText = "UPDATE pedidos SET ALBARANADO = @estAlb WHERE NUMPEDIDO = @num AND NUMLINEA = @linea";
                                cmdUpd.Parameters.AddWithValue("@estAlb", nuevoEstadoAlbaranado);
                                cmdUpd.Parameters.AddWithValue("@num", numPedido.PadLeft(9));
                                cmdUpd.Parameters.AddWithValue("@linea", numLineaPed);
                                cmdUpd.ExecuteNonQuery();
                            }

                            contadorLineaAlb += 10;
                        }

                        string estadoGlobalPedido = "101";
                        using (var cmdEst = conexion.CreateCommand())
                        {
                            cmdEst.Transaction = transaccion;
                            cmdEst.CommandText = @"
                        SELECT 
                            SUM(CASE WHEN ALBARANADO = 'N' THEN 1 ELSE 0 END) as Pendientes,
                            SUM(CASE WHEN ALBARANADO = 'P' THEN 1 ELSE 0 END) as Parciales,
                            SUM(CASE WHEN ALBARANADO = 'S' THEN 1 ELSE 0 END) as Completadas,
                            COUNT(*) as Total
                        FROM pedidos WHERE NUMPEDIDO = @num";
                            cmdEst.Parameters.AddWithValue("@num", numPedido.PadLeft(9));
                            using (var reader = cmdEst.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int pendientes = Convert.ToInt32(reader["Pendientes"]);
                                    int parciales = Convert.ToInt32(reader["Parciales"]);
                                    int completadas = Convert.ToInt32(reader["Completadas"]);
                                    int total = Convert.ToInt32(reader["Total"]);

                                    if (pendientes > 0 || parciales > 0) estadoGlobalPedido = "104";
                                    else if (completadas == total) estadoGlobalPedido = "102";
                                }
                            }
                        }

                        using (var cmdGlobal = conexion.CreateCommand())
                        {
                            cmdGlobal.Transaction = transaccion;
                            cmdGlobal.CommandText = "UPDATE pedidos SET ESTADO = @est WHERE NUMPEDIDO = @num";
                            cmdGlobal.Parameters.AddWithValue("@est", estadoGlobalPedido);
                            cmdGlobal.Parameters.AddWithValue("@num", numPedido.PadLeft(9));
                            cmdGlobal.ExecuteNonQuery();
                        }

                        foreach (var linea in lineasSeleccionadas)
                        {
                            using (var cmdInv = conexion.CreateCommand())
                            {
                                cmdInv.Transaction = transaccion;
                                cmdInv.CommandText = @"INSERT INTO inventario (articulo, descripcion, cantidad, tipo, origen, referencia, cuenta, fecha) 
                       VALUES (@articulo, @descripcion, @cantidad, 'S', 'VENTAS', @referencia, @cuenta, NOW())";

                                string articuloInv = linea.ContainsKey("ARTI") && linea["ARTI"] != null ? linea["ARTI"].ToString() : "";
                                string descripcionInv = linea.ContainsKey("DESARTI") && linea["DESARTI"] != null ? linea["DESARTI"].ToString() : "";

                                cmdInv.Parameters.AddWithValue("@articulo", articuloInv);
                                cmdInv.Parameters.AddWithValue("@descripcion", descripcionInv);
                                cmdInv.Parameters.AddWithValue("@cantidad", -Convert.ToDecimal(linea["CANTI"]));
                                cmdInv.Parameters.AddWithValue("@referencia", nuevoNumAlb);
                                cmdInv.Parameters.AddWithValue("@cuenta", cuenta);
                                cmdInv.ExecuteNonQuery();
                            }
                        }

                        transaccion.Commit();
                        return Json(new { success = true, mensaje = $"Albarán {nuevoNumAlb} generado correctamente." });
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }

        [HttpPost]
        public JsonResult ExisteNumeroAlbaran(string numAlb)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numAlb))
                return Json(new { existe = false });

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM albaranes WHERE NUMALB = @num";
                        cmd.Parameters.AddWithValue("@num", numAlb.PadLeft(9));
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
        public JsonResult ObtenerPedidosPendientesPorCliente(string cuenta)
        {
            List<object> pedidosPendientes = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr)) return Json(pedidosPendientes);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT p.NUMPEDIDO, p.FECHPED, p.ESTADO, p.FCOBRO,
                       c.NOMBRE_FISCAL as NombreCliente
                FROM pedidos p
                LEFT JOIN clientes c ON p.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                WHERE p.CUENTA = @cuenta AND EXISTS (
                    SELECT 1 FROM pedidos sub 
                    WHERE sub.NUMPEDIDO = p.NUMPEDIDO AND sub.ALBARANADO IN ('N', 'P')
                )
                GROUP BY p.NUMPEDIDO, p.FECHPED, p.ESTADO, p.FCOBRO, c.NOMBRE_FISCAL
                ORDER BY p.NUMPEDIDO ASC";
                    cmd.Parameters.AddWithValue("@cuenta", cuenta.PadLeft(9));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pedidosPendientes.Add(new
                            {
                                NUMPEDIDO = reader["NUMPEDIDO"].ToString().Trim(),
                                FECHAPED = Convert.ToDateTime(reader["FECHPED"]).ToString("dd/MM/yyyy"),
                                ESTADO = reader["ESTADO"].ToString().Trim(),
                                FCOBRO = reader["FCOBRO"].ToString().Trim(),
                                NombreCliente = reader["NombreCliente"].ToString().Trim()
                            });
                        }
                    }
                }
            }
            return Json(pedidosPendientes);
        }


        [HttpPost]
        public JsonResult ObtenerAlbaranesPendientesPorCliente(string cuenta)
        {
            List<object> albaranesPendientes = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr)) return Json(albaranesPendientes);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT a.NUMALB, a.FECHALB, a.ESTADO, a.FCOBRO,
                       IFNULL(fp.FORMACOBPAG, a.FCOBRO) as FormaCobroDesc
                FROM albaranes a
                LEFT JOIN fpagcob fp ON a.FCOBRO = fp.CODIGO
                WHERE a.CUENTA = @cuenta AND EXISTS (
                    SELECT 1 FROM albaranes sub 
                    WHERE sub.NUMALB = a.NUMALB AND sub.FACTURADO IN ('N', 'P')
                )
                GROUP BY a.NUMALB, a.FECHALB, a.ESTADO, a.FCOBRO, fp.FORMACOBPAG
                ORDER BY a.NUMALB ASC";
                    cmd.Parameters.AddWithValue("@cuenta", cuenta.PadLeft(9));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            albaranesPendientes.Add(new
                            {
                                NUMALB = reader["NUMALB"].ToString().Trim(),
                                FECHALB = Convert.ToDateTime(reader["FECHALB"]).ToString("dd/MM/yyyy"),
                                ESTADO = reader["ESTADO"].ToString().Trim(),
                                FCOBRO = reader["FormaCobroDesc"].ToString().Trim()
                            });
                        }
                    }
                }
            }
            return Json(albaranesPendientes);
        }

        [HttpPost]
        public JsonResult ObtenerLineasAlbaranParaFacturar(string numAlbaran)
        {
            List<object> lineas = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numAlbaran))
                return Json(lineas);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT a.NUMLINEA, a.ARTI, a.DESARTI, a.CANTI as CantidadOriginal, 
                       a.UNIDAD, a.EUROS, a.DTOARTI, a.IVARTI, a.FACTURADO,
                       IFNULL((SELECT SUM(f.CANTI) FROM facturas f WHERE f.NUMALBARAN = a.NUMALB AND f.NUMLINEA = a.NUMLINEA), 0) as CantidadFacturada,
                       (a.CANTI - IFNULL((SELECT SUM(f.CANTI) FROM facturas f WHERE f.NUMALBARAN = a.NUMALB AND f.NUMLINEA = a.NUMLINEA), 0)) as Pendiente
                FROM albaranes a
                WHERE a.NUMALB = @num AND a.FACTURADO IN ('N','P')
                ORDER BY a.NUMLINEA ASC";
                    cmd.Parameters.AddWithValue("@num", numAlbaran.PadLeft(9));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lineas.Add(new
                            {
                                NUMLINEA = Convert.ToInt32(reader["NUMLINEA"]),
                                ARTI = reader["ARTI"].ToString().Trim(),
                                DESARTI = reader["DESARTI"].ToString().Trim(),
                                CantidadOriginal = Convert.ToDecimal(reader["CantidadOriginal"]),
                                UNIDAD = reader["UNIDAD"].ToString().Trim(),
                                EUROS = Convert.ToDecimal(reader["EUROS"]),
                                DTOARTI = Convert.ToDecimal(reader["DTOARTI"]),
                                IVARTI = Convert.ToDecimal(reader["IVARTI"]),
                                FACTURADO = reader["FACTURADO"].ToString().Trim(),
                                CantidadFacturada = Convert.ToDecimal(reader["CantidadFacturada"]),
                                Pendiente = Convert.ToDecimal(reader["Pendiente"])
                            });
                        }
                    }
                }
            }
            return Json(lineas);
        }



    }
}