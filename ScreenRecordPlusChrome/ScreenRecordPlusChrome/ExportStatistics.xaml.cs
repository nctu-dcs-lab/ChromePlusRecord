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
using System.Data.SQLite;
using System.ComponentModel;
namespace ScreenRecordPlusChrome
{

    public partial class ExportStatistics : Window
    {
        private BackgroundWorker _bgWorker = new BackgroundWorker();
        private SQLiteConnection _sqliteConnect = null;
        private string _mainDir = "";
        private string _projectName = "";
        private List<string> _subjectNames = null;
        private string _saveFolder = "";
        private bool _closePending = false;
        private bool _isMerge = false;


        public ExportStatistics(string mainDir, string projectName, List<string> subjectNames)
        {
            InitializeComponent();

            InitComponent(mainDir, projectName, subjectNames);
        }
        private void InitComponent(string mainDir, string projectName, List<string> subjectNames)
        {
            if (mainDir != null && projectName != null)
            {
                bt_exportStatistics_cancel.IsEnabled = false;
                //Set Variables
                _mainDir = mainDir;
                _projectName = projectName;
                _subjectNames = subjectNames;
                _saveFolder = _mainDir + @"\" + _projectName + @"\StatisticsInfo";
                tb_exportStatistics_saveFolder.Text = _saveFolder;

                //DB 
                DBConnect();

                //Set BackgroundWorker
                _bgWorker.WorkerReportsProgress = true;
                _bgWorker.WorkerSupportsCancellation = true;
                _bgWorker.DoWork += bgWorker_DoWork;
                _bgWorker.ProgressChanged += bgWorker_ProgressChanged;
                _bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Can't find database!", "ERROR");
                this.Close();
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_bgWorker.IsBusy)
                _closePending = true;
            if (_sqliteConnect != null)
                DBDisconnect();
        }

