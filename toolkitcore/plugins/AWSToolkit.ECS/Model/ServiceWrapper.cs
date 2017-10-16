using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.ECS.Model;
using System.Windows.Media;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ServiceWrapper : PropertiesModel, IWrapper
    {
        private Service _service;

        public ServiceWrapper(Service service)
        {
            _service = service;
        }

        public void LoadFrom(Service service)
        {
            _service = service;
            NotifyPropertyChanged("");
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Service";
            componentName = this._service.ServiceName;
        }

        [DisplayName("Name")]
        public string ServiceName
        {
            get
            {
                return _service.ServiceName;
            }
        }

        [DisplayName("ServiceArn")]
        public string ServiceArn
        {
            get
            {
                return _service.ServiceArn;
            }
        }

        [DisplayName("RoleArn")]
        public string RoleArn
        {
            get
            {
                return _service.RoleArn;
            }
        }

        [DisplayName("RoleName")]
        public string RoleName
        {
            get
            {
                return _service.RoleArn.Substring(_service.RoleArn.IndexOf('/') + 1);
            }
        }

        [DisplayName("Status")]
        public string Status
        {
            get
            {
                return _service.Status;
            }
        }

        public SolidColorBrush StatusHealthColor
        {
            get
            {
                Color clr;
                switch (this.Status)
                {
                    case "ACTIVE":
                        clr = Colors.Green;
                        break;

                    case "DRAINING":
                    case "INACTIVE":
                        clr = Colors.Blue;
                        break;

                    default:
                        clr = ThemeUtil.GetCurrentTheme() == VsTheme.Dark
                            ? Colors.White
                            : new Color() { A = 255 };
                        break;
                }

                return new SolidColorBrush(clr);
            }
        }

        [DisplayName("RunningCount")]
        public int RunningCount
        {
            get
            {
                return _service.RunningCount;
            }
        }

        [DisplayName("PendingCount")]
        public int PendingCount
        {
            get
            {
                return _service.PendingCount;
            }
        }

        [DisplayName("DesiredCount")]
        public int DesiredCount
        {
            get
            {
                return _service.DesiredCount;
            }
        }

        [DisplayName("CreatedAt")]
        public DateTime CreatedAt
        {
            get
            {
                return _service.CreatedAt;
            }
        }

        [DisplayName("TaskDefinition")]
        public string TaskDefinition
        {
            get
            {
                return _service.TaskDefinition;
            }
        }



        [Browsable(false)]
        public string TypeName
        {
            get { return "Service"; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get
            {
                return this.ServiceName;
            }
        }
    }
}
