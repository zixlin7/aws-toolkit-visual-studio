using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public interface IEndPointSupport : IViewModel
    {
        RegionEndPointsManager.EndPoint CurrentEndPoint { get; }
        void UpdateEndPoint(string regionName);
    }
}
