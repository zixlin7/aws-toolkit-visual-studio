using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.View
{
    /// <summary>
    /// Interaction logic for NewPolicyNameControl.xaml
    /// </summary>
    public partial class NewPolicyNameControl : BaseAWSControl
    {
        static readonly HashSet<char> VALID_SPECIAL_CHARACTERS;

        static NewPolicyNameControl()
        {
            VALID_SPECIAL_CHARACTERS = new HashSet<char>();
            VALID_SPECIAL_CHARACTERS.Add('+');
            VALID_SPECIAL_CHARACTERS.Add('=');
            VALID_SPECIAL_CHARACTERS.Add(',');
            VALID_SPECIAL_CHARACTERS.Add('.');
            VALID_SPECIAL_CHARACTERS.Add('@');
            VALID_SPECIAL_CHARACTERS.Add('_');
            VALID_SPECIAL_CHARACTERS.Add('-');
        }

        public NewPolicyNameControl()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "New Policy Name";
            }
        }

        public string NewPolicyName
        {
            get;
            set;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewPolicyName.Focus();
        }

        public override bool Validated()
        {
            foreach (var c in this.NewPolicyName.ToCharArray())
            {
                if (!char.IsLetterOrDigit(c) && !VALID_SPECIAL_CHARACTERS.Contains(c))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("The name " + this.NewPolicyName + " is invalid.  It must contain only alphanumeric characters and/or the following: +=,.@_-");
                    return false;
                }
            }

            return true;
        }
    }
}
