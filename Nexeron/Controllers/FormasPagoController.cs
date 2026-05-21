using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;
using Nexeron.Models;

namespace Nexeron.Controllers
{
    public class FormasPagoController : Controller
    {

        public ActionResult Index()
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<fpagcob> lista = new List<fpagcob>();
            string connStr = Session["cadenaConexion"].ToString();

            using (MySqlConnection conexion = new MySqlConnection(connStr))
            {
                try
                {
                    conexion.Open();
                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = "SELECT CODIGO, FORMACOBPAG, ACEPTO, REMESACOBRO, REMESAPAGO, NUMCOBROS, PRIMVENCI, DIASVENCI FROM fpagcob ORDER BY CODIGO ASC";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new fpagcob
                                {
                                    CODIGO = reader["CODIGO"].ToString(),
                                    FORMACOBPAG = reader["FORMACOBPAG"].ToString(),
                                    ACEPTO = reader["ACEPTO"].ToString(),
                                    REMESACOBRO = reader["REMESACOBRO"].ToString(),
                                    REMESAPAGO = reader["REMESAPAGO"].ToString(),
                                    NUMCOBROS = Convert.ToInt32(reader["NUMCOBROS"]),
                                    PRIMVENCI = Convert.ToInt32(reader["PRIMVENCI"]),
                                    DIASVENCI = Convert.ToInt32(reader["DIASVENCI"])
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al cargar formas de pago: " + ex.Message;
                }
            }
            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(fpagcob nuevo)
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
                        cmdCheck.CommandText = "SELECT COUNT(*) FROM fpagcob WHERE CODIGO = @codigo";
                        cmdCheck.Parameters.AddWithValue("@codigo", nuevo.CODIGO);
                        if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                        {
                            TempData["Error"] = "El código de forma de pago ya se encuentra registrado.";
                            TempData["TipoError"] = "Crear";
                            return RedirectToAction("Index");
                        }
                    }

                    using (var cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO fpagcob (CODIGO, FORMACOBPAG, ACEPTO, REMESACOBRO, REMESAPAGO, NUMCOBROS, PRIMVENCI, DIASVENCI) 
                                            VALUES (@codigo, @formaCobPag, @acepto, @remesaCobro, @remesaPago, @numCobros, @primVenci, @diasVenci)";
                        cmd.Parameters.AddWithValue("@codigo", nuevo.CODIGO);
                        cmd.Parameters.AddWithValue("@formaCobPag", nuevo.FORMACOBPAG);
                        cmd.Parameters.AddWithValue("@acepto", nuevo.ACEPTO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaCobro", nuevo.REMESACOBRO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaPago", nuevo.REMESAPAGO ?? "N");
                        cmd.Parameters.AddWithValue("@numCobros", nuevo.NUMCOBROS);
                        cmd.Parameters.AddWithValue("@primVenci", nuevo.PRIMVENCI);
                        cmd.Parameters.AddWithValue("@diasVenci", nuevo.DIASVENCI);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Forma de pago registrada correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar el registro: " + ex.Message;
                    TempData["TipoError"] = "Crear";
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(fpagcob modificado)
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
                        cmd.CommandText = @"UPDATE fpagcob SET 
                                            FORMACOBPAG = @formaCobPag, ACEPTO = @acepto, REMESACOBRO = @remesaCobro, 
                                            REMESAPAGO = @remesaPago, NUMCOBROS = @numCobros, PRIMVENCI = @primVenci, DIASVENCI = @diasVenci 
                                            WHERE CODIGO = @codigo";
                        cmd.Parameters.AddWithValue("@formaCobPag", modificado.FORMACOBPAG);
                        cmd.Parameters.AddWithValue("@acepto", modificado.ACEPTO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaCobro", modificado.REMESACOBRO ?? "N");
                        cmd.Parameters.AddWithValue("@remesaPago", modificado.REMESAPAGO ?? "N");
                        cmd.Parameters.AddWithValue("@numCobros", modificado.NUMCOBROS);
                        cmd.Parameters.AddWithValue("@primVenci", modificado.PRIMVENCI);
                        cmd.Parameters.AddWithValue("@diasVenci", modificado.DIASVENCI);
                        cmd.Parameters.AddWithValue("@codigo", modificado.CODIGO);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Forma de pago actualizada correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al modificar el registro: " + ex.Message;
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
                        cmd.CommandText = "DELETE FROM fpagcob WHERE CODIGO = @codigo";
                        cmd.Parameters.AddWithValue("@codigo", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Forma de pago eliminada correctamente.";
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451)
                        TempData["Error"] = "No se puede eliminar. Esta forma de pago ya está asignada a clientes, proveedores o facturas activas.";
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
        [HttpPost]
        public ActionResult DescargarInforme(string datosFiltradosJson)
        {
            if (Session["Usuario"] == null || Session["cadenaConexion"] == null)
                return RedirectToAction("Login", "Home");

            List<fpagcob> lista = new List<fpagcob>();

            try
            {
                if (!string.IsNullOrEmpty(datosFiltradosJson))
                {
                    var serializer = new JavaScriptSerializer();
                    lista = serializer.Deserialize<List<fpagcob>>(datosFiltradosJson);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar los datos del filtro: " + ex.Message;
                return RedirectToAction("Index");
            }

            if (lista == null || lista.Count == 0)
            {
                TempData["Error"] = "No hay datos disponibles para imprimir con el filtro actual.";
                return RedirectToAction("Index");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document documento = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
                PdfWriter writer = PdfWriter.GetInstance(documento, ms);
                documento.Open();

                Font fuenteTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                Font fuenteCabecera = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
                Font fuenteDatos = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);

                Paragraph titulo = new Paragraph("INFORME FILTRADO DE FORMAS DE PAGO Y COBRO", fuenteTitulo);
                titulo.Alignment = Element.ALIGN_CENTER;
                titulo.SpacingAfter = 15f;
                documento.Add(titulo);

                PdfPTable tabla = new PdfPTable(8);
                tabla.WidthPercentage = 100;
                float[] anchosColumnas = { 8f, 28f, 10f, 12f, 12f, 10f, 10f, 10f };
                tabla.SetWidths(anchosColumnas);

                string[] cabeceras = { "Código", "Forma de cobro", "Aceptable", "Remesa cobros", "Remesa pagos", "Nº de cobros", "Primer vencimiento", "Dias entre vtos" };
                foreach (string cab in cabeceras)
                {
                    PdfPCell celda = new PdfPCell(new Phrase(cab, fuenteCabecera))
                    {
                        BackgroundColor = new BaseColor(0, 123, 255),
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6f
                    };
                    tabla.AddCell(celda);
                }

                foreach (var item in lista)
                {
                    tabla.AddCell(new PdfPCell(new Phrase(item.CODIGO, fuenteDatos)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(item.FORMACOBPAG, fuenteDatos)) { Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(item.ACEPTO, fuenteDatos)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(item.REMESACOBRO, fuenteDatos)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(item.REMESAPAGO, fuenteDatos)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(item.NUMCOBROS.ToString(), fuenteDatos)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(item.PRIMVENCI.ToString(), fuenteDatos)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(item.DIASVENCI.ToString(), fuenteDatos)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                }

                documento.Add(tabla);
                documento.Close();

                string nombreArchivo = "Informe_FormasPago_" + DateTime.Now.ToString("yyyyMMdd") + ".pdf";
                return File(ms.ToArray(), "application/pdf", nombreArchivo);
            }
        }
    }
}