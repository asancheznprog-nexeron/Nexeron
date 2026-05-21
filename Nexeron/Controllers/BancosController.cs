using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models; 

namespace Nexeron.Controllers
{
    public class BancosController : Controller
    {

        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<bancos> listaBancos = new List<bancos>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT CUENTA, NOMBRE_CUENTA, NOMBRE_FISCAL, CIF, 
                                                   DIRECCION, CP, POBLACION, TELEFONO, IBAN, BIC, EMAIL 
                                            FROM bancos 
                                            ORDER BY CUENTA ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaBancos.Add(new bancos
                                {
                                    CUENTA = reader["CUENTA"].ToString(),
                                    NOMBRE_CUENTA = reader["NOMBRE_CUENTA"].ToString(),
                                    NOMBRE_FISCAL = reader["NOMBRE_FISCAL"].ToString(),
                                    CIF = reader["CIF"].ToString(),
                                    DIRECCION = reader["DIRECCION"].ToString(),
                                    CP = reader["CP"].ToString(),
                                    POBLACION = reader["POBLACION"].ToString(),
                                    TELEFONO = reader["TELEFONO"].ToString(),
                                    IBAN = reader["IBAN"].ToString(),
                                    BIC = reader["BIC"].ToString(),
                                    EMAIL = reader["EMAIL"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar bancos: " + ex.Message;
                }
            }

            return View(listaBancos);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(bancos nuevo)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string connStr = Session["cadenaConexion"].ToString();
            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        using (var cmdCheck = conexion.CreateCommand())
                        {
                            cmdCheck.Transaction = transaccion;
                            cmdCheck.CommandText = "SELECT COUNT(*) FROM bancos WHERE CUENTA = @cuenta OR CIF = @cif";
                            cmdCheck.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                            cmdCheck.Parameters.AddWithValue("@cif", nuevo.CIF);

                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                            {
                                TempData["Error"] = "La cuenta contable o el CIF ya existen en el fichero de bancos.";
                                TempData["TipoError"] = "Crear";
                                return RedirectToAction("Index");
                            }
                        }

                        bool existeCuenta = false;
                        using (var cmdCheckCta = conexion.CreateCommand())
                        {
                            cmdCheckCta.Transaction = transaccion;
                            cmdCheckCta.CommandText = "SELECT COUNT(*) FROM cuentas WHERE CUENTA = @cuenta";
                            cmdCheckCta.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                            if (Convert.ToInt32(cmdCheckCta.ExecuteScalar()) > 0)
                                existeCuenta = true;
                        }

                        if (!existeCuenta)
                        {
                            using (var cmdInsertCta = conexion.CreateCommand())
                            {
                                cmdInsertCta.Transaction = transaccion;
                                cmdInsertCta.CommandText = "INSERT INTO cuentas (CUENTA, NOMBRE_CUENTA) VALUES (@cuenta, @nombre)";
                                cmdInsertCta.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                                cmdInsertCta.Parameters.AddWithValue("@nombre", nuevo.NOMBRE_CUENTA);
                                cmdInsertCta.ExecuteNonQuery();
                            }
                        }

                        using (var cmdInsertBan = conexion.CreateCommand())
                        {
                            cmdInsertBan.Transaction = transaccion;
                            cmdInsertBan.CommandText = @"INSERT INTO bancos (CUENTA, NOMBRE_CUENTA, NOMBRE_FISCAL, DIRECCION, CP, POBLACION, CIF, TELEFONO, EMAIL, IBAN, BIC) 
                                                VALUES (@cuenta, @nombreCuenta, @nombreFiscal, @direccion, @cp, @poblacion, @cif, @telefono, @email, @iban, @bic)";

                            cmdInsertBan.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                            cmdInsertBan.Parameters.AddWithValue("@nombreCuenta", nuevo.NOMBRE_CUENTA);
                            cmdInsertBan.Parameters.AddWithValue("@nombreFiscal", nuevo.NOMBRE_FISCAL);
                            cmdInsertBan.Parameters.AddWithValue("@direccion", nuevo.DIRECCION ?? "");
                            cmdInsertBan.Parameters.AddWithValue("@cp", nuevo.CP ?? "");
                            cmdInsertBan.Parameters.AddWithValue("@poblacion", nuevo.POBLACION ?? "");
                            cmdInsertBan.Parameters.AddWithValue("@cif", nuevo.CIF);
                            cmdInsertBan.Parameters.AddWithValue("@telefono", nuevo.TELEFONO ?? "");
                            cmdInsertBan.Parameters.AddWithValue("@email", nuevo.EMAIL ?? "");
                            cmdInsertBan.Parameters.AddWithValue("@iban", nuevo.IBAN ?? "");
                            cmdInsertBan.Parameters.AddWithValue("@bic", nuevo.BIC ?? "");

                            cmdInsertBan.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Entidad bancaria dada de alta correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al guardar la entidad: " + ex.Message;
                        TempData["TipoError"] = "Crear";
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(bancos modificado)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

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
                            cmdCta.CommandText = "UPDATE cuentas SET NOMBRE_CUENTA = @nombre WHERE CUENTA = @cuenta";
                            cmdCta.Parameters.AddWithValue("@nombre", modificado.NOMBRE_CUENTA);
                            cmdCta.Parameters.AddWithValue("@cuenta", modificado.CUENTA);
                            cmdCta.ExecuteNonQuery();
                        }

                        using (var cmdBan = conexion.CreateCommand())
                        {
                            cmdBan.Transaction = transaccion;
                            cmdBan.CommandText = @"UPDATE bancos SET 
                                                NOMBRE_CUENTA = @nombreCuenta, NOMBRE_FISCAL = @nombreFiscal, DIRECCION = @direccion, 
                                                CP = @cp, POBLACION = @poblacion, CIF = @cif, TELEFONO = @telefono, EMAIL = @email, 
                                                IBAN = @iban, BIC = @bic 
                                                WHERE CUENTA = @cuenta";

                            cmdBan.Parameters.AddWithValue("@nombreCuenta", modificado.NOMBRE_CUENTA);
                            cmdBan.Parameters.AddWithValue("@nombreFiscal", modificado.NOMBRE_FISCAL);
                            cmdBan.Parameters.AddWithValue("@direccion", modificado.DIRECCION ?? "");
                            cmdBan.Parameters.AddWithValue("@cp", modificado.CP ?? "");
                            cmdBan.Parameters.AddWithValue("@poblacion", modificado.POBLACION ?? "");
                            cmdBan.Parameters.AddWithValue("@cif", modificado.CIF);
                            cmdBan.Parameters.AddWithValue("@telefono", modificado.TELEFONO ?? "");
                            cmdBan.Parameters.AddWithValue("@email", modificado.EMAIL ?? "");
                            cmdBan.Parameters.AddWithValue("@iban", modificado.IBAN ?? "");
                            cmdBan.Parameters.AddWithValue("@bic", modificado.BIC ?? "");
                            cmdBan.Parameters.AddWithValue("@cuenta", modificado.CUENTA);

                            cmdBan.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Entidad bancaria y cuenta contable actualizadas.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al modificar la entidad: " + ex.Message;
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
                conexion.Open();
                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        using (var cmdBan = conexion.CreateCommand())
                        {
                            cmdBan.Transaction = transaccion;
                            cmdBan.CommandText = "DELETE FROM bancos WHERE CUENTA = @cuenta";
                            cmdBan.Parameters.AddWithValue("@cuenta", id);
                            cmdBan.ExecuteNonQuery();
                        }

                        using (var cmdCta = conexion.CreateCommand())
                        {
                            cmdCta.Transaction = transaccion;
                            cmdCta.CommandText = "DELETE FROM cuentas WHERE CUENTA = @cuenta";
                            cmdCta.Parameters.AddWithValue("@cuenta", id);
                            cmdCta.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Banco y cuenta contable eliminados del sistema.";
                    }
                    catch (MySqlException ex)
                    {
                        transaccion.Rollback();
                        if (ex.Number == 1451)
                            TempData["Error"] = "No se puede eliminar. Esta cuenta bancaria tiene extractos, cobros o pagos asentados.";
                        else
                            TempData["Error"] = "Error de base de datos al eliminar: " + ex.Message;
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al eliminar: " + ex.Message;
                    }
                }
            }
            return RedirectToAction("Index");
        }
    }
}