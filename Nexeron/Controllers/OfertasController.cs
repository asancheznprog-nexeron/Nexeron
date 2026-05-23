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

                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT t.*, IFNULL(e.estado, 'Desconocido') as NombreEstado
                            FROM (
                                SELECT o.NUMOFERTA, o.FECHOFERTA, o.CUENTA, c.NOMBRE_FISCAL as NombreCliente, 
                                       o.FCOBRO, o.OBSERVACIONES,
                                       IF(COUNT(DISTINCT o.ESTADOLIN) > 1, '104', MAX(o.ESTADO)) as ESTADO,
                                       SUM(ROUND(o.CANTI * o.EUROS * (1 - (o.DTOARTI / 100)), 2)) as BaseTotal,
                                       SUM(ROUND(ROUND(o.CANTI * o.EUROS * (1 - (o.DTOARTI / 100)), 2) * (o.IVARTI / 100), 2)) as IvaTotal
                                FROM ofertas o
                                LEFT JOIN clientes c ON o.CUENTA = c.CUENTA COLLATE utf8mb4_spanish_ci
                                GROUP BY o.NUMOFERTA, o.FECHOFERTA, o.CUENTA, c.NOMBRE_FISCAL, o.FCOBRO, o.OBSERVACIONES
                            ) t
                            LEFT JOIN estados e ON t.ESTADO = e.codigo
                            ORDER BY t.NUMOFERTA ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal baseTotal = Convert.ToDecimal(reader["BaseTotal"]);
                                decimal ivaTotal = Convert.ToDecimal(reader["IvaTotal"]);

                                lista.Add(new ofertas
                                {
                                    NUMOFERTA = reader["NUMOFERTA"].ToString(),
                                    FECHOFERTA = Convert.ToDateTime(reader["FECHOFERTA"]),
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
                        cmd.CommandText = @"
                            SELECT o.*, 
                                   (SELECT IF(COUNT(DISTINCT sub.ESTADOLIN) > 1, '104', o.ESTADO) 
                                    FROM ofertas sub WHERE sub.NUMOFERTA = o.NUMOFERTA) as ESTADO_CALCULADO,
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
                                    ESTADO = reader["ESTADO_CALCULADO"].ToString().Trim(),
                                    ESTADOLIN = reader["ESTADOLIN"].ToString().Trim(),
                                    NUMPEDIDO = reader["NUMPEDIDO"].ToString().Trim(),
                                    FECHA = Convert.ToDateTime(reader["FECHOFERTA"]).ToString("yyyy-MM-dd"),
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

            string numOferta = form["NUMOFERTA"].PadLeft(9);
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();

                using (var cmdCheck = conexion.CreateCommand())
                {
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM ofertas WHERE NUMOFERTA = @numCheck";
                    cmdCheck.Parameters.AddWithValue("@numCheck", numOferta);
                    long conteo = Convert.ToInt64(cmdCheck.ExecuteScalar());

                    if (conteo > 0)
                    {
                        TempData["Error"] = "El número de oferta '" + numOferta.Trim() + "' ya se encuentra registrado en el sistema. Introduzca uno diferente.";
                        TempData["TipoError"] = "Crear";
                        return RedirectToAction("Index");
                    }
                }

                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
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
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, @estadolin, '', @observaciones
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
                                cmd.Parameters.AddWithValue("@estadolin", string.IsNullOrEmpty(linea.ESTADOLIN) ? estadoGlobal : linea.ESTADOLIN);
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

                            if (todasLasLineasSonIguales)
                            {
                                estadoGlobal = primerEstadoLin;
                            }
                            else
                            {
                                estadoGlobal = "104";
                            }
                        }
                        

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
                                    @unidad, @canti, @euros, @ivarti, @dtoarti, @estado, @estadolin, @numpedido, @observaciones
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
                                cmdInsert.Parameters.AddWithValue("@estadolin", string.IsNullOrEmpty(linea.ESTADOLIN) ? estadoGlobal : linea.ESTADOLIN);
                                string pedidoFormateado = string.IsNullOrEmpty(linea.NUMPEDIDO) ? "" : linea.NUMPEDIDO.PadLeft(9);
                                cmdInsert.Parameters.AddWithValue("@numpedido", pedidoFormateado);

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

        [HttpPost]
        public JsonResult ObtenerOfertasPorCliente(string cuenta)
        {
            List<object> ofertasPendientes = new List<object>();
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr)) return Json(ofertasPendientes);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT o.NUMOFERTA, o.FECHOFERTA, o.ESTADO, o.FCOBRO,
                       IFNULL(fp.FORMACOBPAG, o.FCOBRO) as FormaCobroDesc,
                       SUM(ROUND(o.CANTI * o.EUROS * (1 - (o.DTOARTI / 100)), 2)) as BaseTotal,
                       SUM(ROUND(ROUND(o.CANTI * o.EUROS * (1 - (o.DTOARTI / 100)), 2) * (1 + (o.IVARTI / 100)), 2)) as Total
                FROM ofertas o
                LEFT JOIN fpagcob fp ON o.FCOBRO = fp.CODIGO
                WHERE o.CUENTA = @cuenta AND o.ESTADO IN ('101','104')
                GROUP BY o.NUMOFERTA, o.FECHOFERTA, o.ESTADO, o.FCOBRO, fp.FORMACOBPAG
                ORDER BY o.NUMOFERTA ASC";
                    cmd.Parameters.AddWithValue("@cuenta", cuenta.PadLeft(9));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ofertasPendientes.Add(new
                            {
                                NUMOFERTA = reader["NUMOFERTA"].ToString().Trim(),
                                FECHOFERTA = Convert.ToDateTime(reader["FECHOFERTA"]).ToString("dd/MM/yyyy"),
                                ESTADO = reader["ESTADO"].ToString().Trim(),
                                FCOBRO = reader["FormaCobroDesc"].ToString().Trim(),
                                BaseTotal = Convert.ToDecimal(reader["BaseTotal"]).ToString("N2"),
                                Total = Convert.ToDecimal(reader["Total"]).ToString("N2")
                            });
                        }
                    }
                }
            }
            return Json(ofertasPendientes);
        }

        [HttpPost]
        public JsonResult ObtenerClienteCompleto(string cuenta)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr)) return Json(null);

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"SELECT CUENTA, NOMBRE_FISCAL, CIF, DIRECCION, CP, POBLACION, PROVINCIA, TELEFONO, EMAIL
                                        FROM clientes
                                        WHERE CUENTA = @cuenta";
                    cmd.Parameters.AddWithValue("@cuenta", cuenta.PadLeft(9));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new
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
                        else
                        {
                            return Json(null);
                        }
                    }
                }
            }
        }

        [HttpPost]
        public JsonResult ProcesarGestionOferta(string numOferta, string lineas)
        {
            var serializer = new JavaScriptSerializer();
            var listaProcesar = serializer.Deserialize<List<Dictionary<string, object>>>(lineas);

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        string nuevoNumPedido = "";
                        bool hayAceptadas = listaProcesar.Exists(l => l["Estado"].ToString() == "ACEPTAR");

                        if (hayAceptadas)
                        {
                            using (var cmdMax = conexion.CreateCommand())
                            {
                                cmdMax.Transaction = transaccion;
                                cmdMax.CommandText = "SELECT IFNULL(MAX(CAST(NUMPEDIDO AS UNSIGNED)), 0) + 1 FROM pedidos";
                                object result = cmdMax.ExecuteScalar();
                                if (result != null) nuevoNumPedido = result.ToString().PadLeft(9, '0');
                            }
                        }
                        foreach (var item in listaProcesar)
                        {
                            int numLinea = Convert.ToInt32(item["NUMLINEA"]);
                            string estadoAccion = item["Estado"].ToString();

                            if (estadoAccion == "ACEPTAR")
                            {
                                using (var cmdIns = conexion.CreateCommand())
                                {
                                    cmdIns.Transaction = transaccion;
                                    cmdIns.CommandText = @"INSERT INTO pedidos (NUMPEDIDO, FECHPED, FECHENT, NUMOFERTA, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, ESTADO, ESTADOLIN, OBSERVACIONES)
                                                   SELECT @nuevoNumPedido, NOW(), NULL, NUMOFERTA, CUENTA, FCOBRO, NUMLINEA, ARTI, DESARTI, UNIDAD, CANTI, EUROS, IVARTI, DTOARTI, '101', '101', OBSERVACIONES
                                                   FROM ofertas WHERE NUMOFERTA = @num AND NUMLINEA = @linea";
                                    cmdIns.Parameters.AddWithValue("@nuevoNumPedido", nuevoNumPedido);
                                    cmdIns.Parameters.AddWithValue("@num", numOferta.PadLeft(9));
                                    cmdIns.Parameters.AddWithValue("@linea", numLinea);
                                    cmdIns.ExecuteNonQuery();
                                }
                                using (var cmdUpd = conexion.CreateCommand())
                                {
                                    cmdUpd.Transaction = transaccion;
                                    cmdUpd.CommandText = "UPDATE ofertas SET ESTADOLIN = '102', NUMPEDIDO = @nuevoNumPedido WHERE NUMOFERTA = @num AND NUMLINEA = @linea";
                                    cmdUpd.Parameters.AddWithValue("@nuevoNumPedido", nuevoNumPedido);
                                    cmdUpd.Parameters.AddWithValue("@num", numOferta.PadLeft(9));
                                    cmdUpd.Parameters.AddWithValue("@linea", numLinea);
                                    cmdUpd.ExecuteNonQuery();
                                }
                            }
                            else if (estadoAccion == "RECHAZAR")
                            {
                                using (var cmdUpd = conexion.CreateCommand())
                                {
                                    cmdUpd.Transaction = transaccion;
                                    cmdUpd.CommandText = "UPDATE ofertas SET ESTADOLIN = '103' WHERE NUMOFERTA = @num AND NUMLINEA = @linea";
                                    cmdUpd.Parameters.AddWithValue("@num", numOferta.PadLeft(9));
                                    cmdUpd.Parameters.AddWithValue("@linea", numLinea);
                                    cmdUpd.ExecuteNonQuery();
                                }
                            }
                        }
                        string estadoGlobalFinal = "101";
                        using (var cmdEst = conexion.CreateCommand())
                        {
                            cmdEst.Transaction = transaccion;
                            cmdEst.CommandText = @"SELECT 
                                            SUM(CASE WHEN ESTADOLIN = '101' THEN 1 ELSE 0 END) as Pendientes,
                                            SUM(CASE WHEN ESTADOLIN = '102' THEN 1 ELSE 0 END) as Aceptadas,
                                            SUM(CASE WHEN ESTADOLIN = '103' THEN 1 ELSE 0 END) as Rechazadas,
                                            COUNT(*) as Total
                                           FROM ofertas WHERE NUMOFERTA = @num";
                            cmdEst.Parameters.AddWithValue("@num", numOferta.PadLeft(9));

                            using (var reader = cmdEst.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int pendientes = Convert.ToInt32(reader["Pendientes"]);
                                    int aceptadas = Convert.ToInt32(reader["Aceptadas"]);
                                    int rechazadas = Convert.ToInt32(reader["Rechazadas"]);
                                    int total = Convert.ToInt32(reader["Total"]);

                                    if (pendientes > 0 || (aceptadas > 0 && rechazadas > 0))
                                    {
                                        estadoGlobalFinal = "104";
                                    }
                                    else if (aceptadas == total)
                                    {
                                        estadoGlobalFinal = "102";
                                    }
                                    else if (rechazadas == total)
                                    {
                                        estadoGlobalFinal = "103";
                                    }
                                }
                            }
                        }

                        using (var cmdGlobal = conexion.CreateCommand())
                        {
                            cmdGlobal.Transaction = transaccion;
                            cmdGlobal.CommandText = "UPDATE ofertas SET ESTADO = @estado WHERE NUMOFERTA = @num";
                            cmdGlobal.Parameters.AddWithValue("@estado", estadoGlobalFinal);
                            cmdGlobal.Parameters.AddWithValue("@num", numOferta.PadLeft(9));
                            cmdGlobal.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        return Json(new { success = true, mensaje = hayAceptadas ? $"Pedido {nuevoNumPedido} generado con éxito" : "Líneas rechazadas procesadas correctamente" });
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
        public JsonResult ExisteNumeroOferta(string numOferta)
        {
            string connStr = Session["cadenaConexion"]?.ToString();
            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(numOferta))
                return Json(new { existe = false });

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        // Aplicamos el PadLeft(9) para igualar el formato de la base de datos
                        cmd.CommandText = "SELECT COUNT(*) FROM ofertas WHERE NUMOFERTA = @numCheck";
                        cmd.Parameters.AddWithValue("@numCheck", numOferta.PadLeft(9));
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