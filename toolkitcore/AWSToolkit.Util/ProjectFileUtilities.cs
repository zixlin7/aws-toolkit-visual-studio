using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
