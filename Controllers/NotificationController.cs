using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using System;
using SOMIOD.Models;
using System.Xml;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Xml.Schema;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using System.Threading;
using SOMIOD.Controllers;

namespace SOMIOD.Controllers
{

    public class NotificationController : ApiController
    {

        private string DBConnection = ConfigurationManager.ConnectionStrings["Database"].ConnectionString; //Definir conexão


        public int notificateEndPoint(string RecEvent, string applicationName, string containerName, XmlDocument xmlRecord) //funcao para notificar o endpoit
        { 
            int iEvent = -1;

            if (RecEvent == "insert") {
                iEvent = 1;
            }
            else if (RecEvent == "delete") {
                iEvent = 2;

            }
            else
            {
                return iEvent;
            }
            SqlConnection conn = new SqlConnection(DBConnection); ;
            conn.Open();

            //VERIFICAR SE DENTRO DAQUELE CONTAINER ESTA ALGUMA NOTIFICACAO ATIVA PARA O EVENTO PEDIDO, SE TIVER ENVIO A NOTIFICACAO PARA O ENDPOINT
            string query = @"
                                SELECT n.* 
                                FROM Notification n
                                INNER JOIN Container c ON n.parent = c.id
                                INNER JOIN Application a ON c.parent = a.id
                                WHERE a.name = @application and n.event = @iEvent and n.enabled = 1
                                ORDER BY n.id";

            SqlCommand command = new SqlCommand(query, conn);
            command.Parameters.AddWithValue("@application", applicationName);
            command.Parameters.AddWithValue("@container", containerName);
            command.Parameters.AddWithValue("@iEvent", iEvent);

            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {

                string endpoint = reader["endpoint"].ToString();

                XmlDocument sendDoc = new XmlDocument();

                XmlNode recordElem = xmlRecord.SelectSingleNode("//Record");

                
                XmlElement root = sendDoc.CreateElement("NotificationTrigger"); // Ou outro nome de nó raiz apropriado
                sendDoc.AppendChild(root);

                
                XmlNode importedRecord = sendDoc.ImportNode(recordElem, true);

                root.AppendChild(importedRecord);

                XmlElement evento = sendDoc.CreateElement("Event");
                evento.InnerText = RecEvent;

                root.AppendChild(evento);





                //enviar para o endpoint o respetivo evento, e o record
                if (endpoint.StartsWith("mqtt://")) //significa que vamos publicar no canal api/somiod/applicationXX/containerYY
                  {
                    endpoint = endpoint.Substring(7);
                    string strChannel = $"api/somiod/{applicationName}/{containerName}";

                    try
                    {
                        MqttClient m_cClient = new MqttClient(endpoint);

                        m_cClient.Connect(Guid.NewGuid().ToString());
                        if (!m_cClient.IsConnected)
                        {
                            Console.WriteLine("erro ao conectar ao broker MQTT.");
                        }
                        string strMsgToSend = sendDoc.OuterXml; ;
                        m_cClient.MqttMsgPublished += (sender, e) => {
                            m_cClient.Disconnect();
                        };
                        m_cClient.Publish(strChannel, Encoding.UTF8.GetBytes(strMsgToSend));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro: {ex.Message}");
                    }
                }
                else{
                    using (var client = new HttpClient())
                    {
                        string xmlMessage = sendDoc.OuterXml;
                        var content = new StringContent(xmlMessage, Encoding.UTF8, "application/xml");

                       
                        try
                        {
                            var response = client.PostAsync(endpoint, content).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                Console.WriteLine("Mensagem enviada com sucesso!");
                            }
                            else
                            {
                                Console.WriteLine($"Erro ao enviar mensagem: {response.StatusCode}");
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.WriteLine($"Erro ao enviar a requisição: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro inesperado: {ex.Message}");
                        }
                    }

                }


            }
            //procurar dentro desta application e container todas as notificacoes com o mesmo evento e se estao ativas
            //se tiver mando a notificacao para esses endpoints

            return iEvent;
        }
        public IHttpActionResult locateAllNotifications() //localizar todas as notificacoes do sistema (devolver apenas uma lista com os nomes)
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
                                SELECT n.* 
                                FROM Notification n
                                ORDER BY n.id";



