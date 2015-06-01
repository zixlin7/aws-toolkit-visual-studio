using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;

using AWSDeploymentHostManager;

namespace AWSDeploymentUnitTest
{
    public class TestTask : Task
    {
        // Builds a JSON object of the parameters and returns them in the response
        // if a parameter of response is present, the response field will mirror that value
        public override string Execute()
        {
            JsonData jData = new JsonData();

            foreach (string k in parameters.Keys)
            {
                jData[k] = parameters[k];
            }

            return GenerateResponse(jData);
        }

        public override string Operation
        {
            get { return "TestTask"; }
        }
    }
}
