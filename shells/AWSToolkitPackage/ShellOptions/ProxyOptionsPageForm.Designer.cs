namespace Amazon.AWSToolkit.VisualStudio.ShellOptions
{
    partial class ProxyOptionsPageForm
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this._ctlPassword = new System.Windows.Forms.TextBox();
            this._ctlUsername = new System.Windows.Forms.TextBox();
            this._ctlPort = new System.Windows.Forms.TextBox();
            this._ctlHost = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this._ctlHost);
            this.groupBox1.Controls.Add(this._ctlPort);
            this.groupBox1.Controls.Add(this._ctlUsername);
            this.groupBox1.Controls.Add(this._ctlPassword);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 214);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Toolkit Proxy";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(10, 160);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Password";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(10, 133);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Username";
            // 
            // _ctlPassword
            // 
            this._ctlPassword.Location = new System.Drawing.Point(72, 153);
            this._ctlPassword.Name = "_ctlPassword";
            this._ctlPassword.PasswordChar = '*';
            this._ctlPassword.Size = new System.Drawing.Size(179, 20);
            this._ctlPassword.TabIndex = 3;
            // 
            // _ctlUsername
            // 
            this._ctlUsername.Location = new System.Drawing.Point(72, 126);
            this._ctlUsername.Name = "_ctlUsername";
            this._ctlUsername.Size = new System.Drawing.Size(179, 20);
            this._ctlUsername.TabIndex = 1;
            // 
            // _ctlPort
            // 
            this._ctlPort.Location = new System.Drawing.Point(72, 99);
            this._ctlPort.Name = "_ctlPort";
            this._ctlPort.Size = new System.Drawing.Size(179, 20);
            this._ctlPort.TabIndex = 6;
            // 
            // _ctlHost
            // 
            this._ctlHost.Location = new System.Drawing.Point(72, 72);
            this._ctlHost.Name = "_ctlHost";
            this._ctlHost.Size = new System.Drawing.Size(179, 20);
            this._ctlHost.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 106);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(26, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Port";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Host";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 20);
            this.label1.MaximumSize = new System.Drawing.Size(250, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(231, 26);
            this.label1.TabIndex = 0;
            this.label1.Text = "This configuration will allow the AWS Toolkit to reach AWS services through a pro" +
    "xy.";
            // 
            // ProxyOptionsPageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "ProxyOptionsPageForm";
            this.Size = new System.Drawing.Size(281, 221);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox _ctlPassword;
        private System.Windows.Forms.TextBox _ctlUsername;
        private System.Windows.Forms.TextBox _ctlPort;
        private System.Windows.Forms.TextBox _ctlHost;
        private System.Windows.Forms.Label label3;
    }
}
