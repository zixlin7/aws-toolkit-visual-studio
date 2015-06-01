using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit.CloudFormation.Parser;

namespace CloudFormationEditorTests
{
    [TestClass]
    public class ParserTests
    {
        TemplateParser parser = new TemplateParser();

        [TestMethod]
        public void EmptyTemplate()
        {
            string template = getEmbeddedTemplate("empty.template");
            parser.Parse(template);
        }


        string getEmbeddedTemplate(string name)
        {
            using (var reader = new StreamReader(this.GetType().Assembly.GetManifestResourceStream("CloudFormationEditorTests.SampleTemplates." + name)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
