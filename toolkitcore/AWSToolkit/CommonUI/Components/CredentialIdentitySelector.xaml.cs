using System.Collections;
using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Presentation;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Control that allows users to select from a list of Credential Ids.
    /// The listing is presented so that the Ids are grouped by credential source
    /// (example SDK Credentials, Shared Credentials).
    ///
    /// To use this control, data bind to the <see cref="Identifier"/> and <see cref="Identifiers"/>
    /// properties.
    /// </summary>
    public partial class CredentialIdentitySelector
    {
        public CredentialIdentitySelector()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            // Initialize the list sorting
            CollectionViewSource identifiersView = FindResource("IdentifiersView") as CollectionViewSource;
            if (identifiersView?.View is ListCollectionView listView)
            {
                listView.CustomSort = new CredentialIdentifierGroupComparer();
            }
        }

        /// <summary>
        ///  Gets or sets the currently selected Credential Id
        /// </summary>
        public ICredentialIdentifier Identifier
        {
            get => (ICredentialIdentifier) GetValue(IdentifierProperty);
            set => SetValue(IdentifierProperty, value);
        }

        /// <summary>
        /// Gets or sets the Credential Ids to choose from
        /// </summary>
        public IEnumerable Identifiers
        {
            get => (IEnumerable) GetValue(IdentifiersProperty);
            set => SetValue(IdentifiersProperty, value);
        }

        public static readonly DependencyProperty IdentifierProperty = DependencyProperty.Register(
            nameof(Identifier), typeof(ICredentialIdentifier), typeof(CredentialIdentitySelector),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IdentifiersProperty = DependencyProperty.Register(
            nameof(Identifiers), typeof(IEnumerable), typeof(CredentialIdentitySelector),
            new PropertyMetadata(null));
    }
}
