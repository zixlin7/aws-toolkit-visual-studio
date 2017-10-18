using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.ECS.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using System.Windows.Media;
using System.Windows;

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

        [DisplayName("DeploymentMinimumHealthyPercent")]
        public int? DeploymentMinimumHealthyPercent
        {
            get
            {
                if (this._service.DeploymentConfiguration == null)
                    return null;

                return this._service.DeploymentConfiguration.MinimumHealthyPercent;
            }
        }

        [DisplayName("DeploymentMaximumPercent")]
        public int? DeploymentMaximumPercent
        {
            get
            {
                if (this._service.DeploymentConfiguration == null)
                    return null;

                return this._service.DeploymentConfiguration.MaximumPercent;
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
        public string TaskDefinitionName
        {
            get
            {
                return _service.TaskDefinition.Substring(this._service.TaskDefinition.LastIndexOf('/') + 1);
            }
        }

        public Visibility ShowNoLoadBalancerText
        {
            get { return ShowLoadBalancerPanel == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility ShowLoadBalancerPanel
        {
            get
            {
                if (this._service.LoadBalancers.Count == 0)
                    return Visibility.Collapsed;

                return Visibility.Visible;
            }
        }

        public Visibility ShowLoadBalancerName => LoadBalancerName != null ? Visibility.Visible : Visibility.Collapsed;

        public string LoadBalancerName
        {
            get
            {
                if (ShowLoadBalancerPanel == Visibility.Collapsed)
                    return null;

                return this._service.LoadBalancers[0].LoadBalancerName;
            }
        }

        public Visibility ShowTargetGroupName => TargetGroupName != null ? Visibility.Visible : Visibility.Collapsed;
        public string TargetGroupName
        {
            get
            {
                if (ShowLoadBalancerPanel == Visibility.Collapsed)
                    return null;

                var targetArn = this._service.LoadBalancers[0].TargetGroupArn;
                if (string.IsNullOrEmpty(targetArn))
                    return null;

                // Target Group ARNs format: arn:aws:elasticloadbalancing:us-east-2:626492997873:targetgroup/TeamDemo/95f4adac60376451
                int endPos = targetArn.LastIndexOf('/');
                int startPos = targetArn.LastIndexOf('/', endPos - 1) + 1;

                return targetArn.Substring(startPos, endPos - startPos);
            }
        }

        public string TargetGroupArn
        {
            get
            {
                if (ShowLoadBalancerPanel == Visibility.Collapsed)
                    return null;

                return this._service.LoadBalancers[0].TargetGroupArn;
            }
        }

        public Visibility ShowLoadBalancedContainerName => LoadBalancedContainerName != null ? Visibility.Visible : Visibility.Collapsed;
        public string LoadBalancedContainerName
        {
            get
            {
                if (ShowLoadBalancerPanel == Visibility.Collapsed)
                    return null;

                return this._service.LoadBalancers[0].ContainerName;
            }
        }

        public Visibility ShowLoadBalancedContainerPort => LoadBalancedContainerPort != null ? Visibility.Visible : Visibility.Collapsed;
        public int? LoadBalancedContainerPort
        {
            get
            {
                if (ShowLoadBalancerPanel == Visibility.Collapsed)
                    return null;

                return this._service.LoadBalancers[0].ContainerPort;
            }
        }

        Amazon.ElasticLoadBalancingV2.Model.LoadBalancer _loadBalancer;
        Amazon.ElasticLoadBalancingV2.Model.Listener _listener;
        Amazon.ElasticLoadBalancingV2.Model.TargetGroup _targetGroup;

        public void UploadLoadBalancerInfo(string loadBalancerUrl, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer loadBalancer, Listener listener, TargetGroup targetGroup)
        {
            this._loadBalancerUrl = loadBalancerUrl;
            this._loadBalancer = loadBalancer;
            this._listener = listener;
            this._targetGroup = targetGroup;

            StringBuilder healthBuilder = new StringBuilder();
            healthBuilder.AppendFormat("{0}://{1}", listener.Protocol.ToString().ToLower(), loadBalancer.DNSName.ToLower());
            if (listener.Port != 80)
                healthBuilder.AppendFormat(":{0}", listener.Port);

            healthBuilder.Append(targetGroup.HealthCheckPath);
            _healthCheckUrl = healthBuilder.ToString();

            NotifyPropertyChanged("LoadBalancerUrl");
            NotifyPropertyChanged("LoadBalancerHealthCheck");
            NotifyPropertyChanged("LoadBalancerStatus");
            NotifyPropertyChanged("LoadBalancerStatusHealthColor");
        }

        [DisplayName("LoadBalancerStatus")]
        public string LoadBalancerStatus
        {
            get
            {
                if (this._loadBalancer == null)
                    return null;

                return this._loadBalancer.State.Code.ToString().ToUpper();
            }
        }

        public SolidColorBrush LoadBalancerStatusHealthColor
        {
            get
            {
                if (this._loadBalancer == null)
                    return null;

                Color clr;
                if (this._loadBalancer.State.Code == LoadBalancerStateEnum.Active)
                    clr = Colors.Green;
                else if (this._loadBalancer.State.Code == LoadBalancerStateEnum.Active_impaired)
                    clr = Colors.Green;
                else if (this._loadBalancer.State.Code == LoadBalancerStateEnum.Provisioning)
                    clr = Colors.Blue;
                else if (this._loadBalancer.State.Code == LoadBalancerStateEnum.Failed)
                    clr = Colors.Red;
                else
                {
                    if(ThemeUtil.GetCurrentTheme() == VsTheme.Dark)
                        clr = Colors.WhiteSmoke;
                    else
                        clr = Colors.Black;
                }

                return new SolidColorBrush(clr);
            }
        }

        private string _loadBalancerUrl;
        [DisplayName("LoadBalancerUrl")]
        public string LoadBalancerUrl
        {
            get
            {
                return _loadBalancerUrl;
            }
        }

        private string _healthCheckUrl;
        [DisplayName("LoadBalancerHealthCheck")]
        public string LoadBalancerHealthCheck
        {
            get
            {
                return _healthCheckUrl;
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
