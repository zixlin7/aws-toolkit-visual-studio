using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.ElasticLoadBalancingV2.Model;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for DeleteServiceConfirmation.xaml
    /// </summary>
    public partial class DeleteServiceConfirmation
    {
        public bool CanDeleteLoadBalancer { get; set; }
        public LoadBalancer _loadBalancer;
        public bool CanDeleteListener { get; set; }
        public Listener _listener;
        public bool CanDeleteTargetGroup { get; set; }
        public TargetGroup _targetGroup;

        public DeleteServiceConfirmation()
        {
            InitializeComponent();
        }

        public DeleteServiceConfirmation(bool canDeleteLoadBalancer, LoadBalancer loadBalancer, bool canDeleteListener, Listener listener, bool canDeleteTargetGroup, TargetGroup targetGroup)
            : this()
        {
            this.CanDeleteLoadBalancer = canDeleteLoadBalancer;
            this._loadBalancer = loadBalancer;
            this.CanDeleteListener = canDeleteListener;
            this._listener = listener;
            this.CanDeleteTargetGroup = canDeleteTargetGroup;
            this._targetGroup = targetGroup;

            if (!this.CanDeleteLoadBalancer && !this.CanDeleteListener && !this.CanDeleteTargetGroup)
                this._ctlELBConfirmPanel.Visibility = Visibility.Collapsed;
            else
            {
                if (!this.CanDeleteLoadBalancer)
                    this._ctlDeleteLoadBalancer.Visibility = Visibility.Collapsed;
                else
                {
                    this._ctlDeleteLoadBalancer.IsChecked = true;
                    this._ctlDeleteLoadBalancer.Content = "Load Balancer: " + this._loadBalancer.LoadBalancerName;
                }

                if (!this.CanDeleteListener)
                    this._ctlDeleteListener.Visibility = Visibility.Collapsed;
                else
                {
                    this._ctlDeleteListener.IsChecked = true;
                    this._ctlDeleteListener.Content = "Listener: " + this._listener.Port + "(" + this._listener.Protocol + ")";
                }

                if (!this.CanDeleteTargetGroup)
                    this._ctlDeleteTargetGroup.Visibility = Visibility.Collapsed;
                else
                {
                    this._ctlDeleteTargetGroup.IsChecked = true;
                    this._ctlDeleteTargetGroup.Content = "Target Group: " + this._targetGroup.TargetGroupName;
                }
            }
        }

        public bool DeleteLoadbalancer
        {
            get;set;
        }

        public bool DeleteListener
        {
            get; set;
        }

        public bool DeleteTargetGroup
        {
            get; set;
        }
    }
}