        #region Button
        private void bt_exportStatistics_browser_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _saveFolder = fbd.SelectedPath;
                tb_exportStatistics_saveFolder.Text = _saveFolder;
            }
            fbd.Dispose();
        }
        private void cb_exportStatistics_merge_Click(object sender, RoutedEventArgs e)
        {
            _isMerge = (bool)cb_exportStatistics_merge.IsChecked;
        }
        private void bt_exportStatistics_export_Click(object sender, RoutedEventArgs e)
        {
            bt_exportStatistics_export.IsEnabled = false;
            bt_exportStatistics_cancel.IsEnabled = true;
            if (!Directory.Exists(_saveFolder))
                Directory.CreateDirectory(_saveFolder);
            if (!_bgWorker.IsBusy)
                _bgWorker.RunWorkerAsync();

        }
        private void bt_exportStatistics_cancel_Click(object sender, RoutedEventArgs e)
        {
            _bgWorker.CancelAsync();
        }
        #endregion

        #region BackgroundWorker
        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            ExportEntireSubject(worker, e, _subjectNames, _saveFolder, _isMerge);
        }

        void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pb_exportStatistics_processbar.Value = e.ProgressPercentage;
            tooltip_exportStatistics_processbar.Text = e.ProgressPercentage.ToString();
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bt_exportStatistics_export.IsEnabled = true;
            bt_exportStatistics_cancel.IsEnabled = false;
            if (e.Cancelled)
            {
                MessageBox.Show("Export task has been canceled.", "INFO");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Export task error! " + e.Error.ToString(), "ERROR");
            }
            else
            {
                MessageBox.Show("Export task finished!", "INFO");
            }
            pb_exportStatistics_processbar.Value = 0;
            tooltip_exportStatistics_processbar.Text = "0";
        }
        #endregion

        private void DBConnect()
        {
            string DBPath = _mainDir + @"\" + _projectName + @"\Database\" + _projectName;
            if (File.Exists(DBPath))
            {
                try
                {
                    _sqliteConnect = new SQLiteConnection("Data source=" + DBPath);
                    _sqliteConnect.Open();
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Can't open database!", "ERROR");
                    this.Close();
                }
            }
        }
        private void DBDisconnect()
        {
            try
            {
                _sqliteConnect.Close();
                _sqliteConnect = null;
            }
            catch (Exception e) { }
        }

        private float CalculateDistance(float pos1x, float pos1y, float pos2x, float pos2y)
        {
            return (float)Math.Sqrt((pos2x - pos1x) * (pos2x - pos1x) + (pos2y - pos1y) * (pos2y - pos1y));
        }
        private void ExportWithDifferentURL(BackgroundWorker worker, DoWorkEventArgs e, string subjectNmae, string filepath, bool merge)
        {
            if (!merge && File.Exists(filepath))
            {
                File.Delete(filepath);
            }
            StreamWriter sw = new StreamWriter(filepath, true);
            sw.Write("Subject,WebpageID,Webpage,Keyword,");
            sw.Write("Gaze:Fixation (count),Gaze:Fixation (count/s),Gaze:Fixation Duration Mean (ms),Gaze:Fixation Duration Median (ms),Gaze:Fixation Duration (ms),Gaze:Fixation Duration/URL Duration Ratio,");
            sw.Write("Gaze:Average Saccade Length (px),Gaze:Average Saccade Duration (ms),Gaze:Average Saccade Rate (px/s),");
            sw.Write("Gaze:Path Length (px),Gaze:Path Duration (ms),");
            sw.Write("Gaze:Fixations Until First Click (count),Gaze:Average Gaze Mouse Distance (px),");
            sw.Write("Mouse:Fixation (count),Mouse:Fixation (count/s),Mouse:Fixation Duration Mean (ms),Mouse:Fixation Duration Median (ms),Mouse:Fixation Duration (ms),Mouse:Fixation Duration/URL Duration Ratio,");
            sw.Write("Mouse:Average Saccade Length (px),Mouse:Average Saccade Duration (ms),Mouse:Average Saccade Rate (px/s),");
            sw.Write("Mouse:Path Length (px),Mouse:Path Duration (ms),");
            sw.Write("Mouse:L Click (count),Mouse:L Click Rate (count/s),Mouse:L FUFC (count),Mouse:L TUFC (ms),");
            sw.Write("Mouse:R Click (count),Mouse:R Click Rate (count/s),Mouse:R FUFC (count),Mouse:R TUFC (ms),");
            sw.WriteLine("Mouse:M Click (count),Mouse:M Click Rate (count/s),Mouse:M FUFC (count),Mouse:M TUFC (ms)");

            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                int urlid = 0;
                string url = "";
                string keyword = "";
                int lastEndTime = 0;
                int startRID = -1;
                int endRID = -1;
                int startTime = 0;
                int endTime = 0;

                List<FixationStatisticsInfo> gfsinfos = new List<FixationStatisticsInfo>();
                List<float> gfd = new List<float>();
                List<float> gft = new List<float>();
                List<FixationStatisticsInfo> mfsinfos = new List<FixationStatisticsInfo>();
                List<float> mfd = new List<float>();
                List<float> mft = new List<float>();
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"SELECT MAX(Time) FROM " + subjectNmae + "Rawdata";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0))
                            lastEndTime = rdr.GetInt32(0);
                    }
                    rdr.Close();
                }
                #region URL
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"SELECT URL, RID, URLEventID, StartTime, Keyword FROM URLEvent WHERE SubjectName=@subjectname ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@subjectname", subjectNmae);
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        startRID = endRID;
                        endRID = rdr.GetInt32(1);
                        startTime = endTime;
                        endTime = rdr.GetInt32(3);
                        int urlduration = endTime - startTime;
                        if (startRID != -1)
                        {
                            StatisticsInfo sinfo = new StatisticsInfo();
                            sinfo.id = urlid;
                            sinfo.subject = subjectNmae;
                            sinfo.webpage = url;
                            sinfo.keyword = keyword;
                            //gaze
                            #region Gaze
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = @"SELECT StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY, EndID FROM GazeFixation WHERE SubjectName=@subjectname AND StartID>=@startid AND EndID<@endid ORDER BY ID";
                                _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", startRID);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", endRID);
                                var rdr2 = _sqliteCmd2.ExecuteReader();
                                while (rdr2.Read())
                                {
                                    FixationStatisticsInfo fsinfo = new FixationStatisticsInfo();
                                    fsinfo.starttime = rdr2.GetInt32(0);
                                    fsinfo.duration = rdr2.GetInt32(1);
                                    fsinfo.posx = rdr2.GetFloat(2);
                                    fsinfo.posy = rdr2.GetFloat(3);
                                    fsinfo.scrolltopx = rdr2.GetFloat(4);
                                    fsinfo.scrolltopy = rdr2.GetFloat(5);
                                    fsinfo.endid = rdr2.GetInt32(6);
                                    gfsinfos.Add(fsinfo);
                                }
                                for (int i = 1; i < gfsinfos.Count; i++)
                                {
                                    gfd.Add(CalculateDistance(gfsinfos[i - 1].posx, gfsinfos[i - 1].posy, gfsinfos[i].posx, gfsinfos[i].posy));
                                    gft.Add((gfsinfos[i].starttime) - (gfsinfos[i - 1].starttime + gfsinfos[i - 1].duration));
                                }
                                rdr2.Close();
                            }
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = @"SELECT MIN(RID) FROM MKEvent WHERE SubjectName=@subjectname AND RID>=@startid AND RID<@endid AND EventType=@eventtype AND EventTask=@eventtask ORDER BY ID";
                                _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", startRID);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", endRID);
                                _sqliteCmd2.Parameters.AddWithValue("@eventtype", "Mouse");
                                _sqliteCmd2.Parameters.AddWithValue("@eventtask", "Down");
                                var rdr2 = _sqliteCmd2.ExecuteReader();
                                while (rdr2.Read())
                                {
                                    if (gfsinfos.Count > 0 && !rdr2.IsDBNull(0))
                                        sinfo.ginteraction1 = gfsinfos.Count(x => x.endid <= rdr2.GetInt32(0));
                                }
                                rdr2.Close();
                            }
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = @"SELECT AVG(SQRT((GazePosX-MousePosX)*(GazePosX-MousePosX)+(GazePosY-MousePosY)*(GazePosY-MousePosY))) FROM " + subjectNmae + "Rawdata WHERE ID>=@startid AND ID<@endid AND GazePosX>=0 AND GazePosY>=0 AND MousePosX>=0 AND MousePosY>=0 ORDER BY ID";
                                _sqliteCmd2.Parameters.AddWithValue("@startid", startRID);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", endRID);
                                var rdr2 = _sqliteCmd2.ExecuteReader();
                                while (rdr2.Read())
                                {
                                    if (!rdr2.IsDBNull(0))
                                        sinfo.ginteraction2 = rdr2.GetFloat(0);
                                }
                                rdr2.Close();
                            }
                            if (gfd.Count > 0)
                            {
                                //path
                                sinfo.gpath1 = gfd.Sum();
                                if (gfsinfos.Count > 1)
                                    sinfo.gpath2 = (gfsinfos[gfsinfos.Count - 1].starttime + gfsinfos[gfsinfos.Count - 1].duration) - gfsinfos[0].starttime;
                                //saccade
                                sinfo.gsaccade1 = gfd.Average();
                                for (int i = 0; i < gfd.Count; i++)
                                {
                                    if (gft[i] > 0)
                                        sinfo.gsaccade3 += (gfd[i] / gft[i]);
                                }
                                sinfo.gsaccade3 = sinfo.gsaccade3 * (float)1000 / (float)gfd.Count;
                            }
                            //fixation
                            if (gfsinfos.Count > 0)
                            {
                                sinfo.gfixation1 = gfsinfos.Count;
                                sinfo.gfixation3 = (float)gfsinfos.Average(x => x.duration);
                                sinfo.gfixation5 = (float)gfsinfos.Sum(x => x.duration);
                                if (urlduration != 0)
                                {
                                    sinfo.gfixation2 = (float)sinfo.gfixation1 / (float)urlduration * (float)1000;
                                    sinfo.gfixation6 = sinfo.gfixation5 / (float)urlduration;
                                }
                                gfsinfos.Sort((x, y) => { return x.duration.CompareTo(y.duration); });
                                if (gfsinfos.Count % 2 == 0)
                                    sinfo.gfixation4 = ((float)gfsinfos[gfsinfos.Count / 2].duration + (float)gfsinfos[gfsinfos.Count / 2 - 1].duration) / (float)2;
                                else
                                    sinfo.gfixation4 = (float)gfsinfos[gfsinfos.Count / 2].duration;
                            }
                            //saccade
                            if (gfd.Count > 0)
                            {
                                sinfo.gsaccade2 = (sinfo.gpath2 - sinfo.gfixation5) / (float)gfd.Count;
                            }

                            //clear
                            gfsinfos.Clear();
                            gfd.Clear();
                            gft.Clear();
                            #endregion
                            //mouse
                            #region Mouse
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = @"SELECT StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY, EndID FROM MouseFixation WHERE SubjectName=@subjectname AND StartID>=@startid AND EndID<@endid ORDER BY ID";
                                _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", startRID);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", endRID);
                                var rdr2 = _sqliteCmd2.ExecuteReader();
                                while (rdr2.Read())
                                {
                                    FixationStatisticsInfo fsinfo = new FixationStatisticsInfo();
                                    fsinfo.starttime = rdr2.GetInt32(0);
                                    fsinfo.duration = rdr2.GetInt32(1);
                                    fsinfo.posx = rdr2.GetFloat(2);
                                    fsinfo.posy = rdr2.GetFloat(3);
                                    fsinfo.scrolltopx = rdr2.GetFloat(4);
                                    fsinfo.scrolltopy = rdr2.GetFloat(5);
                                    fsinfo.endid = rdr2.GetInt32(6);
                                    mfsinfos.Add(fsinfo);
                                }
                                for (int i = 1; i < mfsinfos.Count; i++)
                                {
                                    mfd.Add(CalculateDistance(mfsinfos[i - 1].posx, mfsinfos[i - 1].posy, mfsinfos[i].posx, mfsinfos[i].posy));
                                    mft.Add((mfsinfos[i].starttime) - (mfsinfos[i - 1].starttime + mfsinfos[i - 1].duration));
                                }
                                rdr2.Close();
                            }
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = @"SELECT EventTime, EventParam, RID FROM MKEvent WHERE SubjectName=@subjectname AND RID>=@startid AND RID<@endid AND EventType=@eventtype AND EventTask=@eventtask ORDER BY ID";
                                _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", startRID);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", endRID);
                                _sqliteCmd2.Parameters.AddWithValue("@eventtype", "Mouse");
                                _sqliteCmd2.Parameters.AddWithValue("@eventtask", "Down");
                                var rdr2 = _sqliteCmd2.ExecuteReader();
                                bool setmlclick = false;
                                bool setmrclick = false;
                                bool setmmclick = false;
                                while (rdr2.Read())
                                {
                                    String ep = rdr2.GetString(1);
                                    if (ep.Contains("Left"))
                                    {
                                        sinfo.mlclick1++;
                                        if (!setmlclick)
                                        {
                                            if (mfsinfos.Count > 0)
                                                sinfo.mlclick3 = mfsinfos.Count(x => x.endid <= rdr2.GetInt32(2));
                                            sinfo.mlclick4 = rdr2.GetInt32(0) - startTime;
                                            setmlclick = true;
                                        }
                                    }
                                    else if (ep.Contains("Right"))
                                    {
                                        sinfo.mrclick1++;
                                        if (!setmrclick)
                                        {
                                            if (mfsinfos.Count > 0)
                                                sinfo.mrclick3 = mfsinfos.Count(x => x.endid <= rdr2.GetInt32(2));
                                            sinfo.mrclick4 = rdr2.GetInt32(0) - startTime;
                                            setmrclick = true;
                                        }
                                    }
                                    else if (ep.Contains("Middle"))
                                    {
                                        sinfo.mmclick1++;
                                        if (!setmmclick)
                                        {
                                            if (mfsinfos.Count > 0)
                                                sinfo.mmclick3 = mfsinfos.Count(x => x.endid <= rdr2.GetInt32(2));
                                            sinfo.mmclick4 = rdr2.GetInt32(0) - startTime;
                                            setmmclick = true;
                                        }
                                    }
                                }
                                if (urlduration != 0)
                                {
                                    sinfo.mlclick2 = sinfo.mlclick1 / (float)urlduration * (float)1000;
                                    sinfo.mrclick2 = sinfo.mrclick1 / (float)urlduration * (float)1000;
                                    sinfo.mmclick2 = sinfo.mmclick1 / (float)urlduration * (float)1000;
                                }
                                rdr2.Close();
                            }
                            if (mfd.Count > 0)
                            {
                                //path
                                sinfo.mpath1 = mfd.Sum();
                                if (mfsinfos.Count > 1)
                                    sinfo.mpath2 = (mfsinfos[mfsinfos.Count - 1].starttime + mfsinfos[mfsinfos.Count - 1].duration) - mfsinfos[0].starttime;
                                //saccade
                                sinfo.msaccade1 = mfd.Average();
                                for (int i = 0; i < mfd.Count; i++)
                                {
                                    if (mft[i] > 0)
                                        sinfo.msaccade3 += (mfd[i] / mft[i]);
                                }
                                sinfo.msaccade3 = sinfo.msaccade3 * (float)1000 / (float)mfd.Count;
                            }
                            //fixation
                            if (mfsinfos.Count > 0)
                            {
                                sinfo.mfixation1 = mfsinfos.Count;
                                sinfo.mfixation3 = (float)mfsinfos.Average(x => x.duration);
                                sinfo.mfixation5 = (float)mfsinfos.Sum(x => x.duration);
                                if (urlduration != 0)
                                {
                                    sinfo.mfixation2 = (float)sinfo.mfixation1 / (float)urlduration * (float)1000;
                                    sinfo.mfixation6 = sinfo.mfixation5 / (float)urlduration;
                                }
                                mfsinfos.Sort((x, y) => { return x.duration.CompareTo(y.duration); });
                                if (mfsinfos.Count % 2 == 0)
                                    sinfo.mfixation4 = ((float)mfsinfos[mfsinfos.Count / 2].duration + (float)mfsinfos[mfsinfos.Count / 2 - 1].duration) / (float)2;
                                else
                                    sinfo.mfixation4 = (float)mfsinfos[mfsinfos.Count / 2].duration;
                            }
                            //saccade
                            if (mfd.Count > 0)
                            {
                                sinfo.msaccade2 = (sinfo.mpath2 - sinfo.mfixation5) / (float)mfd.Count;
                            }

                            //clear
                            mfsinfos.Clear();
                            mfd.Clear();
                            mft.Clear();
                            #endregion
                            //write row sinfo
                            if (sw != null)
                            {
                                sw.Write(sinfo.subject + "," + sinfo.id + "," + sinfo.webpage + "," + sinfo.keyword + ",");
                                sw.Write(sinfo.gfixation1 + "," + sinfo.gfixation2 + "," + sinfo.gfixation3 + "," + sinfo.gfixation4 + "," + sinfo.gfixation5 + "," + sinfo.gfixation6 + ",");
                                sw.Write(sinfo.gsaccade1 + "," + sinfo.gsaccade2 + "," + sinfo.gsaccade3 + ",");
                                sw.Write(sinfo.gpath1 + "," + sinfo.gpath2 + ",");
                                sw.Write(sinfo.ginteraction1 + "," + sinfo.ginteraction2 + ",");
                                sw.Write(sinfo.mfixation1 + "," + sinfo.mfixation2 + "," + sinfo.mfixation3 + "," + sinfo.mfixation4 + "," + sinfo.mfixation5 + "," + sinfo.mfixation6 + ",");
                                sw.Write(sinfo.msaccade1 + "," + sinfo.msaccade2 + "," + sinfo.msaccade3 + ",");
                                sw.Write(sinfo.mpath1 + "," + sinfo.mpath2 + ",");
                                sw.Write(sinfo.mlclick1 + "," + sinfo.mlclick2 + "," + sinfo.mlclick3 + "," + sinfo.mlclick4 + ",");
                                sw.Write(sinfo.mrclick1 + "," + sinfo.mrclick2 + "," + sinfo.mrclick3 + "," + sinfo.mrclick4 + ",");
                                sw.WriteLine(sinfo.mmclick1 + "," + sinfo.mmclick2 + "," + sinfo.mmclick3 + "," + sinfo.mmclick4);
                            }

                            //Check cancel
                            if (worker.CancellationPending || _closePending)
                            {
                                //clear
                                if (sw != null)
                                {
                                    sw.Close();
                                    sw = null;
                                }
                                rdr.Close();

                                e.Cancel = true;
                                return;
                            }
                        }
                        urlid = rdr.GetInt32(2);
                        url = rdr.GetString(0);
                        if (!rdr.IsDBNull(4))
                            keyword = rdr.GetString(4);
                        else
                            keyword = "";
                    }
                    rdr.Close();
                }
                #endregion

                #region Last URL
                int lasturlduration = lastEndTime - endTime;
                StatisticsInfo lastsinfo = new StatisticsInfo();
                lastsinfo.id = urlid;
                lastsinfo.subject = subjectNmae;
                lastsinfo.webpage = url;
                lastsinfo.keyword = keyword;
                //gaze
                #region Gaze
                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd2.CommandText = @"SELECT StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY, EndID FROM GazeFixation WHERE SubjectName=@subjectname AND StartID>=@startid ORDER BY ID";
                    _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                    _sqliteCmd2.Parameters.AddWithValue("@startid", endRID);
                    var rdr2 = _sqliteCmd2.ExecuteReader();
                    while (rdr2.Read())
                    {
                        FixationStatisticsInfo fsinfo = new FixationStatisticsInfo();
                        fsinfo.starttime = rdr2.GetInt32(0);
                        fsinfo.duration = rdr2.GetInt32(1);
                        fsinfo.posx = rdr2.GetFloat(2);
                        fsinfo.posy = rdr2.GetFloat(3);
                        fsinfo.scrolltopx = rdr2.GetFloat(4);
                        fsinfo.scrolltopy = rdr2.GetFloat(5);
                        fsinfo.endid = rdr2.GetInt32(6);
                        gfsinfos.Add(fsinfo);
                    }
                    for (int i = 1; i < gfsinfos.Count; i++)
                    {
                        gfd.Add(CalculateDistance(gfsinfos[i - 1].posx, gfsinfos[i - 1].posy, gfsinfos[i].posx, gfsinfos[i].posy));
                        gft.Add((gfsinfos[i].starttime) - (gfsinfos[i - 1].starttime + gfsinfos[i - 1].duration));
                    }
                    rdr2.Close();
                }
                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd2.CommandText = @"SELECT MIN(RID) FROM MKEvent WHERE SubjectName=@subjectname AND RID>=@startid AND EventType=@eventtype AND EventTask=@eventtask ORDER BY ID";
                    _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                    _sqliteCmd2.Parameters.AddWithValue("@startid", endRID);
                    _sqliteCmd2.Parameters.AddWithValue("@eventtype", "Mouse");
                    _sqliteCmd2.Parameters.AddWithValue("@eventtask", "Down");
                    var rdr2 = _sqliteCmd2.ExecuteReader();
                    while (rdr2.Read())
                    {
                        if (gfsinfos.Count > 0 && !rdr2.IsDBNull(0))
                            lastsinfo.ginteraction1 = gfsinfos.Count(x => x.endid <= rdr2.GetInt32(0));
                    }
                    rdr2.Close();
                }
                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd2.CommandText = @"SELECT AVG(SQRT((GazePosX-MousePosX)*(GazePosX-MousePosX)+(GazePosY-MousePosY)*(GazePosY-MousePosY))) FROM " + subjectNmae + "Rawdata WHERE ID>=@startid AND ID<@endid AND GazePosX>=0 AND GazePosY>=0 AND MousePosX>=0 AND MousePosY>=0 ORDER BY ID";
                    _sqliteCmd2.Parameters.AddWithValue("@startid", startRID);
                    _sqliteCmd2.Parameters.AddWithValue("@endid", endRID);
                    var rdr2 = _sqliteCmd2.ExecuteReader();
                    while (rdr2.Read())
                    {
                        if (!rdr2.IsDBNull(0))
                            lastsinfo.ginteraction2 = rdr2.GetFloat(0);
                    }
                    rdr2.Close();
                }
                if (gfd.Count > 0)
                {
                    //path
                    lastsinfo.gpath1 = gfd.Sum();
                    if (gfsinfos.Count > 1)
                        lastsinfo.gpath2 = (gfsinfos[gfsinfos.Count - 1].starttime + gfsinfos[gfsinfos.Count - 1].duration) - gfsinfos[0].starttime;
                    //saccade
                    lastsinfo.gsaccade1 = gfd.Average();
                    for (int i = 0; i < gfd.Count; i++)
                    {
                        if (gft[i] > 0)
                            lastsinfo.gsaccade3 += (gfd[i] / gft[i]);
                    }
                    lastsinfo.gsaccade3 = lastsinfo.gsaccade3 * (float)1000 / (float)gfd.Count;
                }
                //fixation
                if (gfsinfos.Count > 0)
                {
                    lastsinfo.gfixation1 = gfsinfos.Count;
                    lastsinfo.gfixation3 = (float)gfsinfos.Average(x => x.duration);
                    lastsinfo.gfixation5 = (float)gfsinfos.Sum(x => x.duration);
                    if (lasturlduration != 0)
                    {
                        lastsinfo.gfixation2 = (float)lastsinfo.gfixation1 / (float)lasturlduration * (float)1000;
                        lastsinfo.gfixation6 = lastsinfo.gfixation5 / (float)lasturlduration;
                    }
                    gfsinfos.Sort((x, y) => { return x.duration.CompareTo(y.duration); });
                    if (gfsinfos.Count % 2 == 0)
                        lastsinfo.gfixation4 = ((float)gfsinfos[gfsinfos.Count / 2].duration + (float)gfsinfos[gfsinfos.Count / 2 - 1].duration) / (float)2;
                    else
                        lastsinfo.gfixation4 = (float)gfsinfos[gfsinfos.Count / 2].duration;
                }
                //saccade
                if (gfd.Count > 0)
                {
                    lastsinfo.gsaccade2 = (lastsinfo.gpath2 - lastsinfo.gfixation5) / (float)gfd.Count;
                }

                //clear
                gfsinfos.Clear();
                gfd.Clear();
                gft.Clear();
                #endregion
                //mouse
                #region Mouse
                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd2.CommandText = @"SELECT StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY, EndID FROM MouseFixation WHERE SubjectName=@subjectname AND StartID>=@startid ORDER BY ID";
                    _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                    _sqliteCmd2.Parameters.AddWithValue("@startid", endRID);
                    var rdr2 = _sqliteCmd2.ExecuteReader();
                    while (rdr2.Read())
                    {
                        FixationStatisticsInfo fsinfo = new FixationStatisticsInfo();
                        fsinfo.starttime = rdr2.GetInt32(0);
                        fsinfo.duration = rdr2.GetInt32(1);
                        fsinfo.posx = rdr2.GetFloat(2);
                        fsinfo.posy = rdr2.GetFloat(3);
                        fsinfo.scrolltopx = rdr2.GetFloat(4);
                        fsinfo.scrolltopy = rdr2.GetFloat(5);
                        fsinfo.endid = rdr2.GetInt32(6);
                        mfsinfos.Add(fsinfo);
                    }
                    for (int i = 1; i < mfsinfos.Count; i++)
                    {
                        mfd.Add(CalculateDistance(mfsinfos[i - 1].posx, mfsinfos[i - 1].posy, mfsinfos[i].posx, mfsinfos[i].posy));
                        mft.Add((mfsinfos[i].starttime) - (mfsinfos[i - 1].starttime + mfsinfos[i - 1].duration));
                    }
                    rdr2.Close();
                }
                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd2.CommandText = @"SELECT EventTime, EventParam, RID FROM MKEvent WHERE SubjectName=@subjectname AND RID>=@startid AND EventType=@eventtype AND EventTask=@eventtask ORDER BY ID";
                    _sqliteCmd2.Parameters.AddWithValue("@subjectname", subjectNmae);
                    _sqliteCmd2.Parameters.AddWithValue("@startid", endRID);
                    _sqliteCmd2.Parameters.AddWithValue("@eventtype", "Mouse");
                    _sqliteCmd2.Parameters.AddWithValue("@eventtask", "Down");
                    var rdr2 = _sqliteCmd2.ExecuteReader();
                    bool setmlclick = false;
                    bool setmrclick = false;
                    bool setmmclick = false;
                    while (rdr2.Read())
                    {
                        String ep = rdr2.GetString(1);
                        if (ep.Contains("Left"))
                        {
                            lastsinfo.mlclick1++;
                            if (!setmlclick)
                            {
                                if (mfsinfos.Count > 0)
                                    lastsinfo.mlclick3 = mfsinfos.Count(x => x.endid <= rdr2.GetInt32(2));
                                lastsinfo.mlclick4 = rdr2.GetInt32(0) - endTime;
                                setmlclick = true;
                            }
                        }
                        else if (ep.Contains("Right"))
                        {
                            lastsinfo.mrclick1++;
                            if (!setmrclick)
                            {
                                if (mfsinfos.Count > 0)
                                    lastsinfo.mrclick3 = mfsinfos.Count(x => x.endid <= rdr2.GetInt32(2));
                                lastsinfo.mrclick4 = rdr2.GetInt32(0) - endTime;
                                setmrclick = true;
                            }
                        }
                        else if (ep.Contains("Middle"))
                        {
                            lastsinfo.mmclick1++;
                            if (!setmmclick)
                            {
                                if (mfsinfos.Count > 0)
                                    lastsinfo.mmclick3 = mfsinfos.Count(x => x.endid <= rdr2.GetInt32(2));
                                lastsinfo.mmclick4 = rdr2.GetInt32(0) - endTime;
                                setmmclick = true;
                            }
                        }
                    }
                    if (lasturlduration != 0)
                    {
                        lastsinfo.mlclick2 = lastsinfo.mlclick1 / (float)lasturlduration * (float)1000;
                        lastsinfo.mrclick2 = lastsinfo.mrclick1 / (float)lasturlduration * (float)1000;
                        lastsinfo.mmclick2 = lastsinfo.mmclick1 / (float)lasturlduration * (float)1000;
                    }
                    rdr2.Close();
                }
                if (mfd.Count > 0)
                {
                    //path
                    lastsinfo.mpath1 = mfd.Sum();
                    if (mfsinfos.Count > 1)
                        lastsinfo.mpath2 = (mfsinfos[mfsinfos.Count - 1].starttime + mfsinfos[mfsinfos.Count - 1].duration) - mfsinfos[0].starttime;
                    //saccade
                    lastsinfo.msaccade1 = mfd.Average();
                    for (int i = 0; i < mfd.Count; i++)
                    {
                        if (mft[i] > 0)
                            lastsinfo.msaccade3 += (mfd[i] / mft[i]);
                    }
                    lastsinfo.msaccade3 = lastsinfo.msaccade3 * (float)1000 / (float)mfd.Count;
                }
                //fixation
                if (mfsinfos.Count > 0)
                {
                    lastsinfo.mfixation1 = mfsinfos.Count;
                    lastsinfo.mfixation3 = (float)mfsinfos.Average(x => x.duration);
                    lastsinfo.mfixation5 = (float)mfsinfos.Sum(x => x.duration);
                    if (lasturlduration != 0)
                    {
                        lastsinfo.mfixation2 = (float)lastsinfo.mfixation1 / (float)lasturlduration * (float)1000;
                        lastsinfo.mfixation6 = lastsinfo.mfixation5 / (float)lasturlduration;
                    }
                    mfsinfos.Sort((x, y) => { return x.duration.CompareTo(y.duration); });
                    if (mfsinfos.Count % 2 == 0)
                        lastsinfo.mfixation4 = ((float)mfsinfos[mfsinfos.Count / 2].duration + (float)mfsinfos[mfsinfos.Count / 2 - 1].duration) / (float)2;
                    else
                        lastsinfo.mfixation4 = (float)mfsinfos[mfsinfos.Count / 2].duration;
                }
                //saccade
                if (mfd.Count > 0)
                {
                    lastsinfo.msaccade2 = (lastsinfo.mpath2 - lastsinfo.mfixation5) / (float)mfd.Count;
                }

                //clear
                mfsinfos.Clear();
                mfd.Clear();
                mft.Clear();
                #endregion
                //write row lastsinfo
                if (sw != null)
                {
                    sw.Write(lastsinfo.subject + "," + lastsinfo.id + "," + lastsinfo.webpage + "," + lastsinfo.keyword + ",");
                    sw.Write(lastsinfo.gfixation1 + "," + lastsinfo.gfixation2 + "," + lastsinfo.gfixation3 + "," + lastsinfo.gfixation4 + "," + lastsinfo.gfixation5 + "," + lastsinfo.gfixation6 + ",");
                    sw.Write(lastsinfo.gsaccade1 + "," + lastsinfo.gsaccade2 + "," + lastsinfo.gsaccade3 + ",");
                    sw.Write(lastsinfo.gpath1 + "," + lastsinfo.gpath2 + ",");
                    sw.Write(lastsinfo.ginteraction1 + "," + lastsinfo.ginteraction2 + ",");
                    sw.Write(lastsinfo.mfixation1 + "," + lastsinfo.mfixation2 + "," + lastsinfo.mfixation3 + "," + lastsinfo.mfixation4 + "," + lastsinfo.mfixation5 + "," + lastsinfo.mfixation6 + ",");
                    sw.Write(lastsinfo.msaccade1 + "," + lastsinfo.msaccade2 + "," + lastsinfo.msaccade3 + ",");
                    sw.Write(lastsinfo.mpath1 + "," + lastsinfo.mpath2 + ",");
                    sw.Write(lastsinfo.mlclick1 + "," + lastsinfo.mlclick2 + "," + lastsinfo.mlclick3 + "," + lastsinfo.mlclick4 + ",");
                    sw.Write(lastsinfo.mrclick1 + "," + lastsinfo.mrclick2 + "," + lastsinfo.mrclick3 + "," + lastsinfo.mrclick4 + ",");
                    sw.WriteLine(lastsinfo.mmclick1 + "," + lastsinfo.mmclick2 + "," + lastsinfo.mmclick3 + "," + lastsinfo.mmclick4);
                }

                #endregion

                tr.Commit();
            }
            if (sw != null)
            {
                sw.Close();
                sw = null;
            }
        }
        private void ExportEntireSubject(BackgroundWorker worker, DoWorkEventArgs e, List<string> subjectNmae, string fileroot, bool merge)
        {
            StreamWriter sw = null;
            StreamWriter sw2 = null;
            string filepath2 = fileroot + @"\EntireSubject.csv";
            if (File.Exists(filepath2))
            {
                File.Delete(filepath2);
            }
            sw2 = new StreamWriter(filepath2, true);
            sw2.WriteLine("Subject,Duration(ms),EyeDevice,Webpage(count),Webpage With Different URL(count),Gaze:Fixation(count),Gaze:Fixation Duration(ms),Mouse:Fixation(count),Mouse:Fixation Duration(ms)");

            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                int subjectid = 0;
                foreach (string name in subjectNmae)
                {
                    if (merge)
                    {
                        string filepath = fileroot + @"\" + name + ".csv";
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        sw = new StreamWriter(filepath, true);
                        sw.WriteLine("Subject,Duration(ms),Webpage(count),Webpage With Different URL(count),Gaze:Fixation(count),Gaze:Fixation Duration(ms),Mouse:Fixation(count),Mouse:Fixation Duration(ms)");
                    }
                    StatisticsEntireSubjectInfo result = new StatisticsEntireSubjectInfo();
                    result.subject = name;
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT MAX(Time) FROM " + name + "Rawdata";
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                result.duration = (float)rdr.GetInt32(0);
                        }
                        rdr.Close();
                    }
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT EyeDeviceType FROM Statistics WHERE SubjectName=@subjectname";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                result.eyedevice = rdr.GetString(0);
                        }
                        rdr.Close();
                    }
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT COUNT(*) FROM URLEvent WHERE SubjectName=@subjectname";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                result.webpage1 = rdr.GetInt32(0);
                        }
                        rdr.Close();
                    }
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT COUNT(DISTINCT URL) FROM URLEvent WHERE SubjectName=@subjectname";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                result.webpage2 = rdr.GetInt32(0);
                        }
                        rdr.Close();
                    }
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT COUNT(*), SUM(Duration) FROM GazeFixation WHERE SubjectName=@subjectname";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                result.gfixation1 = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                result.gfixation2 = rdr.GetInt32(1);
                        }
                        rdr.Close();
                    }
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT COUNT(*), SUM(Duration) FROM MouseFixation WHERE SubjectName=@subjectname";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                result.mfixation1 = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                result.mfixation2 = rdr.GetInt32(1);
                        }
                        rdr.Close();
                    }
                    //write row result
                    if (sw2 != null)
                    {
                        string str = result.subject + "," + result.duration + "," + result.eyedevice + "," + result.webpage1 + "," + result.webpage2 + "," + result.gfixation1 + "," + result.gfixation2 + "," + result.mfixation1 + "," + result.mfixation2;
                        sw2.WriteLine(str);
                    }
                    if (sw != null)
                    {
                        string str = result.subject + "," + result.duration + "," + result.eyedevice + "," + result.webpage1 + "," + result.webpage2 + "," + result.gfixation1 + "," + result.gfixation2 + "," + result.mfixation1 + "," + result.mfixation2;
                        sw.WriteLine(str);
                        sw.Close();
                        sw = null;
                    }

                    string filepathtourl = fileroot + @"\" + name + ".csv";
                    ExportWithDifferentURL(worker, e, name, filepathtourl, merge);

                    //Updata Processbar
                    subjectid++;
                    if (worker.CancellationPending || _closePending)
                    {
                        //clear
                        if (sw2 != null)
                        {
                            sw2.Close();
                            sw2 = null;
                        }

                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        worker.ReportProgress(100 * subjectid / subjectNmae.Count);
                        System.Threading.Thread.Sleep(10);
                    }
                }
                tr.Commit();
            }

            if (sw2 != null)
            {
                sw2.Close();
                sw2 = null;
            }

        }

        

        


    }
}
