using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using log4net;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework
{
    /// <summary>
    /// Base control class for wizard-type dialog interaction. The wizard accepts a variable number
    /// of 'pages' (control class + associated user control for UI) that it arranges into request order.
    /// As the wizard runs, pages post data into a central dictionary (available to all pages); pages
    /// are queried before transition to determine if they want to be shown and can customise their
    /// appearance based on data posted by earlier pages.
    /// </summary>
    public partial class AWSStandardWizard : IAWSWizard, INotifyPropertyChanged
    {
        private readonly AWSBaseWizardImpl _awsBaseWizardImpl;
        bool _autoEnableFinish = true;
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSStandardWizard));

        /// <summary>
        /// Parameterless ctor to satisfy designer
        /// </summary>
        internal AWSStandardWizard()
        {
            DataContext = this;
            InitializeComponent();

            ThemeUtil.UpdateDictionariesForTheme(this.Resources);
        }

        /// <summary>
        /// Construct from factory method supplying common implementor and optional
        /// header control
        /// </summary>
        internal AWSStandardWizard(AWSBaseWizardImpl awsBaseWizardImpl)
            : this()
        {
            _awsBaseWizardImpl = awsBaseWizardImpl;
            _awsBaseWizardImpl.SetHostWizard(this); // allows impl to work with control buttons conveniently
        }

        #region Navigation button event handlers

        private void _btnBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                ReshowNavButtons(AWSWizardConstants.NavigationReason.movingBack);
                _awsBaseWizardImpl.TransitionPage(AWSWizardConstants.NavigationReason.movingBack, _pagesContainer.Children);
            }
            catch (Exception exc)
            {
                LOGGER.ErrorFormat("Caught exception in wizard during Back transition; exception message = '{0}'", exc.Message);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void _btnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                ReshowNavButtons(AWSWizardConstants.NavigationReason.movingForward);
                _awsBaseWizardImpl.TransitionPage(AWSWizardConstants.NavigationReason.movingForward, _pagesContainer.Children);
            }
            catch (Exception exc)
            {
                LOGGER.ErrorFormat("Caught exception in wizard during Next transition; exception message = '{0}'", exc.Message);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void _btnFinish_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            ReshowNavButtons(AWSWizardConstants.NavigationReason.movingForward);
            bool quit = _awsBaseWizardImpl.FinishPressed(_pagesContainer.Children);
            Cursor = Cursors.Arrow;
            if (quit)
            {
                this.DialogResult = true;
                Close();
            }
        }

        #endregion

        /// <summary>
        /// If a page has hidden one or more of the Back/Next/Finish buttons, turn them back on
        /// before we navigate away as a convenience to the next page.
        /// </summary>
        /// <param name="direction"></param>
        void ReshowNavButtons(AWSWizardConstants.NavigationReason direction)
        {
            if (direction == AWSWizardConstants.NavigationReason.movingBack)
            {
                if (_btnNext.Visibility == Visibility.Hidden)
                    _btnNext.Visibility = Visibility.Visible;
                if (_btnFinish.Visibility == Visibility.Hidden)
                    _btnFinish.Visibility = Visibility.Visible;
            }
            else
            {
                if (_btnBack.Visibility == Visibility.Hidden)
                    _btnBack.Visibility = Visibility.Visible;
            }
        }

        void UpdateNavigationButtonText(AWSWizardConstants.NavigationButtons button, string buttonText)
        {
            switch (button)
            {
                case AWSWizardConstants.NavigationButtons.Back:
                    _btnBack.Content = buttonText;
                    break;
                case AWSWizardConstants.NavigationButtons.Cancel:
                    _btnCancel.Content = buttonText;
                    break;
                case AWSWizardConstants.NavigationButtons.Finish:
                    _btnFinish.Content = buttonText;
                    break;
                case AWSWizardConstants.NavigationButtons.Forward:
                    _btnNext.Content = buttonText;
                    break;
                case AWSWizardConstants.NavigationButtons.Help:
                    _btnHelp.Content = buttonText;
                    break;
            }
        }

        void UpdateNavigationButtonVisibility(AWSWizardConstants.NavigationButtons button, bool isVisible)
        {
            switch (button)
            {
                case AWSWizardConstants.NavigationButtons.Back:
                    _btnBack.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    break;
                case AWSWizardConstants.NavigationButtons.Cancel:
                    _btnCancel.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    break;
                case AWSWizardConstants.NavigationButtons.Finish:
                    _btnFinish.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    break;
                case AWSWizardConstants.NavigationButtons.Forward:
                    _btnNext.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    break;
                case AWSWizardConstants.NavigationButtons.Help:
                    _btnHelp.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    break;
            }
        }

        // pass null image to have the button collapse to text only
        void UpdateNavigationButtonGlyph(AWSWizardConstants.NavigationButtons button, Image buttonGlyph)
        {
        }

        //void StyleWizardFromProperties()
        //{
        //    if (_awsbasewizardimpl.ispropertyset(awswizardconstants.wizardoptions.propkey_navcontainerbackground))
        //    {
        //        var navcontainerbackgroundkey 
        //            = _awsbasewizardimpl.getproperty(awswizardconstants.wizardoptions.propkey_navcontainerbackground) as string;
        //        _wizardfootercontainer.setresourcereference(panel.backgroundproperty, navcontainerbackgroundkey);
        //    }
        //}

        #region IAWSWizard implementation

        string IAWSWizard.WizardID { get { return _awsBaseWizardImpl.WizardID; } }

        string IAWSWizard.Title 
        { 
            set 
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Missing/invalid value");

                Title = value; 
            } 
        }

        public string ActivePageTitle
        {
            get
            {
                if (_awsBaseWizardImpl == null)
                    return string.Empty;

                var activePage = _awsBaseWizardImpl.ActivePageController;
                return activePage != null ? activePage.PageTitle : string.Empty;
            }
        }

        public string ActivePageDescription
        {
            get
            {
                if (_awsBaseWizardImpl == null)
                    return string.Empty;

                var activePage = _awsBaseWizardImpl.ActivePageController;
                return activePage != null ? activePage.PageDescription : string.Empty;
            }
        }

        readonly ObservableCollection<TableOfContentEntry> _tableOfContents = new ObservableCollection<TableOfContentEntry>();
        public ObservableCollection<TableOfContentEntry> TableOfContents
        {
            get { return _tableOfContents; }    
        }

        public TableOfContentEntry ActiveTableOfContentEntry
        {
            get
            {
                if (_awsBaseWizardImpl == null)
                    return null;

                return _awsBaseWizardImpl.ActiveTableOfContentEntry;
            }
        }

        public bool HasPageGroups
        {
            get
            {
                if (_awsBaseWizardImpl == null)
                    return false;

                return _awsBaseWizardImpl.HasPageGroups;
            }
        }

        bool IAWSWizard.DisplayHeader
        {
            get { return _wizardHeaderContainer.Visibility == Visibility.Visible; }
            set { _wizardHeaderContainer.Visibility = Visibility.Collapsed; } 
        }

        bool IAWSWizard.AutoPollFinishEnablement
        {
            get { return this._autoEnableFinish; }
            set { this._autoEnableFinish = value; }
        }

        void IAWSWizard.RegisterPageGroups(IEnumerable<string> groupNames)
        {
            _awsBaseWizardImpl.PageGroupNames = groupNames;
        }

        void IAWSWizard.RegisterPageControllers(IEnumerable<IAWSWizardPageController> pageControllers, int priority)
        {
            _awsBaseWizardImpl.RegisterPageControllers(pageControllers, priority, this);
        }

        object IAWSWizard.GetProperty(string key)
        {
            return _awsBaseWizardImpl.GetProperty(key);
        }

        T IAWSWizard.GetProperty<T>(string key)
        {
            return _awsBaseWizardImpl.GetPropertyValue<T>(key);
        }

        void IAWSWizard.SetProperty(string key, object value)
        {
            _awsBaseWizardImpl.SetProperty(key, value);
        }

        void IAWSWizard.SetProperties(Dictionary<string, object> properties)
        {
            _awsBaseWizardImpl.SetProperties(properties);
        }

        bool IAWSWizard.IsPropertySet(string key)
        {
            return _awsBaseWizardImpl.IsPropertySet(key);
        }

        bool IAWSWizard.Run()
        {
            Cursor = Cursors.Wait;
            var ret = false;

            try
            {
                _awsBaseWizardImpl.FinalizeForRun();
                // if we have only one group, we can dispense with the nav panel
                if (_awsBaseWizardImpl.PageGroupCount == 1)
                    _groupsPanel.Visibility = Visibility.Collapsed;
                else
                {
                    TableOfContents.Clear();
                    foreach (var t in _awsBaseWizardImpl.TableOfContents)
                    {
                        TableOfContents.Add(t);
                    }
                    NotifyPropertyChanged("TableOfContents");
                }

                _awsBaseWizardImpl.TransitionPage(AWSWizardConstants.NavigationReason.movingForward, _pagesContainer.Children);
                Cursor = Cursors.Arrow;

                var wizard = this as IAWSWizard;
                var wizardID = (wizard == null) ? null : wizard.WizardID;
                ret = ToolkitFactory.Instance.ShellProvider.ShowModal(this, wizardID);
            }
            catch (Exception exc)
            {
                LOGGER.ErrorFormat("Caught exception in wizard, exception message = '{0}'.", exc.Message);
            }
            finally
            {
                // in vase we fail prior to ShowModal
                Cursor = Cursors.Arrow;
            }

            return ret;
        }

        void IAWSWizard.SetNavigationEnablement(IAWSWizardPageController requestorPage, AWSWizardConstants.NavigationButtons button, bool enable)
        {
            switch (button)
            {
                case AWSWizardConstants.NavigationButtons.Back:
                    _btnBack.IsEnabled = enable;
                    break;

                case AWSWizardConstants.NavigationButtons.Forward:
                    _btnNext.IsEnabled = enable;
                    if (enable)
                        (this as IAWSWizard).RequestFinishEnablement(requestorPage);  // must poll other pages for acceptance
                    else
                        _btnFinish.IsEnabled = _awsBaseWizardImpl.IsFinalPage(requestorPage);
                    break;

                case AWSWizardConstants.NavigationButtons.Finish:
                    if (!enable)
                        _btnFinish.IsEnabled = false;
                    else
                        (this as IAWSWizard).RequestFinishEnablement(requestorPage);
                    break;

                default:
                    throw new ArgumentException("Expected only Back, Next or Finish buttons for enablement");
            }
        }

        bool IAWSWizard.RequestFinishEnablement(IAWSWizardPageController requestorPage)
        {
            return _btnFinish.IsEnabled = _awsBaseWizardImpl.RequestFinishEnablement(requestorPage);
        }

        object IAWSWizard.this[string propertyKey]
        {
            get { return _awsBaseWizardImpl.GetProperty(propertyKey); }
            set { _awsBaseWizardImpl.SetProperty(propertyKey, value); }
        }

        Dictionary<string, object> IAWSWizard.CollectedProperties 
        {
            get
            {
                return _awsBaseWizardImpl.CopyProperties();
            }
        }

        void IAWSWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons button, string buttonText)
        {
            // don't forward to SetNavigationButtonUI with null image; this allows us to 
            // reserve use null to mean 'remove glyph'
            UpdateNavigationButtonText(button, buttonText);
        }

        void IAWSWizard.SetNavigationButtonUI(AWSWizardConstants.NavigationButtons button, string buttonText, Image buttonGlyph)
        {
            UpdateNavigationButtonText(button, buttonText);
            UpdateNavigationButtonGlyph(button, buttonGlyph);
        }

        void IAWSWizard.SetNavigationButtonVisibility(WizardFramework.AWSWizardConstants.NavigationButtons button, bool isVisible)
        {
            UpdateNavigationButtonVisibility(button, isVisible);
        }

        void IAWSWizard.SetNavigationPanelBackground(Brush navPanelBrush)
        {
            this._wizardFooterContainer.Background = navPanelBrush;
        }

        void IAWSWizard.SetShortCircuitPage(string shortCircuitPageID)
        {
            _awsBaseWizardImpl.SetShortCircuitPage(shortCircuitPageID);
        }

        void IAWSWizard.ResetFirstActivePage()
        {
            _awsBaseWizardImpl.ResetFirstActivePage();
        }

        Func<bool> IAWSWizard.CommitAction
        {
            get { return this._awsBaseWizardImpl.CommitAction; }
            set { this._awsBaseWizardImpl.CommitAction = value; }
        }

        ILog IAWSWizard.Logger { get { return _awsBaseWizardImpl.Logger; } }

        void IAWSWizard.CancelRun()
        {
            this.DialogResult = false;
            Close();
        }

        void IAWSWizard.CancelRun(string propertyKey, object propertyValue)
        {
            _awsBaseWizardImpl.SetProperty(propertyKey, propertyValue);
            this.DialogResult = false;
            Close();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
