using SOMIOD.Controllers;
using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace SOMOID.Controllers
{
    [RoutePrefix("api/somiod")]
    public class SomiodController : ApiController
    {

        [Route("{application?}/{container?}/{RecNot?}/{name?}")]
        public IHttpActionResult Get(string application = null, string container = null, string RecNot = null, string name = null)
        {
            try
            {
                if (Request.Headers.Contains("somiod-locate"))           ////////// LOCATE ////////////
                {

                    string locateValue = Request.Headers.GetValues("somiod-locate").FirstOrDefault();
                    if (isNull(RecNot))
                    {

                        switch (locateValue)
                        {
                            case "notification":

                                var notificationController = (NotificationController)createController("Notification");
                                if (!isNull(container))
                                    return notificationController.locateAllNotificationsByContainer(application, container);
                                if (!isNull(application))
                                    return notificationController.locateAllNotificationsByApplication(application);
                                return notificationController.locateAllNotifications();
                            case "record":

                                var recordController = (RecordController)createController("Record");
                                if (!isNull(container))
                                    return recordController.locateAllRecordsByContainer(application, container);
                                if (!isNull(application))
                                    return recordController.locateAllRecordsByApplication(application);
                                return recordController.locateAllRecords();

                            case "container":
                                var containerController = (ContainerController)createController("Container");
                                if (!isNull(application))
                                    return containerController.locateAllContainersByApplication(application);
                                return containerController.locateAllContainers();                            

                            case "application":
                                if (isNull(application))
                                {
                                    var applicationController = (ApplicationController)createController("Application");
                                    return applicationController.locateAllApplications();
                                }
                                break;

                        }
                    }
                }
                else                                                     ////////// GET ////////////
                {
                    if (isNull(application)) { return BadRequest(); }
                    if (isNull(container))
                    {
                        var applicationController = (ApplicationController)createController("Application");
                        return applicationController.GetApplication(application);
                    }
                    if (isNull(RecNot))
                    {
                        var containerController = (ContainerController)createController("Container");
                        return containerController.GetContainer(application, container);
                    }
                    if (RecNot.ToUpper() == "NOTIFICATION")
                    {
                        var notificationController = (NotificationController)createController("Notification");
                        return notificationController.GetNotification(application, container, name);
                    }
                    else if (RecNot.ToUpper() == "RECORD")
                    {
                        var recordController = (RecordController)createController("Record");
                        return recordController.GetRecord(application, container, name);
                    }


                }
                return BadRequest("Ups, nenhuma rota encontrada");
            }
            
             catch (Exception ex) { return InternalServerError(); }

        



        }



        [Route("{application?}/{container?}")]
        public IHttpActionResult Post([FromBody] XmlDocument xmlDocument, string application = null, string container = null)
        {
            try
            {
                if (xmlDocument == null)
                {
                    return BadRequest("Invalid XML payload.");
                }

                if (isNull(application)) {
                    var applicationController = (ApplicationController)createController("Application");
                    return applicationController.PostApplication(xmlDocument);
                }

                if (isNull(container))
                {
                    var containerController = (ContainerController)createController("Container");

                    return containerController.PostContainer(application, xmlDocument);
                }


                if (xmlDocument.DocumentElement?.Name == "Notification") //LUIS COMO EXTRA PERMITIR JSON
                {
                    var notificationController = (NotificationController)createController("Notification");

                    return notificationController.PostNotification(application, container, xmlDocument);
                }
                else if (xmlDocument.DocumentElement?.Name == "Record")
                {

                    var recordController = (RecordController)createController("Record");

                    return recordController.PostRecord(application, container, xmlDocument);


                }

                return BadRequest("Ups, nenhuma rota encontrada");
            }
            catch (Exception ex) { return InternalServerError(); }
            
        }


        [Route("{application}/{container?}/{RecNot?}/{name?}")]
        public IHttpActionResult Patch([FromBody] XmlElement xmlElement, string application, string container = null, string RecNot = null, string name = null)
        {
            if (isNull(container))
            {
                var applicationController = (ApplicationController)createController("Application");
                return applicationController.PatchApplication(application, xmlElement);
            }
            if (isNull(RecNot))
            {
                var containerController = (ContainerController)createController("Container");
                return containerController.PatchContainer(application, container, xmlElement);
            }
            if (RecNot.ToUpper() == "NOTIFICATION")
            {
                var notificationController = (NotificationController)createController("Notification");
                return notificationController.PatchNotificationEnabled(application, container, name, xmlElement);
            }
            return BadRequest("Ups, nenhuma rota encontrada");
        }



        [HttpDelete]
        [Route("{application}/{container?}/{RecNot?}/{name?}")]
        public IHttpActionResult Delete(string application, string container = null, string RecNot = null, string name = null)
        {
            if (isNull(container))
            {
                var applicationController = (ApplicationController)createController("Application");
                return applicationController.DeleteApplication(application);
            }
            if (isNull(RecNot))
            {
                var containerController = (ContainerController)createController("Container");
                return containerController.DeleteContainer(application, container);
            }
            if (RecNot.ToUpper() == "NOTIFICATION")
            {
                var notificationController = (NotificationController)createController("Notification");
                return notificationController.DeleteNotification(application, container, name);
            }
            else if (RecNot.ToUpper() == "RECORD")
            {
                var recordController = (RecordController)createController("Record");
                return recordController.DeleteRecord(application, container, name);
            }

            return BadRequest("Ups, nenhuma rota encontrada");
        }

        private object createController(string typeController)
        {
            switch (typeController)
            {
                case "Notification":
                    return new NotificationController
                    {
                        ControllerContext = new HttpControllerContext
                        {
                            Request = this.Request,
                            Configuration = this.Configuration,
                            ControllerDescriptor = new HttpControllerDescriptor(this.Configuration, "Notification", typeof(NotificationController))
                        }
                    };

                case "Record":
                    return new RecordController
                    {
                        ControllerContext = new HttpControllerContext
                        {
                            Request = this.Request,
                            Configuration = this.Configuration,
                            ControllerDescriptor = new HttpControllerDescriptor(this.Configuration, "Record", typeof(RecordController))
                        }
                    };

                case "Container":
                    return new ContainerController
                    {
                        ControllerContext = new HttpControllerContext
                        {
                            Request = this.Request,
                            Configuration = this.Configuration,
                            ControllerDescriptor = new HttpControllerDescriptor(this.Configuration, "Container", typeof(ContainerController))
                        }
                    };

                case "Application":
                    return new ApplicationController
                    {
                        ControllerContext = new HttpControllerContext
                        {
                            Request = this.Request,
                            Configuration = this.Configuration,
                            ControllerDescriptor = new HttpControllerDescriptor(this.Configuration, "Application", typeof(ApplicationController))
                        }
                    };

                default:
                    return null;
            }
        }

        private bool isNull(string s)
        {
            return string.IsNullOrEmpty(s);
        }


    }
}