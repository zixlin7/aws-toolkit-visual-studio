using System.IO;
using System.Linq;
using System.Xml;

namespace Amazon.AWSToolkit
{
    public static class ProjectFileUtilities
    {
        /// <summary>
        /// The Lambda project id specified in the AWSProjectType property. This identifies a project file as being a project
        /// that can be deployed to Lambda.
        /// </summary>
        public const string LAMBDA_PROJECT_TYPE_ID = "Lambda";


        public static bool IsProjectType(string projectfileContent, string type)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(projectfileContent);
            var projectTypeNode = xmlDoc.SelectSingleNode("//PropertyGroup/AWSProjectType") as XmlElement;
            if (projectTypeNode != null && !string.IsNullOrEmpty(projectTypeNode.InnerText))
            {
                var tokens = projectTypeNode.InnerText.Split(';');
                if (tokens.Contains(type))
                {
                    return true;
                }
            }

            return false;
        }

        public static string SetAWSProjectType(string projectFileContent, string projectType)
        {
            bool projectChanged = false;
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(projectFileContent);
            var projectTypeElement = xmlDoc.SelectSingleNode("//PropertyGroup/AWSProjectType");

            if(projectTypeElement == null)
            {
                projectTypeElement = xmlDoc.CreateElement("AWSProjectType");
                projectTypeElement.AppendChild(xmlDoc.CreateTextNode(LAMBDA_PROJECT_TYPE_ID));

                var propertyGroup = xmlDoc.SelectSingleNode("//PropertyGroup");
                propertyGroup.AppendChild(projectTypeElement);

                projectChanged = true;
            }

            if(projectChanged)
            {
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = true
                }))
                {
                    xmlDoc.WriteTo(writer);
                }

                return stringWriter.ToString();
            }

            return projectFileContent;
        }
    }
}
