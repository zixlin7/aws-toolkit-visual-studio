namespace TemplateWizard
{
    partial class UserInputForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserInputForm));
            this.okButton = new System.Windows.Forms.Button();
            this.titleLabel = new System.Windows.Forms.Label();
            this.awsSecurityURLLabel = new System.Windows.Forms.LinkLabel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.storedAccountRadioButton = new System.Windows.Forms.RadioButton();
            this.newAccountPanel = new System.Windows.Forms.Panel();
            this.isGovCloudAccount = new System.Windows.Forms.CheckBox();
            this.displayNameBox = new System.Windows.Forms.TextBox();
            this.displayNameLabel = new System.Windows.Forms.Label();
            this.accountNumberBox = new System.Windows.Forms.TextBox();
            this.accountNumberLabel = new System.Windows.Forms.Label();
            this.secretKeyBox = new System.Windows.Forms.TextBox();
            this.accessKeyBox = new System.Windows.Forms.TextBox();
            this.secretKeyLabel = new System.Windows.Forms.Label();
            this.accessKeyLabel = new System.Windows.Forms.Label();
            this.storedAccountPanel = new System.Windows.Forms.Panel();
            this.btnDeleteAccount = new System.Windows.Forms.Button();
            this.accountSelectorComboBox = new System.Windows.Forms.ComboBox();
            this.newAccountRadioButton = new System.Windows.Forms.RadioButton();
            this.panelRegion = new System.Windows.Forms.Panel();
            this.regionSelectorComboBox = new System.Windows.Forms.ComboBox();
            this.labelRegion = new System.Windows.Forms.Label();
            this.newAccountPanel.SuspendLayout();
            this.storedAccountPanel.SuspendLayout();
            this.panelRegion.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // titleLabel
            // 
            resources.ApplyResources(this.titleLabel, "titleLabel");
            this.titleLabel.Name = "titleLabel";
            // 
            // awsSecurityURLLabel
            // 
            resources.ApplyResources(this.awsSecurityURLLabel, "awsSecurityURLLabel");
            this.awsSecurityURLLabel.Name = "awsSecurityURLLabel";
            this.awsSecurityURLLabel.TabStop = true;
            this.awsSecurityURLLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.awsSecurityURLLabel_LinkClicked);
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // storedAccountRadioButton
            // 
            resources.ApplyResources(this.storedAccountRadioButton, "storedAccountRadioButton");
            this.storedAccountRadioButton.Name = "storedAccountRadioButton";
            this.storedAccountRadioButton.TabStop = true;
            this.storedAccountRadioButton.UseVisualStyleBackColor = true;
            this.storedAccountRadioButton.CheckedChanged += new System.EventHandler(this.accountSelectionChanged);
            // 
            // newAccountPanel
            // 
            resources.ApplyResources(this.newAccountPanel, "newAccountPanel");
            this.newAccountPanel.Controls.Add(this.isGovCloudAccount);
            this.newAccountPanel.Controls.Add(this.displayNameBox);
            this.newAccountPanel.Controls.Add(this.displayNameLabel);
            this.newAccountPanel.Controls.Add(this.accountNumberBox);
            this.newAccountPanel.Controls.Add(this.accountNumberLabel);
            this.newAccountPanel.Controls.Add(this.secretKeyBox);
            this.newAccountPanel.Controls.Add(this.accessKeyBox);
            this.newAccountPanel.Controls.Add(this.secretKeyLabel);
            this.newAccountPanel.Controls.Add(this.accessKeyLabel);
            this.newAccountPanel.Name = "newAccountPanel";
            // 
            // isGovCloudAccount
            // 
            resources.ApplyResources(this.isGovCloudAccount, "isGovCloudAccount");
            this.isGovCloudAccount.Name = "isGovCloudAccount";
            this.isGovCloudAccount.UseVisualStyleBackColor = true;
            // 
            // displayNameBox
            // 
            resources.ApplyResources(this.displayNameBox, "displayNameBox");
            this.displayNameBox.Name = "displayNameBox";
            // 
            // displayNameLabel
            // 
            resources.ApplyResources(this.displayNameLabel, "displayNameLabel");
            this.displayNameLabel.Name = "displayNameLabel";
            // 
            // accountNumberBox
            // 
            resources.ApplyResources(this.accountNumberBox, "accountNumberBox");
            this.accountNumberBox.Name = "accountNumberBox";
            // 
            // accountNumberLabel
            // 
            resources.ApplyResources(this.accountNumberLabel, "accountNumberLabel");
            this.accountNumberLabel.Name = "accountNumberLabel";
            // 
            // secretKeyBox
            // 
            this.secretKeyBox.AcceptsTab = true;
            resources.ApplyResources(this.secretKeyBox, "secretKeyBox");
            this.secretKeyBox.Name = "secretKeyBox";
            // 
            // accessKeyBox
            // 
            this.accessKeyBox.AcceptsTab = true;
            this.accessKeyBox.AllowDrop = true;
            resources.ApplyResources(this.accessKeyBox, "accessKeyBox");
            this.accessKeyBox.Name = "accessKeyBox";
            // 
            // secretKeyLabel
            // 
            resources.ApplyResources(this.secretKeyLabel, "secretKeyLabel");
            this.secretKeyLabel.Name = "secretKeyLabel";
            // 
            // accessKeyLabel
            // 
            resources.ApplyResources(this.accessKeyLabel, "accessKeyLabel");
            this.accessKeyLabel.Name = "accessKeyLabel";
            // 
            // storedAccountPanel
            // 
            resources.ApplyResources(this.storedAccountPanel, "storedAccountPanel");
            this.storedAccountPanel.Controls.Add(this.btnDeleteAccount);
            this.storedAccountPanel.Controls.Add(this.accountSelectorComboBox);
            this.storedAccountPanel.Name = "storedAccountPanel";
            // 
            // btnDeleteAccount
            // 
            resources.ApplyResources(this.btnDeleteAccount, "btnDeleteAccount");
            this.btnDeleteAccount.Name = "btnDeleteAccount";
            this.btnDeleteAccount.UseVisualStyleBackColor = true;
            this.btnDeleteAccount.Click += new System.EventHandler(this.btnDeleteAccount_Click);
            // 
            // accountSelectorComboBox
            // 
            this.accountSelectorComboBox.DisplayMember = "Name";
            this.accountSelectorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.accountSelectorComboBox.FormattingEnabled = true;
            resources.ApplyResources(this.accountSelectorComboBox, "accountSelectorComboBox");
            this.accountSelectorComboBox.Name = "accountSelectorComboBox";
            this.accountSelectorComboBox.SelectedIndexChanged += new System.EventHandler(this.accountSelectorComboBox_SelectedIndexChanged);
            // 
            // newAccountRadioButton
            // 
            resources.ApplyResources(this.newAccountRadioButton, "newAccountRadioButton");
            this.newAccountRadioButton.Name = "newAccountRadioButton";
            this.newAccountRadioButton.TabStop = true;
            this.newAccountRadioButton.UseVisualStyleBackColor = true;
            // 
            // panelRegion
            // 
            this.panelRegion.Controls.Add(this.regionSelectorComboBox);
            this.panelRegion.Controls.Add(this.labelRegion);
            resources.ApplyResources(this.panelRegion, "panelRegion");
            this.panelRegion.Name = "panelRegion";
            // 
            // regionSelectorComboBox
            // 
            this.regionSelectorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.regionSelectorComboBox.FormattingEnabled = true;
            resources.ApplyResources(this.regionSelectorComboBox, "regionSelectorComboBox");
            this.regionSelectorComboBox.Name = "regionSelectorComboBox";
            // 
            // labelRegion
            // 
            resources.ApplyResources(this.labelRegion, "labelRegion");
            this.labelRegion.Name = "labelRegion";
            // 
            // UserInputForm
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.panelRegion);
            this.Controls.Add(this.newAccountRadioButton);
            this.Controls.Add(this.storedAccountPanel);
            this.Controls.Add(this.newAccountPanel);
            this.Controls.Add(this.storedAccountRadioButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.awsSecurityURLLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.okButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserInputForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.UserInputForm_FormClosed);
            this.newAccountPanel.ResumeLayout(false);
            this.newAccountPanel.PerformLayout();
            this.storedAccountPanel.ResumeLayout(false);
            this.panelRegion.ResumeLayout(false);
            this.panelRegion.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.LinkLabel awsSecurityURLLabel;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.RadioButton storedAccountRadioButton;
        private System.Windows.Forms.Panel newAccountPanel;
        private System.Windows.Forms.TextBox displayNameBox;
        private System.Windows.Forms.Label displayNameLabel;
        private System.Windows.Forms.TextBox accountNumberBox;
        private System.Windows.Forms.Label accountNumberLabel;
        private System.Windows.Forms.TextBox secretKeyBox;
        private System.Windows.Forms.TextBox accessKeyBox;
        private System.Windows.Forms.Label secretKeyLabel;
        private System.Windows.Forms.Label accessKeyLabel;
        private System.Windows.Forms.Panel storedAccountPanel;
        private System.Windows.Forms.ComboBox accountSelectorComboBox;
        private System.Windows.Forms.RadioButton newAccountRadioButton;
        private System.Windows.Forms.Button btnDeleteAccount;
        private System.Windows.Forms.CheckBox isGovCloudAccount;
        private System.Windows.Forms.Panel panelRegion;
        private System.Windows.Forms.ComboBox regionSelectorComboBox;
        private System.Windows.Forms.Label labelRegion;
    }
}