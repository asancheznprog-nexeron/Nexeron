using MySql.Data.MySqlClient;
using Nexeron.Models;
using System;
using System.Web.Mvc;


namespace Nexeron.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Session["Usuario"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel login)
        {
            if (ModelState.IsValid)
            {
                string connStrOriginal = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionWinNetHost"].ConnectionString;

                using (MySqlConnection conexionBase = new MySqlConnection(connStrOriginal))
                {
                    try
                    {
                        conexionBase.Open();
                        using (var cmdCheck = conexionBase.CreateCommand())
                        {

                            cmdCheck.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @empresa";
                            cmdCheck.Parameters.AddWithValue("@empresa", login.Empresa);

                            int existeEmpresa = Convert.ToInt32(cmdCheck.ExecuteScalar());

                            if (existeEmpresa == 0)
                            {

                                ModelState.AddModelError("", "La empresa introducida no existe o es incorrecta.");
                                return View(login);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("", "No se pudo comunicar con el servidor principal.");
                        return View(login);
                    }
                }
                string connStrEmpresa = connStrOriginal;
                if (connStrEmpresa.Contains("cortecheco"))
                {
                    connStrEmpresa = connStrEmpresa.Replace("database=cortecheco", "database=" + login.Empresa);
                }

                using (MySqlConnection conexionEmpresa = new MySqlConnection(connStrEmpresa))
                {
                    try
                    {
                        conexionEmpresa.Open();

                        bool loginExitoso = false;
                        bool usuarioEncontrado = false;
                        bool passwordCorrecta = false;
                        int fallosActuales = 0;
                        bool cuentaBloqueada = false;


                        using (var cmd = conexionEmpresa.CreateCommand())
                        {
                            cmd.CommandText = "SELECT PASSWORD, fallos FROM usuarios WHERE USUARIO = @usuario";
                            cmd.Parameters.AddWithValue("@usuario", login.Usuario);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    usuarioEncontrado = true;
                                    fallosActuales = reader["fallos"] != DBNull.Value ? Convert.ToInt32(reader["fallos"]) : 0;

                                    string passwordBd = reader["PASSWORD"] as string ?? "";
                                    string passwordIngresada = login.Password ?? "";

                                    if (fallosActuales >= 3)
                                    {
                                        cuentaBloqueada = true;
                                    }
                                    else
                                    {

                                        if (passwordBd.StartsWith("$2"))
                                        {
                                            if (BCrypt.Net.BCrypt.Verify(passwordIngresada, passwordBd))
                                                passwordCorrecta = true;
                                        }
                                        else if (passwordIngresada == passwordBd && !string.IsNullOrEmpty(passwordBd))
                                        {
                                            passwordCorrecta = true;
                                        }
                                    }
                                }
                            }
                        }


                        if (cuentaBloqueada)
                        {
                            ModelState.AddModelError("", "La cuenta está bloqueada por demasiados intentos fallidos. Contacte con un administrador.");
                        }
                        else if (usuarioEncontrado)
                        {
                            if (passwordCorrecta)
                            {
                                loginExitoso = true;


                                if (fallosActuales > 0)
                                {
                                    using (var cmdUpdate = conexionEmpresa.CreateCommand())
                                    {
                                        cmdUpdate.CommandText = "UPDATE usuarios SET fallos = 0 WHERE USUARIO = @usuario";
                                        cmdUpdate.Parameters.AddWithValue("@usuario", login.Usuario);
                                        cmdUpdate.ExecuteNonQuery();
                                    }
                                }
                            }
                            else
                            {

                                fallosActuales++;

                                using (var cmdUpdate = conexionEmpresa.CreateCommand())
                                {
                                    cmdUpdate.CommandText = "UPDATE usuarios SET fallos = @fallos WHERE USUARIO = @usuario";
                                    cmdUpdate.Parameters.AddWithValue("@fallos", fallosActuales);
                                    cmdUpdate.Parameters.AddWithValue("@usuario", login.Usuario);
                                    cmdUpdate.ExecuteNonQuery();
                                }

                                if (fallosActuales >= 3)
                                {
                                    ModelState.AddModelError("", "Contraseña incorrecta. Tu cuenta ha sido bloqueada por seguridad.");
                                }
                                else
                                {
                                    ModelState.AddModelError("", $"Contraseña incorrecta. Te quedan {3 - fallosActuales} intento(s).");
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Usuario o contraseña incorrecta.");
                        }


                        if (loginExitoso)
                        {
                            Session.Timeout = 12 * 60;
                            Session["Usuario"] = login.Usuario;
                            Session["cadenaConexion"] = connStrEmpresa;

                            return RedirectToAction("Index", "Home");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error en el login: {ex.Message}");
                        Session.Clear();
                        ModelState.AddModelError("", "Empresa o usuario incorrectos.");
                    }
                }
            }

            return View(login);
        }


        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return Redirect("~/");
        }
    }
}