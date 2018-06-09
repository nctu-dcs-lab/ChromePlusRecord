/*******************************
 * Title: C# screen capture video recorder AfrogeNet ffmpeg
 * Author: Дмитрий Пичугин
 * Date: 2014
 * Availability: https://www.youtube.com/watch?v=ERbzPeevjS8
 * *****************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Timers;

using Accord.Math;
using Accord.Video;
using Accord.Video.FFMPEG;
using System.Diagnostics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using EyeTribe.ClientSdk;
using EyeTribe.ClientSdk.Data;
using EyeTribe.Controls;
using EyeTribe.Controls.TrackBox;
using EyeTribe.Controls.Calibration;

using Gma.System.MouseKeyHook;
using System.Data.SQLite;

namespace ScreenRecordPlusChrome
{
    public partial class MainWindow : Window
    {
        private bool _isRecording;
        private List<string> _screenNames;
        private UInt32 _frameCount;
        private VideoFileWriter _writer;
        private int _width;
        private int _height;
        private ScreenCaptureStream _streamVideo;
        private Stopwatch _stopWatch;
        private Rectangle _screenArea;

        //URL ReadWriter thread
        private Thread _URLReadWriteThread;
        //Eye Device
        private EyeDevice _EyeDeviceUC = null;
        private EyeDeviceType _EyeDeviceType;
        //Mouse and Eye Timer
        private System.Timers.Timer _MouseGazeTimer;
        //Mouse and Keyboard event
        private IKeyboardMouseEvents _GlobalHook;
        //DB saver
        private DBSaver _DBSaver = null;
        //Save Info
        private string _SaveMainDir;
        private string _SaveDBName;
        private string _SaveUserName;
        //Lock DB data
        private static object _MKEventLock;
        private static object _URLEventLock;
        private static object _RawdataLock;
        //DB Event ID
        private int _MKEventID;
        private int _URLEventID;
        private int _RawdataID;
        private int _FixationID;
        //FPS and Video time(ms)
        private int _FPS;
        private int _VideoTime;
        //Get data
        private bool _isGettingData;
        //Get URL data
        private string[] _URLData;
        private bool _getURL;
        private bool _isNewURL;
        private string _WebPos;
        private string _WebPosTemp;
        private bool _isNewStartScroll;
        private bool _isNextURLSave;
        private string _WinPos;


        public MainWindow()
        {
            InitializeComponent();

            this._isRecording = false;
            this._frameCount = 0;
            this._width = SystemInformation.VirtualScreen.Width;
            this._height = SystemInformation.VirtualScreen.Height;
            this._stopWatch = new Stopwatch();
            this._screenArea = Rectangle.Empty;

            this.bt_Save.IsEnabled = false;
            this._writer = new VideoFileWriter();

            _screenNames = new List<string>();
            _screenNames.Add(@"Select ALL");
            foreach (var screen in Screen.AllScreens)
            {
                _screenNames.Add(screen.DeviceName);
            }
            this.cb_screenSelector.ItemsSource = _screenNames;
            this.cb_screenSelector.SelectedIndex = 0;

            // Codec ComboBox
            this.cb_VideoCodec.ItemsSource = Enum.GetValues(typeof(VideoCodec));
            this.cb_VideoCodec.SelectedIndex = 0;

            // BitRate 2000kbit/s 2000000 1000000
            this.cb_BitRate.ItemsSource = Enum.GetValues(typeof(BitRate));
            this.cb_BitRate.SelectedIndex = 5;

            // Eye Device Selection
            this.cb_eyeDevice.ItemsSource = Enum.GetValues(typeof(EyeDeviceType));
            this.cb_eyeDevice.SelectedIndex = 0;
            _EyeDeviceType = EyeDeviceType.None;

            //URL ReadWriter
            _URLReadWriteThread = new Thread(URLReadWriter);

            // Mouse and Eye Timer
            this._MouseGazeTimer = new System.Timers.Timer();
            this._MouseGazeTimer.Elapsed += new ElapsedEventHandler(OnMouseGazeTimedEvent);

            //Lock DB data
            _MKEventLock = new object();
            _URLEventLock = new object();
            _RawdataLock = new object();
            //DB Event ID
            _MKEventID = 0;
            _URLEventID = 0;
            _RawdataID = 1;
            //_FirstRaw = true;
            _FixationID = 0;
            //FPS and Video time(ms)
            _FPS = 10;
            _VideoTime = 0;
            //Get data
            _isGettingData = false;
            //Get URL data
            _URLData = new string[] { null, null };
            _getURL = false;
            _isNewURL = false;
            //_WebPos = new System.Windows.Point();
            _WebPos = null;
            _WebPosTemp = null;
            _isNewStartScroll = false;
            _isNextURLSave = false;
            _WinPos = null;

            Console.SetOut(TextWriter.Null);
        }

        #region Record Video
        /*********************  Record Video  ***************************/

        private void bt_start_Click(object sender, RoutedEventArgs e)
        {
            //Minimize Form
            //this.WindowState = FormWindowState.Minimized;
            ButtonStart();
        }

        private void bt_Save_Click(object sender, RoutedEventArgs e)
        {
            this.SetVisible(false);
        }

        private void bt_browse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.tb_SaveFolder.Text = fbd.SelectedPath;
            }
        }

        private void bt_analyze_Click(object sender, RoutedEventArgs e)
        {
            new Analyze(tb_SaveFolder.Text).Show();
        }

        private void ButtonStart()
        {
            try
            {
                if (String.Compare(tb_dbName.Text.Trim(), "", true) == 0 || String.Compare(tb_userName.Text.Trim(), "", true) == 0)
                {
                    System.Windows.Forms.MessageBox.Show("Please fill in DB Name or User Name!", "ERROR");
                    return;
                }
                if (this.tb_SaveFolder.Text.Length < 1)
                {
                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        this.StartRec(fbd.SelectedPath);
                    }
                }
                else
                {
                    this.StartRec(this.tb_SaveFolder.Text);
                }
            }
            catch (Exception exc)
            {
                System.Windows.Forms.MessageBox.Show(exc.Message, "ERROR");
            }
        }

        private void StartRec(string path)
        {
            if (_isRecording == false)
            {
                this.SetScreenArea();

                this.SetVisible(true);

                //Set parameter
                this._frameCount = 0;
                _MKEventID = 0;
                _URLEventID = 0;
                _RawdataID = 1;
                _FixationID = 0;
                _VideoTime = 0;
                _FPS = (int)nud_FPS.Value;
                _isGettingData = false;
                _URLData[0] = null;
                _URLData[1] = null;
                _getURL = false;
                _isNewURL = false;
                _WebPos = null;
                _WebPosTemp = null;
                _isNewStartScroll = false;
                _isNextURLSave = false;
                _WinPos = null;

                this.tb_SaveFolder.Text = path;

                //Set Mouse and Eye Timer parameter
                _MouseGazeTimer.Interval = 1000 / (int)(nud_sampleRate.Value+20);

                //Start URL Thread
                if (_URLReadWriteThread.ThreadState == System.Threading.ThreadState.Unstarted)
                {
                    _URLReadWriteThread.IsBackground = true;
                    _URLReadWriteThread.Start();
                }

                //Set DB parameter and Connect it
                this._SaveMainDir = path + @"\MoniChrome\";
                this._SaveDBName = this.tb_dbName.Text;
                this._SaveUserName = this.tb_userName.Text;
                this._DBSaver = new DBSaver(_SaveDBName, _SaveMainDir, _SaveUserName, cb_eyeDevice.Text);
                this._DBSaver.DBConnect();

                //Set video_write parameter
                if (!Directory.Exists(_SaveMainDir + _SaveDBName + @"\Video"))
                {
                    Directory.CreateDirectory(_SaveMainDir + _SaveDBName + @"\Video");
                }
                string fullName = string.Format(@"{0}\{1}.avi", _SaveMainDir + _SaveDBName + @"\Video", _SaveUserName);
                _writer.Open(
                    fullName,
                    this._width,
                    this._height,
                    (Rational)(int)nud_FPS.Value,
                    (VideoCodec)cb_VideoCodec.SelectedValue,
                    (int)(BitRate)this.cb_BitRate.SelectedValue);

                //Wait for getting Chrome data
                Thread.Sleep(500);

                // Start main work
                this.StartRecord();
            }
        }

        private void SetScreenArea()
        {
            // get entire desktop area size
            string screenName = this.cb_screenSelector.SelectedValue.ToString();
            if (string.Compare(screenName, @"Select ALL", StringComparison.OrdinalIgnoreCase) == 0)
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    this._screenArea = Rectangle.Union(_screenArea, screen.Bounds);
                }
            }
            else
            {
                this._screenArea = Screen.AllScreens.First(scr => scr.DeviceName.Equals(screenName)).Bounds;
                this._width = this._screenArea.Width;
                this._height = this._screenArea.Height;
            }
        }

        private void StartRecord() //Object stateInfo
        {
            //Minimize Form
            this.WindowState = WindowState.Minimized;
            Thread.Sleep(80);

            //Set screen_capture parameter
            this._streamVideo = new ScreenCaptureStream(this._screenArea);
            this._streamVideo.FrameInterval = 1000 / this._FPS;
            this._streamVideo.NewFrame += new NewFrameEventHandler(this.video_NewFrame);

            //Start Mouse and Eye Timer
            this._MouseGazeTimer.Start();

            //Start mouse and keyboard event
            MouseKeySubscribe();

            //Start screen_capture
            this._streamVideo.Start();

            //Start stopWatch
            this._stopWatch.Start();

        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (this._isRecording)
            {
                this._frameCount++;

                //Get and save URL data
                this.GetURL();

                //Video time
                this._VideoTime = (int)Math.Round((double)this._frameCount * (double)1000 / (double)this._FPS);

                //Get data
                this._isGettingData = true;

                this._writer.WriteVideoFrame(eventArgs.Frame);

                this.lb_1.Dispatcher.Invoke(new Action(() =>
                {
                    lb_1.Content = string.Format(@"Frames: {0}", _frameCount);
                }));

                this.lb_stopWatch.Dispatcher.Invoke(new Action(() =>
                {
                    this.lb_stopWatch.Content = _stopWatch.Elapsed.ToString();
                }));
            }
            else
            {
                this._isGettingData = false;

                _stopWatch.Reset();
                Thread.Sleep(100);
                _streamVideo.SignalToStop();
                Thread.Sleep(100);
                _writer.Close();

                //Close Mouse and Keyboard Event
                Thread.Sleep(100);
                MouseKeyUnsubscribe();

                //Close Mouse and Eye Timer
                Thread.Sleep(100);
                _MouseGazeTimer.Stop();

                //Close DB save
                Thread.Sleep(100);
                this._DBSaver.DBDisconnect();

                System.Windows.Forms.MessageBox.Show("File saved!", "INFO");
            }
        }

        private void SetVisible(bool visible)
        {
            this.bt_start.IsEnabled = !visible;
            this.bt_Save.IsEnabled = visible;
            this.cb_eyeDevice.IsEnabled = !visible;
            this._isRecording = visible;
            if (_EyeDeviceType != EyeDeviceType.None && _EyeDeviceUC != null)
            {
                this._EyeDeviceUC.SetButtonEnabled(!visible);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this._isRecording = false;
            //Close Mouse and Eye Timer
            Thread.Sleep(100);
            if (_MouseGazeTimer != null)
                _MouseGazeTimer.Dispose();

            //Close URL ReadWriter
            Thread.Sleep(100);
            if (_URLReadWriteThread != null)
                _URLReadWriteThread.Abort();

            //Close EyeTribeUC
            Thread.Sleep(100);
            if (_EyeDeviceUC != null)
            {
                if (_EyeDeviceUC.hasFixation())
                    _EyeDeviceUC.OnFixationEvent -= OnGazeFixationEvent;
                this._EyeDeviceUC.Dispose();
                this._EyeDeviceUC = null;
            }
        }
        #endregion

        #region Communicate With Chrome Extension
        /*********************  Communicate With Chrome Extension  ***************************/
        private void URLReadWriter()
        {
            try
            {
                while (true)
                {
                    var stdin = Console.OpenStandardInput();
                    var length = 0;

                    var lengthBytes = new byte[4];
                    stdin.Read(lengthBytes, 0, 4);
                    length = BitConverter.ToInt32(lengthBytes, 0);

                    var buffer = new char[length];
                    using (var reader = new StreamReader(stdin))
                    {
                        while (reader.Peek() >= 0)
                        {
                            reader.Read(buffer, 0, buffer.Length);
                        }
                        JObject url = (JObject)JsonConvert.DeserializeObject<JObject>(new string(buffer));
                        if (url != null)
                        {
                            //Into DB
                            string str = url["text"].Value<string>();
                            int type = str.IndexOf(":");
                            //URLST: First URL, DB: URLEvent
                            if (type == 5)
                            {
                                lock (_URLEventLock)
                                {
                                    string urlst = str.Substring(type + 1, str.Length - type - 1);
                                    string[] urlsplit = urlst.Split(',');
                                    if (urlsplit.Length == 2)
                                    {
                                        //scroll
                                        _isNextURLSave = true;
                                        if (_isNewStartScroll && _isNextURLSave)
                                        {
                                            _WebPos = _WebPosTemp;
                                            _isNewStartScroll = false;
                                            _isNextURLSave = false;
                                        }

                                        string value = "'" + _URLEventID + "', '" + urlsplit[0] + "', '" + (this._frameCount + 1) + "', '0', '" + urlsplit[1] + "', '" + this._RawdataID + "'";
                                        _DBSaver.URLEventInsert(value);
                                    }
                                    else
                                    {
                                        //scroll
                                        _isNextURLSave = true;
                                        if (_isNewStartScroll && _isNextURLSave)
                                        {
                                            _WebPos = _WebPosTemp;
                                            _isNewStartScroll = false;
                                            _isNextURLSave = false;
                                        }

                                        string value = "'" + _URLEventID + "', '" + urlsplit[0] + "', '" + (this._frameCount + 1) + "', '0', null, '" + this._RawdataID + "'";
                                        _DBSaver.URLEventInsert(value);
                                    }
                                    _URLEventID++;
                                }
                            }
                            //S: Scrolling, DB: Rawdata
                            else if (type == 1)
                            {
                                _WebPosTemp = str.Substring(type + 1, str.Length - type - 1);
                                if (str[0] == 'S')
                                    _isNewStartScroll = true;
                                if (_isNewStartScroll && _isNextURLSave) {
                                    _WebPos = _WebPosTemp;
                                    _isNewStartScroll = false;
                                    _isNextURLSave = false;
                                }
                                if (!_isNewStartScroll)
                                    _WebPos = _WebPosTemp;
                            }
                            else if (type == 2)
                            {
                                _WinPos = str.Substring(type + 1, str.Length - type - 1);
                            }
                            else if (this._isGettingData)
                            {
                                //SCROLLST SCROLLE: Scroll start and end, DB: MKEvent
                                if (type == 8)
                                {
                                    lock (_MKEventLock)
                                    {
                                        string value = "'" + _MKEventID + "', '" + (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds) + "', " + "'Scroll', 'Start', '" + str.Substring(type + 1, str.Length - type - 1) + "', '" + this._RawdataID + "'";
                                        _DBSaver.DBMKEventInsert(value);
                                        _MKEventID++;
                                    }
                                }
                                else if (type == 7)
                                {
                                    lock (_MKEventLock)
                                    {
                                        string value = "'" + _MKEventID + "', '" + (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds) + "', " + "'Scroll', 'End', '" + str.Substring(type + 1, str.Length - type - 1) + "', '" + this._RawdataID + "'";
                                        _DBSaver.DBMKEventInsert(value);
                                        _MKEventID++;
                                    }
                                }
                                //URL URLK: URL (with keyword), DB: URLEvent
                                else if (type == 3)
                                {
                                    this._URLData[0] = str.Substring(type + 1, str.Length - type - 1);
                                    this._URLData[1] = null;
                                    this._isNewURL = true;
                                }
                                else if (type == 4)
                                {
                                    string urlkey = str.Substring(type + 1, str.Length - type - 1);
                                    int cut = urlkey.IndexOf(",");
                                    this._URLData[0] = urlkey.Substring(0, cut);
                                    this._URLData[1] = urlkey.Substring(cut + 1, urlkey.Length - cut - 1);
                                    this._isNewURL = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException e) { }
            catch (IOException e) { }
            catch (FormatException e) { }
        }
        private void GetURL()
        {
            if (_isNewURL)
            {
                this._getURL = true;
                this._isNewURL = false;
            }
        }
        private void SaveURL(string[] URL, int videoTime, uint frame)
        {
            if (URL[0] != null && this._getURL)
            {
                lock (_URLEventLock)
                {
                    if (URL[1] != null)
                    {
                        //scroll
                        _isNextURLSave = true;
                        if (_isNewStartScroll && _isNextURLSave)
                        {
                            _WebPos = _WebPosTemp;
                            _isNewStartScroll = false;
                            _isNextURLSave = false;
                        }

                        string value = "'" + _URLEventID + "', '" + URL[0] + "', '" + frame + "', '" + videoTime + "', '" + URL[1] + "', '" + this._RawdataID + "'";
                        _DBSaver.URLEventInsert(value);
                        _URLEventID++;
                        this._getURL = false;
                    }
                    else
                    {
                        //scroll
                        _isNextURLSave = true;
                        if (_isNewStartScroll && _isNextURLSave)
                        {
                            _WebPos = _WebPosTemp;
                            _isNewStartScroll = false;
                            _isNextURLSave = false;
                        }

                        string value = "'" + _URLEventID + "', '" + URL[0] + "', '" + frame + "', '" + videoTime + "', null, '" + this._RawdataID + "'";
                        _DBSaver.URLEventInsert(value);
                        _URLEventID++;
                        this._getURL = false;
                    }

                }
            }
        }
        #endregion

        #region Eye Device
        /*********************  Eye Device  ***************************/
        private void cb_eyeDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Get Eye Device Type
            string text= (sender as System.Windows.Controls.ComboBox).SelectedIndex.ToString();
            this._EyeDeviceType = (EyeDeviceType)Enum.Parse(typeof(EyeDeviceType), text);
            
            //Close eye device
            if (_EyeDeviceUC != null)
            {
                if (_EyeDeviceUC.hasFixation())
                    _EyeDeviceUC.OnFixationEvent -= OnGazeFixationEvent;
                this._EyeDeviceUC.Dispose();
                this._EyeDeviceUC = null;
            }

            //Do
            if (this._EyeDeviceType == EyeDeviceType.None) {
                this.g_eyedevice.Children.Clear();
                if (this.Height > 291)
                    this.Height = this.Height - 260;
                this.bt_start.IsEnabled = true;
            }
            else {
                //new eye device
                _EyeDeviceUC = new EyeDevice(_EyeDeviceType);
                if (_EyeDeviceUC.hasFixation())
                    _EyeDeviceUC.OnFixationEvent += OnGazeFixationEvent;

                this.bt_start.IsEnabled = false;
                if (this.Height <= 291)
                    this.Height = this.Height + 260;
                this.g_eyedevice.Children.Clear();
                this.g_eyedevice.Children.Add(_EyeDeviceUC);
            }
            
        }
        #endregion

        #region Mouse and Eye Data Timer
        /*********************  Mouse and Eye Data Timer  ***************************/
        private void OnMouseGazeTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (this._isGettingData)
                {
                    int videotime = (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds);
                    uint frame = this._frameCount;
                    SaveURL(_URLData, videotime, frame);

                    if (this._EyeDeviceType == EyeDeviceType.None) {
                        //Write mouse position into DB
                        lock (_RawdataLock)
                        {
                            string value = "'" + frame + "', '" + videotime + "', '" + System.Windows.Forms.Cursor.Position.X + "', '" + System.Windows.Forms.Cursor.Position.Y + "', null, null, ";
                            if (String.Compare("", _WebPos) == 0 || _WebPos == null)
                                value += "null, ";
                            else
                                value += "'" + _WebPos + "', ";
                            if (String.Compare("", _WinPos) == 0 || _WinPos == null)
                                value += "null, ";
                            else
                                value += "'" + _WinPos + "', ";
                            value += "null, null";
                            _DBSaver.RawdataInsert(value);
                            _RawdataID++;
                        }
                    }
                    else {
                        //Write mouse and eye position into DB
                        System.Windows.Point gazePos = new System.Windows.Point(0, 0);
                        if (_EyeDeviceUC != null)
                            gazePos = _EyeDeviceUC.GetGazePoints();

                        lock (_RawdataLock)
                        {
                            string value = "'" + frame + "', '" + videotime + "', '" + System.Windows.Forms.Cursor.Position.X + "', '" + System.Windows.Forms.Cursor.Position.Y + "', '" + gazePos.X + "', '" + gazePos.Y + "', ";
                            if (String.Compare("", _WebPos) == 0 || _WebPos == null)
                                value += "null, ";
                            else
                                value += "'" + _WebPos + "', ";
                            if (String.Compare("", _WinPos) == 0 || _WinPos == null)
                                value += "null, ";
                            else
                                value += "'" + _WinPos + "', ";
                            value += "null, null";
                            _DBSaver.RawdataInsert(value);
                            _RawdataID++;
                        }
                    }

                }
            }
            catch (IOException ioe) { }
        }
        #endregion

        #region Mouse and Keyboard Event
        /*********************  Mouse and Keyboard Event  ***************************/
        private void MouseKeySubscribe()
        {
            _GlobalHook = Hook.GlobalEvents();

            _GlobalHook.MouseDown += GlobalHookMouseDown;
            _GlobalHook.MouseUp += GlobalHookMouseUp;
            _GlobalHook.KeyDown += GlobalHookKeyDown;
        }

        private void GlobalHookKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (this._isGettingData)
            {
                lock (_MKEventLock)
                {
                    string value = "'" + _MKEventID + "', '" + (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds) + "', " + "'Key', 'Down', '" + e.KeyCode + ":" + e.KeyValue + "', '" + this._RawdataID + "'";
                    _DBSaver.DBMKEventInsert(value);
                    _MKEventID++;
                }
            }
        }

        private void GlobalHookMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this._isGettingData)
            {
                lock (_MKEventLock)
                {
                    string value = "'" + _MKEventID + "', '" + (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds) + "', " + "'Mouse', 'Up', '" + e.Button + ":{" + e.X + ", " + e.Y + "}', '" + this._RawdataID + "'";
                    _DBSaver.DBMKEventInsert(value);
                    _MKEventID++;
                }
            }
        }

        private void GlobalHookMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this._isGettingData)
            {
                lock (_MKEventLock)
                {
                    string value = "'" + _MKEventID + "', '" + (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds) + "', " + "'Mouse', 'Down', '" + e.Button + ":{" + e.X + ", " + e.Y + "}', '" + this._RawdataID + "'";
                    _DBSaver.DBMKEventInsert(value);
                    _MKEventID++;
                }
            }
        }

        private void MouseKeyUnsubscribe()
        {
            _GlobalHook.MouseDown -= GlobalHookMouseDown;
            _GlobalHook.MouseUp -= GlobalHookMouseUp;
            _GlobalHook.KeyDown -= GlobalHookKeyDown;

            _GlobalHook.Dispose();
        }
        #endregion

        #region Fixation Event
        private int _FixationStartRID;
        private int _FixationStartTime;
        private System.Windows.Point _FixationScrollTop;
        private void OnGazeFixationEvent(object sender, FixationEventArgs e) {
            if (this._isGettingData && _stopWatch.IsRunning)
            {
                if (e.isBegin)
                {
                    _FixationStartRID = this._RawdataID;
                    _FixationStartTime = (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds);
                    if (String.Compare("", _WebPos) != 0 && _WebPos != null)
                        _FixationScrollTop = System.Windows.Point.Parse(_WebPos);
                }
                else
                {
                    if (_FixationStartRID == 0)
                        return;
                    int duration = (int)Math.Round(_stopWatch.Elapsed.TotalMilliseconds) - _FixationStartTime;
                    int endid = this._RawdataID;
                    if (duration >= _EyeDeviceUC.GetGazeDurationThreshold())
                    {
                        //Add GazeFixation
                        string value = "'" + _FixationStartTime + "', '" + duration + "', '" + (float)e.fixation.X + "', '" + (float)e.fixation.Y + "', '" + (float)_FixationScrollTop.X + "', '" + (float)_FixationScrollTop.Y + "', '" + _FixationStartRID + "', '" + endid + "', '" + _FixationID + "'";
                        _DBSaver.GazeFixationInsert(value);
                        _FixationID++;
                    }
                }
            }
        }
        #endregion

    }

    public enum BitRate
    {
        _50kbit = 5000,
        _100kbit = 10000,
        _500kbit = 50000,
        _1000kbit = 1000000,
        _2000kbit = 2000000,
        _3000kbit = 3000000,
        _6000kbit = 6000000
    }

    public class DBSaver
    {
        private string _Path;
        private string _DBName;
        private string _DBPath;
        private string _SubjectName;
        private string _EyeDeviceType;
        private SQLiteConnection _sqliteConnect;
        private SQLiteTransaction _trans;

        public DBSaver(string DBName, string MainPath, string SubjectName, string EyeDeviceType)
        {
            _Path = MainPath + DBName + @"\";
            _DBName = DBName;
            _DBPath = _Path + @"Database\";
            _SubjectName = SubjectName;
            _EyeDeviceType = EyeDeviceType;
        }

        public void print()
        {
            Console.WriteLine(_DBName + " : " + _DBPath + " : " + _SubjectName);

        }

        public void DBConnect()
        {
            try
            {
                //Create Folder
                if (!Directory.Exists(_DBPath))
                {
                    Directory.CreateDirectory(_DBPath);
                }
                
                //Create DB and table
                if (!File.Exists(_DBPath + _DBName))
                {
                    SQLiteConnection.CreateFile(_DBPath + _DBName);

                    _sqliteConnect = new SQLiteConnection("Data source=" + _DBPath + _DBName);
                    _sqliteConnect.Open();
                    using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                    {
                        using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd.CommandText = @"CREATE TABLE IF NOT EXISTS MKEvent (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), EventID integer, EventTime integer, EventType varchar(50), EventTask varchar(50), EventParam varchar(2000), RID integer)";
                            _sqliteCmd.ExecuteNonQuery();
                            _sqliteCmd.CommandText = @"CREATE TABLE IF NOT EXISTS URLEvent (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), URLEventID integer, URL TEXT, Frame integer, StartTime integer, Keyword varchar(500), RID integer)";
                            _sqliteCmd.ExecuteNonQuery();
                            _sqliteCmd.CommandText = @"CREATE TABLE IF NOT EXISTS Statistics (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), EyeDeviceType varchar(50))";
                            _sqliteCmd.ExecuteNonQuery();
                            _sqliteCmd.CommandText = @"CREATE TABLE IF NOT EXISTS " + _SubjectName + @"Rawdata (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), Frame integer, Time integer, MousePosX float, MousePosY float, GazePosX float, GazePosY float, ScrollTop varchar(50), WindowPos varchar(100), PupilDiaX float, PupilDiaY float)";
                            _sqliteCmd.ExecuteNonQuery();
                            _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS MouseFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                            _sqliteCmd.ExecuteNonQuery();
                            _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS GazeFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                            _sqliteCmd.ExecuteNonQuery();
                        }
                        tr.Commit();
                    }
                    _trans = _sqliteConnect.BeginTransaction();
                }
                else
                {
                    _sqliteConnect = new SQLiteConnection("Data source=" + _DBPath + _DBName);
                    _sqliteConnect.Open();
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"CREATE TABLE IF NOT EXISTS " + _SubjectName + @"Rawdata (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), Frame integer, Time integer, MousePosX float, MousePosY float, GazePosX float, GazePosY float, ScrollTop varchar(50), WindowPos varchar(100), PupilDiaX float, PupilDiaY float)";
                        _sqliteCmd.ExecuteNonQuery();
                    }
                    _trans = _sqliteConnect.BeginTransaction();
                }
                SaveStatistics();
                SignalToChrome("get");
            }
            catch (Exception e) { }
        }
        public void DBDisconnect()
        {
            try
            {
                _trans.Commit();
                CorrectTime();
                _sqliteConnect.Close();
            }
            catch (Exception e) { }
            SignalToChrome("end");
        }
        public void DBMKEventInsert(string value)
        {
            try
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"INSERT INTO MKEvent VALUES (null, '" + _SubjectName + "', " + value + ")";
                    _sqliteCmd.ExecuteNonQuery();
                }
            }
            catch (Exception e) { }
        }
        public void RawdataInsert(string value)
        {
            try
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"INSERT INTO " + _SubjectName + @"Rawdata VALUES (null, '" + _SubjectName + "', " + value + ")";
                    _sqliteCmd.ExecuteNonQuery();
                }
            }
            catch (Exception e) { }
        }
        public void URLEventInsert(string value)
        {
            try
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"INSERT INTO URLEvent VALUES (null, '" + _SubjectName + "', " + value + ")";
                    _sqliteCmd.ExecuteNonQuery();
                }
            }
            catch (Exception e) { }
        }
        public void GazeFixationInsert(string value)
        {
            try
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', " + value + ")";
                    _sqliteCmd.ExecuteNonQuery();
                }
            }
            catch (Exception e) { }
        }

        private void SaveStatistics()
        {
            try
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"INSERT INTO Statistics VALUES (null, '" + _SubjectName + "', '" + _EyeDeviceType + "')";
                        _sqliteCmd.ExecuteNonQuery();
                    }
                    tr.Commit();
                }
            }
            catch (Exception e) { }
        }
        private void CorrectTime()
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    int previousRID = -1;
                    _sqliteCmd.CommandText = @"SELECT RID FROM URLEvent WHERE SubjectName=@subjectname ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@subjectname", _SubjectName);
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        int currentRID = rdr.GetInt32(0);
                        if (previousRID != -1)
                        {
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                int tryStep = 10;
                                string previosScrollTop = null;
                                _sqliteCmd2.CommandText = "SELECT ScrollTop, ID FROM " + _SubjectName + "Rawdata WHERE ID>=@previos AND ID<@current";
                                _sqliteCmd2.Parameters.AddWithValue("@previos", previousRID);
                                _sqliteCmd2.Parameters.AddWithValue("@current", currentRID);
                                var rdr2 = _sqliteCmd2.ExecuteReader();
                                while (rdr2.Read())
                                {
                                    string currentScrollTop = null;
                                    if (!rdr2.IsDBNull(0)) {
                                        currentScrollTop = rdr2.GetString(0);
                                    }
                                    if (previosScrollTop != null) {
                                        if (currentScrollTop != previosScrollTop) {
                                            if (String.Compare(previosScrollTop, "0,0") == 0)
                                            {
                                                System.Windows.Point pc = System.Windows.Point.Parse(currentScrollTop);
                                                if (Math.Abs(pc.X ) > 15 || Math.Abs(pc.Y) > 15)
                                                {
                                                    //update
                                                    using (SQLiteCommand _sqliteCmd3 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd3.CommandText = "UPDATE " + _SubjectName + "Rawdata SET ScrollTop=@scrolltop WHERE ID>=@start AND ID<@end";
                                                        _sqliteCmd3.Parameters.AddWithValue("@scrolltop", currentScrollTop);
                                                        _sqliteCmd3.Parameters.AddWithValue("@start", previousRID);
                                                        _sqliteCmd3.Parameters.AddWithValue("@end", rdr2.GetInt32(1));
                                                        _sqliteCmd3.ExecuteNonQuery();
                                                    }
                                                    break;
                                                }
                                            }
                                            else {
                                                System.Windows.Point pc = System.Windows.Point.Parse(currentScrollTop);
                                                System.Windows.Point pp = System.Windows.Point.Parse(previosScrollTop);
                                                if (Math.Abs(pc.X - pp.X) > 15 || Math.Abs(pc.Y - pp.Y) > 15)
                                                {
                                                    //update
                                                    using (SQLiteCommand _sqliteCmd3 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd3.CommandText = "UPDATE " + _SubjectName + "Rawdata SET ScrollTop=@scrolltop WHERE ID>=@start AND ID<@end";
                                                        _sqliteCmd3.Parameters.AddWithValue("@scrolltop", currentScrollTop);
                                                        _sqliteCmd3.Parameters.AddWithValue("@start", previousRID);
                                                        _sqliteCmd3.Parameters.AddWithValue("@end", rdr2.GetInt32(1));
                                                        _sqliteCmd3.ExecuteNonQuery();
                                                    }
                                                }
                                            }
                                            
                                        }
                                    }
                                    previosScrollTop = currentScrollTop;
                                    tryStep--;
                                    if (tryStep <= 0) {
                                        break;
                                    }
                                }
                                rdr2.Close();
                            }
                        }
                        previousRID = currentRID;
                    }
                    rdr.Close();
                    //last url
                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                    {
                        int tryStep = 10;
                        string previosScrollTop = null;
                        _sqliteCmd2.CommandText = "SELECT ScrollTop, ID FROM " + _SubjectName + "Rawdata WHERE ID>=@previos";
                        _sqliteCmd2.Parameters.AddWithValue("@previos", previousRID);
                        var rdr2 = _sqliteCmd2.ExecuteReader();
                        while (rdr2.Read())
                        {
                            string currentScrollTop = null;
                            if (!rdr2.IsDBNull(0))
                            {
                                currentScrollTop = rdr2.GetString(0);
                            }
                            if (previosScrollTop != null)
                            {
                                if (currentScrollTop != previosScrollTop)
                                {
                                    if (String.Compare(previosScrollTop, "0,0") == 0)
                                    {
                                        System.Windows.Point pc = System.Windows.Point.Parse(currentScrollTop);
                                        if (Math.Abs(pc.X) > 15 || Math.Abs(pc.Y) > 15)
                                        {
                                            //update
                                            using (SQLiteCommand _sqliteCmd3 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd3.CommandText = "UPDATE " + _SubjectName + "Rawdata SET ScrollTop=@scrolltop WHERE ID>=@start AND ID<@end";
                                                _sqliteCmd3.Parameters.AddWithValue("@scrolltop", currentScrollTop);
                                                _sqliteCmd3.Parameters.AddWithValue("@start", previousRID);
                                                _sqliteCmd3.Parameters.AddWithValue("@end", rdr2.GetInt32(1));
                                                _sqliteCmd3.ExecuteNonQuery();
                                            }
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        System.Windows.Point pc = System.Windows.Point.Parse(currentScrollTop);
                                        System.Windows.Point pp = System.Windows.Point.Parse(previosScrollTop);
                                        if (Math.Abs(pc.X - pp.X) > 15 || Math.Abs(pc.Y - pp.Y) > 15)
                                        {
                                            //update
                                            using (SQLiteCommand _sqliteCmd3 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd3.CommandText = "UPDATE " + _SubjectName + "Rawdata SET ScrollTop=@scrolltop WHERE ID>=@start AND ID<@end";
                                                _sqliteCmd3.Parameters.AddWithValue("@scrolltop", currentScrollTop);
                                                _sqliteCmd3.Parameters.AddWithValue("@start", previousRID);
                                                _sqliteCmd3.Parameters.AddWithValue("@end", rdr2.GetInt32(1));
                                                _sqliteCmd3.ExecuteNonQuery();
                                            }
                                        }
                                    }

                                }
                            }
                            previosScrollTop = currentScrollTop;
                            tryStep--;
                            if (tryStep <= 0)
                            {
                                break;
                            }
                        }
                        rdr2.Close();
                    }
                }
                tr.Commit();
            }
            System.Windows.Forms.MessageBox.Show("DONE");
        }
        

        /*********************  Communicate With Chrome Extension  ***************************/
        private void SignalToChrome(JToken data)
        {
            var json = new JObject();
            json["signal"] = data;

            var bytes = System.Text.Encoding.UTF8.GetBytes(json.ToString(Formatting.None));

            var stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
            stdout.Write(bytes, 0, bytes.Length);
            stdout.Flush();

            Console.SetOut(TextWriter.Null);
        }
    }
}
