namespace AWSDeploymentService
{
    partial class HarpStringProjectInstaller
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
            this.serviceInstaller2 = new System.ServiceProcess.ServiceInstaller();
            this.serviceProcessInstaller2 = new System.ServiceProcess.ServiceProcessInstaller();
            // 
            // serviceInstaller2
            // 
            this.serviceInstaller2.DisplayName = "Harp String";
            this.serviceInstaller2.ServiceName = "HarpString";
            // 
            // serviceProcessInstaller2
            // 
            this.serviceProcessInstaller2.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.serviceProcessInstaller2.Password = null;
            this.serviceProcessInstaller2.Username = null;
            // 
            // HarpStringProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceInstaller2,
            this.serviceProcessInstaller2});

        }

        #endregion

        private System.ServiceProcess.ServiceInstaller serviceInstaller2;
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller2;
    }
}