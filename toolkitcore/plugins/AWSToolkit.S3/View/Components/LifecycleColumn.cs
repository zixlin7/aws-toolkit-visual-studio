using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.S3.Model;
using System.Windows.Input;


namespace Amazon.AWSToolkit.S3.View.Components
{
    public abstract class LifecycleColumn : DataGridTextColumn
    {
        protected ComboBox _cellEditControl;

        protected TextBlock _cellValueTextBox;

        protected RadioButton _useNoExp;
        protected RadioButton _useDays;
        protected RadioButton _useDates;

        protected TextBox _days;
        protected Calendar _calendar;

        protected LifecycleRuleModel _rule;

        Button _okButton;
        Button _cancelButton;

        TextBlock _infoText;

        DataGrid _parentGrid;

        protected static readonly Thickness TEXT_MARGIN = new Thickness(5, 0, 5, 0);

        protected FrameworkElement GenerateElement(DataGridCell cell, object dataItem, Func<LifecycleRuleModel, string> funcToGetValue)
        {
            var tb = new TextBlock();
            tb.Margin = TEXT_MARGIN;

            var rule = dataItem as LifecycleRuleModel;
            if (rule != null)
                tb.Text = funcToGetValue(rule);

            return tb;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            this._parentGrid = AWSToolkit.CommonUI.DataGridHelper.GetVisualParent<DataGrid>(cell);
            this._rule = dataItem as LifecycleRuleModel;
            if (this._rule == null)
            {
                this._rule = new LifecycleRuleModel();
            }

            this._cellEditControl = new ComboBox();
            this._cellEditControl.Loaded += new RoutedEventHandler(CurrentEditedControl_Loaded);
            this._cellEditControl.Style = cell.FindResource("CellExpirationEditPanel") as Style;

            return this._cellEditControl;
        }

        void CurrentEditedControl_Loaded(object sender, RoutedEventArgs e)
        {
            BindToEditControlSubControls();
            SetupEditPanelControlValues();

            if (this._infoText != null)
                this._infoText.Text = this.GetInfoText();
        }

        protected abstract void SetupEditPanelControlValues();

        protected virtual string GetInfoText() { return ""; }
        protected abstract string GetFeatureName();

        protected void BindToEditControlSubControls()
        {
            ControlTemplate template = this._cellEditControl.Template;
            _cellValueTextBox = template.FindName("PART_ValueTextBox", this._cellEditControl) as TextBlock;

            _useNoExp = template.FindName("PART_NoExpiration", this._cellEditControl) as RadioButton;

            _useDays = template.FindName("PART_UseDays", this._cellEditControl) as RadioButton;
            _days = template.FindName("PART_Days", this._cellEditControl) as TextBox;

            _useDates = template.FindName("PART_UseDate", this._cellEditControl) as RadioButton;
            _calendar = template.FindName("PART_Date", this._cellEditControl) as Calendar;

            _okButton = template.FindName("PART_OKButton", this._cellEditControl) as Button;
            if (_okButton != null)
                _okButton.Click += new RoutedEventHandler(OKButton_Click);
            _cancelButton = template.FindName("PART_CancelButton", this._cellEditControl) as Button;
            if (_cancelButton != null)
                _cancelButton.Click += new RoutedEventHandler(CancelButton_Click);

            _infoText = template.FindName("PART_InfoText", this._cellEditControl) as TextBlock;
            _useNoExp.Content = "Do not use " + this.GetFeatureName().ToLower();

            _days.PreviewTextInput += new TextCompositionEventHandler(_days_PreviewTextInput);
            _calendar.BlackoutDates.AddDatesInPast();
        }

        void _days_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this._parentGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this._parentGrid.CancelEdit(DataGridEditingUnit.Row);
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }
    }
}
