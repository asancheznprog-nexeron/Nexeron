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
                string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionWinNetHost"].ConnectionString;
                if (connStr.Contains("cortecheco"))
                {
                    connStr = connStr.Replace("database=cortecheco", "database=" + login.Empresa);
                }
                else
                {
                    ModelState.AddModelError("", "Error de configuración de conexión.");
                    return View(login);
                }

                using (MySqlConnection conexion = new MySqlConnection(connStr))
                {
                    try
                    {
                        conexion.Open();
                        bool loginExitoso = false;

                        using (var cmd = conexion.CreateCommand())
                        {
                            cmd.CommandText = "SELECT PASSWORD FROM usuarios WHERE USUARIO = @usuario";
                            cmd.Parameters.AddWithValue("@usuario", login.Usuario);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string passwordBd = reader["PASSWORD"] as string ?? "";
                                    string passwordIngresada = login.Password ?? "";

                                    if (passwordBd.StartsWith("$2"))
                                    {
                                        if (BCrypt.Net.BCrypt.Verify(passwordIngresada, passwordBd))
                                        {
                                            loginExitoso = true;
                                        }
                                    }
                                    else if (passwordIngresada == passwordBd && !string.IsNullOrEmpty(passwordBd))
                                    {
                                        loginExitoso = true;
                                    }
                                }
                            }
                        }

                        if (loginExitoso)
                        {
                            Session.Timeout = 12 * 60;
                            Session["Usuario"] = login.Usuario;
                            Session["cadenaConexion"] = connStr;

                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Usuario o contraseña incorrecta.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Session.Clear();
                        ModelState.AddModelError("", "ERROR CRÍTICO: " + ex.Message);
                    }
                }
            }

            return View(login);
        }

    }
}