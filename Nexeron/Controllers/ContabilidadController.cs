using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class ContabilidadController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");
            return View();
        }
        public ActionResult Cuentas()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string connStr = Session["cadenaConexion"].ToString();
            List<CuentaModel> listaCuentas = new List<CuentaModel>();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "SELECT CUENTA, NOMBRE_CUENTA FROM cuentas ORDER BY CUENTA ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaCuentas.Add(new CuentaModel
                                {
                                    Codigo = reader["CUENTA"].ToString(),
                                    Nombre = reader["NOMBRE_CUENTA"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar las cuentas: " + ex.Message;
                }
            }

            return View(listaCuentas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearCuenta(CuentaModel nuevaCuenta)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                string connStr = Session["cadenaConexion"].ToString();

                using (MySqlConnection conexion = new MySqlConnection(connStr))
                {
                    try
                    {
                        conexion.Open();

                        using (var cmdCheck = conexion.CreateCommand())
                        {
                            
                            cmdCheck.CommandText = "SELECT COUNT(*) FROM cuentas WHERE CUENTA = @codigo";
                            cmdCheck.Parameters.AddWithValue("@codigo", nuevaCuenta.Codigo);

                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                            {
                                TempData["Error"] = "El código contable ya está asignado a otra cuenta.";
                                return RedirectToAction("Cuentas");
                            }
                        }

                        using (var cmd = conexion.CreateCommand())
                        {
                            
                            cmd.CommandText = "INSERT INTO cuentas (CUENTA, NOMBRE_CUENTA) VALUES (@codigo, @nombre)";
                            cmd.Parameters.AddWithValue("@codigo", nuevaCuenta.Codigo);
                            cmd.Parameters.AddWithValue("@nombre", nuevaCuenta.Nombre);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error de BD: " + ex.Message;
                    }
                }
            }

            
            return RedirectToAction("Cuentas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarCuenta(CuentaModel cuentaModificada)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                string connStr = Session["cadenaConexion"].ToString();

                using (MySqlConnection conexion = new MySqlConnection(connStr))
                {
                    try
                    {
                        conexion.Open();
                        using (var cmd = conexion.CreateCommand())
                        {
                            
                            cmd.CommandText = "UPDATE cuentas SET NOMBRE_CUENTA = @nombre WHERE CUENTA = @codigo";
                            cmd.Parameters.AddWithValue("@nombre", cuentaModificada.Nombre);
                            cmd.Parameters.AddWithValue("@codigo", cuentaModificada.Codigo);

                            cmd.ExecuteNonQuery();
                            TempData["MensajeExito"] = "Cuenta modificada correctamente.";
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error al modificar la cuenta: " + ex.Message;
                        TempData["TipoError"] = "Editar";
                    }
                }
            }

            return RedirectToAction("Cuentas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarCuenta(string id)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Cuentas");

            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM cuentas WHERE CUENTA = @codigo";
                        cmd.Parameters.AddWithValue("@codigo", id);

                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                            TempData["MensajeExito"] = "Cuenta eliminada correctamente.";
                        else
                            TempData["Error"] = "La cuenta no existe o ya ha sido eliminada.";
                    }
                }
                catch (MySqlException ex)
                {
                    
                    if (ex.Number == 1451)
                        TempData["Error"] = "No se puede eliminar. Esta cuenta ya está asociada a facturas, asientos o movimientos contables.";
                    else
                        TempData["Error"] = "Error de base de datos: " + ex.Message;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al eliminar: " + ex.Message;
                }
            }

            return RedirectToAction("Cuentas");
        }
    }

}