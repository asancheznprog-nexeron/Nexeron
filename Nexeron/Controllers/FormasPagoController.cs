using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class FormasPagoController : Controller
    {

        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<fpagcob> lista = new List<fpagcob>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "SELECT CODIGO, FORMACOBPAG, ACEPTO, REMESACOBRO, REMESAPAGO, NUMCOBROS, PRIMVENCI, DIASVENCI FROM fpagcob ORDER BY CODIGO ASC";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new fpagcob
                                {
                                    CODIGO = reader["CODIGO"].ToString(),
                                    FORMACOBPAG = reader["FORMACOBPAG"].ToString(),
                                    ACEPTO = reader["ACEPTO"].ToString(),
                                    REMESACOBRO = reader["REMESACOBRO"].ToString(),
                                    REMESAPAGO = reader["REMESAPAGO"].ToString(),
                                    NUMCOBROS = Convert.ToInt32(reader["NUMCOBROS"]),
                                    PRIMVENCI = Convert.ToInt32(reader["PRIMVENCI"]),
                                    DIASVENCI = Convert.ToInt32(reader["DIASVENCI"])
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar formas de pago: " + ex.Message;
                }
            }
            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(fpagcob nuevo)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmdCheck = conexion.CreateCommand())
                    {
                        cmdCheck.CommandText = "SELECT COUNT(*) FROM fpagcob WHERE CODIGO = @codigo";
                        cmdCheck.Parameters.AddWithValue("@codigo", nuevo.CODIGO);
                        if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                        {
                            TempData["Error"] = "El código de forma de pago ya se encuentra registrado.";
                            TempData["TipoError"] = "Crear";
                            return RedirectToAction("Index");
                        }
                    }

                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO fpagcob (CODIGO, FORMACOBPAG, ACEPTO, REMESACOBRO, REMESAPAGO, NUMCOBROS, PRIMVENCI, DIASVENCI) 
                                            VALUES (@codigo, @formaCobPag, @acepto, @remesaCobro, @remesaPago, @numCobros, @primVenci, @diasVenci)";
                        cmd.Parameters.AddWithValue("@codigo", nuevo.CODIGO);
                        cmd.Parameters.AddWithValue("@formaCobPag", nuevo.FORMACOBPAG);
                        cmd.Parameters.AddWithValue("@acepto", nuevo.ACEPTO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaCobro", nuevo.REMESACOBRO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaPago", nuevo.REMESAPAGO ?? "N");
                        cmd.Parameters.AddWithValue("@numCobros", nuevo.NUMCOBROS);
                        cmd.Parameters.AddWithValue("@primVenci", nuevo.PRIMVENCI);
                        cmd.Parameters.AddWithValue("@diasVenci", nuevo.DIASVENCI);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Forma de pago registrada correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar el registro: " + ex.Message;
                    TempData["TipoError"] = "Crear";
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(fpagcob modificado)
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
                        cmd.CommandText = @"UPDATE fpagcob SET 
                                            FORMACOBPAG = @formaCobPag, ACEPTO = @acepto, REMESACOBRO = @remesaCobro, 
                                            REMESAPAGO = @remesaPago, NUMCOBROS = @numCobros, PRIMVENCI = @primVenci, DIASVENCI = @diasVenci 
                                            WHERE CODIGO = @codigo";
                        cmd.Parameters.AddWithValue("@formaCobPag", modificado.FORMACOBPAG);
                        cmd.Parameters.AddWithValue("@acepto", modificado.ACEPTO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaCobro", modificado.REMESACOBRO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaPago", modificado.REMESAPAGO ?? "N");
                        cmd.Parameters.AddWithValue("@numCobros", modificado.NUMCOBROS);
                        cmd.Parameters.AddWithValue("@primVenci", modificado.PRIMVENCI);
                        cmd.Parameters.AddWithValue("@diasVenci", modificado.DIASVENCI);
                        cmd.Parameters.AddWithValue("@codigo", modificado.CODIGO);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Forma de pago actualizada correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al modificar el registro: " + ex.Message;
                    TempData["TipoError"] = "Editar";
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
                        cmd.CommandText = "DELETE FROM fpagcob WHERE CODIGO = @codigo";
                        cmd.Parameters.AddWithValue("@codigo", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Forma de pago eliminada correctamente.";
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451)
                        TempData["Error"] = "No se puede eliminar. Esta forma de pago ya está asignada a clientes, proveedores o facturas activas.";
                    else
                        TempData["Error"] = "Error de base de datos: " + ex.Message;
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