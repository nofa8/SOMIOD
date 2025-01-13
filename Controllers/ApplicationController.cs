using SOMIOD.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Web;
using System.Xml.Schema;


namespace SOMOID.Controllers
{
    public class ApplicationController : ApiController
    {
        private string DBConnection = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;

        public IHttpActionResult locateAllApplications() //localizar todas as aplicacoes do sistema (devolver apenas uma lista com os nomes)
        {
            SqlConnection conn = null;

            XmlDocument doc = new XmlDocument(); // Criação do documento XML
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null); // Definir a versão XML e codificação
            doc.AppendChild(dec);

            try
            {
                conn = new SqlConnection(DBConnection);
                conn.Open();

                string query = @"
                                SELECT a.* 
                                FROM Application a
                                ORDER BY a.id";



                SqlCommand command = new SqlCommand(query, conn);

                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("applications");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement applicationElement = doc.CreateElement("application");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    applicationElement.AppendChild(nameElement);


                    root.AppendChild(applicationElement);
                }

                reader.Close();

                return Content(HttpStatusCode.OK, doc, Configuration.Formatters.XmlFormatter);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
            finally
            {
                if (conn?.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }
        }



        public IHttpActionResult GetApplication(string application)
        {
            XmlDocument docApp = new XmlDocument();

            try
            {
                docApp = ObtainXmlDocOfApplication(application);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            if (docApp == null)
            {
                return NotFound();
            }
            return Content(HttpStatusCode.OK, docApp, Configuration.Formatters.XmlFormatter);
        }




        public IHttpActionResult PostApplication([FromBody] XmlDocument xmlDocument)
        {
            SqlConnection conn = null;

            try
            {

                XmlNode applicationNode = xmlDocument.SelectSingleNode("//Application");
                if (applicationNode == null)
                {
                    return BadRequest("Missing Notification element.");
                }

                string schemaPath = HttpContext.Current.Server.MapPath("~/XML/SystemSchema.xsd");


                XmlSchemaSet schemas = new XmlSchemaSet();
                schemas.Add("", schemaPath);

                xmlDocument.Schemas = schemas;
                xmlDocument.Validate((sender, e) =>
                {
                    if (e.Severity == XmlSeverityType.Error)
                    {
                        throw new InvalidOperationException($"{e.Message}");
                    }
                });

               conn = new SqlConnection(DBConnection);
                conn.Open();
                int nRows = 1;

                


                string nameReceived = null;
                XmlNode nameNode = applicationNode["name"];

                if (nameNode != null)
                {
                    nameReceived = SanitizeForUrl(nameNode.InnerText);  // Para evitar (por exemplo espaços no nome) uma vez que depois nos get/delete o nome com espaços fica invalido no url
                    string queryNameExiste = @"Select COUNT(*) from Application where name = @nameReceive";
                    SqlCommand vCommand = new SqlCommand(queryNameExiste, conn);
                    vCommand.Parameters.AddWithValue("@nameReceive", nameReceived);


                    nRows = (int)vCommand.ExecuteScalar();
                }


                if (nRows > 0)
                {
                    nameReceived = $"app-{Guid.NewGuid()}";
                }




                string insertQuery = @"
                    INSERT INTO Application (name, creation_datetime) 
                    VALUES (
                        @newName, 
                        GETDATE() 
                       
                    )";

                SqlCommand insertCommand = new SqlCommand(insertQuery, conn);
                insertCommand.Parameters.AddWithValue("@newName", nameReceived);
               

                nRows = insertCommand.ExecuteNonQuery();
                conn.Close();

                if (nRows > 0)
                {
                    XmlDocument docNot = new XmlDocument();

                    docNot = ObtainXmlDocOfApplication(nameReceived);

                    return Content(HttpStatusCode.OK, docNot, Configuration.Formatters.XmlFormatter);

                }
                else
                {
                    return NotFound();
                }
            }
            catch (SqlException ex)
            {

                if (ex.Number == 2627 || ex.Number == 2601) // Erros comuns para chave única no SQL Server
                {
                    return Conflict();
                }
                return InternalServerError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"XML validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }
        }




        public IHttpActionResult PatchApplication(string application,  [FromBody] XmlElement xmlElement)
        {
            SqlConnection conn = null;


            try
            {
                
                string nameReceived = xmlElement.InnerText;


                if (xmlElement == null)
                {
                    return BadRequest("Invalid XML payload.");
                }





                conn = new SqlConnection(DBConnection);
                conn.Open();
                int nRows = 1;




               

                if (nameReceived != null)
                {
                    nameReceived = SanitizeForUrl(nameReceived);  // Para evitar (por exemplo espaços no nome) uma vez que depois nos get/delete o nome com espaços fica invalido no url
                    string queryNameExiste = @"Select COUNT(*) from Application where name = @nameReceive";
                    SqlCommand vCommand = new SqlCommand(queryNameExiste, conn);
                    vCommand.Parameters.AddWithValue("@nameReceive", nameReceived);


                    nRows = (int)vCommand.ExecuteScalar();
                }

                if (nRows > 0)
                {
                    return Content(HttpStatusCode.Conflict, "Name already exist");
                }

                string patchQuery = @"
                    UPDATE Application 
                    SET name = @name 
                    WHERE name = @app";
                SqlCommand command = new SqlCommand(patchQuery, conn);

                command.Parameters.AddWithValue("@name", nameReceived);
                command.Parameters.AddWithValue("@app", application);

                nRows = command.ExecuteNonQuery();
                conn.Close();

                if (nRows > 0)
                {
                    XmlDocument doc = new XmlDocument();
                    doc = ObtainXmlDocOfApplication(nameReceived);
                    return Content(HttpStatusCode.OK, doc, Configuration.Formatters.XmlFormatter);

                }
                else
                {
                    return NotFound();
                }
            }
            catch (SqlException ex)
            {

                if (ex.Number == 2627 || ex.Number == 2601) // Erros comuns para chave única no SQL Server
                {
                    return Conflict();
                }
                return InternalServerError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"XML validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

        }



        private XmlDocument ObtainXmlDocOfApplication(string application)
        {

            SqlConnection conn = null;

            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(dec);

            try
            {

                conn = new SqlConnection(DBConnection);
                conn.Open();

                string query = @"
                                        SELECT a.* 
                                        FROM Application a
                                        WHERE a.name = @application ";



                SqlCommand command = new SqlCommand(query, conn);

                
                command.Parameters.AddWithValue("@application", application);


                SqlDataReader reader = command.ExecuteReader();


                XmlElement root = doc.CreateElement("Application");
                doc.AppendChild(root);



                if (reader.Read())
                {

                    root.SetAttribute("id", reader["Id"].ToString());

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    root.AppendChild(nameElement);

                    XmlElement creationDateTimeElement = doc.CreateElement("creation_datetime");
                    creationDateTimeElement.InnerText = ((DateTime)reader["creation_datetime"]).ToString("yyyy-MM-ddTHH:mm:ss");
                    root.AppendChild(creationDateTimeElement);

                }
                else
                {
                    return null;
                }


                reader.Close();
                return doc;


            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public IHttpActionResult DeleteApplication(string application)
        {
            SqlConnection connection = null;

            try
            {   
                connection = new SqlConnection(DBConnection);
                connection.Open();

                // Delete notifications for this container
                SqlCommand deleteNotificationsCmd = new SqlCommand(@"
                            DELETE FROM Notification
                            WHERE parent IN (
                                SELECT c.id
                                FROM Container c
                                INNER JOIN Application a ON c.parent = a.id
                                WHERE a.name = @application
                            )", connection);
                deleteNotificationsCmd.Parameters.AddWithValue("@application", application);
                int notificationsDeleted = deleteNotificationsCmd.ExecuteNonQuery();
                // Delete records for this container
                SqlCommand deleteRecordsCmd = new SqlCommand(@"
                        DELETE FROM Record
                        WHERE parent IN (
                            SELECT c.id
                            FROM Container c
                            INNER JOIN Application a ON c.parent = a.id
                            WHERE a.name = @application
                        )", connection);
                deleteRecordsCmd.Parameters.AddWithValue("@application", application);
                int recordsDeleted = deleteRecordsCmd.ExecuteNonQuery();
                // Delete the container
                SqlCommand deleteContainerCmd = new SqlCommand(@"
                        DELETE FROM Container
                        WHERE parent = (
                            SELECT a.id
                            FROM Application a
                            WHERE a.name = @application
                        )"
                        , connection);
                deleteContainerCmd.Parameters.AddWithValue("@application", application);
                int containerDeleted = deleteContainerCmd.ExecuteNonQuery();

                SqlCommand deleteAppCmd = new SqlCommand(@"
                        DELETE FROM Application where name = @application"
                        
                       , connection);
                deleteAppCmd.Parameters.AddWithValue("@application", application);
                int appDeleted = deleteAppCmd.ExecuteNonQuery();

                // If no container was deleted, return NotFound
                if (appDeleted == 0)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (connection != null)
                    connection.Close();
            }
        }



        private bool isNull(string s) // só para facilitar a leitura (fica mais simples)
        {
            return string.IsNullOrEmpty(s);
        }
        string SanitizeForUrl(string input)
        {
            // Substituir espaços por "-"
            string sanitized = input.Replace(" ", "-");
            // Remover carateres especiais
            char[] invalidChars = { ':', '/', '?', '#', '[', ']', '@', '"', '<', '>', '\\', '^', '`', '{', '|', '}', '~' };
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }
            return sanitized;
        }
    }
}