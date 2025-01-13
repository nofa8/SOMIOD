using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;

namespace XMLHandler
{
    class HandlerXML
    {
        public HandlerXML(string xmlFile)
        {
            XmlFilePath = xmlFile;
        }

        public HandlerXML(string xmlFile, string xsdFile)
        {
            XmlFilePath = xmlFile;
            XsdFilePath = xsdFile;
        }

        public string XmlFilePath { get; set; }
        public string XsdFilePath { get; set; }

        private bool isValid = true;
        private string validationMessage;
        public string ValidationMessage
        {
            get { return validationMessage; }
        }

        //**********************************************
        // Ex. 7
        //**********************************************
        public List<string> GetTitles()
        {
            if (isValid != true)
            {
                return null;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(XmlFilePath);
                List<string> titles = new List<string>();
                foreach (XmlNode node in doc.SelectNodes("//book/title"))
                {
                    titles.Add(node.InnerText);
                }
                return titles;
            }
            catch (XmlException ex)
            {
                return null;
            }
        }
        //**********************************************
        // Ex. 8
        //**********************************************       
        public void UpdateAuthorByTitle(string title, string author)
        {
            if (isValid != true)
            {
                validationMessage = "Erro, xml não válido";
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(XmlFilePath);
                XmlNode node = doc.SelectNodes("//book[title='" + title + "' ]/author")[0];
                node.InnerText = author;
                doc.Save(XmlFilePath);
                validationMessage = "Mudança com sucesso!";
            }
            catch (XmlException ex)
            {
                validationMessage = "Erro alteração não ocurreu";
                return;
            }
        }

        //**********************************************
        // Ex. 9
        //**********************************************  
        public void AddRateToBook(string title, string rate)
        {
            if (isValid != true)
            {
                validationMessage = "Erro, xml não válido";
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(XmlFilePath);
                XmlNodeList nodes = doc.SelectNodes("//book[title='" + title + "' ]");
                foreach (XmlNode node in nodes)
                {
                    if (node.Attributes["rate"] == null)
                    {
                        XmlElement elem = doc.CreateElement("rate");
                        elem.InnerText = rate;
                        node.AppendChild(elem);
                    }
                    else
                    {
                        node["rate"].InnerText = rate;
                    }
                }

                doc.Save(XmlFilePath);
                validationMessage = "Mudança com sucesso!";
            }
            catch (XmlException ex)
            {
                validationMessage = "Erro alteração não ocurreu";
                return;
            }
        }
        //**********************************************
        // Ex. 10 Add Attribute
        //**********************************************  
        public void AddAttributeISBNToBook(string title, string isbn)
        {
            if (isValid != true)
            {
                validationMessage = "Erro, xml não válido";
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(XmlFilePath);

                if (doc.SelectNodes("//book[isbn='" + isbn + "' ]").Count > 0)
                {
                    validationMessage = "Erro alteração não ocurreu, isbn repetido!";
                    return;
                }

                XmlNodeList nodes = doc.SelectNodes("//book[title='" + title + "' ]");


                foreach (XmlNode node in nodes)
                {
                    if (node.Attributes["isbn"] == null)
                    {
                        XmlElement elem = doc.CreateElement("isbn");
                        elem.InnerText = isbn;
                        node.AppendChild(elem);
                    }
                    else
                    {
                        node["isbn"].InnerText = isbn;
                    }
                }

                doc.Save(XmlFilePath);
                validationMessage = "Mudança com sucesso!";
            }
            catch (XmlException ex)
            {
                validationMessage = "Erro alteração não ocurreu";
                return;
            }
        }

        #region Ex. 6 - Validate XML with XML Schema (xsd)
        public bool ValidateXML()
        {
            isValid = true;
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(XmlFilePath);
                ValidationEventHandler eventHandler = new ValidationEventHandler(MyValidateMethod); // == v-on:problema?
                doc.Schemas.Add(null, XsdFilePath);
                doc.Validate(eventHandler);
            }
            catch (XmlException ex)
            {
                isValid = false;
                validationMessage = string.Format("ERROR: {0}", ex.ToString());
            }
            return isValid;
        }

        private void MyValidateMethod(object sender, ValidationEventArgs args)
        {
            isValid = false;
            switch (args.Severity)
            {
                case XmlSeverityType.Error:
                    validationMessage = string.Format("ERROR: {0}", args.Message);
                    break;
                case XmlSeverityType.Warning:
                    validationMessage = string.Format("WARNING: {0}", args.Message);
                    break;
                default:
                    break;
            }
        }
        #endregion


    }
}
