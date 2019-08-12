using System;
using System.Collections.Generic;
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

        public string AlarmName => this._originalMetricAlarm.AlarmName;

        public string MetricName => this._originalMetricAlarm.MetricName;

        public string State => this._originalMetricAlarm.StateValue;

        public string Statistic => this._originalMetricAlarm.Statistic;

        public string Threshold => this._originalMetricAlarm.Threshold.ToString();

        public string Unit => this._originalMetricAlarm.Unit;

        public string ComparisonOperator => this._originalMetricAlarm.ComparisonOperator;


        //Properites below here are not shown
        public string ActionsEnabled => this._originalMetricAlarm.ActionsEnabled.ToString();

        public string Namespace => this._originalMetricAlarm.Namespace;

        public string AlarmLastUpdated => this._originalMetricAlarm.AlarmConfigurationUpdatedTimestamp.ToString();

        public string StateLastUpdated => this._originalMetricAlarm.StateUpdatedTimestamp.ToString();

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
        public string Period => this._originalMetricAlarm.Period.ToString();

        public string EvaluationPeriods => this._originalMetricAlarm.EvaluationPeriods.ToString();

        public string StateReason => this._originalMetricAlarm.StateReason;

        public string StateReasonData => this._originalMetricAlarm.StateReasonData;

        public string AlarmARN => this._originalMetricAlarm.AlarmArn;

        public string AlarmDescription => this._originalMetricAlarm.AlarmDescription;

        public string OKActions => String.Join(", ", this._originalMetricAlarm.OKActions.ToArray());

        public string AlarmActions => String.Join(", ", this._originalMetricAlarm.AlarmActions.ToArray());

        public string InsufficientDataActions => String.Join(", ", this._originalMetricAlarm.InsufficientDataActions.ToArray());
    }
}
