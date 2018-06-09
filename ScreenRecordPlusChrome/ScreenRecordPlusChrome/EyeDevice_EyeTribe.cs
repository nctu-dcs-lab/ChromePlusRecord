using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EyeTribe.ClientSdk;
using EyeTribe.ClientSdk.Data;
using EyeTribe.Controls;
using EyeTribe.Controls.TrackBox;
using EyeTribe.Controls.Calibration;

using System.Windows.Forms;
namespace ScreenRecordPlusChrome
{
    public class CalibrateEventArgs : EventArgs
    {
        public string calibrateResult { get; set; }
        public CalibrateEventArgs(string result) {
            calibrateResult = result;
        }
    }
    class EyeDevice_EyeTribe : IGazeListener
    {
        private System.Windows.Point _GazePosition = new System.Windows.Point(0, 0);
        public delegate void CalibrateEventHandler(object sender, CalibrateEventArgs e);
        public event CalibrateEventHandler CalibrateOnResult;

        public EyeDevice_EyeTribe() {}
        #region Public Function
        public void Connect()
        {
            // Connect client
            GazeManager.Instance.Activate();
            // Start getting eye data
            StartGetGazePoints();
        }
        public void Disconnect()
        {
            // Stop getting eye data
            StopGetGazePoints();
            // Disconnect client
            GazeManager.Instance.Deactivate();
        }
        public bool isConnected()
        {
            return GazeManager.Instance.IsActivated;
        }
        public void Calibrate()
        {
            // Update screen to calibrate where the window currently is
            Screen activeScreen = Screen.PrimaryScreen;

            // Initialize and start the calibration
            CalibrationRunner calRunner = new CalibrationRunner(activeScreen, activeScreen.Bounds.Size, 9);
            calRunner.OnResult += calRunner_OnResult;
            calRunner.Start();
        }
        public System.Windows.Point GetGazePoints()
        {
            return _GazePosition;
        }
        public TrackBoxStatus GetGazeStatusBox() {
            // Return a fresh instance of the trackbox in case we reinitialize the client connection.
            return new TrackBoxStatus();
        }
        public void Dispose() {
            Disconnect();
        }
        #endregion

        #region Calibrate Fuction
        private void calRunner_OnResult(object sender, CalibrationRunnerEventArgs e)
        {
            if (e.Result == CalibrationRunnerResult.Success) {
                if (GazeManager.Instance.LastCalibrationResult != null) {
                    // Send event to parent
                    if (CalibrateOnResult != null)
                        CalibrateOnResult(this, new CalibrateEventArgs(RatingFunction(GazeManager.Instance.LastCalibrationResult)));
                }
            }
            else {
                // Send event to parent
                if (CalibrateOnResult != null)
                    CalibrateOnResult(this, new CalibrateEventArgs("FAILED"));
            }
        }
        private string RatingFunction(CalibrationResult result)
        {
            if (result == null)
                return "FAILED";

            double accuracy = result.AverageErrorDegree;

            if (accuracy < 0.5)
                return "PERFECT";

            if (accuracy < 0.7)
                return "GOOD";

            if (accuracy < 1)
                return "MODERATE";

            if (accuracy < 1.5)
                return "POOR";

            return "REDO";
        }
        #endregion

        #region Get Eye Data
        public void OnGazeUpdate(GazeData gazeData)
        {
            _GazePosition.X = gazeData.SmoothedCoordinates.X;
            _GazePosition.Y = gazeData.SmoothedCoordinates.Y;
        }
        private void StartGetGazePoints()
        {
            GazeManager.Instance.AddGazeListener(this);
        }
        private void StopGetGazePoints()
        {
            GazeManager.Instance.RemoveGazeListener(this);
        }
        #endregion

    }
}
