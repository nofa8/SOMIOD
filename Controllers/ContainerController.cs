using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using System;
using SOMIOD.Models;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Drawing;
using System.Data.Common;
using System.Net;
using System.Web;
using System.Xml.Schema;
using System.ComponentModel;
using System.Net.Http;


namespace SOMIOD.Controllers
{
    public class ContainerController : ApiController
    {
        private string DBConnection = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;
        public IHttpActionResult locateAllContainers()
        {
            SqlConnection conn = null;

            // Criação do documento XML
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null); // Definir a versão XML e codificação
            doc.AppendChild(dec);

            try
            {
                conn = new SqlConnection(DBConnection);
                conn.Open();

                string query = @"
                                SELECT c.* 
                                FROM Container c
                                ORDER BY c.id";



                SqlCommand command = new SqlCommand(query, conn);

                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("containers");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement containerElement = doc.CreateElement("container");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    containerElement.AppendChild(nameElement);


                    root.AppendChild(containerElement);
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


        public IHttpActionResult locateAllContainersByApplication(string application)
        {
            SqlConnection conn = null;


            var verify = verifyPathUrl(application);
            if (verify != "OK")
                return BadRequest(verify);

            // Criação do documento XML
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null); // Definir a versão XML e codificação
            doc.AppendChild(dec);

            try
            {
                conn = new SqlConnection(DBConnection);
                conn.Open();

                string query = @"
                                SELECT c.* 
                                FROM Container c
                                INNER JOIN Application a ON c.parent = a.id
                                WHERE a.name = @application 
                                ORDER BY c.id";



                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@application", application);

                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("containers");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement containerElement = doc.CreateElement("container");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    containerElement.AppendChild(nameElement);


                    root.AppendChild(containerElement);
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




        public IHttpActionResult GetContainer(string application, string container)
        {
            XmlDocument docCont = new XmlDocument();

            try
            {
                docCont = ObtainXmlDocOfContainer(application, container, true);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            if (docCont == null)
            {
                return NotFound();
            }
            return Content(HttpStatusCode.OK, docCont, Configuration.Formatters.XmlFormatter);
        }


        public IHttpActionResult PatchContainer(string application, string container, [FromBody] XmlElement xmlElementEn)
        {
            SqlConnection conn = null;


            try
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return BadRequest(verify);

                string newName = xmlElementEn.InnerText;


                if (xmlElementEn == null)
                {
                    return BadRequest("Invalid XML payload.");
                }

                conn = new SqlConnection(DBConnection);
                conn.Open();

                int nRows = 1;

                if (newName != null)
                {
                    newName = SanitizeForUrl(newName);  // Para evitar (por exemplo espaços no nome) uma vez que depois nos get/delete o nome com espaços fica invalido no url
                    string queryNameExiste = @"Select COUNT(*) from Container where name = @nameReceive";
                    SqlCommand vCommand = new SqlCommand(queryNameExiste, conn);
                    vCommand.Parameters.AddWithValue("@nameReceive", newName);


                    nRows = (int)vCommand.ExecuteScalar();
                }


                if (nRows > 0)
                {
                    return Content(HttpStatusCode.Conflict, "Name already exist");
                }


                string updateContainerQuery = @"
                    UPDATE Container 
                    SET name = @newName 
                    WHERE name = @name 
                      AND parent = (SELECT id FROM Application WHERE name = @application)";

                SqlCommand command = new SqlCommand(updateContainerQuery, conn);

                command.Parameters.AddWithValue("@newName", newName);
                command.Parameters.AddWithValue("@name", container);
                command.Parameters.AddWithValue("@application", application);

                nRows = command.ExecuteNonQuery();
                conn.Close();

                if (nRows > 0)
                {
                    XmlDocument docCont = new XmlDocument();


                    docCont = ObtainXmlDocOfContainer(application, container, false);

                    return Content(HttpStatusCode.OK, docCont, Configuration.Formatters.XmlFormatter);

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





        public IHttpActionResult PostContainer(string application, [FromBody] XmlDocument xmlDocumentCont)
        {
            SqlConnection conn = null;

            try
            {
                var verify = verifyPathUrl(application);
                if (verify != "OK")
                    return BadRequest(verify);

                XmlNode containerNode = xmlDocumentCont.SelectSingleNode("//Container");
                if (containerNode == null)
                {
                    return BadRequest("Missing Container element.");
                }

                string schemaPath = HttpContext.Current.Server.MapPath("~/XML/SystemSchema.xsd");


                XmlSchemaSet schemas = new XmlSchemaSet();
                schemas.Add("", schemaPath);

                xmlDocumentCont.Schemas = schemas;
                xmlDocumentCont.Validate((sender, e) => {
                    if (e.Severity == XmlSeverityType.Error)
                    {
                        throw new InvalidOperationException($"{e.Message}");
                    }
                });

                conn = new SqlConnection(DBConnection);
                conn.Open();
                int nRows = 1;

                string nameReceived = null;
                XmlNode nameNode = containerNode["name"];

                if (nameNode != null)
                {
                    nameReceived = SanitizeForUrl(nameNode.InnerText);  // Para evitar (por exemplo espaços no nome) uma vez que depois nos get/delete o nome com espaços fica invalido no url
                    string queryNameExiste = @"Select COUNT(*) from Container where name = @nameReceive";
                    SqlCommand vCommand = new SqlCommand(queryNameExiste, conn);
                    vCommand.Parameters.AddWithValue("@nameReceive", nameReceived);


                    nRows = (int)vCommand.ExecuteScalar();
                }


                if (nRows > 0)
                {
                    nameReceived = $"cont-{Guid.NewGuid()}";
                }




                string insertContainerQuery = @"
                    INSERT INTO Container (name, creation_datetime, parent) 
                    VALUES (
                        @newName, 
                        GETDATE(), 
                        (SELECT id FROM Application WHERE name = @newParent)
                    )";

                SqlCommand insertCommand = new SqlCommand(insertContainerQuery, conn);
                insertCommand.Parameters.AddWithValue("@newName", nameReceived);
                insertCommand.Parameters.AddWithValue("@newParent", application);

                nRows = insertCommand.ExecuteNonQuery();
                conn.Close();

                if (nRows > 0)
                {
                    XmlDocument docCont = new XmlDocument();


                    docCont = ObtainXmlDocOfContainer(application, nameReceived, false);

                    return Content(HttpStatusCode.OK, docCont, Configuration.Formatters.XmlFormatter);

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




        public IHttpActionResult DeleteContainer(string application, string container)
        {
            SqlConnection connection = null;

            try
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return BadRequest(verify);

                connection = new SqlConnection(DBConnection);
                connection.Open();

                // Delete notifications for this container
                SqlCommand deleteNotificationsCmd = new SqlCommand(@"
                            DELETE FROM Notification
                            WHERE parent IN (
                                SELECT c.id
                                FROM Container c
                                INNER JOIN Application a ON c.parent = a.id
                                WHERE a.name = @application AND c.name = @container
                            )", connection);
                deleteNotificationsCmd.Parameters.AddWithValue("@application", application);
                deleteNotificationsCmd.Parameters.AddWithValue("@container", container);
                int notificationsDeleted = deleteNotificationsCmd.ExecuteNonQuery();

                // Delete records for this container
                SqlCommand deleteRecordsCmd = new SqlCommand(@"
                        DELETE FROM Record
                        WHERE parent IN (
                            SELECT c.id
                            FROM Container c
                            INNER JOIN Application a ON c.parent = a.id
                            WHERE a.name = @application AND c.name = @container
                        )", connection);
                deleteRecordsCmd.Parameters.AddWithValue("@application", application);
                deleteRecordsCmd.Parameters.AddWithValue("@container", container);
                int recordsDeleted = deleteRecordsCmd.ExecuteNonQuery();

                // Delete the container
                SqlCommand deleteContainerCmd = new SqlCommand(@"
                        DELETE FROM Container
                        WHERE parent = (
                            SELECT a.id
                            FROM Application a
                            WHERE a.name = @application
                        )
                        AND name = @container", connection);
                deleteContainerCmd.Parameters.AddWithValue("@application", application);
                deleteContainerCmd.Parameters.AddWithValue("@container", container);
                int containerDeleted = deleteContainerCmd.ExecuteNonQuery();

                // If no container was deleted, return NotFound
                if (containerDeleted == 0)
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


        private string verifyPathUrl(string application, string container = null)
        {
            SqlConnection connection = new SqlConnection(DBConnection);
            connection.Open();
            string verifyQuery;

            if (isNull(container))
            {
                verifyQuery = @"SELECT id FROM Application WHERE name = @applicationName";
                SqlCommand getAppIdCommand = new SqlCommand(verifyQuery, connection);
                getAppIdCommand.Parameters.AddWithValue("@applicationName", application);
                var appId = getAppIdCommand.ExecuteScalar();
                if (appId == null)
                {
                    return $"App with name '{application}' do not exist.";
                }

            }
            else
            {
                verifyQuery = @"
                SELECT Id FROM Container WHERE Name = @containerName AND parent IN (
                SELECT id FROM Application WHERE name = @applicationName)";

                SqlCommand getContainerIdCommand = new SqlCommand(verifyQuery, connection);
                getContainerIdCommand.Parameters.AddWithValue("@containerName", container);
                getContainerIdCommand.Parameters.AddWithValue("@applicationName", application);

                var containerId = getContainerIdCommand.ExecuteScalar();
                if (containerId == null)
                {
                    return $"Container with name '{container}' is not a child of application '{application}'.";
                }
            }

            return "OK";
        }
        private XmlDocument ObtainXmlDocOfContainer(string application, string container, bool verifyPath)
        {

            SqlConnection conn = null;

            if (verifyPath)
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return null;
            }


            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(dec);




            try
            {

                conn = new SqlConnection(DBConnection);
                conn.Open();

                string query = @"
                                        SELECT c.* 
                                        FROM Container c
                                        INNER JOIN Application a ON c.parent = a.id
                                        WHERE a.name = @application AND c.name = @container";



                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.AddWithValue("@container", container);

                command.Parameters.AddWithValue("@application", application);


                SqlDataReader reader = command.ExecuteReader();


                XmlElement root = doc.CreateElement("Container");
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

                    XmlElement parentElement = doc.CreateElement("parent");
                    parentElement.InnerText = reader["Parent"].ToString();
                    root.AppendChild(parentElement);

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

        private bool isNull(string s)
        {
            return string.IsNullOrEmpty(s);
        }
    }
}