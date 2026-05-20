using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class CuentasController : Controller
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
                    conexion.Open();
                    using (MySqlTransaction transaccion = conexion.BeginTransaction())
                    {
                        try
                        {
                            
                            using (var cmdCta = conexion.CreateCommand())
                            {
                                cmdCta.Transaction = transaccion;
                                cmdCta.CommandText = "UPDATE cuentas SET NOMBRE_CUENTA = @nombre WHERE CUENTA = @codigo";
                                cmdCta.Parameters.AddWithValue("@nombre", cuentaModificada.Nombre);
                                cmdCta.Parameters.AddWithValue("@codigo", cuentaModificada.Codigo);
                                cmdCta.ExecuteNonQuery();
                            }

                            switch (cuentaModificada.Codigo)
                            {
                                case string c when c.StartsWith("43"): // Clientes
                                    using (var cmdCli = conexion.CreateCommand())
                                    {
                                        cmdCli.Transaction = transaccion;
                                        cmdCli.CommandText = "UPDATE clientes SET NOMBRE_CUENTA = @nombre WHERE CUENTA = @codigo";
                                        cmdCli.Parameters.AddWithValue("@nombre", cuentaModificada.Nombre);
                                        cmdCli.Parameters.AddWithValue("@codigo", cuentaModificada.Codigo);
                                        cmdCli.ExecuteNonQuery();
                                    }
                                    break;

                                case string c when c.StartsWith("40") || c.StartsWith("41"): // Proveedores / Acreedores
                                    using (var cmdProv = conexion.CreateCommand())
                                    {
                                        cmdProv.Transaction = transaccion;
                                        cmdProv.CommandText = "UPDATE proveedores SET NOMBRE_CUENTA = @nombre WHERE CUENTA = @codigo";
                                        cmdProv.Parameters.AddWithValue("@nombre", cuentaModificada.Nombre);
                                        cmdProv.Parameters.AddWithValue("@codigo", cuentaModificada.Codigo);
                                        cmdProv.ExecuteNonQuery();
                                    }
                                    break;

                                case string c when c.StartsWith("5"): // Bancos
                                    using (var cmdBan = conexion.CreateCommand())
                                    {
                                        cmdBan.Transaction = transaccion;
                                        cmdBan.CommandText = "UPDATE bancos SET NOMBRE_CUENTA = @nombre WHERE CUENTA = @codigo";
                                        cmdBan.Parameters.AddWithValue("@nombre", cuentaModificada.Nombre);
                                        cmdBan.Parameters.AddWithValue("@codigo", cuentaModificada.Codigo);
                                        cmdBan.ExecuteNonQuery();
                                    }
                                    break;
                            }

                            transaccion.Commit();
                            TempData["MensajeExito"] = "Cuenta modificada y sincronizada correctamente.";
                        }
                        catch (Exception ex)
                        {
                            transaccion.Rollback();
                            TempData["Error"] = "Error al modificar la cuenta: " + ex.Message;
                            TempData["TipoError"] = "Editar";
                        }
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
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        switch (id)
                        {
                            case string c when c.StartsWith("43"): // Clientes
                                using (var cmdCli = conexion.CreateCommand())
                                {
                                    cmdCli.Transaction = transaccion;
                                    cmdCli.CommandText = "DELETE FROM clientes WHERE CUENTA = @codigo";
                                    cmdCli.Parameters.AddWithValue("@codigo", id);
                                    cmdCli.ExecuteNonQuery();
                                }
                                break;

                            case string c when c.StartsWith("40") || c.StartsWith("41"): // Proveedores / Acreedores
                                using (var cmdProv = conexion.CreateCommand())
                                {
                                    cmdProv.Transaction = transaccion;
                                    cmdProv.CommandText = "DELETE FROM proveedores WHERE CUENTA = @codigo";
                                    cmdProv.Parameters.AddWithValue("@codigo", id);
                                    cmdProv.ExecuteNonQuery();
                                }
                                break;

                            case string c when c.StartsWith("5"): // Bancos
                                using (var cmdBan = conexion.CreateCommand())
                                {
                                    cmdBan.Transaction = transaccion;
                                    cmdBan.CommandText = "DELETE FROM bancos WHERE CUENTA = @codigo";
                                    cmdBan.Parameters.AddWithValue("@codigo", id);
                                    cmdBan.ExecuteNonQuery();
                                }
                                break;
                        }

                        using (var cmdCta = conexion.CreateCommand())
                        {
                            cmdCta.Transaction = transaccion;
                            cmdCta.CommandText = "DELETE FROM cuentas WHERE CUENTA = @codigo";
                            cmdCta.Parameters.AddWithValue("@codigo", id);

                            int filasAfectadas = cmdCta.ExecuteNonQuery();

                            if (filasAfectadas > 0)
                                TempData["MensajeExito"] = "Cuenta y registros asociados eliminados correctamente.";
                            else
                                TempData["Error"] = "La cuenta no existe o ya ha sido eliminada.";
                        }

                        transaccion.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        transaccion.Rollback();
                        if (ex.Number == 1451)
                            TempData["Error"] = "No se puede eliminar. Esta cuenta ya está asociada a facturas o movimientos.";
                        else
                            TempData["Error"] = "Error de base de datos: " + ex.Message;
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al intentar eliminar la cuenta: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Cuentas");
        }
    }

}