using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class CreateTaskDefinitionController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            return new ActionResults().WithSuccess(true);
        }
    }
}
