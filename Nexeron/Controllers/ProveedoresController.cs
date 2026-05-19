using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class ProveedoresController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<proveedores> listaProveedores = new List<proveedores>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT CUENTA, NOMBRE_CUENTA, NOMBRE_FISCAL, CIF, 
                                                   DIRECCION, CP, POBLACION, TELEFONO,
                                                   NUMERO, PROVINCIA, PAIS, FECHA_ALTA, FECHA_BAJA, IBAN, EMAIL 
                                            FROM proveedores 
                                            ORDER BY CUENTA ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaProveedores.Add(new proveedores
                                {
                                    CUENTA = reader["CUENTA"].ToString(),
                                    NOMBRE_CUENTA = reader["NOMBRE_CUENTA"].ToString(),
                                    NOMBRE_FISCAL = reader["NOMBRE_FISCAL"].ToString(),
                                    CIF = reader["CIF"].ToString(),
                                    DIRECCION = reader["DIRECCION"].ToString(),
                                    CP = reader["CP"].ToString(),
                                    POBLACION = reader["POBLACION"].ToString(),
                                    TELEFONO = reader["TELEFONO"].ToString(),
                                    NUMERO = reader["NUMERO"].ToString(),
                                    PROVINCIA = reader["PROVINCIA"].ToString(),
                                    PAIS = reader["PAIS"].ToString(),
                                    FECHA_ALTA = reader["FECHA_ALTA"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_ALTA"]) : DateTime.Now,
                                    FECHA_BAJA = reader["FECHA_BAJA"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_BAJA"]) : (DateTime?)null,
                                    IBAN = reader["IBAN"].ToString(),
                                    EMAIL = reader["EMAIL"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar proveedores: " + ex.Message;
                }
            }

            return View(listaProveedores);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(proveedores nuevo)
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
                            cmdCheck.CommandText = "SELECT COUNT(*) FROM proveedores WHERE CUENTA = @cuenta OR CIF = @cif";
                            cmdCheck.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                            cmdCheck.Parameters.AddWithValue("@cif", nuevo.CIF);

                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                            {
                                TempData["Error"] = "La cuenta contable o el CIF ya existen en el fichero de proveedores.";
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

                        using (var cmdInsertProv = conexion.CreateCommand())
                        {
                            cmdInsertProv.Transaction = transaccion;
                            cmdInsertProv.CommandText = @"INSERT INTO proveedores (CUENTA, NOMBRE_CUENTA, NOMBRE_FISCAL, DIRECCION, POBLACION, NUMERO, CP, PROVINCIA, PAIS, FECHA_ALTA, FECHA_BAJA, IBAN, TELEFONO, EMAIL, CIF) 
                                                VALUES (@cuenta, @nombreCuenta, @nombreFiscal, @direccion, @poblacion, @numero, @cp, @provincia, @pais, @fechaAlta, @fechaBaja, @iban, @telefono, @email, @cif)";

                            cmdInsertProv.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                            cmdInsertProv.Parameters.AddWithValue("@nombreCuenta", nuevo.NOMBRE_CUENTA);
                            cmdInsertProv.Parameters.AddWithValue("@nombreFiscal", nuevo.NOMBRE_FISCAL);
                            cmdInsertProv.Parameters.AddWithValue("@direccion", nuevo.DIRECCION ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@poblacion", nuevo.POBLACION ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@numero", nuevo.NUMERO ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@cp", nuevo.CP ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@provincia", nuevo.PROVINCIA ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@pais", nuevo.PAIS ?? "España");
                            cmdInsertProv.Parameters.AddWithValue("@fechaAlta", nuevo.FECHA_ALTA);
                            cmdInsertProv.Parameters.AddWithValue("@fechaBaja", (object)nuevo.FECHA_BAJA ?? DBNull.Value);
                            cmdInsertProv.Parameters.AddWithValue("@iban", nuevo.IBAN ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@telefono", nuevo.TELEFONO ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@email", nuevo.EMAIL ?? "");
                            cmdInsertProv.Parameters.AddWithValue("@cif", nuevo.CIF);

                            cmdInsertProv.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Proveedor dado de alta correctamente en el sistema.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al guardar el proveedor: " + ex.Message;
                        TempData["TipoError"] = "Crear";
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(proveedores modificado)
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

                        using (var cmdProv = conexion.CreateCommand())
                        {
                            cmdProv.Transaction = transaccion;
                            cmdProv.CommandText = @"UPDATE proveedores SET 
                                                NOMBRE_CUENTA = @nombreCuenta, NOMBRE_FISCAL = @nombreFiscal, DIRECCION = @direccion, 
                                                POBLACION = @poblacion, NUMERO = @numero, CP = @cp, PROVINCIA = @provincia, PAIS = @pais, 
                                                FECHA_BAJA = @fechaBaja, IBAN = @iban, TELEFONO = @telefono, EMAIL = @email, CIF = @cif 
                                                WHERE CUENTA = @cuenta";

                            cmdProv.Parameters.AddWithValue("@nombreCuenta", modificado.NOMBRE_CUENTA);
                            cmdProv.Parameters.AddWithValue("@nombreFiscal", modificado.NOMBRE_FISCAL);
                            cmdProv.Parameters.AddWithValue("@direccion", modificado.DIRECCION ?? "");
                            cmdProv.Parameters.AddWithValue("@poblacion", modificado.POBLACION ?? "");
                            cmdProv.Parameters.AddWithValue("@numero", modificado.NUMERO ?? "");
                            cmdProv.Parameters.AddWithValue("@cp", modificado.CP ?? "");
                            cmdProv.Parameters.AddWithValue("@provincia", modificado.PROVINCIA ?? "");
                            cmdProv.Parameters.AddWithValue("@pais", modificado.PAIS ?? "");
                            cmdProv.Parameters.AddWithValue("@fechaBaja", (object)modificado.FECHA_BAJA ?? DBNull.Value);
                            cmdProv.Parameters.AddWithValue("@iban", modificado.IBAN ?? "");
                            cmdProv.Parameters.AddWithValue("@telefono", modificado.TELEFONO ?? "");
                            cmdProv.Parameters.AddWithValue("@email", modificado.EMAIL ?? "");
                            cmdProv.Parameters.AddWithValue("@cif", modificado.CIF);
                            cmdProv.Parameters.AddWithValue("@cuenta", modificado.CUENTA);

                            cmdProv.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Proveedor y cuenta contable actualizados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al modificar el proveedor: " + ex.Message;
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
                        using (var cmdProv = conexion.CreateCommand())
                        {
                            cmdProv.Transaction = transaccion;
                            cmdProv.CommandText = "DELETE FROM proveedores WHERE CUENTA = @cuenta";
                            cmdProv.Parameters.AddWithValue("@cuenta", id);
                            cmdProv.ExecuteNonQuery();
                        }

                        using (var cmdCta = conexion.CreateCommand())
                        {
                            cmdCta.Transaction = transaccion;
                            cmdCta.CommandText = "DELETE FROM cuentas WHERE CUENTA = @cuenta";
                            cmdCta.Parameters.AddWithValue("@cuenta", id);
                            cmdCta.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Proveedor y cuenta contable eliminados del sistema.";
                    }
                    catch (MySqlException ex)
                    {
                        transaccion.Rollback();
                        if (ex.Number == 1451)
                            TempData["Error"] = "No se puede eliminar. Este proveedor ya posee histórico, movimientos contables o facturas asociadas.";
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