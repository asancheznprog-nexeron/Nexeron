using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class ComprasPedidosController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<compras_pedidos> lista = new List<compras_pedidos>();
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
                        cmd.CommandText = @"SELECT p.NUMPEDIDO, p.FECHPED, p.CUENTA, pr.NOMBRE_FISCAL as NombreProveedor, 
                                                   p.ESTADO, p.FCOBRO, p.OBSERVACIONES,
                                                   SUM(ROUND(p.CANTI * p.EUROS * (1 - (p.DTOARTI / 100)), 2)) as BaseTotal,
                                                   SUM(ROUND(ROUND(p.CANTI * p.EUROS * (1 - (p.DTOARTI / 100)), 2) * (p.IVARTI / 100), 2)) as IvaTotal
                                            FROM compras_pedidos p
                                            LEFT JOIN proveedores pr ON p.CUENTA = pr.CUENTA COLLATE utf8mb4_spanish_ci
                                            GROUP BY p.NUMPEDIDO, p.FECHPED, p.CUENTA, pr.NOMBRE_FISCAL, p.ESTADO, p.FCOBRO, p.OBSERVACIONES
                                            ORDER BY p.NUMPEDIDO DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal baseTotal = Convert.ToDecimal(reader["BaseTotal"]);
                                decimal ivaTotal = Convert.ToDecimal(reader["IvaTotal"]);

                                lista.Add(new compras_pedidos
                                {
                                    NUMPEDIDO = reader["NUMPEDIDO"].ToString(),
                                    FECHPED = Convert.ToDateTime(reader["FECHPED"]),
                                    CUENTA = reader["CUENTA"].ToString(),
                                    NombreProveedor = reader["NombreProveedor"].ToString(),
                                    ESTADO = reader["ESTADO"].ToString(),
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
                    ViewBag.Error = "Error al cargar datos del pedido de compra: " + ex.Message;
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
        public JsonResult ObtenerDetallePedido(string numPedido)
        {
            List<object> lineas = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numPedido))
                return Json(lineas);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT p.*, pr.NOMBRE_FISCAL, pr.CIF, pr.DIRECCION, pr.CP, pr.POBLACION, pr.PROVINCIA, pr.TELEFONO, pr.EMAIL
                                            FROM compras_pedidos p
                                            LEFT JOIN proveedores pr ON p.CUENTA = pr.CUENTA COLLATE utf8mb4_spanish_ci
                                            WHERE p.NUMPEDIDO = @num
                                            ORDER BY p.NUMLINEA ASC";
                        cmd.Parameters.AddWithValue("@num", numPedido.PadLeft(9));
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
                                    ESTADO = reader["ESTADO"].ToString().Trim(),
                                    FECHA = Convert.ToDateTime(reader["FECHPED"]).ToString("yyyy-MM-dd"),
                                    OBSERVACIONES = reader["OBSERVACIONES"].ToString(),
                                    NUMLINEA = Convert.ToInt32(reader["NUMLINEA"]),
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
        [ValidateAntiForgeryToken]
        public ActionResult Crear(FormCollection form, string lineasJson)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(lineasJson))
            {
                TempData["Error"] = "El pedido debe contener al menos un artículo.";
                TempData["TipoError"] = "Crear";
                return RedirectToAction("Index");
            }

            List<compras_pedidos> lineasArticulos = new List<compras_pedidos>();
            try
            {
                var serializer = new JavaScriptSerializer();
                lineasArticulos = serializer.Deserialize<List<compras_pedidos>>(lineasJson);
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
                        string numPedido = form["NUMPEDIDO"].PadLeft(9);
                        DateTime fechPed = Convert.ToDateTime(form["FECHPED"]);
                        DateTime? fechEnt = string.IsNullOrEmpty(form["FECHENT"]) ? (DateTime?)null : Convert.ToDateTime(form["FECHENT"]);
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string observaciones = form["OBSERVACIONES"];

                        int contadorLinea = 10;
                        foreach (var linea in lineasArticulos)
                        {
                            using (var cmd = conexion.CreateCommand())
                            {
                                cmd.Transaction = transaccion;
                                cmd.CommandText = @"INSERT INTO compras_pedidos (
                                                    NUMPEDIDO, FECHPED, FECHENT, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES
                                                ) VALUES (
                                                    @numpedido, @fechped, @fechent, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                                    @unidad, @canti, @euros, @ivarti, @dtoarti, '101', '101', @observaciones
                                                )";

                                cmd.Parameters.AddWithValue("@numpedido", numPedido);
                                cmd.Parameters.AddWithValue("@fechped", fechPed);
                                cmd.Parameters.AddWithValue("@fechent", fechEnt ?? (object)DBNull.Value);
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

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Pedido de compra creado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al crear el pedido de compra: " + ex.Message;
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
                TempData["Error"] = "El pedido debe contener al menos un artículo.";
                TempData["TipoError"] = "Editar";
                return RedirectToAction("Index");
            }

            List<compras_pedidos> nuevasLineas = new List<compras_pedidos>();
            try
            {
                var serializer = new JavaScriptSerializer();
                nuevasLineas = serializer.Deserialize<List<compras_pedidos>>(lineasJsonModificadas);
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
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        string numPedido = form["NUMPEDIDO"].PadLeft(9);
                        DateTime fechPed = Convert.ToDateTime(form["FECHPED"]);
                        DateTime? fechEnt = string.IsNullOrEmpty(form["FECHENT"]) ? (DateTime?)null : Convert.ToDateTime(form["FECHENT"]);
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string observaciones = form["OBSERVACIONES"];

                        using (var cmdDelete = conexion.CreateCommand())
                        {
                            cmdDelete.Transaction = transaccion;
                            cmdDelete.CommandText = "DELETE FROM compras_pedidos WHERE NUMPEDIDO = @numpedido";
                            cmdDelete.Parameters.AddWithValue("@numpedido", numPedido);
                            cmdDelete.ExecuteNonQuery();
                        }

                        int contadorLinea = 10;
                        foreach (var linea in nuevasLineas)
                        {
                            using (var cmdInsert = conexion.CreateCommand())
                            {
                                cmdInsert.Transaction = transaccion;
                                cmdInsert.CommandText = @"INSERT INTO compras_pedidos (
                                                            NUMPEDIDO, FECHPED, FECHENT, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                                            UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES
                                                        ) VALUES (
                                                            @numpedido, @fechped, @fechent, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                                            @unidad, @canti, @euros, @ivarti, @dtoarti, '201', '201', @observaciones
                                                        )";

                                cmdInsert.Parameters.AddWithValue("@numpedido", numPedido);
                                cmdInsert.Parameters.AddWithValue("@fechped", fechPed);
                                cmdInsert.Parameters.AddWithValue("@fechent", fechEnt ?? (object)DBNull.Value);
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
                                cmdInsert.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmdInsert.ExecuteNonQuery();
                            }
                            contadorLinea += 10;
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Pedido de compra actualizado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Fallo al actualizar el pedido de compra: " + ex.Message;
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
                        cmd.CommandText = "DELETE FROM compras_pedidos WHERE NUMPEDIDO = @num";
                        cmd.Parameters.AddWithValue("@num", id.PadLeft(9));
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Pedido de compra eliminado correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al eliminar el pedido de compra: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
    }
}