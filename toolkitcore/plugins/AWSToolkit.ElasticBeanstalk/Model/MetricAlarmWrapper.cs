using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

using Amazon.ElasticBeanstalk.Model;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class MetricAlarmWrapper
    {
        MetricAlarm _originalMetricAlarm;

        public MetricAlarmWrapper(MetricAlarm originalMetricAlarm)
        {
            this._originalMetricAlarm = originalMetricAlarm;
        }

        public string AlarmName
        {
            get { return this._originalMetricAlarm.AlarmName; }
        }
        public string MetricName
        {
            get { return this._originalMetricAlarm.MetricName; }
        }
        public string State
        {
            get { return this._originalMetricAlarm.StateValue; }
        }

        public string Statistic
        {
            get { return this._originalMetricAlarm.Statistic; }
        }
        public string Threshold
        {
            get { return this._originalMetricAlarm.Threshold.ToString(); }
        }
        public string Unit
        {
            get { return this._originalMetricAlarm.Unit; }
        }
        public string ComparisonOperator
        {
            get { return this._originalMetricAlarm.ComparisonOperator; }
        }

        
        //Properites below here are not shown
        public string ActionsEnabled
        {
            get { return this._originalMetricAlarm.ActionsEnabled.ToString(); }
        }
        public string Namespace
        {
            get { return this._originalMetricAlarm.Namespace; }
        }
        public string AlarmLastUpdated
        {
            get { return this._originalMetricAlarm.AlarmConfigurationUpdatedTimestamp.ToString(); }
        }
        public string StateLastUpdated
        {
            get { return this._originalMetricAlarm.StateUpdatedTimestamp.ToString(); }
        }
        public string Dimensions
        {
            get
            {
                List<string> dimensions = new List<string>();
                foreach (Dimension dimension in this._originalMetricAlarm.Dimensions)
                {
                    dimensions.Add(String.Format("{0}:{1}", dimension.Name, dimension.Value));
                }
                return String.Join(", ", dimensions.ToArray());
            }
        }
        public string Period
        {
            get { return this._originalMetricAlarm.Period.ToString(); }
        }
        public string EvaluationPeriods
        {
            get { return this._originalMetricAlarm.EvaluationPeriods.ToString(); }
        }

        public string StateReason
        {
            get { return this._originalMetricAlarm.StateReason; }
        }
        public string StateReasonData
        {
            get { return this._originalMetricAlarm.StateReasonData; }
        }

        public string AlarmARN
        {
            get { return this._originalMetricAlarm.AlarmArn; }
        }
        public string AlarmDescription
        {
            get { return this._originalMetricAlarm.AlarmDescription; }
        }

        public string OKActions
        {
            get { return String.Join(", ", this._originalMetricAlarm.OKActions.ToArray()); }
        }
        public string AlarmActions
        {
            get { return String.Join(", ", this._originalMetricAlarm.AlarmActions.ToArray()); }
        }
        public string InsufficientDataActions
        {
            get { return String.Join(", ", this._originalMetricAlarm.InsufficientDataActions.ToArray()); }
        }
    }
}
