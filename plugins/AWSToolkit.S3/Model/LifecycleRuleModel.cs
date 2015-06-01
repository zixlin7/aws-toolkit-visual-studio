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
        Amazon.S3.Model.LifecycleRule _rule;

        public LifecycleRuleModel()
            : this(null)
        {
        }

        public LifecycleRuleModel(Amazon.S3.Model.LifecycleRule rule)
        {
            this._rule = rule;
            if (this._rule == null)
                this._rule = new Amazon.S3.Model.LifecycleRule();
        }

        public string RuleId
        {
            get 
            {
                if (this._rule.Id == null)
                    return "";

                return this._rule.Id; 
            }
            set
            {
                this._rule.Id = value;
                base.NotifyPropertyChanged("RuleId");
            }
        }

        public string Prefix
        {
            get { return this._rule.Prefix; }
            set
            {
                this._rule.Prefix = value;
                base.NotifyPropertyChanged("Prefix");
            }
        }

        public string FormattedExpiration
        {
            get
            {
                if (this.ExpirationDays == null && this.ExpirationDate == null)
                    return "";

                var exp = this._rule.Expiration;
                if (exp.Date != DateTime.MinValue)
                    return exp.Date.ToString("d");

                return exp.Days.ToString() + " Days";
            }
        }

        public int? ExpirationDays
        {
            get
            {
                if (this._rule.Expiration == null || this._rule.Expiration.Days == 0)
                    return null;

                return this._rule.Expiration.Days;
            }
        }

        public DateTime? ExpirationDate
        {
            get
            {
                if (this._rule.Expiration == null || this._rule.Expiration.Date == DateTime.MinValue)
                    return null;

                return this._rule.Expiration.Date;
            }
        }

        public void SetExpiration(int? days, DateTime? date)
        {
            if (!days.HasValue && !date.HasValue)
            {
                this._rule.Expiration = null;
            }
            else
            {
                this._rule.Expiration = new Amazon.S3.Model.LifecycleRuleExpiration();
                if (days.HasValue)
                {
                    this._rule.Expiration.Days = days.GetValueOrDefault();
                }
                else
                {
                    this._rule.Expiration.Date = date.GetValueOrDefault();
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
                if (this._rule.Transition == null)
                    return "";

                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("Transition to {0}", this._rule.Transition.StorageClass.ToString());

                if (this._rule.Transition.Days != 0)
                    sb.AppendFormat(" in {0} days", this._rule.Transition.Days);
                else if (this._rule.Transition.Date != DateTime.MinValue)
                    sb.AppendFormat(" on {0}", this._rule.Transition.Date.ToString("d"));

                return sb.ToString();
            }
        }

        public int? TransitionDays
        {
            get
            {
                if (this._rule.Transition == null || this._rule.Transition.Days == 0)
                    return null;

                return this._rule.Transition.Days;
            }
        }

        public DateTime? TransitionDate
        {
            get
            {
                if (this._rule.Transition == null || this._rule.Transition.Date == DateTime.MinValue)
                    return null;

                return this._rule.Transition.Date;
            }
        }

        public void SetTransition(int? days, DateTime? date)
        {
            if (!days.HasValue && !date.HasValue)
            {
                this._rule.Transition = null;
            }
            else
            {
                this._rule.Transition = new Amazon.S3.Model.LifecycleTransition();
                if (days.HasValue)
                {
                    this._rule.Transition.Days = days.GetValueOrDefault();
                }
                else
                {
                    this._rule.Transition.Date = date.GetValueOrDefault();
                }

                this._rule.Transition.StorageClass = S3StorageClass.Glacier;
            }

            base.NotifyPropertyChanged("TransitionDate");
            base.NotifyPropertyChanged("TransitionDays");
            base.NotifyPropertyChanged("FormattedTransition");
        }

        public string Status
        {
            get { return this._rule.Status.ToString(); }
            set
            {
                if (string.Equals("Enabled", value, StringComparison.InvariantCultureIgnoreCase))
                    this._rule.Status = LifecycleRuleStatus.Enabled;
                else
                    this._rule.Status = LifecycleRuleStatus.Disabled;

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
            newRule.Id = this._rule.Id;
            newRule.Prefix = this._rule.Prefix;
            newRule.Status = this._rule.Status;

            if (this._rule.Expiration != null)
            {
                newRule.Expiration = new Amazon.S3.Model.LifecycleRuleExpiration();
                if (this._rule.Expiration.Date != DateTime.MinValue)
                    newRule.Expiration.Date = this._rule.Expiration.Date;
                if (this._rule.Expiration.Days != 0)
                    newRule.Expiration.Days = this._rule.Expiration.Days;
            }

            if (this._rule.Transition != null)
            {
                newRule.Transition = new Amazon.S3.Model.LifecycleTransition();
                if (this._rule.Transition.Date != DateTime.MinValue)
                    newRule.Transition.Date = this._rule.Transition.Date;
                if (this._rule.Transition.Days != 0)
                    newRule.Transition.Days = this._rule.Transition.Days;
                newRule.Transition.StorageClass = this._rule.Transition.StorageClass;
            }

            return newRule;
        }

        public void Validate()
        {
        }
    }
}
