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

        public ActionResult Index()
        {
            var lista = new List<ArticuloModel>();
            using (var con = new MySqlConnection(GetConnectionString()))
            {
                con.Open();
                string sql = "SELECT * FROM articulo";
                using (var cmd = new MySqlCommand(sql, con))
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
            return View(lista);
        }

        public ActionResult AddOrEdit(int id = 0)
        {
            if (id == 0) return View(new ArticuloModel());

            var articulo = new ArticuloModel();
            using (var con = new MySqlConnection(GetConnectionString()))
            {
                con.Open();
                string sql = "SELECT * FROM articulo WHERE codigo = @codigo";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@codigo", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            articulo.codigo = Convert.ToInt32(reader["codigo"]);
                            articulo.articulo = reader["articulo"].ToString();
                            articulo.descripcion = reader["descripcion"].ToString();
                            articulo.longitud = Convert.ToDecimal(reader["longitud"]);
                            articulo.altura = Convert.ToDecimal(reader["altura"]);
                            articulo.anchura = Convert.ToDecimal(reader["anchura"]);
                            articulo.tipo = reader["tipo"].ToString();
                            articulo.unidad_medida = reader["unidad_medida"].ToString();
                            articulo.pais_origen = reader["pais_origen"].ToString();
                            articulo.iva = Convert.ToDecimal(reader["iva"]);
                            articulo.es_reutilizable = Convert.ToBoolean(reader["es_reutilizable"]);
                            articulo.huella_carbono = Convert.ToDecimal(reader["huella_carbono"]);
                            articulo.dto_base = Convert.ToDecimal(reader["dto_base"]);
                            articulo.fecha_alta = reader["fecha_alta"] != DBNull.Value ? (DateTime?)reader["fecha_alta"] : null;
                            articulo.fecha_baja = reader["fecha_baja"] != DBNull.Value ? (DateTime?)reader["fecha_baja"] : null;
                            articulo.activo = Convert.ToBoolean(reader["activo"]);
                            articulo.observaciones = reader["observaciones"].ToString();
                        }
                    }
                }
            }
            return View(articulo);
        }

        [HttpPost]
        public ActionResult Guardar(ArticuloModel a)
        {
            if (!ModelState.IsValid) return View("AddOrEdit", a);

            using (var con = new MySqlConnection(GetConnectionString()))
            {
                con.Open();
                string sql;
                bool esNuevo = a.codigo == 0;

                if (esNuevo)
                {
                    sql = "INSERT INTO articulo (articulo, descripcion, longitud, altura, anchura, tipo, unidad_medida, pais_origen, iva, es_reutilizable, huella_carbono, dto_base, fecha_alta, fecha_baja, activo, observaciones) " +
                          "VALUES (@articulo, @descripcion, @longitud, @altura, @anchura, @tipo, @unidad_medida, @pais_origen, @iva, @es_reutilizable, @huella_carbono, @dto_base, @fecha_alta, @fecha_baja, @activo, @observaciones)";
                }
                else
                {
                    sql = "UPDATE articulo SET articulo=@articulo, descripcion=@descripcion, longitud=@longitud, altura=@altura, anchura=@anchura, " +
                          "tipo=@tipo, unidad_medida=@unidad_medida, pais_origen=@pais_origen, iva=@iva, es_reutilizable=@es_reutilizable, " +
                          "huella_carbono=@huella_carbono, dto_base=@dto_base, fecha_alta=@fecha_alta, fecha_baja=@fecha_baja, activo=@activo, observaciones=@observaciones WHERE codigo=@codigo";
                }

                using (var cmd = new MySqlCommand(sql, con))
                {
                    if (!esNuevo) cmd.Parameters.AddWithValue("@codigo", a.codigo);
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
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Borrar(int id)
        {
            using (var con = new MySqlConnection(GetConnectionString()))
            {
                con.Open();
                string sql = "DELETE FROM articulo WHERE codigo = @codigo";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@codigo", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
    }
}