                SqlCommand command = new SqlCommand(query, conn);

                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("notifications");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement notificationElement = doc.CreateElement("notification");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    notificationElement.AppendChild(nameElement);


                    root.AppendChild(notificationElement);
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


        public IHttpActionResult locateAllNotificationsByApplication(string application)
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
                                SELECT n.* 
                                FROM Notification n
                                INNER JOIN Container c ON n.parent = c.id
                                INNER JOIN Application a ON c.parent = a.id
                                WHERE a.name = @application 
                                ORDER BY n.id";



                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@application", application);

                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("notifications");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement notificationElement = doc.CreateElement("notification");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    notificationElement.AppendChild(nameElement);


                    root.AppendChild(notificationElement);
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


        public IHttpActionResult locateAllNotificationsByContainer(string application, string container)
        {
            SqlConnection conn = null;

            var verify = verifyPathUrl(application, container);
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
                        SELECT n.* 
                        FROM Notification n
                        INNER JOIN Container c ON n.parent = c.id
                        INNER JOIN Application a ON c.parent = a.id
                        WHERE a.name = @application AND c.name = @container
                        ORDER BY n.id";



                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@application", application);
                command.Parameters.AddWithValue("@container", container);


                SqlDataReader reader = command.ExecuteReader();

                XmlElement root = doc.CreateElement("notifications");
                doc.AppendChild(root);

                while (reader.Read())
                {
                    XmlElement notificationElement = doc.CreateElement("notification");

                    XmlElement nameElement = doc.CreateElement("name");
                    nameElement.InnerText = reader["Name"].ToString();
                    notificationElement.AppendChild(nameElement);


                    root.AppendChild(notificationElement);
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



        

   
        public IHttpActionResult GetNotification(string application, string container, string name)
        {
            XmlDocument docNot = new XmlDocument();

            try
            {
                docNot = ObtainXmlDocOfNotification(application, container, name, true);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            if (docNot == null)
            {
                return NotFound();
            }
            return Content(HttpStatusCode.OK, docNot, Configuration.Formatters.XmlFormatter);
        }


        public IHttpActionResult PatchNotificationEnabled(string application, string container, string name, [FromBody] XmlElement xmlElementEn)
        {
            SqlConnection conn = null;


            try
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return BadRequest(verify);

                bool enabled = Convert.ToBoolean(xmlElementEn.InnerText);


                if (xmlElementEn == null)
                {
                    return BadRequest("Invalid XML payload.");
                }

                

                conn = new SqlConnection(DBConnection);
                conn.Open();




                string updateNotificationQuery = @"
                    UPDATE Notification 
                    SET enabled = @enabled 
                    WHERE name = @name 
                      AND parent = (SELECT id FROM Container WHERE name = @container)";
                SqlCommand command = new SqlCommand(updateNotificationQuery, conn);

                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@container", container);
                command.Parameters.AddWithValue("@enabled", enabled);

                int nRows = command.ExecuteNonQuery();
                conn.Close();

                if (nRows > 0)
                {
                    XmlDocument docNot = new XmlDocument();
                    docNot = ObtainXmlDocOfNotification(application, container, name, false);
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




        public IHttpActionResult PostNotification(string application, string container, [FromBody] XmlDocument xmlDocumentNot)
        {
            SqlConnection conn = null;

            try
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return BadRequest(verify);

                XmlNode notificationNode = xmlDocumentNot.SelectSingleNode("//Notification");
                if (notificationNode == null)
                {
                    return BadRequest("Missing Notification element.");
                }

                string schemaPath = HttpContext.Current.Server.MapPath("~/XML/SystemSchema.xsd");


                XmlSchemaSet schemas = new XmlSchemaSet();
                schemas.Add("", schemaPath);

                xmlDocumentNot.Schemas = schemas;
                xmlDocumentNot.Validate((sender, e) =>
                {
                    if (e.Severity == XmlSeverityType.Error)
                    {
                        throw new InvalidOperationException($"{e.Message}");
                    }
                });

                conn = new SqlConnection(DBConnection);
                conn.Open();
                int nRows = 1;

                int eventCode = int.Parse(notificationNode["event"].InnerText );
                string endpoint = notificationNode["endpoint"].InnerText;
                bool enabled = bool.Parse(notificationNode["enabled"].InnerText);


                string nameReceived = null;
                XmlNode nameNode = notificationNode["name"];

                if (nameNode != null)
                {
                    nameReceived = SanitizeForUrl(nameNode.InnerText);  // Para evitar (por exemplo espaços no nome) uma vez que depois nos get/delete o nome com espaços fica invalido no url
                    string queryNameExiste = @"Select COUNT(*) from Notification where name = @nameReceive";
                    SqlCommand vCommand = new SqlCommand(queryNameExiste, conn);
                    vCommand.Parameters.AddWithValue("@nameReceive", nameReceived);


                    nRows = (int)vCommand.ExecuteScalar();
                }

                
                if (nRows > 0)
                {
                    nameReceived = $"notif-{Guid.NewGuid()}";
                }


               

                string insertNotificationQuery = @"
                    INSERT INTO Notification (name, creation_datetime, parent, event, endpoint, enabled) 
                    VALUES (
                        @newName, 
                        GETDATE(), 
                        (SELECT id FROM Container WHERE name = @newParent), 
                        @newEvent, 
                        @newEndpoint, 
                        @newEnabled
                    )";

                SqlCommand insertCommand = new SqlCommand(insertNotificationQuery, conn);
                insertCommand.Parameters.AddWithValue("@newName", nameReceived);
                insertCommand.Parameters.AddWithValue("@newParent", container);
                insertCommand.Parameters.AddWithValue("@newEvent", eventCode);
                insertCommand.Parameters.AddWithValue("@newEndpoint", endpoint);
                insertCommand.Parameters.AddWithValue("@newEnabled", enabled);

                nRows = insertCommand.ExecuteNonQuery();
                conn.Close();

                if (nRows > 0)
                {
                    XmlDocument docNot = new XmlDocument();

                 
                       docNot = ObtainXmlDocOfNotification(application, container, nameReceived, false);

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




        public IHttpActionResult DeleteNotification(string application, string container, string name)
        {
            SqlConnection connection = null;
            try
            {
                var verify = verifyPathUrl(application, container);
                if (verify != "OK")
                    return BadRequest(verify);

                connection = new SqlConnection(DBConnection);
                connection.Open();

                //para garantir que nao deleta o errado
                SqlCommand cmd = new SqlCommand(@"
                    DELETE FROM Notification
                    WHERE parent IN (
                        SELECT c.id
                        FROM Container c
                        INNER JOIN Application a ON c.parent = a.id
                        WHERE a.name = @application AND c.name = @container
                    )
                    AND name = @name", connection);

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@container", container);
                cmd.Parameters.AddWithValue("@application", application);

                if (cmd.ExecuteNonQuery() != 1)
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
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }


        private string verifyPathUrl(string application, string container = null) // apenas verificar se o container exist dentre daquela applicacao
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
        private XmlDocument ObtainXmlDocOfNotification(string application, string container, string name, bool verifyPath)
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
                                        SELECT n.* 
                                        FROM Notification n
                                        INNER JOIN Container c ON n.parent = c.id
                                        INNER JOIN Application a ON c.parent = a.id
                                        WHERE a.name = @application AND c.name = @container AND n.name = @nameNot ";



                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.AddWithValue("@container", container);

                command.Parameters.AddWithValue("@nameNot", name);

                command.Parameters.AddWithValue("@application", application);


                SqlDataReader reader = command.ExecuteReader();


                XmlElement root = doc.CreateElement("Notification");
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

                    XmlElement eventElement = doc.CreateElement("event");
                    eventElement.InnerText = reader["Event"].ToString();
                    root.AppendChild(eventElement);

                    XmlElement endpointElement = doc.CreateElement("endpoint");
                    endpointElement.InnerText = reader["Endpoint"].ToString();
                    root.AppendChild(endpointElement);

                    XmlElement enabledElement = doc.CreateElement("enabled");
                    enabledElement.InnerText = ((bool)reader["Enabled"]).ToString().ToLower();
                    root.AppendChild(enabledElement);


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