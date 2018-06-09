using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using System.Windows.Forms;
using System.Windows.Media;
using System.Drawing;
using System.IO;

namespace ScreenRecordPlusChrome
{
    public enum EyeDeviceType
    {
        None = 0,
        EyeTribe = 1,
        Tobii = 2
    }
    public partial class EyeDevice
    {
        private EyeDeviceType _eyeDeviceType;
        private EyeDevice_EyeTribe _eyeDeviceEyeTribe = null;
        private EyeDevice_Tobii _eyeDeviceTobii = null;
        private int _GazeDurationThreshold;

        public EyeDevice(EyeDeviceType eyeDeviceType)
        {
            InitializeComponent();

            InitComponent(eyeDeviceType);
        }
        private void InitComponent(EyeDeviceType eyeDeviceType)
        {
            //Set Variables
            _eyeDeviceType = eyeDeviceType;
            if (_eyeDeviceType == EyeDeviceType.None)
            {
                return;
            }
            else if (_eyeDeviceType == EyeDeviceType.EyeTribe)
            {
                _eyeDeviceEyeTribe = new EyeDevice_EyeTribe();
            }
            else if (_eyeDeviceType == EyeDeviceType.Tobii)
            {
                _eyeDeviceTobii = new EyeDevice_Tobii();
            }

            //Set Button
            this.bt_connect.Background = System.Windows.Media.Brushes.OrangeRed;
            this.bt_calibrate.IsEnabled = false;
            this.bt_accept.IsEnabled = false;
            this.bt_accept.Visibility = System.Windows.Visibility.Hidden;
            this.lb_calibrationQ.Content = "Calibration Quality:";
            if(!hasFixation())
                this.g_gdthreshold.Visibility = System.Windows.Visibility.Hidden;

            this.g_eyeTraceBox.Children.Clear();
            switch (_eyeDeviceType)
            {
                case EyeDeviceType.EyeTribe:
                    if (_eyeDeviceEyeTribe != null)
                    {
                        this.g_eyeTraceBox.Children.Add(_eyeDeviceEyeTribe.GetGazeStatusBox());
                        _eyeDeviceEyeTribe.CalibrateOnResult += new EyeDevice_EyeTribe.CalibrateEventHandler(OnCalibrateResult);
                    }
                    break;
                case EyeDeviceType.Tobii:
                    if (_eyeDeviceTobii != null) {
                        this.g_eyeTraceBox.Children.Add(_eyeDeviceTobii.GetGazeStatusBox());
                        _eyeDeviceTobii.OnFixation += new EyeDevice_Tobii.FixationEventHandler(OnFixation);
                    }
                    break;
                default:
                    break;
            }
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
        private void OnCalibrateResult(object sender, CalibrateEventArgs e)
        {
            this.lb_calibrationQ.Dispatcher.Invoke(new Action(() =>
            {
                this.lb_calibrationQ.Content = "Calibration Quality: " + e.calibrateResult;
            }));
        }
        private void OnFixation(object sender, FixationEventArgs e)
        {
            if (hasFixation())
            {
                OnFixationEvent(this, e);
            }
        }
        private void UpdataState(bool isConnected)
        {
            if (!isConnected)
            {
                this.bt_connect.Background = System.Windows.Media.Brushes.OrangeRed;
                this.bt_calibrate.IsEnabled = false;
                this.bt_accept.IsEnabled = false;
                this.bt_accept.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this.bt_connect.Background = System.Windows.Media.Brushes.YellowGreen;
                this.bt_calibrate.IsEnabled = true;
                this.bt_accept.IsEnabled = true;
                this.bt_accept.Visibility = System.Windows.Visibility.Visible;
            }
        }

        #region Button
        private void bt_connect_Click(object sender, RoutedEventArgs e)
        {
            switch (_eyeDeviceType)
            {
                case EyeDeviceType.EyeTribe:
                    if (_eyeDeviceEyeTribe != null)
                    {
                        if (_eyeDeviceEyeTribe.isConnected())
                            _eyeDeviceEyeTribe.Disconnect();
                        else {
                            _eyeDeviceEyeTribe.Connect();
                            this.g_eyeTraceBox.Children.Add(_eyeDeviceEyeTribe.GetGazeStatusBox());
                        }
                        UpdataState(_eyeDeviceEyeTribe.isConnected());
                    }
                    break;
                case EyeDeviceType.Tobii:
                    if (_eyeDeviceTobii != null)
                    {
                        if (_eyeDeviceTobii.isConnected())
                            _eyeDeviceTobii.Disconnect();
                        else {
                            _eyeDeviceTobii.Connect();
                        }
                        UpdataState(_eyeDeviceTobii.isConnected());
                    }
                    break;
                default:
                    break;
            }
        }
        private void bt_calibrate_Click(object sender, RoutedEventArgs e)
        {
            switch (_eyeDeviceType)
            {
                case EyeDeviceType.EyeTribe:
                    if (_eyeDeviceEyeTribe != null)
                    {
                        // Check connectivitiy status
                        if (!_eyeDeviceEyeTribe.isConnected())
                        {
                            _eyeDeviceEyeTribe.Connect();
                            this.g_eyeTraceBox.Children.Add(_eyeDeviceEyeTribe.GetGazeStatusBox());
                            UpdataState(_eyeDeviceEyeTribe.isConnected());
                        }
                        else
                            _eyeDeviceEyeTribe.Calibrate();
                    }
                    break;
                case EyeDeviceType.Tobii:
                    if (_eyeDeviceTobii != null)
                    {
                        // Check connectivitiy status
                        if (!_eyeDeviceTobii.isConnected())
                        {
                            _eyeDeviceTobii.Connect();
                            UpdataState(_eyeDeviceTobii.isConnected());
                        }
                        else
                            _eyeDeviceTobii.Calibrate();
                    }
                    break;
                default:
                    break;
            }
        }
        public void bt_accept_Click(object sender, RoutedEventArgs e)
        {
            Window mw = System.Windows.Application.Current.MainWindow;
            foreach (System.Windows.Controls.Button bt in FindVisualChildren<System.Windows.Controls.Button>(mw))
            {
                if (bt.Content.ToString() == "Start")
                {
                    bt.IsEnabled = true;
                }
            }
        }
        private void iud_gdthreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _GazeDurationThreshold = (int)iud_gdthreshold.Value;
        }
        #endregion

