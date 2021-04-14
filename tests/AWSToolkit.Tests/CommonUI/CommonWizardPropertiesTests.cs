using System;
using System.Collections.Generic;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class CommonWizardPropertiesTests
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        private readonly AccountViewModel _account =
            new AccountViewModel(null, null, new SharedCredentialIdentifier("profile-name"), null);

        private readonly ToolkitRegion _region = new ToolkitRegion()
        {
            PartitionId = "aws",
            DisplayName = "US West",
            Id = "us-west-2",
        };

        [Fact]
        public void GetSelectedAccount()
        {
            _properties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] = _account;

            Assert.Equal(_account, CommonWizardProperties.AccountSelection.GetSelectedAccount(_properties));
        }

        [Fact]
        public void GetSelectedAccount_NoEntry()
        {
            Assert.Null(CommonWizardProperties.AccountSelection.GetSelectedAccount(_properties));
        }

        [Fact]
        public void GetSelectedAccount_UnexpectedType()
        {
            _properties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] = 3;

            Assert.Throws<InvalidCastException>(() => CommonWizardProperties.AccountSelection.GetSelectedAccount(_properties));
        }

        [Fact]
        public void GetSelectedRegion()
        {
            _properties[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] = _region;

            Assert.Equal(_region, CommonWizardProperties.AccountSelection.GetSelectedRegion(_properties));
        }

        [Fact]
        public void GetSelectedRegion_NoEntry()
        {
            Assert.Null(CommonWizardProperties.AccountSelection.GetSelectedRegion(_properties));
        }

        [Fact]
        public void GetSelectedRegion_UnexpectedType()
        {
            _properties[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] = 3;

            Assert.Throws<InvalidCastException>(() => CommonWizardProperties.AccountSelection.GetSelectedRegion(_properties));
        }
    }
}
