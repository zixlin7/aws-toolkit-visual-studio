using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class SaveServiceSpecificCredentialsModel : BaseModel
    {
        // matches what the user can download from the console
        private readonly string[] _fileHeaders = {"User Name", "Password"};

        public SaveServiceSpecificCredentialsModel(ServiceSpecificCredential generatedCredentials)
        {
            GeneratedCredentials = generatedCredentials;
        }

        private ServiceSpecificCredential GeneratedCredentials { get; }

        private string _filename;
        public string Filename
        {
            get { return _filename; }
            set { _filename = value; NotifyPropertyChanged("Filename"); }
        }

        public bool SaveToFile()
        {
            try
            {
                var csv = new HeaderedCsvFile(_fileHeaders);
                csv.AddRowData(new[] { GeneratedCredentials.ServiceUserName, GeneratedCredentials.ServicePassword });
                csv.WriteTo(Filename);

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Failed to Save", "Failed to save the credentials to the specified file. Exception message " + e.Message);
            }

            return false;
        }
    }
}
