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
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar clientes: " + ex.Message;
                }
            }

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
                try
                {
                    conexion.Open();
                    using (var cmdCheck = conexion.CreateCommand())
                    {
                        cmdCheck.CommandText = "SELECT COUNT(*) FROM clientes WHERE CUENTA = @cuenta OR CIF = @cif";
                        cmdCheck.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                        cmdCheck.Parameters.AddWithValue("@cif", nuevo.CIF);

                        if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                        {
                            TempData["Error"] = "La cuenta contable o el CIF ya se encuentran registrados.";
                            TempData["TipoError"] = "Crear";
                            return RedirectToAction("Index");
                        }
                    }

                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO clientes (CUENTA, NOMBRE_CUENTA, NOMBRE_FISCAL, DIRECCION, POBLACION, NUMERO, CP, PROVINCIA, PAIS, FECHA_ALTA, FECHA_BAJA, IBAN, TELEFONO, EMAIL, CIF) 
                                            VALUES (@cuenta, @nombreCuenta, @nombreFiscal, @direccion, @poblacion, @numero, @cp, @provincia, @pais, @fechaAlta, @fechaBaja, @iban, @telefono, @email, @cif)";

                        cmd.Parameters.AddWithValue("@cuenta", nuevo.CUENTA);
                        cmd.Parameters.AddWithValue("@nombreCuenta", nuevo.NOMBRE_CUENTA);
                        cmd.Parameters.AddWithValue("@nombreFiscal", nuevo.NOMBRE_FISCAL);
                        cmd.Parameters.AddWithValue("@direccion", nuevo.DIRECCION ?? "");
                        cmd.Parameters.AddWithValue("@poblacion", nuevo.POBLACION ?? "");
                        cmd.Parameters.AddWithValue("@numero", nuevo.NUMERO ?? "");
                        cmd.Parameters.AddWithValue("@cp", nuevo.CP ?? "");
                        cmd.Parameters.AddWithValue("@provincia", nuevo.PROVINCIA ?? "");
                        cmd.Parameters.AddWithValue("@pais", nuevo.PAIS ?? "España");
                        cmd.Parameters.AddWithValue("@fechaAlta", nuevo.FECHA_ALTA);
                        cmd.Parameters.AddWithValue("@fechaBaja", (object)nuevo.FECHA_BAJA ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@iban", nuevo.IBAN ?? "");
                        cmd.Parameters.AddWithValue("@telefono", nuevo.TELEFONO ?? "");
                        cmd.Parameters.AddWithValue("@email", nuevo.EMAIL ?? "");
                        cmd.Parameters.AddWithValue("@cif", nuevo.CIF);

                        cmd.ExecuteNonQuery();
                        TempData["MensajeExito"] = "Cliente creado correctamente.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar el cliente: " + ex.Message;
                    TempData["TipoError"] = "Crear";
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
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE clientes SET 
                                            NOMBRE_CUENTA = @nombreCuenta, NOMBRE_FISCAL = @nombreFiscal, DIRECCION = @direccion, 
                                            POBLACION = @poblacion, NUMERO = @numero, CP = @cp, PROVINCIA = @provincia, PAIS = @pais, 
                                            FECHA_BAJA = @fechaBaja, IBAN = @iban, TELEFONO = @telefono, EMAIL = @email, CIF = @cif 
                                            WHERE CUENTA = @cuenta";

                        cmd.Parameters.AddWithValue("@nombreCuenta", modificado.NOMBRE_CUENTA);
                        cmd.Parameters.AddWithValue("@nombreFiscal", modificado.NOMBRE_FISCAL);
                        cmd.Parameters.AddWithValue("@direccion", modificado.DIRECCION ?? "");
                        cmd.Parameters.AddWithValue("@poblacion", modificado.POBLACION ?? "");
                        cmd.Parameters.AddWithValue("@numero", modificado.NUMERO ?? "");
                        cmd.Parameters.AddWithValue("@cp", modificado.CP ?? "");
                        cmd.Parameters.AddWithValue("@provincia", modificado.PROVINCIA ?? "");
                        cmd.Parameters.AddWithValue("@pais", modificado.PAIS ?? "");
                        cmd.Parameters.AddWithValue("@fechaBaja", (object)modificado.FECHA_BAJA ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@iban", modificado.IBAN ?? "");
                        cmd.Parameters.AddWithValue("@telefono", modificado.TELEFONO ?? "");
                        cmd.Parameters.AddWithValue("@email", modificado.EMAIL ?? "");
                        cmd.Parameters.AddWithValue("@cif", modificado.CIF);
                        cmd.Parameters.AddWithValue("@cuenta", modificado.CUENTA);

                        cmd.ExecuteNonQuery();
                        TempData["MensajeExito"] = "Cliente modificado correctamente.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al modificar el cliente: " + ex.Message;
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
                        cmd.CommandText = "DELETE FROM clientes WHERE CUENTA = @cuenta";
                        cmd.Parameters.AddWithValue("@cuenta", id);
                        cmd.ExecuteNonQuery();
                        TempData["MensajeExito"] = "Cliente eliminado correctamente.";
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451)
                        TempData["Error"] = "No se puede eliminar. Este cliente ya posee facturas o albaranes asociados.";
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