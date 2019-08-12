using System.IO;
using Microsoft.Win32;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.View;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.AWSToolkit.DynamoDB.Util;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class StartLocalDynamoDBController : BaseContextCommand
    {
        StartLocalDynamoDBControl _control;
        StartLocalDynamoDBModel _model;
        ActionResults _results;
        DynamoDBRootViewModel _rootModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as DynamoDBRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new StartLocalDynamoDBModel();
            this._control = new StartLocalDynamoDBControl(this);

            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public StartLocalDynamoDBModel Model => this._model;

        public void LoadModel()
        {
            this._model.Versions = new System.Collections.ObjectModel.ObservableCollection<DynamoDBLocalManager.DynamoDBLocalVersion>();
            foreach (var item in DynamoDBLocalManager.Instance.GetAvailableVersions())
            {
                this._model.Versions.Add(item);
            }
            

            if (this._model.Versions.Count > 0)
            {
                this._model.SelectedVersion = this._model.Versions[0];
            }

            this._model.JavaPath = SearchForJRE();
        }

        public void Start()
        {
            if (this._model.StartNew)
            {
                SaveLastJRE();
                DynamoDBLocalManager.Instance.Start(this._model.SelectedVersion, this._model.Port, this._model.JavaPath);
            }
            else
                DynamoDBLocalManager.Instance.Connect(this._model.Port);

            this._results = new ActionResults().WithSuccess(true);
        }

        public void InstallSelected(DynamoDBLocalManager.DownloadProgress callback)
        {
            DynamoDBLocalManager.Instance.InstallAsync(this._model.SelectedVersion, callback);
            this._model.CheckInstallState();
        }

        public void UninstallSelected()
        {
            DynamoDBLocalManager.Instance.Uninstall(this._model.SelectedVersion);
            this._model.CheckInstallState();
        }

        void SaveLastJRE()
        {
            var userPreferences = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.UserPreferences);
            var locations = userPreferences["Locations"];
            locations["java.exe"] = this._model.JavaPath;
            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.UserPreferences, userPreferences);
        }

        string SearchForJRE()
        {
            var userPreferences = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.UserPreferences);
            var locations = userPreferences["Locations"];
            var javaExe = locations["java.exe"];
            if (!string.IsNullOrWhiteSpace(javaExe) && File.Exists(javaExe))
                return javaExe;

            javaExe = SearchForJRE(RegistryView.Registry64);
            if (javaExe == null)
                javaExe = SearchForJRE(RegistryView.Registry32);

            return javaExe;
        }

        string SearchForJRE(RegistryView view)
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view);
            var javaRootKey = localKey.OpenSubKey(@"SOFTWARE\JavaSoft\Java Runtime Environment");
            if (javaRootKey == null || javaRootKey.GetSubKeyNames().Length == 0)
                return null;

            foreach (var jreKeyName in javaRootKey.GetSubKeyNames())
            {
                var jreKey = javaRootKey.OpenSubKey(jreKeyName);

                var javaHome = jreKey.GetValue("JavaHome") as string;
                var javaExe = Path.Combine(javaHome, "bin", "java.exe");
                if (File.Exists(javaExe))
                    return javaExe;
            }

            return null;
        }
    }
}
