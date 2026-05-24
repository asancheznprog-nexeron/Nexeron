using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using Nexeron.Models; 
using BCrypt.Net;

namespace Nexeron.Controllers
{
    public class UsuariosController : Controller
    {
        
        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string connStr = Session["cadenaConexion"].ToString();
            List<usuarios> listaUsuarios = new List<usuarios>();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        
                        cmd.CommandText = "SELECT clave, usuario, fallos FROM usuarios ORDER BY usuario ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaUsuarios.Add(new usuarios
                                {
                                    clave = Convert.ToInt32(reader["clave"]),
                                    usuario = reader["usuario"].ToString(),
                                    fallos = reader["fallos"] != DBNull.Value ? Convert.ToInt32(reader["fallos"]) : 0
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar los usuarios: " + ex.Message;
                }
            }

            return View(listaUsuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearUsuario(usuarios nuevoUsuario)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(nuevoUsuario.password))
            {
                TempData["Error"] = "La contraseña es obligatoria para nuevos usuarios.";
                TempData["TipoError"] = "Crear";
                return RedirectToAction("Index");
            }

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
                            cmdCheck.CommandText = "SELECT COUNT(*) FROM usuarios WHERE usuario = @usuario";
                            cmdCheck.Parameters.AddWithValue("@usuario", nuevoUsuario.usuario);

                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                            {
                                TempData["Error"] = "El nombre de usuario ya está cogido.";
                                TempData["TipoError"] = "Crear";
                                return RedirectToAction("Index");
                            }
                        }

                        
                        string passwordHasheada = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.password);

                        using (var cmd = conexion.CreateCommand())
                        {
                            cmd.CommandText = "INSERT INTO usuarios (usuario, password, fallos) VALUES (@usuario, @password, 0)";
                            cmd.Parameters.AddWithValue("@usuario", nuevoUsuario.usuario);
                            cmd.Parameters.AddWithValue("@password", passwordHasheada);
                            cmd.ExecuteNonQuery();
                        }

                        TempData["MensajeExito"] = "Usuario creado con éxito.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error de BD: " + ex.Message;
                        TempData["TipoError"] = "Crear";
                    }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarUsuario(usuarios usuarioModificado, string NuevaPassword)
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
                            
                            if (!string.IsNullOrEmpty(NuevaPassword))
                            {
                                string passwordHasheada = BCrypt.Net.BCrypt.HashPassword(NuevaPassword);
                                cmd.CommandText = "UPDATE usuarios SET usuario = @usuario, password = @password WHERE clave = @clave";
                                cmd.Parameters.AddWithValue("@password", passwordHasheada);
                            }
                            else
                            {
                                cmd.CommandText = "UPDATE usuarios SET usuario = @usuario WHERE clave = @clave";
                            }

                            cmd.Parameters.AddWithValue("@usuario", usuarioModificado.usuario);
                            cmd.Parameters.AddWithValue("@clave", usuarioModificado.clave);
                            cmd.ExecuteNonQuery();
                        }

                        TempData["MensajeExito"] = "Usuario actualizado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error al modificar el usuario: " + ex.Message;
                        TempData["TipoError"] = "Editar";
                    }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarUsuario(int id) 
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            string connStr = Session["cadenaConexion"].ToString();
            string usuarioActivo = Session["Usuario"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    string usuarioAEliminar = "";
                    using (var cmdTarget = conexion.CreateCommand())
                    {
                        cmdTarget.CommandText = "SELECT usuario FROM usuarios WHERE clave = @clave";
                        cmdTarget.Parameters.AddWithValue("@clave", id);
                        usuarioAEliminar = Convert.ToString(cmdTarget.ExecuteScalar());
                    }

                    if (usuarioActivo.Equals(usuarioAEliminar, StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["Error"] = "No puedes eliminar el usuario con el que estás logueado en este momento.";
                        return RedirectToAction("Index");
                    }

                    using (var cmdDelete = conexion.CreateCommand())
                    {
                        cmdDelete.CommandText = "DELETE FROM usuarios WHERE clave = @clave";
                        cmdDelete.Parameters.AddWithValue("@clave", id);
                        cmdDelete.ExecuteNonQuery();
                    }

                    TempData["MensajeExito"] = "El usuario se ha eliminado de Nexeron ERP.";
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