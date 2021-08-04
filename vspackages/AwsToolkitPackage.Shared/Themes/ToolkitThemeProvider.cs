using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Themes;

using log4net;

using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// VS Specific implementation that applies (or reverts) VS themes and Toolkit themes on controls.
    /// Do not use this class directly. Use <see cref="ToolkitThemes"/> to insulate code from VS packages.
    /// </summary>
    public class ToolkitThemeProvider : IToolkitThemeProvider
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ToolkitThemeProvider));
        private static readonly Collection<ResourceDictionary> ThemeResources = BuildThemeResources();

        private readonly IToolkitThemeBrushKeys _themeBrushKeys = new ToolkitThemeBrushKeys();

        public void UseToolkitThemePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (!DesignerProperties.GetIsInDesignMode(target))
            {
                if (target is FrameworkElement element)
                {
                    bool newPropertyValue = (bool) eventArgs.NewValue;

                    // Handle VS specific Theming
                    Community.VisualStudio.Toolkit.Themes.SetUseVsTheme(element, newPropertyValue);

                    // Apply Toolkit specific Theming and overrides
                    if (newPropertyValue)
                    {
                        if (element is Control control)
                        {
                            // TODO : This is a temporary foreground/background assignment.
                            // Community.VisualStudio.Toolkit version 15.0.75.103 has a bug that sets foreground (text) to a light grey
                            // When the next non-preview version is available, this can be removed.
                            control.SetResourceReference(Control.BackgroundProperty, ThemedDialogColors.WindowPanelBrushKey);
                            control.SetResourceReference(Control.ForegroundProperty, ThemedDialogColors.WindowPanelTextBrushKey);
                        }

                        // Only merge the styles after the element has been initialized.
                        // If the element hasn't been initialized yet, add an event handler
                        // so that we can merge the styles once it has been initialized.
                        if (!element.IsInitialized)
                        {
                            element.Initialized += OnElementInitialized;
                        }
                        else
                        {
                            MergeStyles(element);
                        }
                    }
                    else
                    {
                        UnmergeStyles(element);
                    }
                }
            }
        }

        public IToolkitThemeBrushKeys GetToolkitThemeBrushKeys()
        {
            return _themeBrushKeys;
        }

        private static void OnElementInitialized(object sender, EventArgs args)
        {
            var element = (FrameworkElement) sender;
            MergeStyles(element);
            element.Initialized -= OnElementInitialized;
        }

        /// <summary>
        /// Apply Toolkit Theming to the element's Resources
        /// </summary>
        private static void MergeStyles(FrameworkElement element)
        {
            Collection<ResourceDictionary> dictionaries = element.Resources.MergedDictionaries;
            foreach (var themeResource in ThemeResources)
            {
                if (!dictionaries.Contains(themeResource))
                {
                    dictionaries.Add(themeResource);
                }
            }
        }

        /// <summary>
        /// Remove Toolkit Theming from the element's Resources
        /// </summary>
        private static void UnmergeStyles(FrameworkElement element)
        {
            Collection<ResourceDictionary> dictionaries = element.Resources.MergedDictionaries;
            foreach (var themeResource in ThemeResources)
            {
                dictionaries.Remove(themeResource);
            }
        }

        private static Collection<ResourceDictionary> BuildThemeResources()
        {
            Collection<ResourceDictionary> dictionaries = new Collection<ResourceDictionary>();

            try
            {
                var controlStyles = LoadControlStyles();
                dictionaries.Add(controlStyles);

                // Add and modify theme resources as needed here
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return dictionaries;
        }

        private static ResourceDictionary LoadControlStyles()
        {
            ResourceDictionary resources = new ResourceDictionary();

            try
            {
                var assemblyName = GetAssemblyName();
                var uri = new Uri($"/{assemblyName};component/Themes/ControlStyles.xaml", UriKind.RelativeOrAbsolute);
                resources = new ResourceDictionary()
                {
                    Source = uri
                };

                return resources;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return resources;
        }

        private static string GetAssemblyName()
        {
            return typeof(ToolkitThemeProvider).Assembly.GetName().Name;
        }
    }
}
