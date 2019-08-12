using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using log4net;

namespace Amazon.AWSToolkit.Primitives
{
    /// <summary>
    /// Interaction logic for DateTimePickerControl.xaml
    /// </summary>
    public partial class DateTimePickerControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(DateTimePickerControl));

        const string AM = "AM";
        const string PM = "PM";

        bool _settingUI = false;

        public DateTimePickerControl()
        {
            InitializeComponent();

            fillTimeComboBoxes();
            SelectedDateTime = DateTime.Now;
        }

        void fillTimeComboBoxes()
        {
            var hours = new List<int>();
            for (int i = 1; i <= 12; i++)
            {
                hours.Add(i);
            }
            this._ctlHours.ItemsSource = hours;

            var minutes = new List<string>();
            for (int i = 0; i <= 59; i++)
            {
                minutes.Add(i < 10 ? "0" + i.ToString() : i.ToString());
            }
            this._ctlMinutes.ItemsSource = minutes;

            this._ctlAMPM.ItemsSource = new string[] { AM, PM };
        }

        void onDateTimeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The Capture/Release code is to work around a bug with the calendar control that 
            // grabs control of the mouse and causing the next action to require 2 clicks.
            this._ctlDatePicker.CaptureMouse();
            try
            {
                getUITime();
            }
            finally
            {
                this._ctlDatePicker.ReleaseMouseCapture();
            }
        }


        void setUITime()
        {
            this._settingUI = true;
            try
            {
                DateTime dt = this.SelectedDateTime;

                string halfDay = dt.Hour < 12 ? AM : PM;

                this._ctlDatePicker.SelectedDate = dt.Date;
                this._ctlAMPM.SelectedItem = halfDay;

                if (halfDay == AM)
                {
                    if (dt.Hour == 0)
                        this._ctlHours.SelectedItem = 12;
                    else
                        this._ctlHours.SelectedItem = dt.Hour;
                }
                else
                {
                    if (dt.Hour == 12)
                        this._ctlHours.SelectedItem = 12;
                    else
                        this._ctlHours.SelectedItem = dt.Hour - 12;
                }
                this._ctlMinutes.SelectedItem = dt.Minute < 10 ? "0" + dt.Minute.ToString() : dt.Minute.ToString();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error setting date to control", e);
            }
            finally
            {
                this._settingUI = false;
            }
        }

        void getUITime()
        {
            if (this._settingUI)
                return;

            try
            {
                DateTime dt = this._ctlDatePicker.SelectedDate.GetValueOrDefault();
                string halfDay = (string)this._ctlAMPM.SelectedItem;
                int hours = (int)this._ctlHours.SelectedValue;

                if (halfDay == AM && hours == 12)
                    hours = 0;
                else if (halfDay == PM && hours != 12)
                    hours += 12;

                dt = dt.AddHours(hours);
                dt = dt.AddMinutes(int.Parse(this._ctlMinutes.SelectedValue.ToString()));

                SelectedDateTime = dt;

                var expr = this.GetBindingExpression(SelectedDateTimeProperty);
                if (expr != null)
                    expr.UpdateTarget();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error getting date from control", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting date from control: " + e.Message);
            }
        }

        #region SelectedDateTime Dependency Property
        public static readonly DependencyProperty SelectedDateTimeProperty =
            DependencyProperty.Register("SelectedDateTime", typeof(DateTime), typeof(DateTimePickerControl),
            new PropertyMetadata(DateTime.Now, SelectedDateNameChangedCallback, SelectedDateTimeCoerceCallback),
            SelectedDateTimeValidateCallback);

        public DateTime SelectedDateTime
        {
            get => (DateTime)GetValue(SelectedDateTimeProperty);
            set => SetValue(SelectedDateTimeProperty, value);
        }

        private static void SelectedDateNameChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var picker = obj as DateTimePickerControl;
            if (picker == null)
                return;

            picker.setUITime();
        }

        private static object SelectedDateTimeCoerceCallback(DependencyObject obj, object o)
        {
            DateTime dt;
            DateTime.TryParse(o.ToString(), out dt);
            return dt;
        }

        private static bool SelectedDateTimeValidateCallback(object value)
        {
            DateTime dt;
            return DateTime.TryParse(value.ToString(), out dt);
        }
        #endregion
    }
}
