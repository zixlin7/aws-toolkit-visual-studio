using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using log4net;
using System.Xml.Linq;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for FunctionInvokeControl.xaml
    /// </summary>
    public partial class FunctionInvokeControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(FunctionInvokeControl));

        ViewFunctionController _controller;

        public FunctionInvokeControl()
        {
            InitializeComponent();
            FillExampleRequests();
        }

        public void Initialize(ViewFunctionController controller)
        {
            this._controller = controller;
        }

        private async void Execute_Click(object sender, RoutedEventArgs x)
        {
            bool success = false;
            try
            {
                this._ctlResponse.Text = "";
                this._ctlLogOutput.Text = "";
                StartProgressBar();
                var response = await this._controller.InvokeFunctionAsync(this._ctlSampleInput.Text);

                var payload = new StreamReader(response.Payload).ReadToEnd();
                var log = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));

                this._ctlResponse.Text = payload;
                this._ctlLogOutput.Text = log;

                this._ctlPrettyPrint.IsEnabled = PrettyPrint(payload) != null;
                success = true;
            }
            catch (Exception e)
            {
                success = false;
                LOGGER.Error(string.Format("Error invoking function {0}", this._controller.Model.FunctionName), e);
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    string.Format("Error invoking function: {0}", e.Message));
            }
            finally
            {
                ToolkitFactory.Instance.TelemetryLogger.RecordLambdaInvokeRemote(new LambdaInvokeRemote()
                {
                    Result = success ? Result.Succeeded : Result.Failed,
                    Runtime = new AwsToolkit.Telemetry.Events.Generated.Runtime(this._controller?.Model?.Runtime
                        ?.ToString())
                });

                StopProgressBar();
            }
        }

        private void PrettyPrint_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (string.IsNullOrEmpty(this._ctlResponse.Text))
                    return;

                this._ctlResponse.Text = PrettyPrint(this._ctlResponse.Text);
                this._ctlPrettyPrint.IsEnabled = false;
            }
            catch (Exception)
            {
            }
        }

        private static string PrettyPrint(string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                    return null;

                var data = ThirdParty.Json.LitJson.JsonMapper.ToObject(content);

                StringBuilder sb = new StringBuilder();
                var writer = new ThirdParty.Json.LitJson.JsonWriter(sb);
                writer.PrettyPrint = true;
                ThirdParty.Json.LitJson.JsonMapper.ToJson(data, writer);
                return sb.ToString().Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        const string SAMPLE_REQUESTS_PREFIX = "LambdaSampleFunctions/SampleRequests/";
        Dictionary<string, string> _sampleRequests = new Dictionary<string, string>();

        private void FillExampleRequests()
        {
            try
            {
                var content = S3FileFetcher.Instance.GetFileContent(SAMPLE_REQUESTS_PREFIX + "manifest.xml");
                XDocument xmlDoc = XDocument.Parse(content);

                var query = from item in xmlDoc.Descendants("request")
                    select new
                    {
                        Name = item.Element("name").Value,
                        Filename = item.Element("filename").Value
                    };

                var sampleEvents = from item in xmlDoc.Descendants("request")
                    select new SampleEvent
                    {
                        Group = item.Attribute("category")?.Value ?? string.Empty,
                        Name = item.Element("name")?.Value ?? string.Empty,
                        Filename = item.Element("filename")?.Value ?? string.Empty,
                    };

                _ctlSampleEvents.SetSampleEvents(sampleEvents);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error filling example requests combo box.", e);
            }
        }

        private void _ctlSampleEvents_PropertyChanged(object sender, PropertyChangedEventArgs evnt)
        {
            string content = string.Empty;

            try
            {
                var file = this._ctlSampleEvents.SelectedItem?.Filename;
                if (string.IsNullOrEmpty(file))
                    return;

                content = S3FileFetcher.Instance.GetFileContent(SAMPLE_REQUESTS_PREFIX + file);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating sample input with example.", e);
                content = string.Empty;
            }
            finally
            {
                this._ctlSampleInput.Text = content;
            }
        }

        private void StartProgressBar()
        {
            _ctlProgressBar.Visibility = Visibility.Visible;
            _ctlProgressBar.IsIndeterminate = true;
        }

        private void StopProgressBar()
        {
            _ctlProgressBar.Visibility = Visibility.Hidden;
            _ctlProgressBar.IsIndeterminate = false;
            _ctlProgressBar.Value = _ctlProgressBar.Maximum;
        }
    }
}