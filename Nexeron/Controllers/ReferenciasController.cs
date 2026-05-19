using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class ReferenciasController : Controller
    {
        private string GetConnectionString() => Session["cadenaConexion"]?.ToString();

        private void CargarDesplegables(ArticuloModel articulo)
        {
            var tipos = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione --" } };
            var unidades = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione --" } };
            var paises = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione --" } };

            using (var con = new MySqlConnection(GetConnectionString()))
            {
                con.Open();
                try
                {
                    using (var cmd = new MySqlCommand("SELECT codigo, descripcion FROM tipos", con))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) tipos.Add(new SelectListItem { Value = reader[0].ToString(), Text = reader[1].ToString() });
                }
                catch { }

                try
                {
                    using (var cmd = new MySqlCommand("SELECT codigo, descripcion FROM unidades", con))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) unidades.Add(new SelectListItem { Value = reader[0].ToString(), Text = reader[1].ToString() });
                }
                catch { }

                try
                {
                    using (var cmd = new MySqlCommand("SELECT codigo, descripcion FROM paises", con))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) paises.Add(new SelectListItem { Value = reader[0].ToString(), Text = reader[1].ToString() });
                }
                catch { }
            }

            articulo.TiposList = tipos;
            articulo.UnidadesList = unidades;
            articulo.PaisesList = paises;
        }

        public ActionResult Index(string buscar = "", int? codDesde = null, int? codHasta = null, string tipoDesde = "", string tipoHasta = "", DateTime? fechaAltaDesde = null, DateTime? fechaAltaHasta = null, DateTime? fechaBajaDesde = null, DateTime? fechaBajaHasta = null)
        {
            var lista = new List<ArticuloModel>();
            var dummy = new ArticuloModel();
            CargarDesplegables(dummy);
            ViewBag.TiposList = dummy.TiposList;
            ViewBag.UnidadesList = dummy.UnidadesList;
            ViewBag.PaisesList = dummy.PaisesList;

            using (var con = new MySqlConnection(GetConnectionString()))
            {
                con.Open();
                string sql = "SELECT * FROM articulo WHERE 1=1 ";

                if (!string.IsNullOrEmpty(buscar)) sql += "AND (codigo LIKE @buscar OR articulo LIKE @buscar OR descripcion LIKE @buscar) ";
                if (codDesde.HasValue) sql += "AND codigo >= @codDesde ";
                if (codHasta.HasValue) sql += "AND codigo <= @codHasta ";
                if (!string.IsNullOrEmpty(tipoDesde)) sql += "AND tipo >= @tipoDesde ";
                if (!string.IsNullOrEmpty(tipoHasta)) sql += "AND tipo <= @tipoHasta ";
                if (fechaAltaDesde.HasValue) sql += "AND fecha_alta >= @fechaAltaDesde ";
                if (fechaAltaHasta.HasValue) sql += "AND fecha_alta <= @fechaAltaHasta ";
                if (fechaBajaDesde.HasValue) sql += "AND fecha_baja >= @fechaBajaDesde ";
                if (fechaBajaHasta.HasValue) sql += "AND fecha_baja <= @fechaBajaHasta ";

                using (var cmd = new MySqlCommand(sql, con))
                {
                    if (!string.IsNullOrEmpty(buscar)) cmd.Parameters.AddWithValue("@buscar", "%" + buscar + "%");
                    if (codDesde.HasValue) cmd.Parameters.AddWithValue("@codDesde", codDesde.Value);
                    if (codHasta.HasValue) cmd.Parameters.AddWithValue("@codHasta", codHasta.Value);
                    if (!string.IsNullOrEmpty(tipoDesde)) cmd.Parameters.AddWithValue("@tipoDesde", tipoDesde);
                    if (!string.IsNullOrEmpty(tipoHasta)) cmd.Parameters.AddWithValue("@tipoHasta", tipoHasta);
                    if (fechaAltaDesde.HasValue) cmd.Parameters.AddWithValue("@fechaAltaDesde", fechaAltaDesde.Value);
                    if (fechaAltaHasta.HasValue) cmd.Parameters.AddWithValue("@fechaAltaHasta", fechaAltaHasta.Value);
                    if (fechaBajaDesde.HasValue) cmd.Parameters.AddWithValue("@fechaBajaDesde", fechaBajaDesde.Value);
                    if (fechaBajaHasta.HasValue) cmd.Parameters.AddWithValue("@fechaBajaHasta", fechaBajaHasta.Value);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ArticuloModel
                            {
                                codigo = Convert.ToInt32(reader["codigo"]),
                                articulo = reader["articulo"].ToString(),
                                descripcion = reader["descripcion"].ToString(),
                                longitud = Convert.ToDecimal(reader["longitud"]),
                                altura = Convert.ToDecimal(reader["altura"]),
                                anchura = Convert.ToDecimal(reader["anchura"]),
                                tipo = reader["tipo"].ToString(),
                                unidad_medida = reader["unidad_medida"].ToString(),
                                pais_origen = reader["pais_origen"].ToString(),
                                iva = Convert.ToDecimal(reader["iva"]),
                                es_reutilizable = Convert.ToBoolean(reader["es_reutilizable"]),
                                huella_carbono = Convert.ToDecimal(reader["huella_carbono"]),
                                dto_base = Convert.ToDecimal(reader["dto_base"]),
                                fecha_alta = reader["fecha_alta"] != DBNull.Value ? (DateTime?)reader["fecha_alta"] : null,
                                fecha_baja = reader["fecha_baja"] != DBNull.Value ? (DateTime?)reader["fecha_baja"] : null,
                                activo = Convert.ToBoolean(reader["activo"]),
                                observaciones = reader["observaciones"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.buscar = buscar;
            ViewBag.codDesde = codDesde;
            ViewBag.codHasta = codHasta;
            ViewBag.tipoDesde = tipoDesde;
            ViewBag.tipoHasta = tipoHasta;
            ViewBag.fechaAltaDesde = fechaAltaDesde?.ToString("yyyy-MM-dd");
            ViewBag.fechaAltaHasta = fechaAltaHasta?.ToString("yyyy-MM-dd");
            ViewBag.fechaBajaDesde = fechaBajaDesde?.ToString("yyyy-MM-dd");
            ViewBag.fechaBajaHasta = fechaBajaHasta?.ToString("yyyy-MM-dd");

            return View(lista);
        }

        [HttpGet]
        public JsonResult ObtenerArticuloPorCodigo(int codigo)
        {
            ArticuloModel articulo = null;
            using (var con = new MySqlConnection(GetConnectionString()))
            {
                con.Open();
                string sql = "SELECT * FROM articulo WHERE codigo = @codigo";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            articulo = new ArticuloModel
                            {
                                codigo = Convert.ToInt32(reader["codigo"]),
                                articulo = reader["articulo"].ToString(),
                                descripcion = reader["descripcion"].ToString(),
                                longitud = Convert.ToDecimal(reader["longitud"]),
                                altura = Convert.ToDecimal(reader["altura"]),
                                anchura = Convert.ToDecimal(reader["anchura"]),
                                tipo = reader["tipo"].ToString(),
                                unidad_medida = reader["unidad_medida"].ToString(),
                                pais_origen = reader["pais_origen"].ToString(),
                                iva = Convert.ToDecimal(reader["iva"]),
                                es_reutilizable = Convert.ToBoolean(reader["es_reutilizable"]),
                                huella_carbono = Convert.ToDecimal(reader["huella_carbono"]),
                                dto_base = Convert.ToDecimal(reader["dto_base"]),
                                fecha_alta = reader["fecha_alta"] != DBNull.Value ? (DateTime?)reader["fecha_alta"] : null,
                                fecha_baja = reader["fecha_baja"] != DBNull.Value ? (DateTime?)reader["fecha_baja"] : null,
                                activo = Convert.ToBoolean(reader["activo"]),
                                observaciones = reader["observaciones"].ToString()
                            };
                        }
                    }
                }
            }
            return Json(articulo, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Guardar(ArticuloModel a)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            if (!string.IsNullOrEmpty(Request.Form["longitud"])) { a.longitud = Convert.ToDecimal(Request.Form["longitud"].Replace(",", "."), culture); ModelState.Remove("longitud"); }
            if (!string.IsNullOrEmpty(Request.Form["altura"])) { a.altura = Convert.ToDecimal(Request.Form["altura"].Replace(",", "."), culture); ModelState.Remove("altura"); }
            if (!string.IsNullOrEmpty(Request.Form["anchura"])) { a.anchura = Convert.ToDecimal(Request.Form["anchura"].Replace(",", "."), culture); ModelState.Remove("anchura"); }
            if (!string.IsNullOrEmpty(Request.Form["iva"])) { a.iva = Convert.ToDecimal(Request.Form["iva"].Replace(",", "."), culture); ModelState.Remove("iva"); }
            if (!string.IsNullOrEmpty(Request.Form["dto_base"])) { a.dto_base = Convert.ToDecimal(Request.Form["dto_base"].Replace(",", "."), culture); ModelState.Remove("dto_base"); }
            if (!string.IsNullOrEmpty(Request.Form["huella_carbono"])) { a.huella_carbono = Convert.ToDecimal(Request.Form["huella_carbono"].Replace(",", "."), culture); ModelState.Remove("huella_carbono"); }

            if (!string.IsNullOrEmpty(Request.Form["fecha_alta"]))
            {
                if (DateTime.TryParseExact(Request.Form["fecha_alta"], "yyyy-MM-dd", culture, System.Globalization.DateTimeStyles.None, out DateTime fAlta))
                {
                    a.fecha_alta = fAlta; ModelState.Remove("fecha_alta");
                }
            }
            if (!string.IsNullOrEmpty(Request.Form["fecha_baja"]))
            {
                if (DateTime.TryParseExact(Request.Form["fecha_baja"], "yyyy-MM-dd", culture, System.Globalization.DateTimeStyles.None, out DateTime fBaja))
                {
                    a.fecha_baja = fBaja; ModelState.Remove("fecha_baja");
                }
            }

            // Los checkboxes desde formularios HTML envían "true" o "on".
            a.es_reutilizable = Request.Form["es_reutilizable"] == "true" || Request.Form["es_reutilizable"] == "on";
            a.activo = Request.Form["activo"] == "true" || Request.Form["activo"] == "on";
            ModelState.Remove("es_reutilizable");
            ModelState.Remove("activo");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Error de validación. Por favor, revise que los campos obligatorios estén rellenados.";
                TempData["TipoError"] = a.codigo == 0 ? "Crear" : "Editar";
                return RedirectToAction("Index");
            }

            using (var con = new MySqlConnection(GetConnectionString()))
            {
                try
                {
                    con.Open();
                    string sql;
                    bool existe = false;

                    using (var cmdCheck = new MySqlCommand("SELECT COUNT(1) FROM articulo WHERE codigo = @codigo", con))
                    {
                        cmdCheck.Parameters.AddWithValue("@codigo", a.codigo);
                        existe = Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0;
                    }

                    if (!existe)
                    {
                        if (a.codigo != 0)
                            sql = "INSERT INTO articulo (codigo, articulo, descripcion, longitud, altura, anchura, tipo, unidad_medida, pais_origen, iva, es_reutilizable, huella_carbono, dto_base, fecha_alta, fecha_baja, activo, observaciones) VALUES (@codigo, @articulo, @descripcion, @longitud, @altura, @anchura, @tipo, @unidad_medida, @pais_origen, @iva, @es_reutilizable, @huella_carbono, @dto_base, @fecha_alta, @fecha_baja, @activo, @observaciones)";
                        else
                            sql = "INSERT INTO articulo (articulo, descripcion, longitud, altura, anchura, tipo, unidad_medida, pais_origen, iva, es_reutilizable, huella_carbono, dto_base, fecha_alta, fecha_baja, activo, observaciones) VALUES (@articulo, @descripcion, @longitud, @altura, @anchura, @tipo, @unidad_medida, @pais_origen, @iva, @es_reutilizable, @huella_carbono, @dto_base, @fecha_alta, @fecha_baja, @activo, @observaciones)";
                    }
                    else
                    {
                        sql = "UPDATE articulo SET articulo=@articulo, descripcion=@descripcion, longitud=@longitud, altura=@altura, anchura=@anchura, tipo=@tipo, unidad_medida=@unidad_medida, pais_origen=@pais_origen, iva=@iva, es_reutilizable=@es_reutilizable, huella_carbono=@huella_carbono, dto_base=@dto_base, fecha_alta=@fecha_alta, fecha_baja=@fecha_baja, activo=@activo, observaciones=@observaciones WHERE codigo=@codigo";
                    }

                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        if (a.codigo != 0) cmd.Parameters.AddWithValue("@codigo", a.codigo);
                        cmd.Parameters.AddWithValue("@articulo", a.articulo);
                        cmd.Parameters.AddWithValue("@descripcion", a.descripcion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@longitud", a.longitud);
                        cmd.Parameters.AddWithValue("@altura", a.altura);
                        cmd.Parameters.AddWithValue("@anchura", a.anchura);
                        cmd.Parameters.AddWithValue("@tipo", a.tipo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@unidad_medida", a.unidad_medida ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@pais_origen", a.pais_origen ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@iva", a.iva);
                        cmd.Parameters.AddWithValue("@es_reutilizable", a.es_reutilizable);
                        cmd.Parameters.AddWithValue("@huella_carbono", a.huella_carbono);
                        cmd.Parameters.AddWithValue("@dto_base", a.dto_base);
                        cmd.Parameters.AddWithValue("@fecha_alta", a.fecha_alta ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@fecha_baja", a.fecha_baja ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@activo", a.activo);
                        cmd.Parameters.AddWithValue("@observaciones", a.observaciones ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    TempData["MensajeExito"] = existe ? "Artículo modificado correctamente." : "Artículo creado correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar: " + ex.Message;
                    TempData["TipoError"] = a.codigo == 0 ? "Crear" : "Editar";
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Borrar(int id)
        {
            using (var con = new MySqlConnection(GetConnectionString()))
            {
                try
                {
                    con.Open();
                    string sql = "DELETE FROM articulo WHERE codigo = @codigo";
                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@codigo", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Artículo eliminado correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al eliminar: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
    }
}