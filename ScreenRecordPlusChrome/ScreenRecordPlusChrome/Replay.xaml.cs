/*******************************
 * Title: WPF Color Picker Like Office 2007
 * Author: Saraf Talukder
 * Date: 2010
 * Availability: https://www.codeproject.com/Articles/52795/WPF-Color-Picker-Like-Office
 * *****************************/

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
using System.Windows.Shapes;

using System.IO;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Data.SQLite;

namespace ScreenRecordPlusChrome
{
    public enum PlayState
    {
        Stopped,
        Paused,
        Running,
        Init
    }

    internal enum MediaType
    {
        Audio,
        Video
    }

    internal enum ButtonPic
    {
        Play,
        Pause,
        Mute,
        Unmute
    }

    public partial class Replay : Window
    {
        #region Variable
        private string _mainDir = null;
        private string _projectName = null;
        private long _totalFrameNumber = -1;
        private long _currentFrameNumber = 0;
        private bool _istimeBarEnabled = true;

        private System.Windows.Forms.OpenFileDialog _openFileDialog = null;
        private ReplayControl _replayControl = null;

        private DBReplayInfo _dbReplayInfo = null;
        private List<ReplayInfo> _replayInfo = null;
        #endregion

        public Replay(string mainDir, string projectName)
        {
            InitializeComponent();

            InitComponent(mainDir, projectName);
            Loaded += OnLoaded;
            Closed += OnClosed;
            SizeChanged += OnResized;
        }

