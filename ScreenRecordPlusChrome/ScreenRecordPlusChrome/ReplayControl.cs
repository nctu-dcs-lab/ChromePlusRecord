using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DirectShowLib;
using System.Runtime.InteropServices;
using System.IO;

using SlimDX;
using System.Data.SQLite;

namespace ScreenRecordPlusChrome
{
    public partial class  ReplayControl : UserControl, ISampleGrabberCB
    {
        #region Variable
        private DsROTEntry rot;

        private const int WMGraphNotify = 0x0400 + 13;
        private const int VolumeFull = 0;
        private const int VolumeSilence = -10000;
        Guid TIME_FORMAT_FRAME = new Guid(0x7b785570, 0x8c82, 0x11cf, 0xbc, 0x0c, 0x00, 0xaa, 0x00, 0xac, 0x74, 0xf6);
        Guid TIME_FORMAT_MEDIA_TIME = new Guid(0x7b785574, 0x8c82, 0x11cf, 0xbc, 0x0c, 0x00, 0xaa, 0x00, 0xac, 0x74, 0xf6);

        // Media Controls
        private IFilterGraph2 _graphBuilder = null;
        private IMediaControl _mediaControl = null; //set video stsrt pause stop
        private IMediaSeeking _mediaSeeking = null; //set video position
        private IMediaPosition _mediaPosition = null; //set video speed
        private IMediaEventEx _mediaEventEx = null; //set video complete event
        private IBasicAudio _basicAudio = null; //set audio
        private IBasicVideo _basicVideo = null; //check media (video or audio)

        // Filter
        private IBaseFilter _vmr9 = null;
        private IVMRWindowlessControl9 _windowlessCtrl = null;
        private ISampleGrabber _sampleGrabber = null;
        private IBaseFilter _ibGrabber = null;

        private bool _isAudioOnly = false;
        private bool _isFullScreen = false;
        private bool _isMediaReady = false;
        private int _currentVolume = VolumeFull;
        private PlayState _currentState = PlayState.Stopped;
        private int _currentFrameID = 0;
        private int _mouseTrackType = 0;
        private int _gazeTrackType = 0;
        private int _mouseFixationRate = 50;
        private int _gazeFixationRate = 50;
        private int _mouseBrowserToolbarHeight = 66;
        private int _gazeBrowserToolbarHeight = 66;

        private string _mainDir = null;
        private string _projectName = null;

        private Compositor _compositor = null;
        private DBTrack _dbTrack = null;
        private List<int> _trackStartFrameID = null;
        private List<int> _urlStartRID = null;
        private List<URLInfo> _urlInfo = null;

        public event EventHandler MediaComplete;
        public delegate void FrameEventHandler(object sender, FrameEventArgs e);
        public event FrameEventHandler FrameEvent;
        #endregion

