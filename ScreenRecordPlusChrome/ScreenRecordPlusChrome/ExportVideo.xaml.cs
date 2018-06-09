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

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

using Accord.Video;
using Accord.Video.FFMPEG;

using System.IO;
using System.Drawing;
using System.Data.SQLite;
using System.ComponentModel;
namespace ScreenRecordPlusChrome
{

    public partial class ExportVideo : Window
    {
        private BackgroundWorker _bgWorker = new BackgroundWorker();
        private ExportVideoInfo _MexportVideoInfo;
        private ExportVideoInfo _GexportVideoInfo;
        private VideoCodec _videoCodec;

        private string _mainDir = "";
        private string _projectName = "";
        private string _subjectName = "";
        private string _saveFolder = "";
        private string _sourceVideoPath = "";
        private bool _closePending = false;

        public ExportVideo(string mainDir, string projectName, string videoPath, string subjectName, ExportVideoInfo MexportVideoInfo, ExportVideoInfo GexportVideoInfo)
        {
            InitializeComponent();

            InitComponent(mainDir, projectName, videoPath, subjectName, MexportVideoInfo, GexportVideoInfo);
        }
        private void InitComponent(string mainDir, string projectName, string videoPath, string subjectName, ExportVideoInfo MexportVideoInfo, ExportVideoInfo GexportVideoInfo)
        {
            cb_exportVideo_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec));
            cb_exportVideo_codec.SelectedIndex = 0;
            pb_exportVideo_processbar.Value = 0;
            tooltip_exportVideo_processbar.Text = "0";
            bt_cancelExportVideo_export.IsEnabled = false;
            if (File.Exists(videoPath))
            {
                //Set BackgroundWorker
                _bgWorker.WorkerReportsProgress = true;
                _bgWorker.WorkerSupportsCancellation = true;
                _bgWorker.DoWork += bgWorker_DoWork;
                _bgWorker.ProgressChanged += bgWorker_ProgressChanged;
                _bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;

                //Set Variables
                _mainDir = mainDir;
                _projectName = projectName;
                _sourceVideoPath = videoPath;
                _subjectName = subjectName;
                _MexportVideoInfo = MexportVideoInfo;
                _GexportVideoInfo = GexportVideoInfo;
                _saveFolder = _mainDir + @"\" + _projectName + @"\ProcessVideo";
                tb_exportVideo_savefolder.Text = _saveFolder;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Can't find the video!", "ERROR");
                this.Close();
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_bgWorker.IsBusy) {
                _closePending = true;
            }
        }

        #region Button
        private void bt_exportVideo_browser_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _saveFolder = fbd.SelectedPath;
                tb_exportVideo_savefolder.Text = _saveFolder;
            }
            fbd.Dispose();
        }
        private void bt_exportVideo_export_Click(object sender, RoutedEventArgs e)
        {
            bt_exportVideo_export.IsEnabled = false;
            bt_cancelExportVideo_export.IsEnabled = true;
            _videoCodec = (VideoCodec)cb_exportVideo_codec.SelectedValue;
            if (!Directory.Exists(_saveFolder)) {
                Directory.CreateDirectory(_saveFolder);
            }
            if (!_bgWorker.IsBusy)
            {
                _bgWorker.RunWorkerAsync();
            }
        }
        private void bt_cancelExportVideo_export_Click(object sender, RoutedEventArgs e)
        {
            _bgWorker.CancelAsync();
        }
        #endregion

        #region BackgroundWorker
        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string destinationPath = _saveFolder + @"\" + _subjectName + "_Process.avi";

            #region ReadProcessWriteVideo
            //Read video
            VideoFileReader _videoFileReader = new VideoFileReader();
            _videoFileReader.Open(_sourceVideoPath);
            if (_videoFileReader.IsOpen)
            {
                //Get Total Frames
                int totalFrames = (int)_videoFileReader.FrameCount;

                //DB Open
                DBExport _dbExport = new DBExport(_mainDir, _projectName, _subjectName);
                _dbExport.DBConnect();
                List<int> trackStartFrameID = _dbExport.GetAllPartTrackStartFrameID();

                //Delete File
                if (File.Exists(destinationPath)) {
                    File.Delete(destinationPath);
                }

                //Intit Video Writer
                VideoFileWriter _videoFileWriter = new VideoFileWriter();
                _videoFileWriter.Open(destinationPath, _videoFileReader.Width, _videoFileReader.Height, _videoFileReader.FrameRate, (VideoCodec)_videoCodec, _videoFileReader.BitRate);

                int frameid = 0;
                while (true)
                {
                    Bitmap bitmap = _videoFileReader.ReadVideoFrame();
                    frameid++;
                    if (bitmap == null)
                        break;
                    Image<Bgra, Byte> frame = new Image<Bgra, Byte>(bitmap);

                    //Get Data
                    int startFrameID = _dbExport.CheckPartTrackStartFrameID(trackStartFrameID, frameid);
                    List<ExportTrackInfo> trackinfolist = _dbExport.GetPartTrackFromDBWithFrameID(startFrameID, frameid);
                    ExportData trackInfoScroll = _dbExport.CheckPartTrackScrollEvent(trackinfolist, _MexportVideoInfo.TrackType, _GexportVideoInfo.TrackType, _MexportVideoInfo.FixationRate, _GexportVideoInfo.FixationRate, _MexportVideoInfo.BrowserToolbarHeight, _GexportVideoInfo.BrowserToolbarHeight);

                    //Process Frames
                    #region Mouse
                    if (trackInfoScroll.mouseTracks != null)
                    {
                        if (trackInfoScroll.mouseTracks.Count >= 2 && _MexportVideoInfo.isTrackVisible)
                        {
                            //Track
                            frame.DrawPolyline(trackInfoScroll.mouseTracks.ToArray(), false, new Bgra(_MexportVideoInfo.TrackPenColor.B, _MexportVideoInfo.TrackPenColor.G, _MexportVideoInfo.TrackPenColor.R, _MexportVideoInfo.TrackPenColor.A), _MexportVideoInfo.TrackPenWidth);
                        }
                    }
                    if (_MexportVideoInfo.isFixationVisible && trackInfoScroll.mouseFixations != null)
                    {
                        if (trackInfoScroll.mouseFixations.Count > 0)
                        {
                            List<System.Drawing.Point> scanpath = new List<System.Drawing.Point>();
                            for (int i = 0; i < trackInfoScroll.mouseFixations.Count; i++)
                            {
                                //Fixation
                                frame.Draw(new CircleF(trackInfoScroll.mouseFixations[i].fixation, trackInfoScroll.mouseFixations[i].fsize), new Bgra(_MexportVideoInfo.FixationPenColor.B, _MexportVideoInfo.FixationPenColor.G, _MexportVideoInfo.FixationPenColor.R, _MexportVideoInfo.FixationPenColor.A), _MexportVideoInfo.FixationPenWidth);

                                scanpath.Add(trackInfoScroll.mouseFixations[i].fixation);
                                if (i >= 1 && _MexportVideoInfo.isPathVisible)
                                {
                                    //Path
                                    frame.DrawPolyline(scanpath.ToArray(), false, new Bgra(_MexportVideoInfo.PathPenColor.B, _MexportVideoInfo.PathPenColor.G, _MexportVideoInfo.PathPenColor.R, _MexportVideoInfo.PathPenColor.A), _MexportVideoInfo.PathPenWidth);
                                }

                                //Fixation ID
                                if (_MexportVideoInfo.isFixationIDVisible) 
                                    drawText(frame, (i + 1).ToString(), trackInfoScroll.mouseFixations[i].fixation, _MexportVideoInfo.FixationIDPenColor);
                                    
                            }
                        }
                    }
                    #endregion
                    #region Gaze
                    if (trackInfoScroll.gazeTracks != null)
                    {
                        if (trackInfoScroll.gazeTracks.Count >= 2 && _GexportVideoInfo.isTrackVisible)
                        {
                            //Track
                            frame.DrawPolyline(trackInfoScroll.gazeTracks.ToArray(), false, new Bgra(_GexportVideoInfo.TrackPenColor.B, _GexportVideoInfo.TrackPenColor.G, _GexportVideoInfo.TrackPenColor.R, _GexportVideoInfo.TrackPenColor.A), _GexportVideoInfo.TrackPenWidth);
                        }
                    }
                    if (_GexportVideoInfo.isFixationVisible && trackInfoScroll.gazeFixations != null)
                    {
                        if (trackInfoScroll.gazeFixations.Count > 0)
                        {
                            List<System.Drawing.Point> scanpath = new List<System.Drawing.Point>();
                            for (int i = 0; i < trackInfoScroll.gazeFixations.Count; i++)
                            {
                                //Fixation
                                frame.Draw(new CircleF(trackInfoScroll.gazeFixations[i].fixation, trackInfoScroll.gazeFixations[i].fsize), new Bgra(_GexportVideoInfo.FixationPenColor.B, _GexportVideoInfo.FixationPenColor.G, _GexportVideoInfo.FixationPenColor.R, _GexportVideoInfo.FixationPenColor.A), _GexportVideoInfo.FixationPenWidth);

                                scanpath.Add(trackInfoScroll.gazeFixations[i].fixation);
                                if (i >= 1 && _GexportVideoInfo.isPathVisible)
                                {
                                    //Path
                                    frame.DrawPolyline(scanpath.ToArray(), false, new Bgra(_GexportVideoInfo.PathPenColor.B, _GexportVideoInfo.PathPenColor.G, _GexportVideoInfo.PathPenColor.R, _GexportVideoInfo.PathPenColor.A), _GexportVideoInfo.PathPenWidth);
                                }

                                //Fixation ID
                                if (_GexportVideoInfo.isFixationIDVisible)
                                    drawText(frame, (i + 1).ToString(), trackInfoScroll.gazeFixations[i].fixation, _GexportVideoInfo.FixationIDPenColor);
                               
                            }
                        }
                    }
                    #endregion
                    #region Cursor
                    if (trackInfoScroll.mouseTracks != null && trackInfoScroll.mouseTracks.Count > 0 && _MexportVideoInfo.isCircleVisible)
                    {
                        //Circle
                        frame.Draw(new CircleF(trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1], 50), new Bgra(_MexportVideoInfo.CirclePenColor.B, _MexportVideoInfo.CirclePenColor.G, _MexportVideoInfo.CirclePenColor.R, _MexportVideoInfo.CirclePenColor.A), _MexportVideoInfo.CirclePenWidth);
                    }
                    if (trackInfoScroll.gazeTracks != null && trackInfoScroll.gazeTracks.Count > 0 && _GexportVideoInfo.isCircleVisible)
                    {
                        //Circle
                        frame.Draw(new CircleF(trackInfoScroll.gazeTracks[trackInfoScroll.gazeTracks.Count - 1], 50), new Bgra(_GexportVideoInfo.CirclePenColor.B, _GexportVideoInfo.CirclePenColor.G, _GexportVideoInfo.CirclePenColor.R, _GexportVideoInfo.CirclePenColor.A), _GexportVideoInfo.CirclePenWidth);
                    }
                    if (trackInfoScroll.mouseTracks != null && trackInfoScroll.mouseTracks.Count > 0 && _MexportVideoInfo.isCursorVisible)
                    {
                        System.Drawing.Point[] p = new System.Drawing.Point[7];
                        System.Drawing.Point[] p2 = new System.Drawing.Point[6];
                        p[0] = new System.Drawing.Point(0 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 0 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p[1] = new System.Drawing.Point(0 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 23 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p[2] = new System.Drawing.Point(5 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 19 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p[3] = new System.Drawing.Point(8 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 26 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p[4] = new System.Drawing.Point(13 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 24 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p[5] = new System.Drawing.Point(10 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 18 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p[6] = new System.Drawing.Point(17 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 18 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);

                        p2[0] = new System.Drawing.Point(3 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 7 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p2[1] = new System.Drawing.Point(2 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 18 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p2[2] = new System.Drawing.Point(6 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 15 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p2[3] = new System.Drawing.Point(10 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 23 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p2[4] = new System.Drawing.Point(6 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 15 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);
                        p2[5] = new System.Drawing.Point(12 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].X, 16 + trackInfoScroll.mouseTracks[trackInfoScroll.mouseTracks.Count - 1].Y);

                        frame.DrawPolyline(p2, true, new Bgra(255, 255, 255, 255), 3);
                        frame.DrawPolyline(p, true, new Bgra(0, 0, 0, 255), 2);
                    }
                    if (trackInfoScroll.gazeTracks != null && trackInfoScroll.gazeTracks.Count > 0 && _GexportVideoInfo.isCursorVisible)
                    {
                        frame.Draw(new CircleF(trackInfoScroll.gazeTracks[trackInfoScroll.gazeTracks.Count - 1], 12), new Bgra(255, 255, 255, 255), -1);
                        frame.Draw(new CircleF(trackInfoScroll.gazeTracks[trackInfoScroll.gazeTracks.Count - 1], 12), new Bgra(0, 0, 0, 255), 2);
                        frame.Draw(new CircleF(trackInfoScroll.gazeTracks[trackInfoScroll.gazeTracks.Count - 1], 5), new Bgra(0, 0, 0, 255), -1);
                    }
                    #endregion

                    //Write Video
                    _videoFileWriter.WriteVideoFrame(frame.ToBitmap());

                    //clear
                    bitmap.Dispose();
                    bitmap = null;
                    frame.Dispose();
                    frame = null;

                    //Updata Processbar
                    if (worker.CancellationPending || _closePending)
                    {
                        //clear
                        if (_videoFileWriter != null)
                        {
                            _videoFileWriter.Close();
                            _videoFileWriter.Dispose();
                            _videoFileWriter = null;
                        }
                        if (_videoFileReader != null)
                        {
                            _videoFileReader.Close();
                            _videoFileReader.Dispose();
                            _videoFileReader = null;
                        }
                        if (_dbExport != null)
                        {
                            _dbExport.DBDisconnect();
                            _dbExport = null;
                        }
                        if (bitmap != null) {
                            bitmap.Dispose();
                            bitmap = null;
                        }
                        if (frame != null) {
                            frame.Dispose();
                            frame = null;
                        }

                        e.Cancel = true;
                        break;
                    }
                    else
                    {
                        worker.ReportProgress(100*frameid/totalFrames);
                        //System.Threading.Thread.Sleep(10);
                    }
                }
                //clear
                if (_videoFileWriter != null)
                {
                    _videoFileWriter.Close();
                    _videoFileWriter.Dispose();
                    _videoFileWriter = null;
                }
                if (_videoFileReader != null)
                {
                    _videoFileReader.Close();
                    _videoFileReader.Dispose();
                    _videoFileReader = null;
                }
                if (_dbExport != null)
                {
                    _dbExport.DBDisconnect();
                    _dbExport = null;
                }

            }
            #endregion

        }
       
        void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pb_exportVideo_processbar.Value = e.ProgressPercentage;
            tooltip_exportVideo_processbar.Text = e.ProgressPercentage.ToString();
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bt_exportVideo_export.IsEnabled = true;
            bt_cancelExportVideo_export.IsEnabled = false;
            if (e.Cancelled)
            {
                MessageBox.Show("Export task has been canceled.", "INFO");
            }
            else if (e.Error != null) {
                MessageBox.Show("Export task error! " + e.Error.ToString(),"ERROR");
            }
            else
            {
                MessageBox.Show("Export task finished!", "INFO");
            }
            pb_exportVideo_processbar.Value = 0;
            tooltip_exportVideo_processbar.Text = "0";
        }
        #endregion

        private void drawText(Image<Bgra, Byte> img, string text, System.Drawing.Point pos, System.Drawing.Color color)
        {
            Graphics g = Graphics.FromImage(img.Bitmap);
            Font font = new Font("Tahoma", 30, System.Drawing.FontStyle.Bold);
            g.DrawString(text, font, new System.Drawing.SolidBrush(color), pos);
            g.Dispose();
            font.Dispose();
        }



    }
    public class DBExport : DBBase
    {
        private const int BrowserToolbarHeight = 30;
        protected string _userName = null;

        public DBExport(string mainDir, string projectName, string userName)
            : base(mainDir, projectName)
        {
            _userName = userName;
        }

        public List<ExportTrackInfo> GetPartTrackFromDBWithFrameID(int startframeID, int endframeID)
        {
            List<ExportTrackInfo> result = new List<ExportTrackInfo>();
            if (this._sqliteConnect != null && _userName != null)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT MousePosX, MousePosY, GazePosX, GazePosY, Time, ID, ScrollTop FROM " + _userName + "Rawdata WHERE Frame>=@startframeid AND Frame<=@endframeid ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@startframeid", startframeID);
                        _sqliteCmd.Parameters.AddWithValue("@endframeid", endframeID);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            ExportTrackInfo trackinfo = new ExportTrackInfo();
                            //mouse
                            if (!rdr.IsDBNull(0) && !rdr.IsDBNull(1))
                            {
                                trackinfo.mouseTrack.X = (int)rdr.GetFloat(0);
                                trackinfo.mouseTrack.Y = (int)rdr.GetFloat(1);
                            }
                            //gaze
                            if (!rdr.IsDBNull(2) && !rdr.IsDBNull(3))
                            {
                                trackinfo.gazeTrack.X = (int)rdr.GetFloat(2);
                                trackinfo.gazeTrack.Y = (int)rdr.GetFloat(3);
                            }
                            if (!rdr.IsDBNull(4))
                            {
                                trackinfo.time = rdr.GetInt32(4);
                            }
                            if (!rdr.IsDBNull(5))
                            {
                                trackinfo.id = rdr.GetInt32(5);
                            }
                            if (!rdr.IsDBNull(6))
                            {
                                System.Windows.Point scroll = System.Windows.Point.Parse(rdr.GetString(6));
                                trackinfo.scrollTop.X = (int)scroll.X;
                                trackinfo.scrollTop.Y = (int)scroll.Y;
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

        public List<int> GetAllPartTrackStartFrameID()
        {
            List<int> result = new List<int>();

            if (this._sqliteConnect != null && _userName != null)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT Frame FROM URLEvent WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", _userName);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (rdr.GetInt32(0) == 1)
                            {
                                result.Add(0);
                            }
                            else
                            {
                                result.Add(rdr.GetInt32(0));
                            }
                        }
                        rdr.Close();
                        if (result.Count == 0)
                        {
                            result.Add(0);
                        }
                    }
                    tr.Commit();
                }
            }
            result.Sort((x, y) => { return x.CompareTo(y); });

            return result;
        }

        public int CheckPartTrackStartFrameID(List<int> allStartFrameID, int currentFrameID)
        {
            int result = 0;
            int lagframe = 0;
            result = allStartFrameID.FindLast(item => item <= (currentFrameID + lagframe));
            return result;
        }

        public ExportData CheckPartTrackScrollEvent(List<ExportTrackInfo> track, int mtype, int gtype, int MfixationRate, int GfixationRate, int MBrowserToolbarH, int GBrowserToolbarH)
        {
            ExportData result = CheckFixation(track, MfixationRate, GfixationRate, MBrowserToolbarH, GBrowserToolbarH);
            if (track.Count != 0)
            {
                //Track move when scrolling
                if (mtype == 0 || gtype == 0)
                {
                    float lastx = track[track.Count - 1].scrollTop.X;
                    float lasty = track[track.Count - 1].scrollTop.Y;
                    for (int i = 0; i < track.Count; i++)
                    {
                        if (mtype == 0 && track[i].mouseTrack.X != -1 && track[i].mouseTrack.Y != -1)
                        {
                            System.Drawing.Point m = new System.Drawing.Point((int)(track[i].mouseTrack.X + (track[i].scrollTop.X - lastx)), (int)(track[i].mouseTrack.Y + (track[i].scrollTop.Y - lasty)));
                            result.mouseTracks.Add(m);
                        }
                        if (gtype == 0 && track[i].gazeTrack.X != -1 && track[i].gazeTrack.Y != -1 && track[i].gazeTrack.X != 0 && track[i].gazeTrack.Y != 0)
                        {
                            System.Drawing.Point g = new System.Drawing.Point((int)(track[i].gazeTrack.X + (track[i].scrollTop.X - lastx)), (int)(track[i].gazeTrack.Y + (track[i].scrollTop.Y - lasty)));
                            result.gazeTracks.Add(g);
                        }
                    }
                }
                //Track disappear when scrolling
                if (mtype == 1 || gtype == 1)
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
                    for (int i = startid; i < track.Count; i++)
                    {
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

        private ExportData CheckFixation(List<ExportTrackInfo> track, int MfixationRate, int GfixationRate, int MBrowserToolbarH, int GBrowserToolbarH)
        {
            ExportData result = new ExportData();
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
                                ExportFixationInfo finfo = new ExportFixationInfo();
                                finfo.fid = rdr.GetInt32(0);
                                if (rdr.GetFloat(2) > MBrowserToolbarH)
                                    finfo.fixation = new System.Drawing.Point((int)(rdr.GetFloat(1) + (rdr.GetFloat(3) - lastx)), (int)(rdr.GetFloat(2) + (rdr.GetFloat(4) - lasty)));
                                else
                                    finfo.fixation = new System.Drawing.Point((int)rdr.GetFloat(1), (int)rdr.GetFloat(2));
                                if (rdr.GetInt32(5) <= endid)
                                {
                                    finfo.fsize = (rdr.GetInt32(7)) / ((10-MfixationRate)*10);
                                }
                                else
                                {
                                    finfo.fsize = (lasttime - rdr.GetInt32(6)) / ((10-MfixationRate)*10);
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
                                ExportFixationInfo finfo = new ExportFixationInfo();
                                finfo.fid = rdr.GetInt32(0);
                                if (rdr.GetFloat(2) > GBrowserToolbarH)
                                    finfo.fixation = new System.Drawing.Point((int)(rdr.GetFloat(1) + (rdr.GetFloat(3) - lastx)), (int)(rdr.GetFloat(2) + (rdr.GetFloat(4) - lasty)));
                                else
                                    finfo.fixation = new System.Drawing.Point((int)rdr.GetFloat(1), (int)rdr.GetFloat(2));
                                if (rdr.GetInt32(5) <= endid)
                                {
                                    finfo.fsize = (rdr.GetInt32(7)) / ((10-GfixationRate)*10);
                                }
                                else
                                {
                                    finfo.fsize = (lasttime - rdr.GetInt32(6)) / ((10-GfixationRate)*10);
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
    public class ExportData
    {
        public List<System.Drawing.Point> gazeTracks;
        public List<System.Drawing.Point> mouseTracks;
        public List<ExportFixationInfo> gazeFixations;
        public List<ExportFixationInfo> mouseFixations;
        public ExportData()
        {
            gazeTracks = new List<System.Drawing.Point>();
            mouseTracks = new List<System.Drawing.Point>();
            gazeFixations = new List<ExportFixationInfo>();
            mouseFixations = new List<ExportFixationInfo>();
        }
    }
    public class ExportTrackInfo
    {
        public int time;
        public int id;
        public System.Drawing.Point scrollTop;
        public System.Drawing.Point gazeTrack;
        public System.Drawing.Point mouseTrack;
        public ExportTrackInfo()
        {
            time = -1;
            id = -1;
            scrollTop = new System.Drawing.Point(0, 0);
            gazeTrack = new System.Drawing.Point(-1, -1);
            mouseTrack = new System.Drawing.Point(-1, -1);
        }
    }
    public struct ExportFixationInfo
    {
        public int fid;
        public int fsize;
        public System.Drawing.Point fixation;
    }
    public struct ExportVideoInfo
    {
        public bool isTrackVisible;
        public bool isCursorVisible;
        public bool isCircleVisible;
        public bool isFixationVisible;
        public bool isPathVisible;
        public bool isFixationIDVisible;
        public System.Drawing.Color TrackPenColor;
        public System.Drawing.Color CirclePenColor;
        public System.Drawing.Color FixationPenColor;
        public System.Drawing.Color PathPenColor;
        public System.Drawing.Color FixationIDPenColor;
        public int TrackPenWidth;
        public int CirclePenWidth;
        public int FixationPenWidth;
        public int PathPenWidth;
        public int TrackType;
        public int FixationRate;
        public int BrowserToolbarHeight;
    }
}
