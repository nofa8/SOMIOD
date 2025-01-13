using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using System;
using SOMIOD.Models;
using System.Xml;
using System.Net;
using System.Web;
using System.Xml.Schema;
using SOMIOD.Controllers;
using System.Xml.Linq;

namespace SOMIOD.Controllers
{
    public class RecordController : ApiController
    {

        string DBConnection = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;

        public IHttpActionResult locateAllRecords()
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
                                SELECT r.* 
                                FROM Record r
                                ORDER BY r.id";



                SqlCommand command = new SqlCommand(query, conn);

                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("records");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement recordElement = doc.CreateElement("record");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    recordElement.AppendChild(nameElement);


                    root.AppendChild(recordElement);
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



        public IHttpActionResult locateAllRecordsByApplication(string application)
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
                                SELECT r.* 
                                FROM Record r
                                INNER JOIN Container c ON r.parent = c.id
                                INNER JOIN Application a ON c.parent = a.id
                                WHERE a.name = @application 
                                ORDER BY r.id";



                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@application", application);

                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("records");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement recordElement = doc.CreateElement("record");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    recordElement.AppendChild(nameElement);


                    root.AppendChild(recordElement);
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

        public IHttpActionResult locateAllRecordsByContainer(string application, string container)
        {
            SqlConnection conn = null;

            var verify = verifyPathUrl(application, container);
            if (verify != "OK")
                return BadRequest(verify);

            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(dec);

            try
            {
                conn = new SqlConnection(DBConnection);
                conn.Open();

                string query = @"
                        SELECT r.* 
                        FROM Record r
                        INNER JOIN Container c ON r.parent = c.id
                        INNER JOIN Application a ON c.parent = a.id
                        WHERE a.name = @application AND c.name = @container
                        ORDER BY r.id";



                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@application", application);
                command.Parameters.AddWithValue("@container", container);


                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("records");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement recordElement = doc.CreateElement("record");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    recordElement.AppendChild(nameElement);


                    root.AppendChild(recordElement);
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



        public IHttpActionResult GetRecord(string application, string container, string name)
        {

            XmlDocument docRec = new XmlDocument();

            try
            {
                docRec = ObtainXmlDocOfRecord(application, container, name, true);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            if (docRec == null)
            {
                return NotFound();
            }
            return Content(HttpStatusCode.OK, docRec, Configuration.Formatters.XmlFormatter);

        }




        public IHttpActionResult PostRecord(string application, string container, [FromBody] XmlDocument xmlDocumentNot)
        {
            SqlConnection conn = null;

            try
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return BadRequest(verify);

                XmlNode recordNode = xmlDocumentNot.SelectSingleNode("//Record");
                if (recordNode == null)
                {
                    return BadRequest("Missing Record element.");
                }

                string schemaPath = HttpContext.Current.Server.MapPath("~/XML/SystemSchema.xsd");


                XmlSchemaSet schemas = new XmlSchemaSet();
                schemas.Add("", schemaPath);

                xmlDocumentNot.Schemas = schemas;
                xmlDocumentNot.Validate((sender, e) => {
                    if (e.Severity == XmlSeverityType.Error)
                    {
                        throw new InvalidOperationException($"{e.Message}");
                    }
                });


                string nameReceived = null;
                XmlNode nameNode = recordNode["name"];

                conn = new SqlConnection(DBConnection);
                conn.Open();
                int nRows = 1;

                if (nameNode != null)
                {
                    nameReceived = SanitizeForUrl(nameNode.InnerText);
                    string queryNameExiste = @"Select COUNT(*) from Record where name = @nameReceive";
                    SqlCommand vCommand = new SqlCommand(queryNameExiste, conn);
                    vCommand.Parameters.AddWithValue("@nameReceive", nameReceived);


                    nRows = (int)vCommand.ExecuteScalar();
                }


                if (nRows > 0)
                {
                    nameReceived = $"rec-{Guid.NewGuid()}";
                }

                string content = recordNode["content"].InnerText;

              

                string insertRecordQuery = @"
                    INSERT INTO Record (name, content, creation_datetime, parent) 
                    VALUES (
                        @newName,
                        @newContent,
                        GETDATE(), 
                        (SELECT id FROM Container WHERE name = @newParent)
                    )";

                SqlCommand insertCommand = new SqlCommand(insertRecordQuery, conn);
                insertCommand.Parameters.AddWithValue("@newName", nameReceived);
                insertCommand.Parameters.AddWithValue("@newContent", content);
                insertCommand.Parameters.AddWithValue("@newParent", container);

                nRows = insertCommand.ExecuteNonQuery();
                conn.Close();

                if (nRows > 0)
                {
                    XmlDocument docRec = new XmlDocument();


                    docRec = ObtainXmlDocOfRecord(application, container, nameReceived, false);

                    var notificationController =new NotificationController();
                    notificationController.notificateEndPoint("insert", application, container, docRec);

                    return Content(HttpStatusCode.OK, docRec, Configuration.Formatters.XmlFormatter);

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

        public IHttpActionResult DeleteRecord(string application, string container, string name)
        {
            SqlConnection connection = null;

            try
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return BadRequest(verify);


                XmlDocument doc = ObtainXmlDocOfRecord(application, container, name, false);

                connection = new SqlConnection(DBConnection);
                connection.Open();

                //para garantir que nao deleta o errado

                string deleteQuery = @"
                                DELETE FROM Record
                                WHERE parent IN (
                                    SELECT c.id
                                    FROM Container c
                                    INNER JOIN Application a ON c.parent = a.id
                                    WHERE a.name = @application AND c.name = @container
                                )
                                AND name = @name";

                SqlCommand cmd = new SqlCommand(deleteQuery, connection);

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@container", container);
                cmd.Parameters.AddWithValue("@application", application);

                if (cmd.ExecuteNonQuery() != 1)
                {
                    return NotFound();
                }
                var notificationController = new NotificationController();

                notificationController.notificateEndPoint("delete", application, container, doc);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
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

        private XmlDocument ObtainXmlDocOfRecord(string application, string container, string name, bool verifyPath)
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
                                        SELECT r.* 
                                        FROM Record r
                                        INNER JOIN Container c ON r.parent = c.id
                                        INNER JOIN Application a ON c.parent = a.id
                                        WHERE a.name = @application AND c.name = @container AND r.name = @nameNot ";



                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.AddWithValue("@container", container);

                command.Parameters.AddWithValue("@nameNot", name);

                command.Parameters.AddWithValue("@application", application);


                SqlDataReader reader = command.ExecuteReader();


                XmlElement root = doc.CreateElement("Record");
                doc.AppendChild(root);



                if (reader.Read())
                {

                    root.SetAttribute("id", reader["Id"].ToString());

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    root.AppendChild(nameElement);

                    XmlElement contentElement = doc.CreateElement("content");
                    contentElement.InnerText = reader["Content"].ToString();
                    root.AppendChild(contentElement);

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

       
        private bool isNull(string s)
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