        public ReplayControl(string mainDir, string projectName)
        {
            InitializeComponent();

            _mainDir = mainDir;
            _projectName = projectName;
            _dbTrack = new DBTrack(mainDir, projectName);
            _dbTrack.DBConnect();

            // We paint the windows ourself
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        private void BuildGraph(string filename)
        {
            int hr = 0;

            try
            {
                // QueryInterface for DirectShow interfaces
                _graphBuilder = (IFilterGraph2)new FilterGraph();

                _mediaControl = (IMediaControl)_graphBuilder;
                _mediaSeeking = (IMediaSeeking)_graphBuilder;
                _mediaPosition = (IMediaPosition)_graphBuilder;
                _mediaEventEx = (IMediaEventEx)_graphBuilder;

                // Have the graph signal event via window callbacks for performance
                hr = _mediaEventEx.SetNotifyWindow(this.Handle, WMGraphNotify, IntPtr.Zero);
                DsError.ThrowExceptionForHR(hr);

                rot = new DsROTEntry(_graphBuilder);

                // Build and configure VMR9 filter
                _vmr9 = (IBaseFilter)new VideoMixingRenderer9();
                ConfigureVMR9InWindowlessMode();

                // Build and configure SampleGrabber filter
                _sampleGrabber = new SampleGrabber() as ISampleGrabber;
                _ibGrabber = (IBaseFilter)_sampleGrabber;
                ConfigureSampleGrabber(_sampleGrabber);

                // Set filter in FilterGraph
                hr = _graphBuilder.AddFilter(_vmr9, "Video Mixing Renderer 9");
                DsError.ThrowExceptionForHR(hr);
                hr = _graphBuilder.AddFilter(_ibGrabber, "Sample grabber");
                DsError.ThrowExceptionForHR(hr);
                hr = _graphBuilder.RenderFile(filename, null);
                DsError.ThrowExceptionForHR(hr);

                _mediaSeeking.SetTimeFormat(TIME_FORMAT_FRAME);

                // Query for audio interfaces, which may not be relevant for video-only files
                _basicAudio = _graphBuilder as IBasicAudio;
                // Query for video interfaces, which may not be relevant for audio files
                _basicVideo = _graphBuilder as IBasicVideo;

                // Is this an audio-only file (no video component)?
                CheckVisibility();

                if (!_isAudioOnly)
                {
                    // Preview vedio
                    _currentState = PlayState.Stopped;
                    hr = _mediaControl.Stop();
                    DsError.ThrowExceptionForHR(hr);
                    hr = _mediaSeeking.SetPositions(0, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                    DsError.ThrowExceptionForHR(hr);
                    hr = _mediaControl.Pause();
                    DsError.ThrowExceptionForHR(hr);

                    _isMediaReady = true;
                }
                else
                {
                    CloseInterfaces();
                    MessageBox.Show("This media isn't video type!","ERROR");
                }
            }
            catch (Exception e)
            {
                CloseInterfaces();
                MessageBox.Show("An error occured during the graph building : \r\n\r\n" + e.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckVisibility()
        {
            if ((_basicVideo == null))
            {
                // Audio-only files have no video interfaces.  This might also
                // be a file whose video component uses an unknown video codec.
                _isAudioOnly = true;
                return;
            }
            else
            {
                // Clear the global flag
                _isAudioOnly = false;
            }
        }

        private void ConfigureVMR9InWindowlessMode()
        {
            int hr = 0;

            IVMRFilterConfig9 filterConfig = (IVMRFilterConfig9)_vmr9;

            // Must be called before calling SetImageCompositor
            hr = filterConfig.SetNumberOfStreams(1);
            DsError.ThrowExceptionForHR(hr);

            // Create an instance of the Compositor
            _compositor = new Compositor();

            // Configure the filter with the Compositor
            hr = filterConfig.SetImageCompositor(_compositor);
            DsError.ThrowExceptionForHR(hr);

            // Change VMR9 mode to Windowless
            hr = filterConfig.SetRenderingMode(VMR9Mode.Windowless);
            DsError.ThrowExceptionForHR(hr);

            _windowlessCtrl = (IVMRWindowlessControl9)_vmr9;

            // Set rendering window
            hr = _windowlessCtrl.SetVideoClippingWindow(this.Handle);
            DsError.ThrowExceptionForHR(hr);

            // Set Aspect-Ratio
            hr = _windowlessCtrl.SetAspectRatioMode(VMR9AspectRatioMode.LetterBox);
            DsError.ThrowExceptionForHR(hr);

            // Add delegates for Windowless operations
            AddHandlers();

            // Call the resize handler to configure the output size
            ReplayControl_ResizeMove(null, null);
        }

        private void ConfigureSampleGrabber(ISampleGrabber sampGrabber)
        {
            AMMediaType media;
            int hr;

            // Set the media type to Video/RBG24
            media = new AMMediaType();
            media.majorType = DirectShowLib.MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;
            hr = sampGrabber.SetMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            DsUtils.FreeAMMediaType(media);
            media = null;

            // Choose to call BufferCB instead of SampleCB
            hr = sampGrabber.SetCallback(this, 0);
            DsError.ThrowExceptionForHR(hr);
        }

        private void CloseInterfaces()
        {
            try
            {
                lock (this)
                {
                    if (_mediaControl != null)
                        _mediaControl.Stop();

                    if (this._mediaEventEx != null)
                    {
                        int hr = 0;
                        hr = this._mediaEventEx.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    if (rot != null)
                    {
                        rot.Dispose();
                        rot = null;
                    }

                    RemoveHandlers();

                    if (_compositor != null)
                    {
                        _compositor.Dispose();
                        _compositor = null;
                    }

                    // Release and zero DirectShow interfaces
                    if (_vmr9 != null)
                    {
                        Marshal.ReleaseComObject(_vmr9);
                        _vmr9 = null;
                        _windowlessCtrl = null;
                    }

                    if (_sampleGrabber != null)
                    {
                        Marshal.ReleaseComObject(_sampleGrabber);
                        _sampleGrabber = null;
                    }
                    if (_ibGrabber != null)
                    {
                        Marshal.ReleaseComObject(_ibGrabber);
                        _ibGrabber = null;
                    }


                    if (_graphBuilder != null)
                    {
                        Marshal.ReleaseComObject(_graphBuilder);
                        _graphBuilder = null;
                        _mediaControl = null;
                        _mediaSeeking = null;
                        _mediaPosition = null;
                        _mediaEventEx = null;
                        _basicAudio = null;
                        _basicVideo = null;
                    }

                    // Reset variables
                    _isAudioOnly = false;
                    _isFullScreen = false;
                    _isMediaReady = false;
                    _currentVolume = VolumeFull;
                    _currentState = PlayState.Stopped;
                    _currentFrameID = 0;
                    _mouseTrackType = 0;
                    _gazeTrackType = 0;
                    _mouseFixationRate = 50;
                    _gazeFixationRate = 50;
                    _mouseBrowserToolbarHeight = 66;
                    _gazeBrowserToolbarHeight = 66;
                    if (_trackStartFrameID != null)
                    {
                        _trackStartFrameID.Clear();
                        _trackStartFrameID = null;
                    }
                    if (_urlStartRID != null) {
                        _urlStartRID.Clear();
                        _urlStartRID = null;
                    }
                    if (_urlInfo != null) {
                        _urlInfo.Clear();
                        _urlInfo = null;
                    }

                    GC.Collect();
                }
            }
            catch
            {
            }

        }

        private void CloseWhenDispose()
        {
            // Close DB
            if (_dbTrack != null)
            {
                _dbTrack.DBDisconnect();
                _dbTrack = null;
            }
            _mainDir = null;
            _projectName = null;
        }

        private long GetFrameNumber()
        {
            int hr;
            long total = 0;
            if (_mediaSeeking != null)
            {
                hr = _mediaSeeking.GetStopPosition(out total);
                DsError.ThrowExceptionForHR(hr);
            }
            return (total - 1);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            CloseInterfaces();
            CloseWhenDispose();

            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WMGraphNotify:
                    {
                        HandleGraphEvent();
                        break;
                    }
            }

            base.WndProc(ref m);
        }

        private void HandleGraphEvent()
        {
            int hr = 0;
            EventCode evCode;
            IntPtr evParam1, evParam2;

            // Make sure that we don't access the media event interface
            // after it has already been released.
            if (_mediaEventEx == null)
                return;

            // Process all queued events
            while (_mediaEventEx.GetEvent(out evCode, out evParam1, out evParam2, 0) == 0)
            {
                // Free memory associated with callback, since we're not using it
                hr = _mediaEventEx.FreeEventParams(evCode, evParam1, evParam2);
                DsError.ThrowExceptionForHR(hr);

                // If this is the end of the clip, reset to beginning
                if (evCode == EventCode.Complete)
                {
                    //Console.WriteLine("Complete");
                    if (_mediaControl.Stop() >= 0)
                        _currentState = PlayState.Paused;

                    // Reset to first frame of movie
                    hr = _mediaSeeking.SetPositions(0, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                    DsError.ThrowExceptionForHR(hr);

                    // Send complete event to parent
                    if (MediaComplete != null)
                        MediaComplete(this, new EventArgs());
                }
            }
        }

        #region ISampleGrabberCB
        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            if (_currentState == PlayState.Running)
            {
                FrameEventArgs feargs = new FrameEventArgs();
                feargs.startTime = SampleTime;
                feargs.frameID = GetPosition();
                _currentFrameID = (int)feargs.frameID;
                if (FrameEvent != null)
                    FrameEvent(this, feargs);
            }
            if (!_dbTrack.IsUserNameNull() && _compositor != null)
            {
                int startFrameID = _dbTrack.CheckPartTrackStartFrameID(_trackStartFrameID, _currentFrameID);
                var trackinfolist = _dbTrack.GetPartTrackFromDBWithFrameID(startFrameID, _currentFrameID);
                TrackData trackInfoScroll = _dbTrack.CheckPartTrackScrollEvent(trackinfolist, _mouseTrackType, _gazeTrackType, _mouseFixationRate, _gazeFixationRate, _mouseBrowserToolbarHeight, _gazeBrowserToolbarHeight);
                _compositor.SetPartTrack(trackInfoScroll);

            }
            Marshal.ReleaseComObject(pSample);
            return 0;
        }
        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            return 0;
        }
        #endregion

        #region Media Handler
        private void AddHandlers()
        {
            // Add handlers for VMR purpose
            this.Paint += new PaintEventHandler(ReplayControl_Paint); // for WM_PAINT
            this.Resize += new EventHandler(ReplayControl_ResizeMove); // for WM_SIZE
            this.Move += new EventHandler(ReplayControl_ResizeMove); // for WM_MOVE

        }

        private void RemoveHandlers()
        {
            // remove handlers when they are no more needed
            this.Paint -= new PaintEventHandler(ReplayControl_Paint);
            this.Resize -= new EventHandler(ReplayControl_ResizeMove);
            this.Move -= new EventHandler(ReplayControl_ResizeMove);

        }

        private void ReplayControl_Paint(object sender, PaintEventArgs e)
        {
            if (_windowlessCtrl != null)
            {
                IntPtr hdc = e.Graphics.GetHdc();
                int hr = _windowlessCtrl.RepaintVideo(this.Handle, hdc);
                e.Graphics.ReleaseHdc(hdc);
            }
        }

        private void ReplayControl_ResizeMove(object sender, EventArgs e)
        {
            if (_windowlessCtrl != null)
            {
                int hr = _windowlessCtrl.SetVideoPosition(null, DsRect.FromRectangle(this.ClientRectangle));
            }
        }
        #endregion

        #region Media Control
        public void FileOpen_Click()
        {
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    CloseInterfaces();
                    BuildGraph(openFileDialog.FileName);
                }
            }
        }

        public long OpenFile(string filePath, string userName)
        {
            if (File.Exists(filePath))
            {
                CloseInterfaces();

                //Get Track
                _dbTrack.SetUserName(userName);
                var urlinfo = _dbTrack.GetAllPartTrackStartFrameID();
                _trackStartFrameID = urlinfo.Item1;
                _urlStartRID = urlinfo.Item2;
                _urlInfo = urlinfo.Item3;

                BuildGraph(filePath);
                return GetFrameNumber();
            }
            return -1;
        }

        public bool MediaReady()
        {
            return _isMediaReady;
        }

        public void RunPauseGraph()
        {
            if (this._mediaControl == null)
                return;

            // Toggle play/pause behavior
            if ((this._currentState == PlayState.Paused) || (this._currentState == PlayState.Stopped))
            {
                if (this._mediaControl.Run() >= 0)
                    this._currentState = PlayState.Running;
            }
            else
            {
                if (this._mediaControl.Pause() >= 0)
                    this._currentState = PlayState.Paused;
            }
        }

        public void StopGraph()
        {
            int hr = 0;

            if ((this._mediaControl == null) || (this._mediaSeeking == null))
                return;

            // Stop and reset postion to beginning
            if ((this._currentState == PlayState.Paused) || (this._currentState == PlayState.Running))
            {
                if (this._mediaControl.Stop() >= 0)
                    this._currentState = PlayState.Stopped;

                // Seek to the beginning
                hr = this._mediaSeeking.SetPositions(0, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                DsError.ThrowExceptionForHR(hr);

                // Display the first frame to indicate the reset condition
                hr = this._mediaControl.Pause();
                DsError.ThrowExceptionForHR(hr);
            }
        }

        public int ToggleMute()
        {
            int hr = 0;

            if ((this._graphBuilder == null) || (this._basicAudio == null))
                return 0;

            // Read current volume
            hr = this._basicAudio.get_Volume(out this._currentVolume);
            if (hr == -1) //E_NOTIMPL
            {
                // Fail quietly if this is a video-only media file
                return 0;
            }
            else if (hr < 0)
            {
                return hr;
            }

            // Switch volume levels
            if (this._currentVolume == VolumeFull)
                this._currentVolume = VolumeSilence;
            else
                this._currentVolume = VolumeFull;

            // Set new volume
            hr = this._basicAudio.put_Volume(this._currentVolume);

            return hr;
        }

        public int SetRate(double rate)
        {
            int hr = 0;

            // If the IMediaPosition interface exists, use it to set rate
            if (this._mediaPosition != null)
            {
                hr = this._mediaPosition.put_Rate(rate);
                DsError.ThrowExceptionForHR(hr);
            }

            return hr;
        }

        public void SetPosition(long position)
        {
            _currentFrameID = (int)position;
            int hr;
            hr = this._mediaSeeking.SetPositions(position, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
            DsError.ThrowExceptionForHR(hr);
            hr = this._mediaControl.Pause();
            DsError.ThrowExceptionForHR(hr);
        }

        public long GetPosition()
        {
            long pos = -1;
            int hr = this._mediaSeeking.GetCurrentPosition(out pos);
            DsError.ThrowExceptionForHR(hr);
            return pos;
        }

        public PlayState GetMediaState()
        {
            PlayState ps = _currentState;
            return ps;
        }

        public int GetAudioState()
        {
            return _currentVolume;
        }

        public int GetForwardPageFrameID() {
            int result = 0;
            if (_trackStartFrameID != null && _trackStartFrameID.Count > 0)
            {
                int id = _trackStartFrameID.FindLastIndex(item => item <= (_currentFrameID));
                if (id + 1 < _trackStartFrameID.Count)
                    result = _trackStartFrameID[id + 1];
                else
                    result = _currentFrameID;
            }
            return result;
        }

        public int GetBackwardPageFrameID()
        {
            int result = 0;
            if (_trackStartFrameID != null && _trackStartFrameID.Count > 0)
                result = _trackStartFrameID.FindLast(item => item < (_currentFrameID));
            return result;
        }

        public URLInfo GetURLInfo(int currentFrameID) {
            URLInfo result = new URLInfo();
            result.frame = 0;
            result.id = -1;
            result.size = 0;
            if (_trackStartFrameID != null && _trackStartFrameID.Count > 0)
            {
                int id = _trackStartFrameID.FindLastIndex(item => item <= (currentFrameID));
                if (id < _urlInfo.Count)
                    result = _urlInfo[id];
                if (_urlInfo[0].id != -1) {
                    result.size = _urlInfo.Count;
                }
            }
            return result;
        }

        public List<int> GetAllURLStartRID()
        {
            return _urlStartRID;
        }

        public string GetTotalTime()
        {
            long total = 0;
            if (_dbTrack != null)
            {
                total = _dbTrack.GatTotalTime();
            }
            TimeSpan t = TimeSpan.FromMilliseconds(total);
            string result = t.ToString(@"hh\:mm\:ss");
            return result;
        }

        public string GetCurrentTime(int currentFrameID) {
            long total = 0;
            if (_dbTrack != null)
            {
                total = _dbTrack.GatCurrentTime(currentFrameID);
            }
            TimeSpan t = TimeSpan.FromMilliseconds(total);
            string result = t.ToString(@"hh\:mm\:ss");
            return result;
        }

        public bool SetMouseTrackVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._mouseTrackDraw)
                {
                    _compositor._mouseTrackDraw = false;
                }
                else
                {
                    _compositor._mouseTrackDraw = true;
                }
                FrameRefresh();
                return _compositor._mouseTrackDraw;
            }
            return false;
        }
        public int SetMouseTrackType() {
            if (_mouseTrackType == 0) {
                _mouseTrackType = 1;
            }
            else if (_mouseTrackType == 1) {
                _mouseTrackType = 0;
            }
            FrameRefresh();
            return _mouseTrackType;
        }
        public void SetMouseTrackColor(string hex) {
            if (_compositor != null) {
                _compositor._mouseTrackColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetMouseTrackLineWidth(int width) {
            if (_compositor != null)
            {
                _compositor._mouseTrackLineWidth = width;
            }
            FrameRefresh();
        }

        public bool SetMouseCursorVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._mouseCursorDraw)
                {
                    _compositor._mouseCursorDraw = false;
                }
                else
                {
                    _compositor._mouseCursorDraw = true;
                }
                FrameRefresh();
                return _compositor._mouseCursorDraw;
            }
            return false;
        }
        public bool SetMouseCursorCircleVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._mouseCursorCircleDraw)
                {
                    _compositor._mouseCursorCircleDraw = false;
                }
                else
                {
                    _compositor._mouseCursorCircleDraw = true;
                }
                FrameRefresh();
                return _compositor._mouseCursorCircleDraw;
            }
            return false;
        }
        public void SetMouseCursorCircleColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._mouseCursorCircleColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetMouseCursorCircleLineWidth(int width)
        {
            if (_compositor != null)
            {
                _compositor._mouseCursorCircleLineWidth = width;
            }
            FrameRefresh();
        }

        public bool SetMouseFixationVisible() {
            if (_compositor != null)
            {
                if (_compositor._mouseFixationDraw)
                {
                    _compositor._mouseFixationDraw = false;
                }
                else
                {
                    _compositor._mouseFixationDraw = true;
                }
                FrameRefresh();
                return _compositor._mouseFixationDraw;
            }
            return false;
        }
        public void SetMouseFixationColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._mouseFixationColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetMouseFixationLineWidth(int width)
        {
            if (_compositor != null)
            {
                _compositor._mouseFixationLineWidth = width;
            }
            FrameRefresh();
        }
        public void SetMouseFixationRate(int rate) {
            _mouseFixationRate = (10-rate) * 10;
            FrameRefresh();
        }
        public void SetMouseBrowserToolbarHeight(int height) {
            _mouseBrowserToolbarHeight = height;
            FrameRefresh();
        }

        public bool SetMouseScanpathVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._mouseScanpathDraw)
                {
                    _compositor._mouseScanpathDraw = false;
                }
                else
                {
                    _compositor._mouseScanpathDraw = true;
                }
                FrameRefresh();
                return _compositor._mouseScanpathDraw;
            }
            return false;
        }
        public void SetMouseScanpathColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._mouseScanpathColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetMouseScanpathLineWidth(int width)
        {
            if (_compositor != null)
            {
                _compositor._mouseScanpathLineWidth = width;
            }
            FrameRefresh();
        }

        public bool SetMouseFixationIDVisible() {
            if (_compositor != null)
            {
                if (_compositor._mouseFixationIDDraw)
                {
                    _compositor._mouseFixationIDDraw = false;
                }
                else
                {
                    _compositor._mouseFixationIDDraw = true;
                }
                FrameRefresh();
                return _compositor._mouseFixationIDDraw;
            }
            return false;
        }
        public void SetMouseFixationIDColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._mouseFixationIDColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }


        public bool SetGazeTrackVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._gazeTrackDraw)
                {
                    _compositor._gazeTrackDraw = false;
                }
                else
                {
                    _compositor._gazeTrackDraw = true;
                }
                FrameRefresh();
                return _compositor._gazeTrackDraw;
            }
            return false;
        }
        public int SetGazeTrackType()
        {
            if (_gazeTrackType == 0)
            {
                _gazeTrackType = 1;
            }
            else if (_gazeTrackType == 1)
            {
                _gazeTrackType = 0;
            }
            FrameRefresh();
            return _gazeTrackType;
        }
        public void SetGazeTrackColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._gazeTrackColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetGazeTrackLineWidth(int width)
        {
            if (_compositor != null)
            {
                _compositor._gazeTrackLineWidth = width;
            }
            FrameRefresh();
        }

        public bool SetGazeCursorVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._gazeCursorDraw)
                {
                    _compositor._gazeCursorDraw = false;
                }
                else
                {
                    _compositor._gazeCursorDraw = true;
                }
                FrameRefresh();
                return _compositor._gazeCursorDraw;
            }
            return false;
        }
        public bool SetGazeCursorCircleVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._gazeCursorCircleDraw)
                {
                    _compositor._gazeCursorCircleDraw = false;
                }
                else
                {
                    _compositor._gazeCursorCircleDraw = true;
                }
                FrameRefresh();
                return _compositor._gazeCursorCircleDraw;
            }
            return false;
        }
        public void SetGazeCursorCircleColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._gazeCursorCircleColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetGazeCursorCircleLineWidth(int width)
        {
            if (_compositor != null)
            {
                _compositor._gazeCursorCircleLineWidth = width;
            }
            FrameRefresh();
        }

        public bool SetGazeFixationVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._gazeFixationDraw)
                {
                    _compositor._gazeFixationDraw = false;
                }
                else
                {
                    _compositor._gazeFixationDraw = true;
                }
                FrameRefresh();
                return _compositor._gazeFixationDraw;
            }
            return false;
        }
        public void SetGazeFixationColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._gazeFixationColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetGazeFixationLineWidth(int width)
        {
            if (_compositor != null)
            {
                _compositor._gazeFixationLineWidth = width;
            }
            FrameRefresh();
        }
        public void SetGazeFixationRate(int rate)
        {
            _gazeFixationRate = (10 - rate) * 10;
            FrameRefresh();
        }
        public void SetGazeBrowserToolbarHeight(int height) {
            _gazeBrowserToolbarHeight = height;
            FrameRefresh();
        }

        public bool SetGazeScanpathVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._gazeScanpathDraw)
                {
                    _compositor._gazeScanpathDraw = false;
                }
                else
                {
                    _compositor._gazeScanpathDraw = true;
                }
                FrameRefresh();
                return _compositor._gazeScanpathDraw;
            }
            return false;
        }
        public void SetGazeScanpathColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._gazeScanpathColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        public void SetGazeScanpathLineWidth(int width)
        {
            if (_compositor != null)
            {
                _compositor._gazeScanpathLineWidth = width;
            }
            FrameRefresh();
        }

        public bool SetGazeFixationIDVisible()
        {
            if (_compositor != null)
            {
                if (_compositor._gazeFixationIDDraw)
                {
                    _compositor._gazeFixationIDDraw = false;
                }
                else
                {
                    _compositor._gazeFixationIDDraw = true;
                }
                FrameRefresh();
                return _compositor._gazeFixationIDDraw;
            }
            return false;
        }
        public void SetGazeFixationIDColor(string hex)
        {
            if (_compositor != null)
            {
                _compositor._gazeFixationIDColor = ColorTranslator.FromHtml(hex);
            }
            FrameRefresh();
        }
        #endregion

        #region Media Control Support
        private void FrameRefresh() {
            if (_currentState != PlayState.Running)
            {
                int hr = this._mediaSeeking.SetPositions(_currentFrameID, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                DsError.ThrowExceptionForHR(hr);
                hr = this._mediaControl.Pause();
                DsError.ThrowExceptionForHR(hr);
            }
        }
        #endregion

    }
    public class DBTrack : DBBase
    {
        private const int BrowserToolbarHeight = 30;
        protected string _userName = null;

        public DBTrack(string mainDir, string projectName)
            : base(mainDir, projectName)
        {
        }

        public void SetUserName(string userName)
        {
            this._userName = userName;
        }

        public void ClearUserName()
        {
            _userName = null;
        }

        public bool IsUserNameNull()
        {
            if (_userName == null)
                return true;
            else
                return false;
        }

        public int GatTotalTime() {
            int result = 0;
            try
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"SELECT MAX(Time) FROM " + _userName + "Rawdata";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        result = rdr.GetInt32(0);
                    }
                    rdr.Close();
                }
            }
            catch { }
            return result;
        }

        public int GatCurrentTime(int frameid)
        {
            int result = 0;
            try
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"SELECT MAX(Time) FROM " + _userName + "Rawdata WHERE Frame=@frameid";
                    _sqliteCmd.Parameters.AddWithValue("@frameid", frameid);
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        result = rdr.GetInt32(0);
                    }
                    rdr.Close();
                }
            }
            catch { }
            return result;
        }

        public List<TrackInfo> GetPartTrackFromDBWithFrameID(int startframeID, int endframeID)
        {
            List<TrackInfo> result = new List<TrackInfo>();
            if (this._sqliteConnect != null && _userName != null)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT MousePosX, MousePosY, GazePosX, GazePosY, Time, ID, ScrollTop, WindowPos FROM " + _userName + "Rawdata WHERE Frame>=@startframeid AND Frame<=@endframeid ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@startframeid", startframeID);
                        _sqliteCmd.Parameters.AddWithValue("@endframeid", endframeID);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            TrackInfo trackinfo = new TrackInfo();
                            //mouse
                            if (!rdr.IsDBNull(0) && !rdr.IsDBNull(1))
                            {
                                trackinfo.mouseTrack.X = rdr.GetFloat(0);
                                trackinfo.mouseTrack.Y = rdr.GetFloat(1);
                            }
                            //gaze
                            if (!rdr.IsDBNull(2) && !rdr.IsDBNull(3))
                            {
                                trackinfo.gazeTrack.X = rdr.GetFloat(2);
                                trackinfo.gazeTrack.Y = rdr.GetFloat(3);
                            }
                            if (!rdr.IsDBNull(4)) {
                                trackinfo.time = rdr.GetInt32(4);
                            }
                            if (!rdr.IsDBNull(5)) {
                                trackinfo.id = rdr.GetInt32(5);
                            }
                            if (!rdr.IsDBNull(6))
                            {
                                System.Windows.Point scroll = System.Windows.Point.Parse(rdr.GetString(6));
                                trackinfo.scrollTop.X = (float)scroll.X;
                                trackinfo.scrollTop.Y = (float)scroll.Y;
                            }
                            if (!rdr.IsDBNull(7)) {
                                trackinfo.browserSize = System.Windows.Media.Media3D.Point4D.Parse(rdr.GetString(7));
                            }
                            result.Add(trackinfo);
                        }
                        rdr.Close();
                    }
                    tr.Commit();
                }
            }
            return result;
        }

        public Tuple<List<int>, List<int>, List<URLInfo>> GetAllPartTrackStartFrameID()
        {
            List<int> result = new List<int>();
            List<int> result2 = new List<int>();
            List<URLInfo> result3 = new List<URLInfo>();
            if (this._sqliteConnect != null && _userName != null)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT Frame, URLEventID, URL, Keyword, RID FROM URLEvent WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", _userName);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            URLInfo urlinfo = new URLInfo();
                            urlinfo.id = rdr.GetInt32(1) + 1;
                            if (!rdr.IsDBNull(2))
                                urlinfo.url = rdr.GetString(2);
                            if (!rdr.IsDBNull(3))
                                urlinfo.keyword = rdr.GetString(3);
                            if (rdr.GetInt32(0) == 1)
                            {
                                result.Add(0);
                                urlinfo.frame = 0;
                            }
                            else
                            {
                                result.Add(rdr.GetInt32(0));
                                urlinfo.frame = rdr.GetInt32(0);
                            }
                            result2.Add(rdr.GetInt32(4));
                            result3.Add(urlinfo);
                        }
                        rdr.Close();
                        if (result.Count == 0)
                        {
                            result.Add(0);
                            result2.Add(1);
                            URLInfo urlinfo = new URLInfo();
                            urlinfo.frame = 0;
                            urlinfo.id = -1;
                            result3.Add(urlinfo);
                        }
                    }
                    tr.Commit();
                }
            }
            result.Sort((x, y) => { return x.CompareTo(y); });
            result2.Sort();

            return new Tuple<List<int>, List<int>, List<URLInfo>>(result, result2, result3);
        }

        public int CheckPartTrackStartFrameID(List<int> allStartFrameID, int currentFrameID)
        {
            int result = 0;
            int lagframe = 0;
            result = allStartFrameID.FindLast(item => item <= (currentFrameID + lagframe));
            return result;
        }

        public TrackData CheckPartTrackScrollEvent(List<TrackInfo> track, int mtype, int gtype, int MfixationRate, int GfixationRate, int MBrowserToolbarH, int GBrowserToolbarH)
        {
            TrackData result = CheckFixation(track, MfixationRate, GfixationRate, MBrowserToolbarH, GBrowserToolbarH);
            if (track.Count != 0) { 
                //Track move when scrolling
                if (mtype == 0 || gtype==0)
                {
                    float lastx = track[track.Count-1].scrollTop.X;
                    float lasty = track[track.Count-1].scrollTop.Y;
                    for (int i = 0; i < track.Count; i++)
                    {
                        if (mtype == 0 && track[i].mouseTrack.X != -1 && track[i].mouseTrack.Y != -1)
                        {
                            Vector2 m = new Vector2(track[i].mouseTrack.X + (track[i].scrollTop.X - lastx), track[i].mouseTrack.Y + (track[i].scrollTop.Y - lasty));
                            result.mouseTracks.Add(m);
                        }
                        if (gtype == 0 && track[i].gazeTrack.X != -1 && track[i].gazeTrack.Y != -1 && track[i].gazeTrack.X != 0 && track[i].gazeTrack.Y != 0)
                        {
                            Vector2 g = new Vector2(track[i].gazeTrack.X + (track[i].scrollTop.X - lastx), track[i].gazeTrack.Y + (track[i].scrollTop.Y - lasty));
                            result.gazeTracks.Add(g);
                        }
                    }
                }
                //Track disappear when scrolling
                if (mtype == 1 || gtype==1)
                {
                    float lastx = track[track.Count - 1].scrollTop.X;
                    float lasty = track[track.Count - 1].scrollTop.Y;
                    int startid = track.Count - 1;
                    for (int i = (track.Count - 1); i >= 0; i--)
                    {
                        if ((lastx == track[i].scrollTop.X) && (lasty == track[i].scrollTop.Y))
                            startid = i;
                        else
                            break;
                    }
                    for (int i = startid; i < track.Count; i++) {
                        if (mtype == 1 && track[i].mouseTrack.X != -1 && track[i].mouseTrack.Y != -1)
                        {
                            result.mouseTracks.Add(track[i].mouseTrack);
                        }
                        if (gtype == 1 && track[i].gazeTrack.X != -1 && track[i].gazeTrack.Y != -1 && track[i].gazeTrack.X != 0 && track[i].gazeTrack.Y != 0)
                        {
                            result.gazeTracks.Add(track[i].gazeTrack);
                        }
                    }
                }
            }
            return result;
        }

        private TrackData CheckFixation(List<TrackInfo> track, int MfixationRate, int GfixationRate, int MBrowserToolbarH, int GBrowserToolbarH)
        {
            TrackData result = new TrackData();
            if (track.Count != 0)
            {
                int startid = track[0].id;
                int endid = track[track.Count - 1].id;
                int lasttime = track[track.Count - 1].time;
                float lastx = track[track.Count - 1].scrollTop.X;
                float lasty = track[track.Count - 1].scrollTop.Y;
                
                if (this._sqliteConnect != null && _userName != null)
                {
                    try
                    {
                        using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                        {
                            //mouse
                            _sqliteCmd.CommandText = @"SELECT FID, PositionX, PositionY, ScrollTopX, ScrollTopY, EndID, StartTime, Duration FROM MouseFixation WHERE StartID>=@startid AND StartID<=@endid AND SubjectName=@subjectname ORDER BY ID";
                            _sqliteCmd.Parameters.AddWithValue("@startid", startid);
                            _sqliteCmd.Parameters.AddWithValue("@endid", endid);
                            _sqliteCmd.Parameters.AddWithValue("@subjectname", _userName);
                            var rdr = _sqliteCmd.ExecuteReader();
                            while (rdr.Read())
                            {
                                FixationInfo finfo = new FixationInfo();
                                finfo.fid = rdr.GetInt32(0);
                                if (rdr.GetFloat(2) > MBrowserToolbarH)
                                    finfo.fixation = new Vector2(rdr.GetFloat(1) + (rdr.GetFloat(3) - lastx), rdr.GetFloat(2) + (rdr.GetFloat(4) - lasty));
                                else
                                    finfo.fixation = new Vector2(rdr.GetFloat(1) , rdr.GetFloat(2));
                                if (rdr.GetInt32(5) <= endid)
                                {
                                    finfo.fsize = (rdr.GetInt32(7)) / MfixationRate;
                                }
                                else
                                {
                                    finfo.fsize = (lasttime - rdr.GetInt32(6)) / MfixationRate;
                                }
                                result.mouseFixations.Add(finfo);
                            }
                            rdr.Close();
                        }
                        //gaze
                        using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd.CommandText = @"SELECT FID, PositionX, PositionY, ScrollTopX, ScrollTopY, EndID, StartTime, Duration FROM GazeFixation WHERE StartID>=@startid AND StartID<=@endid AND SubjectName=@subjectname ORDER BY ID";
                            _sqliteCmd.Parameters.AddWithValue("@startid", startid);
                            _sqliteCmd.Parameters.AddWithValue("@endid", endid);
                            _sqliteCmd.Parameters.AddWithValue("@subjectname", _userName);
                            var rdr = _sqliteCmd.ExecuteReader();
                            while (rdr.Read())
                            {
                                FixationInfo finfo = new FixationInfo();
                                finfo.fid = rdr.GetInt32(0);
                                if (rdr.GetFloat(2) > GBrowserToolbarH)
                                    finfo.fixation = new Vector2(rdr.GetFloat(1) + (rdr.GetFloat(3) - lastx), rdr.GetFloat(2) + (rdr.GetFloat(4) - lasty));
                                else
                                    finfo.fixation = new Vector2(rdr.GetFloat(1), rdr.GetFloat(2));
                                if (rdr.GetInt32(5) <= endid)
                                {
                                    finfo.fsize = (rdr.GetInt32(7)) / GfixationRate;
                                }
                                else
                                {
                                    finfo.fsize = (lasttime - rdr.GetInt32(6)) / GfixationRate;
                                }
                                result.gazeFixations.Add(finfo);
                            }
                            rdr.Close();
                        }
                    }
                    catch { }
                }
            }
            return result;
        }
    }

    public class FrameEventArgs : EventArgs
    {
        public double startTime { get; set; }
        public long frameID { get; set; }
    }
    public class TrackData
    {
        public List<Vector2> gazeTracks;
        public List<Vector2> mouseTracks;
        public List<FixationInfo> gazeFixations;
        public List<FixationInfo> mouseFixations;
        public TrackData() {
            gazeTracks = new List<Vector2>();
            mouseTracks = new List<Vector2>();
            gazeFixations = new List<FixationInfo>();
            mouseFixations = new List<FixationInfo>();
        }
    }
    public class TrackInfo
    {
        public int time;
        public int id;
        public Vector2 scrollTop;
        public Vector2 gazeTrack;
        public Vector2 mouseTrack;
        public System.Windows.Media.Media3D.Point4D browserSize;
        public TrackInfo()
        {
            time = -1;
            id = -1;
            scrollTop = new Vector2(0, 0);
            gazeTrack = new Vector2(-1, -1);
            mouseTrack = new Vector2(-1, -1);
            browserSize = new System.Windows.Media.Media3D.Point4D(0, 0, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        }
    }
    public struct FixationInfo {
        public int fid;
        public int fsize;
        public Vector2 fixation;
    }
    public struct URLInfo {
        public int frame;
        public int id;
        public string keyword;
        public string url;
        public int size;
    }
    
}
