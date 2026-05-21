using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class OfertasController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<ofertas> lista = new List<ofertas>();
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

                    
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT o.NUMOFERTA, o.FECHOFERTA, o.CUENTA, c.NOMBRE_FISCAL as NombreCliente, 
                                                   o.ESTADO, o.NUMPEDIDO, o.FCOBRO, o.OBSERVACIONES
                                            FROM ofertas o
                                            LEFT JOIN clientes c ON o.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                            GROUP BY o.NUMOFERTA, o.FECHOFERTA, o.CUENTA, c.NOMBRE_FISCAL, o.ESTADO, o.NUMPEDIDO, o.FCOBRO, o.OBSERVACIONES
                                            ORDER BY o.NUMOFERTA DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new ofertas
                                {
                                    NUMOFERTA = reader["NUMOFERTA"].ToString(),
                                    FECHOFERTA = Convert.ToDateTime(reader["FECHOFERTA"]),
                                    CUENTA = reader["CUENTA"].ToString(),
                                    NombreCliente = reader["NombreCliente"].ToString(),
                                    ESTADO = reader["ESTADO"].ToString(),
                                    NUMPEDIDO = reader["NUMPEDIDO"].ToString(),
                                    FCOBRO = reader["FCOBRO"].ToString(),
                                    OBSERVACIONES = reader["OBSERVACIONES"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al compilar el panel de ofertas: " + ex.Message;
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
        public JsonResult ObtenerDetalleOferta(string numOferta)
        {
            List<object> lineas = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numOferta))
                return Json(lineas);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        
                        cmd.CommandText = @"SELECT o.*, 
                                           c.NOMBRE_FISCAL, c.CIF, c.DIRECCION, c.CP, c.POBLACION, c.PROVINCIA, c.TELEFONO, c.EMAIL
                                    FROM ofertas o
                                    LEFT JOIN clientes c ON o.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                    WHERE o.NUMOFERTA = @num 
                                    ORDER BY o.NUMLINEA ASC";

                        cmd.Parameters.AddWithValue("@num", numOferta.PadLeft(9));
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
                                    FECHA = Convert.ToDateTime(reader["FECHOFERTA"]).ToString("yyyy-MM-dd"),
                                    OBSERVACIONES = reader["OBSERVACIONES"].ToString(),

                                    
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
                TempData["Error"] = "La oferta debe contener al menos un artículo en las líneas.";
                TempData["TipoError"] = "Crear";
                return RedirectToAction("Index");
            }

            List<ofertas> lineasArticulos = new List<ofertas>();
            try
            {
                var serializer = new JavaScriptSerializer();
                lineasArticulos = serializer.Deserialize<List<ofertas>>(lineasJson);
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
                        string numOferta = form["NUMOFERTA"].PadLeft(9);
                        DateTime fechOferta = Convert.ToDateTime(form["FECHOFERTA"]);
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
                                cmd.CommandText = @"INSERT INTO ofertas (
                                    NUMOFERTA, FECHOFERTA, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, NUMPEDIDO, OBSERVACIONES
                                ) VALUES (
                                    @numoferta, @fechoferta, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, '101', '', @observaciones
                                )";

                                cmd.Parameters.AddWithValue("@numoferta", numOferta);
                                cmd.Parameters.AddWithValue("@fechoferta", fechOferta);
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
                                cmd.Parameters.AddWithValue("@estado", estadoGlobal);
                                cmd.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmd.ExecuteNonQuery();
                            }
                            contadorLinea += 10;
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Oferta comercial guardada de forma exitosa.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al insertar la proforma: " + ex.Message;
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
                TempData["Error"] = "La oferta debe contener al menos un artículo válido.";
                TempData["TipoError"] = "Editar";
                return RedirectToAction("Index");
            }

            List<ofertas> nuevasLineas = new List<ofertas>();
            try
            {
                var serializer = new JavaScriptSerializer();
                nuevasLineas = serializer.Deserialize<List<ofertas>>(lineasJsonModificadas);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error en análisis de datos comerciales: " + ex.Message;
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
                        string numOferta = form["NUMOFERTA"].PadLeft(9);
                        DateTime fechOferta = Convert.ToDateTime(form["FECHOFERTA"]);
                        string cuenta = form["CUENTA"].PadLeft(9);
                        string fcobro = form["FCOBRO"];
                        string estadoGlobal = form["ESTADO"];
                        string observaciones = form["OBSERVACIONES"];

                        
                        using (var cmdDelete = conexion.CreateCommand())
                        {
                            cmdDelete.Transaction = transaccion;
                            cmdDelete.CommandText = "DELETE FROM ofertas WHERE NUMOFERTA = @numoferta";
                            cmdDelete.Parameters.AddWithValue("@numoferta", numOferta);
                            cmdDelete.ExecuteNonQuery();
                        }

                        
                        int contadorLinea = 10;
                        foreach (var linea in nuevasLineas)
                        {
                            using (var cmdInsert = conexion.CreateCommand())
                            {
                                cmdInsert.Transaction = transaccion;
                                cmdInsert.CommandText = @"INSERT INTO ofertas (
                                    NUMOFERTA, FECHOFERTA, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, 
                                    UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, NUMPEDIDO, OBSERVACIONES
                                ) VALUES (
                                    @numoferta, @fechoferta, @cuenta, @fcobro, @numlinea, @arti, @desarti, 
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, '101', '', @observaciones
                                )";

                                cmdInsert.Parameters.AddWithValue("@numoferta", numOferta);
                                cmdInsert.Parameters.AddWithValue("@fechoferta", fechOferta);
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
                                cmdInsert.Parameters.AddWithValue("@estado", estadoGlobal);
                                cmdInsert.Parameters.AddWithValue("@observaciones", observaciones ?? "");

                                cmdInsert.ExecuteNonQuery();
                            }
                            contadorLinea += 10;
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Cambios guardados y grilla actualizada con éxito.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Fallo crítico al sobreescribir la oferta: " + ex.Message;
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
                        cmd.CommandText = "DELETE FROM ofertas WHERE NUMOFERTA = @numoferta";
                        cmd.Parameters.AddWithValue("@numoferta", id.PadLeft(9));
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "La oferta seleccionada ha sido eliminada por completo.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "No se pudo eliminar el documento de ofertas: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
    }
}