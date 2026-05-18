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
                        // Usamos los nombres reales de tu base de datos
                        cmd.CommandText = "SELECT id, CUENTA, NOMBRE_CUENTA FROM cuentas ORDER BY CUENTA ASC";

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
                            // Validar que no exista (usando la columna CUENTA)
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
                            // Insertamos usando los nombres correctos de tus columnas
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

            // Al terminar, volvemos a la pantalla de cuentas
            return RedirectToAction("Cuentas");
        }
    }
}