        private void InitComponent(string mainDir, string projectName)
        {
            if (mainDir != null && projectName != null)
            {
                this._mainDir = mainDir;
                this._projectName = projectName;
                this.Title = "Replay - " + this._projectName;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Can't find data folder!", "ERROR");
                this.Close();
            }
            this._openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._openFileDialog.Filter = @"Video Files (*.avi; *.qt; *.mov; *.mpg; *.mpeg; *.m1v)|*.avi; *.qt; *.mov; *.mpg; *.mpeg; *.m1v|Audio files (*.wav; *.mpa; *.mp2; *.mp3; *.au; *.aif; *.aiff; *.snd)|*.wav; *.mpa; *.mp2; *.mp3; *.au; *.aif; *.aiff; *.snd|MIDI Files (*.mid, *.midi, *.rmi)|*.mid; *.midi; *.rmi|Image Files (*.jpg, *.bmp, *.gif, *.tga)|*.jpg; *.bmp; *.gif; *.tga|All Files (*.*)|*.*";

            this._replayControl = new ReplayControl(this._mainDir, this._projectName);
            this._replayControl.MediaComplete += new EventHandler(OnMediaComplete);
            this._replayControl.FrameEvent += new ReplayControl.FrameEventHandler(OnFrameEvent);
            wfh_replay_window.Child = this._replayControl;
        }
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this._dbReplayInfo = new DBReplayInfo(this._mainDir, this._projectName);
            this._dbReplayInfo.DBConnect();
            this._replayInfo = this._dbReplayInfo.GetAllReplayInfo();
            this._dbReplayInfo.DBDisconnect();
            this._dbReplayInfo = null;
            this.cb_replay_videoName.ItemsSource = this._replayInfo.Select(item => item.userName);
            this.cb_replay_videoName.SelectedIndex = 0;
            if (!File.Exists(_mainDir + @"\" + _projectName + @"\Database\" + _projectName))
            {
                System.Windows.Forms.MessageBox.Show("Can't find the database!", "ERROR");
                this.Close();
            }
        }
        private void OnClosed(object sender, EventArgs e)
        {
            CloseInterfaces();
        }
        private void OnResized(object sender, SizeChangedEventArgs e)
        {
            g_replay_window.Height = this.ActualHeight - g_replay_topController.ActualHeight - g_replay_toolbar.ActualHeight - g_replay_url.ActualHeight - g_replay_bottomController.ActualHeight - 40;
        }
        private void OnMediaComplete(object sender, EventArgs e)
        {
            ChangeButtonPicture(ButtonPic.Play);
            s_replay_timeBar.IsEnabled = true;
            _istimeBarEnabled = true;
            bt_replay_pagebackward.IsEnabled = true;
            bt_replay_pageforward.IsEnabled = true;
        }
        private void OnFrameEvent(object sender, FrameEventArgs e)
        {
            if (_istimeBarEnabled == false && this._currentFrameNumber <= this._totalFrameNumber && this._currentFrameNumber >= 0)
            {
                if (_replayControl != null)
                {
                    //this._currentFrameNumber = _replayControl.GetPosition();
                    this._currentFrameNumber = e.frameID;
                    UpateURLAndCurrentTime(this._currentFrameNumber);
                    //Console.WriteLine("OnFrameEvent: " + _currentFrameNumber.ToString());
                }
                this.Dispatcher.BeginInvoke((Action)delegate()
                {

                    s_replay_timeBar.Value = (int)this._currentFrameNumber;
                    //Console.WriteLine("OnFrameEvent: " + this._currentFrameNumber.ToString() + ", " + s_replay_timeBar.Value.ToString() + ", " + _istimeBarEnabled.ToString());
                    //this._currentFrameNumber++;
                });

                //System.Windows.Forms.MessageBox.Show("frame" + _currentFrameNumber.ToString());
            }
        }

        #region Button
        private void bt_replay_exportVideo_Click(object sender, RoutedEventArgs e)
        {
            //_replayControl.FileOpen_Click();
            string userName = this._replayInfo[cb_replay_videoName.SelectedIndex].userName;
            string videoPath = this._replayInfo[cb_replay_videoName.SelectedIndex].videoPath;
            ExportVideoInfo mevinfo = new ExportVideoInfo();
            ExportVideoInfo gevinfo = new ExportVideoInfo();
            if (bt_toolbar_mouse_track.Background == Brushes.Gray)
                mevinfo.isTrackVisible = true;
            if (bt_toolbar_mouse_cursor.Background == Brushes.Gray)
                mevinfo.isCursorVisible = true;
            if (bt_toolbar_mouse_cursor_circle.Background == Brushes.Gray)
                mevinfo.isCircleVisible = true;
            if (bt_toolbar_mouse_fixation.Background == Brushes.Gray)
                mevinfo.isFixationVisible = true;
            if (bt_toolbar_mouse_scanpath.Background == Brushes.Gray)
                mevinfo.isPathVisible = true;
            if (bt_toolbar_mouse_fixationID.Background == Brushes.Gray)
                mevinfo.isFixationIDVisible = true;
            mevinfo.TrackPenWidth = (int)iud_toolbar_mouse_track_linewidth.Value;
            mevinfo.CirclePenWidth = (int)iud_toolbar_mouse_cursor_circle_linewidth.Value;
            mevinfo.FixationPenWidth = (int)iud_toolbar_mouse_fixation_linewidth.Value;
            mevinfo.PathPenWidth = (int)iud_toolbar_mouse_scanpath_linewidth.Value;
            mevinfo.TrackPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_mouse_track_colorpicker.Text);
            mevinfo.CirclePenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_mouse_cursor_circle_colorpicker.Text);
            mevinfo.FixationPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_mouse_fixation_colorpicker.Text);
            mevinfo.PathPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_mouse_scanpath_colorpicker.Text);
            mevinfo.FixationIDPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_mouse_fixationID_colorpicker.Text);
            if (bt_toolbar_mouse_track_type.Content.ToString() == "D")
                mevinfo.TrackType = 1;
            mevinfo.FixationRate = (int)iud_toolbar_mouse_fixation_rate.Value;
            mevinfo.BrowserToolbarHeight = (int)iud_toolbar_mouse_browsertoolbarHeight.Value;


            if (bt_toolbar_eye_track.Background == Brushes.Gray)
                gevinfo.isTrackVisible = true;
            if (bt_toolbar_eye_cursor.Background == Brushes.Gray)
                gevinfo.isCursorVisible = true;
            if (bt_toolbar_eye_cursor_circle.Background == Brushes.Gray)
                gevinfo.isCircleVisible = true;
            if (bt_toolbar_eye_fixation.Background == Brushes.Gray)
                gevinfo.isFixationVisible = true;
            if (bt_toolbar_eye_scanpath.Background == Brushes.Gray)
                gevinfo.isPathVisible = true;
            if (bt_toolbar_eye_fixationID.Background == Brushes.Gray)
                gevinfo.isFixationIDVisible = true;
            gevinfo.TrackPenWidth = (int)iud_toolbar_eye_track_linewidth.Value;
            gevinfo.CirclePenWidth = (int)iud_toolbar_eye_cursor_circle_linewidth.Value;
            gevinfo.FixationPenWidth = (int)iud_toolbar_eye_fixation_linewidth.Value;
            gevinfo.PathPenWidth = (int)iud_toolbar_eye_scanpath_linewidth.Value;
            gevinfo.TrackPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_eye_track_colorpicker.Text);
            gevinfo.CirclePenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_eye_cursor_circle_colorpicker.Text);
            gevinfo.FixationPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_eye_fixation_colorpicker.Text);
            gevinfo.PathPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_eye_scanpath_colorpicker.Text);
            gevinfo.FixationIDPenColor = System.Drawing.ColorTranslator.FromHtml(tb_toolbar_eye_fixationID_colorpicker.Text);
            if (bt_toolbar_eye_track_type.Content.ToString() == "D")
                gevinfo.TrackType = 1;
            gevinfo.FixationRate = (int)iud_toolbar_eye_fixation_rate.Value;
            gevinfo.BrowserToolbarHeight = (int)iud_toolbar_eye_browsertoolbarHeight.Value;

            new ExportVideo(_mainDir, _projectName, videoPath, userName, mevinfo, gevinfo).Show();

        }
        private void bt_replay_play_Click(object sender, RoutedEventArgs e)
        {
            RunPauseVideo();
        }
        private void bt_replay_stop_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }
        private void bt_replay_sound_Click(object sender, RoutedEventArgs e)
        {
            VideoSound();
        }
        private void dud_replay_speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            VideoRate();
        }
        private void cb_replay_videoName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OpenVideo();
        }
        private void s_replay_timeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_istimeBarEnabled == true && _replayControl != null)
            {
                _replayControl.SetPosition((long)(s_replay_timeBar.Value));
            }
            this._currentFrameNumber = (long)(s_replay_timeBar.Value);
            UpateURLAndCurrentTime(this._currentFrameNumber);
            //Console.WriteLine("ValueChanged: " + _currentFrameNumber.ToString() + ", " + s_replay_timeBar.Value.ToString());
        }
        private void ChangeButtonPicture(ButtonPic bp)
        {
            switch (bp)
            {
                case ButtonPic.Play:
                    bt_replay_play.Content = "▶";
                    bt_replay_play.Style = (Style)FindResource("StartButtonStyle");
                    break;
                case ButtonPic.Pause:
                    bt_replay_play.Content = "❙❙";
                    bt_replay_play.Style = (Style)FindResource("PauseButtonStyle");
                    break;
                case ButtonPic.Mute:
                    bt_replay_sound.Content = "✖";
                    break;
                case ButtonPic.Unmute:
                    bt_replay_sound.Content = "♬";
                    break;
                default:
                    break;

            }
        }
        private void bt_replay_calculate_Click(object sender, RoutedEventArgs e)
        {
            if (this._replayInfo != null && this._replayInfo.Count >= cb_replay_videoName.SelectedIndex + 1)
            {
                string dbPath = _mainDir + @"\" + _projectName + @"\Database\" + _projectName;
                List<string> userName = new List<string>();
                userName.Add(this._replayInfo[cb_replay_videoName.SelectedIndex].userName);
                new FixationSetting(dbPath, userName).Show();
                /*
                string userName = this._replayInfo[cb_replay_videoName.SelectedIndex].userName;
                string path = _mainDir + @"\" + _projectName + @"\Database\" + _projectName;
                List<int> urlstartrid = null;
                if (_replayControl != null)
                {
                    urlstartrid = _replayControl.GetAllURLStartRID();
                }
                Fixation f = new Fixation(userName, path);
                if (urlstartrid != null && urlstartrid.Count > 1)
                {
                    f.CalculateWithURL(0, 250, 20, urlstartrid);
                }
                else
                {
                    f.Calculate(0, 250, 20);
                }
                System.Windows.Forms.MessageBox.Show("Success!","INFO");
                */
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Can't calculate fixation!", "ERROR");
            }

        }
        private void bt_replay_pagebackward_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                int frameid = _replayControl.GetBackwardPageFrameID();
                s_replay_timeBar.Value = frameid;
            }
        }
        private void bt_replay_pageforward_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                int frameid = _replayControl.GetForwardPageFrameID();
                s_replay_timeBar.Value = frameid;
            }
        }
        #endregion

        #region Mouse Toolbar Button
        private void bt_toolbar_mouse_track_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetMouseTrackVisible();
                if (visible)
                    bt_toolbar_mouse_track.Background = Brushes.Gray;
                else
                    bt_toolbar_mouse_track.Background = Brushes.Transparent;
            }
        }
        private void bt_toolbar_mouse_track_type_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                int type = _replayControl.SetMouseTrackType();
                if (type == 0)
                {
                    bt_toolbar_mouse_track_type.Content = "S";
                }
                else if (type == 1)
                {
                    bt_toolbar_mouse_track_type.Content = "D";
                }
            }
        }
        private void tb_toolbar_mouse_track_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseTrackColor(tb_toolbar_mouse_track_colorpicker.Text);
            }
        }
        private void iud_toolbar_mouse_track_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseTrackLineWidth((int)iud_toolbar_mouse_track_linewidth.Value);
            }
        }

        private void bt_toolbar_mouse_cursor_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetMouseCursorVisible();
                if (visible)
                    bt_toolbar_mouse_cursor.Background = Brushes.Gray;
                else
                    bt_toolbar_mouse_cursor.Background = Brushes.Transparent;
            }
        }
        private void bt_toolbar_mouse_cursor_circle_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetMouseCursorCircleVisible();
                if (visible)
                {
                    bt_toolbar_mouse_cursor_circle.Background = Brushes.Gray;
                }
                else
                {
                    bt_toolbar_mouse_cursor_circle.Background = Brushes.Transparent;
                }
            }
        }
        private void tb_toolbar_mouse_cursor_circle_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseCursorCircleColor(tb_toolbar_mouse_cursor_circle_colorpicker.Text);
            }
        }
        private void iud_toolbar_mouse_cursor_circle_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseCursorCircleLineWidth((int)iud_toolbar_mouse_cursor_circle_linewidth.Value);
            }
        }

        private void bt_toolbar_mouse_fixation_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetMouseFixationVisible();
                if (visible)
                    bt_toolbar_mouse_fixation.Background = Brushes.Gray;
                else
                    bt_toolbar_mouse_fixation.Background = Brushes.Transparent;
            }
        }
        private void tb_toolbar_mouse_fixation_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseFixationColor(tb_toolbar_mouse_fixation_colorpicker.Text);
            }
        }
        private void iud_toolbar_mouse_fixation_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseFixationLineWidth((int)iud_toolbar_mouse_fixation_linewidth.Value);
            }
        }
        private void iud_toolbar_mouse_fixation_rate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseFixationRate((int)iud_toolbar_mouse_fixation_rate.Value);
            }
        }
        private void iud_toolbar_mouse_browsertoolbarHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseBrowserToolbarHeight((int)iud_toolbar_mouse_browsertoolbarHeight.Value);
            }
        }

        private void bt_toolbar_mouse_scanpath_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetMouseScanpathVisible();
                if (visible)
                    bt_toolbar_mouse_scanpath.Background = Brushes.Gray;
                else
                    bt_toolbar_mouse_scanpath.Background = Brushes.Transparent;
            }
        }
        private void tb_toolbar_mouse_scanpath_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseScanpathColor(tb_toolbar_mouse_scanpath_colorpicker.Text);
            }
        }
        private void iud_toolbar_mouse_scanpath_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseScanpathLineWidth((int)iud_toolbar_mouse_scanpath_linewidth.Value);
            }
        }

        private void bt_toolbar_mouse_fixationID_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetMouseFixationIDVisible();
                if (visible)
                    bt_toolbar_mouse_fixationID.Background = Brushes.Gray;
                else
                    bt_toolbar_mouse_fixationID.Background = Brushes.Transparent;
            }
        }
        private void tb_toolbar_mouse_fixationID_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetMouseFixationIDColor(tb_toolbar_mouse_fixationID_colorpicker.Text);
            }
        }
        #endregion

        #region Eye Toolbar Button
        private void bt_toolbar_eye_track_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetGazeTrackVisible();
                if (visible)
                    bt_toolbar_eye_track.Background = Brushes.Gray;
                else
                    bt_toolbar_eye_track.Background = Brushes.Transparent;
            }
        }
        private void bt_toolbar_eye_track_type_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                int type = _replayControl.SetGazeTrackType();
                if (type == 0)
                {
                    bt_toolbar_eye_track_type.Content = "S";
                }
                else if (type == 1)
                {
                    bt_toolbar_eye_track_type.Content = "D";
                }
            }
        }
        private void tb_toolbar_eye_track_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeTrackColor(tb_toolbar_eye_track_colorpicker.Text);
            }
        }
        private void iud_toolbar_eye_track_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeTrackLineWidth((int)iud_toolbar_eye_track_linewidth.Value);
            }
        }

        private void bt_toolbar_eye_cursor_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetGazeCursorVisible();
                if (visible)
                    bt_toolbar_eye_cursor.Background = Brushes.Gray;
                else
                    bt_toolbar_eye_cursor.Background = Brushes.Transparent;
            }
        }
        private void bt_toolbar_eye_cursor_circle_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetGazeCursorCircleVisible();
                if (visible)
                    bt_toolbar_eye_cursor_circle.Background = Brushes.Gray;
                else
                    bt_toolbar_eye_cursor_circle.Background = Brushes.Transparent;
            }
        }
        private void tb_toolbar_eye_cursor_circle_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeCursorCircleColor(tb_toolbar_eye_cursor_circle_colorpicker.Text);
            }
        }
        private void iud_toolbar_eye_cursor_circle_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeCursorCircleLineWidth((int)iud_toolbar_eye_cursor_circle_linewidth.Value);
            }
        }

        private void bt_toolbar_eye_fixation_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetGazeFixationVisible();
                if (visible)
                    bt_toolbar_eye_fixation.Background = Brushes.Gray;
                else
                    bt_toolbar_eye_fixation.Background = Brushes.Transparent;
            }
        }
        private void tb_toolbar_eye_fixation_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeFixationColor(tb_toolbar_eye_fixation_colorpicker.Text);
            }
        }
        private void iud_toolbar_eye_fixation_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeFixationLineWidth((int)iud_toolbar_eye_fixation_linewidth.Value);
            }
        }
        private void iud_toolbar_eye_fixation_rate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeFixationRate((int)iud_toolbar_eye_fixation_rate.Value);
            }
        }
        private void iud_toolbar_eye_browsertoolbarHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeBrowserToolbarHeight((int)iud_toolbar_eye_browsertoolbarHeight.Value);
            }
        }

        private void bt_toolbar_eye_scanpath_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetGazeScanpathVisible();
                if (visible)
                    bt_toolbar_eye_scanpath.Background = Brushes.Gray;
                else
                    bt_toolbar_eye_scanpath.Background = Brushes.Transparent;
            }
        }
        private void tb_toolbar_eye_scanpath_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeScanpathColor(tb_toolbar_eye_scanpath_colorpicker.Text);
            }
        }
        private void iud_toolbar_eye_scanpath_linewidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeScanpathLineWidth((int)iud_toolbar_eye_scanpath_linewidth.Value);
            }
        }

        private void bt_toolbar_eye_fixationID_Click(object sender, RoutedEventArgs e)
        {
            if (_replayControl != null)
            {
                bool visible = _replayControl.SetGazeFixationIDVisible();
                if (visible)
                    bt_toolbar_eye_fixationID.Background = Brushes.Gray;
                else
                    bt_toolbar_eye_fixationID.Background = Brushes.Transparent;
            }
        }
        private void tb_toolbar_eye_fixationID_colorpicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_replayControl != null)
            {
                _replayControl.SetGazeFixationIDColor(tb_toolbar_eye_fixationID_colorpicker.Text);
            }
        }
        #endregion

        private void CloseInterfaces()
        {
            // Reset variables
            _mainDir = null;
            _projectName = null;
            _totalFrameNumber = -1;
            _currentFrameNumber = 0;
            _istimeBarEnabled = true;

            if (_openFileDialog != null)
            {
                _openFileDialog.Dispose();
                _openFileDialog = null;
            }

            if (_replayControl != null)
            {
                _replayControl.Dispose();
                _replayControl = null;
            }

            if (_dbReplayInfo != null)
            {
                _dbReplayInfo.DBDisconnect();
                _dbReplayInfo = null;
            }

            if (_replayInfo != null)
                _replayInfo = null;

        }
        private void InitControlsAndVariables()
        {
            ChangeButtonPicture(ButtonPic.Play);
            ChangeButtonPicture(ButtonPic.Unmute);
            dud_replay_speed.Value = 1.0;
            s_replay_timeBar.IsEnabled = true;
            _istimeBarEnabled = true;
            s_replay_timeBar.Maximum = 10;
            s_replay_timeBar.Value = 0;

            _totalFrameNumber = -1;
            _currentFrameNumber = 0;
            _istimeBarEnabled = true;
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                tb_replay_urlname.Text = "";
                lb_replay_currenttime.Content = "00:00:00";
                if (_replayControl != null)
                {
                    lb_replay_totaltime.Content = "/ " + _replayControl.GetTotalTime();
                }
            });

            //mouse toolbar
            bt_toolbar_mouse_track.Background = Brushes.Gray;
            bt_toolbar_mouse_track_type.Content = "S";
            bt_toolbar_mouse_track_colorpicker.CurrentColor = Brushes.Red;
            iud_toolbar_mouse_track_linewidth.Value = 3;

            bt_toolbar_mouse_cursor.Background = Brushes.Gray;
            bt_toolbar_mouse_cursor_circle.Background = Brushes.Gray;
            bt_toolbar_mouse_cursor_circle_colorpicker.CurrentColor = Brushes.Red;
            iud_toolbar_mouse_cursor_circle_linewidth.Value = 3;

            bt_toolbar_mouse_fixation.Background = Brushes.Gray;
            bt_toolbar_mouse_fixation_colorpicker.CurrentColor = Brushes.Red;
            iud_toolbar_mouse_fixation_linewidth.Value = 3;
            iud_toolbar_mouse_fixation_rate.Value = 5;
            iud_toolbar_mouse_browsertoolbarHeight.Value = 66;

            bt_toolbar_mouse_scanpath.Background = Brushes.Gray;
            bt_toolbar_mouse_scanpath_colorpicker.CurrentColor = Brushes.Red;
            iud_toolbar_mouse_scanpath_linewidth.Value = 3;

            bt_toolbar_mouse_fixationID.Background = Brushes.Gray;
            bt_toolbar_mouse_fixationID_colorpicker.CurrentColor = Brushes.Red;

            //eye toolbar
            bt_toolbar_eye_track.Background = Brushes.Gray;
            bt_toolbar_eye_track_type.Content = "S";
            bt_toolbar_eye_track_colorpicker.CurrentColor = Brushes.Blue;
            iud_toolbar_eye_track_linewidth.Value = 3;

            bt_toolbar_eye_cursor.Background = Brushes.Gray;
            bt_toolbar_eye_cursor_circle.Background = Brushes.Gray;
            bt_toolbar_eye_cursor_circle_colorpicker.CurrentColor = Brushes.Blue;
            iud_toolbar_eye_cursor_circle_linewidth.Value = 3;

            bt_toolbar_eye_fixation.Background = Brushes.Gray;
            bt_toolbar_eye_fixation_colorpicker.CurrentColor = Brushes.Blue;
            iud_toolbar_eye_fixation_linewidth.Value = 3;
            iud_toolbar_eye_fixation_rate.Value = 5;
            iud_toolbar_eye_browsertoolbarHeight.Value = 66;

            bt_toolbar_eye_scanpath.Background = Brushes.Gray;
            bt_toolbar_eye_scanpath_colorpicker.CurrentColor = Brushes.Blue;
            iud_toolbar_eye_scanpath_linewidth.Value = 3;

            bt_toolbar_eye_fixationID.Background = Brushes.Gray;
            bt_toolbar_eye_fixationID_colorpicker.CurrentColor = Brushes.Blue;
        }
        private void InitTimebar(long max)
        {
            s_replay_timeBar.Maximum = max;
            s_replay_timeBar.Value = 0;
        }
        private void RunPauseVideo()
        {
            if (_replayControl != null && _replayControl.MediaReady())
            {
                if (_currentFrameNumber >= _totalFrameNumber)
                {
                    s_replay_timeBar.Value = 0;
                }

                _replayControl.RunPauseGraph();
                if (_replayControl.GetMediaState() == PlayState.Running)
                {
                    ChangeButtonPicture(ButtonPic.Pause);
                    s_replay_timeBar.IsEnabled = false;
                    _istimeBarEnabled = false;
                    bt_replay_pagebackward.IsEnabled = false;
                    bt_replay_pageforward.IsEnabled = false;
                }
                else
                {
                    ChangeButtonPicture(ButtonPic.Play);
                    //OnFrameEvent(null, null);
                    s_replay_timeBar.IsEnabled = true;
                    _istimeBarEnabled = true;
                    bt_replay_pagebackward.IsEnabled = true;
                    bt_replay_pageforward.IsEnabled = true;

                }

            }
        }
        private void StopVideo()
        {
            if (_replayControl != null && _replayControl.MediaReady())
            {
                _replayControl.StopGraph();
                ChangeButtonPicture(ButtonPic.Play);
                s_replay_timeBar.IsEnabled = true;
                _istimeBarEnabled = true;
                s_replay_timeBar.Value = 0;
                bt_replay_pagebackward.IsEnabled = true;
                bt_replay_pageforward.IsEnabled = true;
            }
        }
        private void VideoSound()
        {
            if (_replayControl != null && _replayControl.MediaReady())
            {
                _replayControl.ToggleMute();
                if (_replayControl.GetAudioState() == 0)
                    ChangeButtonPicture(ButtonPic.Unmute);
                else
                    ChangeButtonPicture(ButtonPic.Mute);
            }
        }
        private void VideoRate()
        {
            if (_replayControl != null && _replayControl.MediaReady())
            {
                _replayControl.SetRate((double)dud_replay_speed.Value);
            }
        }
        private void OpenVideo()
        {
            try
            {
                if (this._replayInfo != null && this._replayInfo.Count >= cb_replay_videoName.SelectedIndex + 1)
                {
                    string filePath = this._replayInfo[cb_replay_videoName.SelectedIndex].videoPath;
                    string userName = this._replayInfo[cb_replay_videoName.SelectedIndex].userName;
                    if (!System.IO.File.Exists(filePath))
                    {
                        System.Windows.Forms.MessageBox.Show("Can't find the video!", "ERROR");
                        this.Close();
                    }

                    // Init Button
                    InitControlsAndVariables();

                    // Start the media file
                    this._totalFrameNumber = _replayControl.OpenFile(filePath, userName);

                    // Init Timbar
                    InitTimebar(this._totalFrameNumber);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Can't find the database!", "ERROR");
                    this.Close();
                }
            }
            catch { }
        }
        private void UpateURLAndCurrentTime(long currentFrameID)
        {
            if (_replayControl != null)
            {
                URLInfo urlinfo = _replayControl.GetURLInfo((int)currentFrameID);
                this.Dispatcher.BeginInvoke((Action)delegate()
                {
                    if (_replayControl != null)
                        lb_replay_currenttime.Content = _replayControl.GetCurrentTime((int)currentFrameID);
                    if (urlinfo.size > 0)
                    {
                        if (urlinfo.keyword != null)
                            tb_replay_urlname.Text = "(" + urlinfo.id + "/" + urlinfo.size + ") Search[" + urlinfo.keyword + "]";
                        else
                            tb_replay_urlname.Text = "(" + urlinfo.id + "/" + urlinfo.size + ") " + urlinfo.url;
                    }
                });
            }
        }

        



    }
    public class DBBase
    {
        protected string _mainDir = null;
        protected string _DBPath = null;
        protected string _DBName = null;
        protected SQLiteConnection _sqliteConnect = null;
        public DBBase(string mainDir, string projectName)
        {
            this._mainDir = mainDir;
            this._DBPath = mainDir + @"\" + projectName + @"\Database\" + projectName;
            this._DBName = projectName;
        }
        public void DBConnect()
        {
            if (File.Exists(_DBPath))
            {
                try
                {
                    _sqliteConnect = new SQLiteConnection("Data source=" + _DBPath);
                    _sqliteConnect.Open();
                }
                catch { }
            }
        }
        public void DBDisconnect()
        {
            try
            {
                _sqliteConnect.Close();
            }
            catch (Exception e) { }
        }
    }

    public class DBReplayInfo : DBBase
    {
        protected string _VideoDir = null;
        public DBReplayInfo(string mainDir, string projectName)
            : base(mainDir, projectName)
        {
            this._VideoDir = mainDir + @"\" + projectName + @"\Video\";
        }
        public List<ReplayInfo> GetAllReplayInfo()
        {
            List<ReplayInfo> result = new List<ReplayInfo>();
            if (this._sqliteConnect != null)
            {
                try
                {
                    using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                    {
                        using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                        {
                            ReplayInfo ri = new ReplayInfo();
                            _sqliteCmd.CommandText = @"SELECT SubjectName, EyeDeviceType FROM Statistics";
                            var rdr = _sqliteCmd.ExecuteReader();
                            while (rdr.Read())
                            {
                                ri.userName = rdr.GetString(0);
                                ri.videoPath = this._VideoDir + ri.userName + ".avi";
                                ri.eyeDeviceType = rdr.GetString(1);
                                result.Add(ri);
                            }
                            rdr.Close();
                        }
                        tr.Commit();
                    }
                }
                catch { }
            }
            return result;
        }
    }

    public struct ReplayInfo
    {
        public string userName;
        public string videoPath;
        public string eyeDeviceType;
    }
}