        #region  Get MainWindow Controls
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #endregion

        #region Public Function
        public delegate void FixationEventHandler(object sender, FixationEventArgs e);
        public event FixationEventHandler OnFixationEvent;

        public void SetButtonEnabled(bool enabled)
        {
            bt_accept.IsEnabled = enabled;
            bt_calibrate.IsEnabled = enabled;
            bt_connect.IsEnabled = enabled;
            iud_gdthreshold.IsEnabled = enabled;
        }
        public bool hasFixation() {
            switch (_eyeDeviceType)
            {
                case EyeDeviceType.EyeTribe:
                    return false;
                case EyeDeviceType.Tobii:
                    return true;
                default:
                    return false;
            }
        }
        public int GetGazeDurationThreshold() {
            return _GazeDurationThreshold;
        }
        public System.Windows.Point GetGazePoints()
        {
            if (_eyeDeviceEyeTribe != null)
                return _eyeDeviceEyeTribe.GetGazePoints();
            else if (_eyeDeviceTobii != null)
                return _eyeDeviceTobii.GetGazePoints();
            else
                return new System.Windows.Point(0,0);
        }
        public void Dispose() {
            //Close EyeTribeUC
            if (_eyeDeviceEyeTribe != null)
            {
                _eyeDeviceEyeTribe.CalibrateOnResult -= new EyeDevice_EyeTribe.CalibrateEventHandler(OnCalibrateResult);
                _eyeDeviceEyeTribe.Dispose();
                _eyeDeviceEyeTribe = null;
            }
            if (_eyeDeviceTobii != null) {
                _eyeDeviceTobii.OnFixation -= new EyeDevice_Tobii.FixationEventHandler(OnFixation);
                _eyeDeviceTobii.Dispose();
                _eyeDeviceTobii = null;
            }
        }
        #endregion

        



    }
}
