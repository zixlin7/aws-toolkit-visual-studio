using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Shared;
using log4net;
using Amazon.AWSToolkit.Themes;

namespace Amazon.AWSToolkit.CommonUI
{
    public class BaseAWSUserControl : UserControl
    {
        public UserControl UserControl => this;

        protected T FindHost<T>() where T : FrameworkElement
        {
            FrameworkElement currentLevel = this;
            while (currentLevel != null && !(currentLevel is T))
            {
                currentLevel = currentLevel.Parent as FrameworkElement;
            }

            if (currentLevel is T)
                return (T)currentLevel;

            return default(T);
        }

        /// <summary>
        /// Inspects all validator bindings on a control to determine if
        /// any have fired, rendering the control invalid from a content
        /// perspective.
        /// </summary>
        /// <param name="obj">The control containing the content we want to ensure is valid</param>
        /// <returns>True if the object content passed all its bound validations</returns>
        public bool HasValidContent(DependencyObject obj)
        {
            // The dependency object is valid if it has no errors and all
            // of its children (that are dependency objects) are error-free.
            return !Validation.GetHasError(obj) &&
                        LogicalTreeHelper.GetChildren(obj)
                        .OfType<DependencyObject>()
                        .All(HasValidContent);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BaseAWSControl : BaseAWSUserControl, IAWSToolkitControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(BaseAWSControl));

        public BaseAWSControl()
        {
            this.IsEnabled = !this.SupportsBackGroundDataLoad;

            SetResourceReference(Control.BackgroundProperty, "awsDialogBackgroundBrushKey");
            SetResourceReference(Control.ForegroundProperty, "awsDialogTextBrushKey");
            SetResourceReference(Window.FontFamilyProperty, ShellProviderThemeResources.EnvironmentFontFamilyKey);
            SetResourceReference(Window.FontSizeProperty, ShellProviderThemeResources.EnvironmentFontSizeKey);
        }

        public virtual string Title => "Undefined";

        public virtual string UniqueId => Guid.NewGuid().ToString();

        public virtual string MetricId => this.GetType().FullName;

        public virtual bool IsUniquePerAccountAndRegion => true;

        public virtual bool Validated()
        {
            return true;
        }

        public virtual bool OnCommit()
        {
            return true;
        }

        public virtual void RefreshInitialData(object initialData)
        {
        }

        public virtual object GetInitialData()
        {
            return null;
        }

        public virtual void OnEditorOpened(bool success)
        {
        }

        public virtual bool SupportsBackGroundDataLoad => false;

        public void ExecuteBackGroundLoadDataLoad()
        {
            if (SupportsBackGroundDataLoad)
            {
                ThreadPool.QueueUserWorkItem(this.executeBackGroundLoadDataLoad);
            }
        }

        public virtual bool SupportsDynamicOKEnablement => false;

        private void executeBackGroundLoadDataLoad(object state)
        {
            try
            {
                LOGGER.InfoFormat("Beginning loading data for {0}", this.UniqueId);
                object dataContent = LoadAndReturnModel();
                if (dataContent == null)
                {
                    LOGGER.InfoFormat("No data returned for {0}", this.UniqueId);
                    return;
                }

                LOGGER.InfoFormat("Data loaded data for {0}", this.UniqueId);

                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    try
                    {
                        if (this.DataContext != dataContent)
                        {
                            this.DataContext = dataContent;
                            this.IsEnabled = true;
                            this.PostDataContextBound();
                        }
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Error setting data context for editor " + this.UniqueId + ".", e);
                    }
                }));

            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading model for editor " + this.UniqueId + ".", e);
                if (this.IsVisible)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error loading data: " + e.Message);
                }
            }
        }

        protected virtual object LoadAndReturnModel()
        {
            return null;
        }

        protected virtual void PostDataContextBound()
        {
        }

    }
}
