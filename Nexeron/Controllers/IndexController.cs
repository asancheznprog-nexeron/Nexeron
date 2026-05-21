using MySql.Data.MySqlClient;
using Nexeron.Models;
using System;
using System.Web.Mvc;


namespace Nexeron.Controllers
{
    public class IndexController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

    }
}