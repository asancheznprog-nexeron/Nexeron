using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class ClientesController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<clientes> listaClientes = new List<clientes>();
            List<SelectListItem> listaPaises = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione --" } };

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
                                            FROM clientes 
                                            ORDER BY NOMBRE_CUENTA ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaClientes.Add(new clientes
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

                    using (var cmdPaises = conexion.CreateCommand())
                    {
                        cmdPaises.CommandText = "SELECT codigo, descripcion FROM paises ORDER BY descripcion ASC";
                        using (var reader = cmdPaises.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaPaises.Add(new SelectListItem
                                {
                                    Value = reader["codigo"].ToString(),
                                    Text = reader["descripcion"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar clientes: " + ex.Message;
                }
            }

            ViewBag.PaisesList = listaPaises;
            return View(listaClientes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(clientes nuevo)
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
                            cmdCheck.CommandText = "SELECT COUNT(*) FROM clientes WHERE CUENTA = @cuenta OR CIF = @cif";
                            cmdCheck.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                            cmdCheck.Parameters.AddWithValue("@cif", nuevo.CIF);

                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                            {
                                TempData["Error"] = "La cuenta contable o el CIF ya existen en el fichero de clientes.";
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
                            {
                                existeCuenta = true;
                            }
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

                        using (var cmdInsertCli = conexion.CreateCommand())
                        {
                            cmdInsertCli.Transaction = transaccion;
                            cmdInsertCli.CommandText = @"INSERT INTO clientes (CUENTA, NOMBRE_CUENTA, NOMBRE_FISCAL, DIRECCION, POBLACION, NUMERO, CP, PROVINCIA, PAIS, FECHA_ALTA, FECHA_BAJA, IBAN, TELEFONO, EMAIL, CIF) 
                                        VALUES (@cuenta, @nombreCuenta, @nombreFiscal, @direccion, @poblacion, @numero, @cp, @provincia, @pais, @fechaAlta, @fechaBaja, @iban, @telefono, @email, @cif)";

                            cmdInsertCli.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                            cmdInsertCli.Parameters.AddWithValue("@nombreCuenta", nuevo.NOMBRE_CUENTA);
                            cmdInsertCli.Parameters.AddWithValue("@nombreFiscal", nuevo.NOMBRE_FISCAL);
                            cmdInsertCli.Parameters.AddWithValue("@direccion", nuevo.DIRECCION ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@poblacion", nuevo.POBLACION ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@numero", nuevo.NUMERO ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@cp", nuevo.CP ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@provincia", nuevo.PROVINCIA ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@pais", nuevo.PAIS ?? "ES");
                            cmdInsertCli.Parameters.AddWithValue("@fechaAlta", nuevo.FECHA_ALTA);
                            cmdInsertCli.Parameters.AddWithValue("@fechaBaja", (object)nuevo.FECHA_BAJA ?? DBNull.Value);
                            cmdInsertCli.Parameters.AddWithValue("@iban", nuevo.IBAN ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@telefono", nuevo.TELEFONO ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@email", nuevo.EMAIL ?? "");
                            cmdInsertCli.Parameters.AddWithValue("@cif", nuevo.CIF);

                            cmdInsertCli.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Cliente dado de alta correctamente en el sistema.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al guardar el cliente: " + ex.Message;
                        TempData["TipoError"] = "Crear";
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(clientes modificado)
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

                        using (var cmdCli = conexion.CreateCommand())
                        {
                            cmdCli.Transaction = transaccion;
                            cmdCli.CommandText = @"UPDATE clientes SET 
                                        NOMBRE_CUENTA = @nombreCuenta, NOMBRE_FISCAL = @nombreFiscal, DIRECCION = @direccion, 
                                        POBLACION = @poblacion, NUMERO = @numero, CP = @cp, PROVINCIA = @provincia, PAIS = @pais, 
                                        FECHA_BAJA = @fechaBaja, IBAN = @iban, TELEFONO = @telefono, EMAIL = @email, CIF = @cif 
                                        WHERE CUENTA = @cuenta";

                            cmdCli.Parameters.AddWithValue("@nombreCuenta", modificado.NOMBRE_CUENTA);
                            cmdCli.Parameters.AddWithValue("@nombreFiscal", modificado.NOMBRE_FISCAL);
                            cmdCli.Parameters.AddWithValue("@direccion", modificado.DIRECCION ?? "");
                            cmdCli.Parameters.AddWithValue("@poblacion", modificado.POBLACION ?? "");
                            cmdCli.Parameters.AddWithValue("@numero", modificado.NUMERO ?? "");
                            cmdCli.Parameters.AddWithValue("@cp", modificado.CP ?? "");
                            cmdCli.Parameters.AddWithValue("@provincia", modificado.PROVINCIA ?? "");
                            cmdCli.Parameters.AddWithValue("@pais", modificado.PAIS ?? "");
                            cmdCli.Parameters.AddWithValue("@fechaBaja", (object)modificado.FECHA_BAJA ?? DBNull.Value);
                            cmdCli.Parameters.AddWithValue("@iban", modificado.IBAN ?? "");
                            cmdCli.Parameters.AddWithValue("@telefono", modificado.TELEFONO ?? "");
                            cmdCli.Parameters.AddWithValue("@email", modificado.EMAIL ?? "");
                            cmdCli.Parameters.AddWithValue("@cif", modificado.CIF);
                            cmdCli.Parameters.AddWithValue("@cuenta", modificado.CUENTA);

                            cmdCli.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Cliente y cuenta contable actualizados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Error al modificar el cliente: " + ex.Message;
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
                        using (var cmdCli = conexion.CreateCommand())
                        {
                            cmdCli.Transaction = transaccion;
                            cmdCli.CommandText = "DELETE FROM clientes WHERE CUENTA = @cuenta";
                            cmdCli.Parameters.AddWithValue("@cuenta", id);
                            cmdCli.ExecuteNonQuery();
                        }

                        using (var cmdCta = conexion.CreateCommand())
                        {
                            cmdCta.Transaction = transaccion;
                            cmdCta.CommandText = "DELETE FROM cuentas WHERE CUENTA = @cuenta";
                            cmdCta.Parameters.AddWithValue("@cuenta", id);
                            cmdCta.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        TempData["MensajeExito"] = "Cliente y cuenta contable eliminados del sistema.";
                    }
                    catch (MySqlException ex)
                    {
                        transaccion.Rollback();
                        if (ex.Number == 1451)
                            TempData["Error"] = "No se puede eliminar. Este registro ya posee histórico, movimientos contables o facturas asociadas.";
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