using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

using Tobii.Interaction;
using Tobii.Interaction.Wpf;

namespace ScreenRecordPlusChrome
{
    public class FixationEventArgs : EventArgs
    {
        public bool isBegin { get; set; }
        public System.Windows.Point fixation { get; set; }
        public FixationEventArgs(bool isbegin, System.Windows.Point f)
        {
            isBegin = isbegin;
            fixation = f;
        }
    }
    class EyeDevice_Tobii
    {
        private Host _TobiiHost = new Host();
        private WpfInteractorAgent _TobiiWPFAgent = null;
        private GazePointDataStream _GazePointDataStream = null;
        private EyePositionStream _EyePositionStream = null;
        private FixationDataStream _FixationDataStream = null;
        private Canvas _TobiiTrackStatus = new Canvas();
        private System.Windows.Shapes.Rectangle _LeftEye = new System.Windows.Shapes.Rectangle();
        private System.Windows.Shapes.Rectangle _RightEye = new System.Windows.Shapes.Rectangle();
        private System.Windows.Point _GazePosition = new System.Windows.Point(0, 0);
        private List<System.Windows.Point> _Fixations = new List<System.Windows.Point>();
        private bool _isConnected = false;

        public delegate void FixationEventHandler(object sender, FixationEventArgs e);
        public event FixationEventHandler OnFixation;

        public EyeDevice_Tobii() {
            _LeftEye.Fill = Brushes.White;
            _LeftEye.Width = 20;
            _LeftEye.Height = 20;
            _LeftEye.RadiusX = 10;
            _LeftEye.RadiusY = 10;
            _LeftEye.Visibility = System.Windows.Visibility.Hidden;

            _RightEye.Fill = Brushes.White;
            _RightEye.Width = 20;
            _RightEye.Height = 20;
            _RightEye.RadiusX = 10;
            _RightEye.RadiusY = 10;
            _RightEye.Visibility = System.Windows.Visibility.Hidden;

            _TobiiTrackStatus.Background = Brushes.Black;
            _TobiiTrackStatus.Children.Add(_LeftEye);
            _TobiiTrackStatus.Children.Add(_RightEye);
        }
        #region Public Function
        public void Connect()
        {
            if (!_isConnected)
            {
                _TobiiHost.EnableConnection();
                _TobiiWPFAgent = _TobiiHost.InitializeWpfAgent();

                _GazePointDataStream = _TobiiHost.Streams.CreateGazePointDataStream(Tobii.Interaction.Framework.GazePointDataMode.Unfiltered);
                _GazePointDataStream.Next += OnGazePointData;
                _FixationDataStream = _TobiiHost.Streams.CreateFixationDataStream();
                _FixationDataStream.Next += OnFixationData;
                _EyePositionStream = _TobiiHost.Streams.CreateEyePositionStream(true);
                _EyePositionStream.Next += OnEyePositionData;
                _isConnected = true;
            }
        }
        public void Disconnect()
        {
            if (_isConnected)
            {
                _GazePointDataStream.Next -= OnGazePointData;
                _FixationDataStream.Next -= OnFixationData;
                _EyePositionStream.Next -= OnEyePositionData;
                _TobiiHost.DisableConnection();
                _isConnected = false;
            }
        }
        public bool isConnected()
        {
            return _isConnected;
        }
        public void Calibrate()
        {
            _TobiiHost.Context.LaunchConfigurationTool(Tobii.Interaction.Framework.ConfigurationTool.TestEyeTracking, (data) =>
            { });
        }
        public System.Windows.Point GetGazePoints() {
            return _GazePosition;
        }
        public Canvas GetGazeStatusBox()
        {
            return _TobiiTrackStatus;
        }
        public void Dispose()
        {
            Disconnect();
            _TobiiHost.Dispose();
        }
        #endregion

        private void OnGazePointData(object sender, StreamData<GazePointData> streamData)
        {
            _GazePosition.X = streamData.Data.X;
            _GazePosition.Y = streamData.Data.Y;
        }
        private void OnFixationData(object sender, StreamData<FixationData> streamData)
        {
            switch (streamData.Data.EventType)
            {
                case Tobii.Interaction.Framework.FixationDataEventType.Begin:
                    _Fixations.Clear();
                    _Fixations.Add(new System.Windows.Point(streamData.Data.X, streamData.Data.Y));
                    OnFixation(this, new FixationEventArgs(true, new System.Windows.Point(0, 0)));
                    break;
                case Tobii.Interaction.Framework.FixationDataEventType.Data:
                    _Fixations.Add(new System.Windows.Point(streamData.Data.X, streamData.Data.Y));
                    break;
                case Tobii.Interaction.Framework.FixationDataEventType.End:
                    _Fixations.Add(new System.Windows.Point(streamData.Data.X, streamData.Data.Y));
                    double avgX = _Fixations.Average(f => f.X);
                    double avgY = _Fixations.Average(f => f.Y);
                    if (avgX > 0 && avgY>0)
                        OnFixation(this, new FixationEventArgs(false, new System.Windows.Point(avgX, avgY)));
                    _Fixations.Clear();
                    break;
                default:
                    break;
            }
        }
        private void OnEyePositionData(object sender, StreamData<EyePositionData> streamData)
        {
            _TobiiTrackStatus.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (streamData.Data.HasLeftEyePosition)
                {
                    _LeftEye.Visibility = System.Windows.Visibility.Visible;
                    Canvas.SetLeft(_LeftEye, (1 - streamData.Data.LeftEyeNormalized.X) * _TobiiTrackStatus.ActualWidth);
                    Canvas.SetTop(_LeftEye, streamData.Data.LeftEyeNormalized.Y * _TobiiTrackStatus.ActualHeight);
                }
                else
                {
                    _LeftEye.Visibility = System.Windows.Visibility.Hidden;
                }

                if (streamData.Data.HasRightEyePosition)
                {
                    _RightEye.Visibility = System.Windows.Visibility.Visible;
                    Canvas.SetLeft(_RightEye, (1 - streamData.Data.RightEyeNormalized.X) * _TobiiTrackStatus.ActualWidth);
                    Canvas.SetTop(_RightEye, streamData.Data.RightEyeNormalized.Y * _TobiiTrackStatus.ActualHeight);
                }
                else
                {
                    _RightEye.Visibility = System.Windows.Visibility.Hidden;
                }
            });
        }
        
    }
}
