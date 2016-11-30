/*
 * Copyright 2010-2011 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TemplateWizard
{
    public partial class UserInputForm : Form
    {
        readonly AccountsModel _accountsModel;

        public UserInputForm()
        {
            InitializeComponent();

            _accountsModel = new AccountsModel();

            accountSelectorComboBox.Items.AddRange(_accountsModel.Accounts);
            SelectedAccount = Account.Empty;

            if (_accountsModel.Count > 0)
            {
                storedAccountRadioButton.Checked = true;
                Account accountToSelect = _accountsModel.LastUsed;
                if (accountToSelect == null)
                {
                    accountToSelect = _accountsModel[0];
                }
                accountSelectorComboBox.SelectedItem = accountToSelect;
            }
            else
            {
                storedAccountRadioButton.Enabled = false;
            }

            accountSelectionChanged(this, null);

            RegionEndPointsManager manager = new RegionEndPointsManager();
            IList<RegionEndPointsManager.RegionEndpoint> regions = manager.GetRegions();
            foreach (var region in regions)
            {
                regionSelectorComboBox.Items.Add(region);
            }


            RegionEndPointsManager.RegionEndpoint defaultRegion = regions.FirstOrDefault(x => x.SystemName == "us-west-2");
            if (defaultRegion == null && regions.Count > 0)
                defaultRegion = regions[0];

            regionSelectorComboBox.SelectedItem = SelectedRegion = defaultRegion;
        }

        public Account SelectedAccount { get; private set; }
        public RegionEndPointsManager.RegionEndpoint SelectedRegion { get; private set; }

        private Account GetEnteredAccount()
        {
            if (storedAccountRadioButton.Checked)
            {
                return accountSelectorComboBox.SelectedItem as Account;
            }
            else
            {
                return new Account(Guid.NewGuid().ToString())
                {
                    Name = this.displayNameBox.Text,
                    AccessKey = this.accessKeyBox.Text,
                    SecretKey = this.secretKeyBox.Text,
                    Number = this.accountNumberBox.Text,
                    IsGovCloudAccount = this.isGovCloudAccount.Checked
                };
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            SelectedAccount = GetEnteredAccount();
            SelectedRegion = regionSelectorComboBox.SelectedItem as RegionEndPointsManager.RegionEndpoint;

            // If current account is not valid (any of the required fields are empty), treat as Skip/Cancel
            if (!SelectedAccount.IsValid)
            {
                SelectedAccount = null;
                MessageBox.Show("Required fields are missing." + Environment.NewLine +
                    "Please enter the missing information or choose 'Skip'");
                return;
            }
            else
            {
                // Try to find a fully-matching account
                if (!_accountsModel.Contains(SelectedAccount))
                {
                    // No fully-matching account? See if the same name is used.
                    if (_accountsModel.NameExists(SelectedAccount.Name))
                    {
                        MessageBox.Show(
                            "Local account with that name but different key information already exists" + Environment.NewLine +
                            "Please enter a different name, select an existing account or 'Skip'.");
                        return;
                    }

                    // Implicitly add new account
                    _accountsModel.AddNewAccount(SelectedAccount);
                }
            }

            _accountsModel.SetLastUsed(SelectedAccount.UniqueKey);

            this.Dispose();
        }

        private void awsSecurityURLLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Determine which link was clicked within the LinkLabel.
            this.awsSecurityURLLabel.Links[this.awsSecurityURLLabel.Links.IndexOf(e.Link)].Visited = true;
            System.Diagnostics.Process.Start("http://aws.amazon.com/security-credentials");
        }

        private void UserInputForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Dispose();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            SelectedAccount = Account.Empty;
            this.Dispose();
        }

        private void accountSelectionChanged(object sender, EventArgs e)
        {
            if (!newAccountRadioButton.Checked && !storedAccountRadioButton.Checked)
            {
                newAccountRadioButton.Checked = true;
            }

            if (newAccountRadioButton.Checked)
            {
                storedAccountPanel.Enabled = false;
                newAccountPanel.Enabled = true;
            }
            else
            {
                storedAccountPanel.Enabled = true;
                newAccountPanel.Enabled = false;
            }
        }

        private void accountSelectorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnDeleteAccount.Enabled = accountSelectorComboBox.Items.Count > 0;
        }

        private void btnDeleteAccount_Click(object sender, EventArgs e)
        {
            Account account = GetEnteredAccount();

            if (MessageBox.Show(string.Format("Are you sure you want to delete the settings for account '{0}'?", account.Name),
                                    "Delete Settings", 
                                    MessageBoxButtons.YesNo, 
                                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _accountsModel.DeleteAccount(account);

                accountSelectorComboBox.Items.Remove(account);
                if (accountSelectorComboBox.Items.Count > 0)
                    accountSelectorComboBox.SelectedIndex = 0;
                else
                {
                    newAccountRadioButton.Checked = true;
                    accountSelectionChanged(this, null);
                }
            }
        }
    }
}
