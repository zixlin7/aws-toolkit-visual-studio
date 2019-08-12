using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.S3;

namespace Amazon.AWSToolkit.S3.Model
{
    public class LifecycleRuleModel : BaseModel, ICloneable
    {
        readonly Amazon.S3.Model.LifecycleRule rule;

        public LifecycleRuleModel()
            : this(null)
        {
        }

        public LifecycleRuleModel(Amazon.S3.Model.LifecycleRule rule)
        {
            this.rule = rule;
            if (this.rule == null)
                this.rule = new Amazon.S3.Model.LifecycleRule();
        }

        public string RuleId
        {
            get => this.rule.Id ?? "";
            set
            {
                this.rule.Id = value;
                base.NotifyPropertyChanged("RuleId");
            }
        }

        public string Prefix
        {
            get => this.rule.Prefix;
            set
            {
                this.rule.Prefix = value;
                base.NotifyPropertyChanged("Prefix");
            }
        }

        public string FormattedExpiration
        {
            get
            {
                if (this.ExpirationDays == null && this.ExpirationDate == null)
                    return "";

                var exp = this.rule.Expiration;
                if (exp.DateUtc != DateTime.MinValue)
                {
                    return exp.DateUtc.ToString("d");
                }

                return exp.Days.ToString() + " Days";
            }
        }

        public int? ExpirationDays
        {
            get
            {
                if (this.rule.Expiration == null || this.rule.Expiration.Days == 0)
                    return null;

                return this.rule.Expiration.Days;
            }
        }

        public DateTime? ExpirationDate
        {
            get
            {
                if (this.rule.Expiration == null || this.rule.Expiration.DateUtc == DateTime.MinValue)
                    return null;

                return this.rule.Expiration.DateUtc;
            }
        }

        public void SetExpiration(int? days, DateTime? date)
        {
            if (!days.HasValue && !date.HasValue)
            {
                this.rule.Expiration = null;
            }
            else
            {
                this.rule.Expiration = new Amazon.S3.Model.LifecycleRuleExpiration();
                if (days.HasValue)
                {
                    this.rule.Expiration.Days = days.GetValueOrDefault();
                }
                else
                {
                    this.rule.Expiration.DateUtc = date.GetValueOrDefault();
                }
            }

            base.NotifyPropertyChanged("ExpirationDate");
            base.NotifyPropertyChanged("ExpirationDays");
            base.NotifyPropertyChanged("FormattedExpiration");
        }

        public string FormattedTransition
        {
            get
            {
                if (this.rule.Transition == null)
                    return "";

                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("Transition to {0}", this.rule.Transition.StorageClass.ToString());

                if (this.rule.Transition.Days != 0)
                    sb.AppendFormat(" in {0} days", this.rule.Transition.Days);
                else if (this.rule.Transition.DateUtc != DateTime.MinValue)
                    sb.AppendFormat(" on {0}", this.rule.Transition.DateUtc.ToString("d"));

                return sb.ToString();
            }
        }

        public int? TransitionDays
        {
            get
            {
                if (this.rule.Transition == null || this.rule.Transition.Days == 0)
                    return null;

                return this.rule.Transition.Days;
            }
        }

        public DateTime? TransitionDate
        {
            get
            {
                if (this.rule.Transition == null || this.rule.Transition.DateUtc == DateTime.MinValue)
                    return null;

                return this.rule.Transition.DateUtc;
            }
        }

        public void SetTransition(int? days, DateTime? date)
        {
            if (!days.HasValue && !date.HasValue)
            {
                this.rule.Transition = null;
            }
            else
            {
                this.rule.Transition = new Amazon.S3.Model.LifecycleTransition();
                if (days.HasValue)
                {
                    this.rule.Transition.Days = days.GetValueOrDefault();
                }
                else
                {
                    this.rule.Transition.DateUtc = date.GetValueOrDefault();
                }

                this.rule.Transition.StorageClass = S3StorageClass.Glacier;
            }

            base.NotifyPropertyChanged("TransitionDate");
            base.NotifyPropertyChanged("TransitionDays");
            base.NotifyPropertyChanged("FormattedTransition");
        }

        public string Status
        {
            get => this.rule.Status.ToString();
            set
            {
                if (string.Equals("Enabled", value, StringComparison.InvariantCultureIgnoreCase))
                    this.rule.Status = LifecycleRuleStatus.Enabled;
                else
                    this.rule.Status = LifecycleRuleStatus.Disabled;

                base.NotifyPropertyChanged("Status");
            }
        }

        public static bool IsDifferent(IList<LifecycleRuleModel> original, IList<LifecycleRuleModel> lifecyle)
        {
            if (original.Count != lifecyle.Count)
                return false;

            for (int i = 0; i < original.Count; i++)
            {
                if (!string.Equals(original[i].RuleId, lifecyle[i].RuleId))
                    return false;
                if (!string.Equals(original[i].Prefix, lifecyle[i].Prefix))
                    return false;
                if (!string.Equals(original[i].Status, lifecyle[i].Status))
                    return false;
                if (!string.Equals(original[i].FormattedExpiration, lifecyle[i].FormattedExpiration))
                    return false;
                if (!string.Equals(original[i].FormattedTransition, lifecyle[i].FormattedTransition))
                    return false;
            }

            return true;
        }

        public object Clone()
        {
            var newRule = ConvertToRule();
            return new LifecycleRuleModel(newRule);
        }

        public Amazon.S3.Model.LifecycleRule ConvertToRule()
        {
            var newRule = new Amazon.S3.Model.LifecycleRule();
            newRule.Id = this.rule.Id;
            newRule.Prefix = this.rule.Prefix;
            newRule.Status = this.rule.Status;

            if (this.rule.Expiration != null)
            {
                newRule.Expiration = new Amazon.S3.Model.LifecycleRuleExpiration();
                if (this.rule.Expiration.DateUtc != DateTime.MinValue)
                    newRule.Expiration.DateUtc = this.rule.Expiration.DateUtc;
                if (this.rule.Expiration.Days != 0)
                    newRule.Expiration.Days = this.rule.Expiration.Days;
            }

            if (this.rule.Transitions.FirstOrDefault() != null)
            {
                newRule.Transitions[0] = new Amazon.S3.Model.LifecycleTransition();
                if (this.rule.Transitions[0].DateUtc != DateTime.MinValue)
                    newRule.Transitions[0].DateUtc = this.rule.Transitions[0].DateUtc;
                if (this.rule.Transitions[0].Days != 0)
                    newRule.Transitions[0].Days = this.rule.Transitions[0].Days;
                newRule.Transitions[0].StorageClass = this.rule.Transitions[0].StorageClass;
            }

            return newRule;
        }

        public void Validate()
        {
        }
    }
}
