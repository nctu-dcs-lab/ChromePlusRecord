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

using System.Data.SQLite;
using System.IO;

namespace ScreenRecordPlusChrome
{
    public partial class Statistics : Window
    {
        #region Variable
        private string _mainDir = null;
        private string _projectName = null;
        private SQLiteConnection _sqliteConnect = null;
        private StatisticsEntireSubject _statisticsEntireSubject = null;
        #endregion

        public Statistics(string mainDir, string projectName)
        {
            InitializeComponent();

            InitComponent(mainDir, projectName);
        }
        private void InitComponent(string mainDir, string projectName)
        {
            if (mainDir != null && projectName != null)
            {
                this._mainDir = mainDir;
                this._projectName = projectName;
                this.Title = "Statistics - " + this._projectName;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Can't find database!", "ERROR");
                this.Close();
            }
        }
        private void CloseInterfaces()
        {
            DBDisconnect();
            if (_statisticsEntireSubject != null)
            {
                _statisticsEntireSubject.Close();
                _statisticsEntireSubject = null;
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            g_statistics_bottom.Height = this.ActualHeight - g_statistics_top.ActualHeight - 45;
            dg_statistics_datagrid.Width = this.ActualWidth - 25;
            tc_statistics_main.Width = this.ActualWidth - 25;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DBConnect();
            this.clb_statistics_subjectname.ItemsSource = GetAllSubjectName();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            CloseInterfaces();
        }

        #region Button
        private void bt_statistics_calculate_Click(object sender, RoutedEventArgs e)
        {
            if (clb_statistics_subjectname.SelectedItems.Count <= 0)
            {
                System.Windows.Forms.MessageBox.Show("Please choose subjects!", "ERROR");
            }
            else
            {
                ShowColumns();
                foreach (var username in clb_statistics_subjectname.SelectedItems)
                {
                    CalculateWithDifferentURL(username.ToString());
                }
            }
        }
        private void bt_statistics_calculatefixation_Click(object sender, RoutedEventArgs e)
        {
            if (clb_statistics_subjectname.SelectedItems.Count <= 0)
            {
                System.Windows.Forms.MessageBox.Show("Please choose subjects!", "ERROR");
            }
            else
            { 
                string dbPath = _mainDir + @"\" + _projectName + @"\Database\" + _projectName;
                new FixationSetting(dbPath, clb_statistics_subjectname.SelectedItems.OfType<string>().ToList()).Show();
            }
        }
        private void bt_statistics_subject_analysis_Click(object sender, RoutedEventArgs e)
        {
            if (clb_statistics_subjectname.SelectedItems.Count > 0)
            {
                if (_statisticsEntireSubject != null)
                {
                    _statisticsEntireSubject.Close();
                    _statisticsEntireSubject = null;
                }
                _statisticsEntireSubject = new StatisticsEntireSubject(this._projectName);
                _statisticsEntireSubject.Show();
                CalculateEntireSubject(clb_statistics_subjectname.SelectedItems.OfType<string>().ToList());
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please choose subjects!","ERROR");
            }
        }
        private void bt_statistics_export_Click(object sender, RoutedEventArgs e)
        {
            if (clb_statistics_subjectname.SelectedItems.Count > 0)
            {
                new ExportStatistics(_mainDir, _projectName, clb_statistics_subjectname.SelectedItems.OfType<string>().ToList()).Show();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please choose subjects!","ERROR");
            }
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
                catch { }
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
        private List<string> GetAllSubjectName()
        {
            List<string> result = new List<string>();
            if (this._sqliteConnect != null)
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = @"SELECT SubjectName FROM Statistics";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        result.Add(rdr.GetString(0));
                    }
                    rdr.Close();
                }
            }
            return result;
        }
        private bool CheckGazeCheckBoxClick()
        {
            return ((bool)cb_statistics_gaze_fixation1.IsChecked || (bool)cb_statistics_gaze_fixation2.IsChecked || (bool)cb_statistics_gaze_fixation3.IsChecked || (bool)cb_statistics_gaze_fixation4.IsChecked || (bool)cb_statistics_gaze_fixation5.IsChecked || (bool)cb_statistics_gaze_fixation6.IsChecked
                || (bool)cb_statistics_gaze_saccade1.IsChecked || (bool)cb_statistics_gaze_saccade2.IsChecked || (bool)cb_statistics_gaze_saccade3.IsChecked
                || (bool)cb_statistics_gaze_path1.IsChecked || (bool)cb_statistics_gaze_path2.IsChecked
                || (bool)cb_statistics_gaze_interaction1.IsChecked || (bool)cb_statistics_gaze_interaction2.IsChecked);
        }
        private bool CheckMouseCheckBoxClick()
        {
            return ((bool)cb_statistics_mouse_fixation1.IsChecked || (bool)cb_statistics_mouse_fixation2.IsChecked || (bool)cb_statistics_mouse_fixation3.IsChecked || (bool)cb_statistics_mouse_fixation4.IsChecked || (bool)cb_statistics_mouse_fixation5.IsChecked || (bool)cb_statistics_mouse_fixation6.IsChecked
                || (bool)cb_statistics_mouse_saccade1.IsChecked || (bool)cb_statistics_mouse_saccade2.IsChecked || (bool)cb_statistics_mouse_saccade3.IsChecked
                || (bool)cb_statistics_mouse_path1.IsChecked || (bool)cb_statistics_mouse_path2.IsChecked
                || (bool)cb_statistics_mouse_clicks_1.IsChecked || (bool)cb_statistics_mouse_clicks_2.IsChecked || (bool)cb_statistics_mouse_clicks_3.IsChecked || (bool)cb_statistics_mouse_clicks_4.IsChecked);
        }
        private void ShowColumns()
        {
            foreach (var col in dg_statistics_datagrid.Columns)
            {
                col.Visibility = System.Windows.Visibility.Hidden;
            }
            dgtc_statistics_subject.Visibility = System.Windows.Visibility.Visible;
            dgtc_statistics_id.Visibility = System.Windows.Visibility.Visible;
            dgtc_statistics_webpage.Visibility = System.Windows.Visibility.Visible;
            dgtc_statistics_keyword.Visibility = System.Windows.Visibility.Visible;
            //gaze column
            if ((bool)cb_statistics_gaze_fixation1.IsChecked)
                dgtc_statistics_gaze_fixation1.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_fixation2.IsChecked)
                dgtc_statistics_gaze_fixation2.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_fixation3.IsChecked)
                dgtc_statistics_gaze_fixation3.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_fixation4.IsChecked)
                dgtc_statistics_gaze_fixation4.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_fixation5.IsChecked)
                dgtc_statistics_gaze_fixation5.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_fixation6.IsChecked)
                dgtc_statistics_gaze_fixation6.Visibility = System.Windows.Visibility.Visible;

            if ((bool)cb_statistics_gaze_saccade1.IsChecked)
                dgtc_statistics_gaze_saccade1.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_saccade2.IsChecked)
                dgtc_statistics_gaze_saccade2.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_saccade3.IsChecked)
                dgtc_statistics_gaze_saccade3.Visibility = System.Windows.Visibility.Visible;

            if ((bool)cb_statistics_gaze_path1.IsChecked)
                dgtc_statistics_gaze_path1.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_path2.IsChecked)
                dgtc_statistics_gaze_path2.Visibility = System.Windows.Visibility.Visible;

            if ((bool)cb_statistics_gaze_interaction1.IsChecked)
                dgtc_statistics_gaze_interaction1.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_gaze_interaction2.IsChecked)
                dgtc_statistics_gaze_interaction2.Visibility = System.Windows.Visibility.Visible;

            //mouse column
            if ((bool)cb_statistics_mouse_fixation1.IsChecked)
                dgtc_statistics_mouse_fixation1.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_fixation2.IsChecked)
                dgtc_statistics_mouse_fixation2.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_fixation3.IsChecked)
                dgtc_statistics_mouse_fixation3.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_fixation4.IsChecked)
                dgtc_statistics_mouse_fixation4.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_fixation5.IsChecked)
                dgtc_statistics_mouse_fixation5.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_fixation6.IsChecked)
                dgtc_statistics_mouse_fixation6.Visibility = System.Windows.Visibility.Visible;

            if ((bool)cb_statistics_mouse_saccade1.IsChecked)
                dgtc_statistics_mouse_saccade1.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_saccade2.IsChecked)
                dgtc_statistics_mouse_saccade2.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_saccade3.IsChecked)
                dgtc_statistics_mouse_saccade3.Visibility = System.Windows.Visibility.Visible;

            if ((bool)cb_statistics_mouse_path1.IsChecked)
                dgtc_statistics_mouse_path1.Visibility = System.Windows.Visibility.Visible;
            if ((bool)cb_statistics_mouse_path2.IsChecked)
                dgtc_statistics_mouse_path2.Visibility = System.Windows.Visibility.Visible;

            //left
            if ((bool)cb_statistics_mouse_click_1.IsChecked)
            {
                if ((bool)cb_statistics_mouse_clicks_1.IsChecked)
                    dgtc_statistics_mouse_lclick1.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_2.IsChecked)
                    dgtc_statistics_mouse_lclick2.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_3.IsChecked)
                    dgtc_statistics_mouse_lclick3.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_4.IsChecked)
                    dgtc_statistics_mouse_lclick4.Visibility = System.Windows.Visibility.Visible;
            }
            //right
            if ((bool)cb_statistics_mouse_click_2.IsChecked)
            {
                if ((bool)cb_statistics_mouse_clicks_1.IsChecked)
                    dgtc_statistics_mouse_rclick1.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_2.IsChecked)
                    dgtc_statistics_mouse_rclick2.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_3.IsChecked)
                    dgtc_statistics_mouse_rclick3.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_4.IsChecked)
                    dgtc_statistics_mouse_rclick4.Visibility = System.Windows.Visibility.Visible;
            }
            //middle
            if ((bool)cb_statistics_mouse_click_3.IsChecked)
            {
                if ((bool)cb_statistics_mouse_clicks_1.IsChecked)
                    dgtc_statistics_mouse_mclick1.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_2.IsChecked)
                    dgtc_statistics_mouse_mclick2.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_3.IsChecked)
                    dgtc_statistics_mouse_mclick3.Visibility = System.Windows.Visibility.Visible;
                if ((bool)cb_statistics_mouse_clicks_4.IsChecked)
                    dgtc_statistics_mouse_mclick4.Visibility = System.Windows.Visibility.Visible;
            }

            //clear row
            dg_statistics_datagrid.Items.Clear();

        }
        private void CalculateWithDifferentURL(string subjectNmae)
        {
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
                            if (CheckGazeCheckBoxClick())
                            {
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
                            }
                            #endregion
                            //mouse
                            #region Mouse
                            if (CheckMouseCheckBoxClick())
                            {
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
                            }
                            #endregion
                            //add row
                            dg_statistics_datagrid.Items.Add(sinfo);
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
                if (CheckGazeCheckBoxClick())
                {
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
                }
                #endregion
                //mouse
                #region Mouse
                if (CheckMouseCheckBoxClick())
                {
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
                }
                #endregion
                //add row
                dg_statistics_datagrid.Items.Add(lastsinfo);
                #endregion

                tr.Commit();
            }
        }
        private float CalculateDistance(float pos1x, float pos1y, float pos2x, float pos2y)
        {
            return (float)Math.Sqrt((pos2x - pos1x) * (pos2x - pos1x) + (pos2y - pos1y) * (pos2y - pos1y));
        }
        private List<int> GetAllURLStartRID(string subjectNmae)
        {
            List<int> result = new List<int>();
            if (this._sqliteConnect != null && subjectNmae != null)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd.CommandText = @"SELECT RID FROM URLEvent WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", subjectNmae);
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                result.Add(rdr.GetInt32(0));
                        }
                        rdr.Close();
                    }
                    tr.Commit();
                }
            }
            result.Sort();

            return result;
        }
        private void CalculateEntireSubject(List<string> subjectNmae)
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                foreach (string name in subjectNmae)
                {
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
                    if (_statisticsEntireSubject != null)
                        _statisticsEntireSubject.AddRowData(result);
                }
                tr.Commit();
            }

        }

    }
    public class StatisticsInfo
    {
        public int id { get; set; }
        public string subject { get; set; }
        public string webpage { get; set; }
        public string keyword { get; set; }

        public int mfixation1 { get; set; }
        public float mfixation2 { get; set; }
        public float mfixation3 { get; set; }
        public float mfixation4 { get; set; }
        public float mfixation5 { get; set; }
        public float mfixation6 { get; set; }
        public float msaccade1 { get; set; }
        public float msaccade2 { get; set; }
        public float msaccade3 { get; set; }
        public float mpath1 { get; set; }
        public float mpath2 { get; set; }
        public float mrclick1 { get; set; }
        public float mrclick2 { get; set; }
        public float mrclick3 { get; set; }
        public float mrclick4 { get; set; }
        public float mlclick1 { get; set; }
        public float mlclick2 { get; set; }
        public float mlclick3 { get; set; }
        public float mlclick4 { get; set; }
        public float mmclick1 { get; set; }
        public float mmclick2 { get; set; }
        public float mmclick3 { get; set; }
        public float mmclick4 { get; set; }

        public int gfixation1 { get; set; }
        public float gfixation2 { get; set; }
        public float gfixation3 { get; set; }
        public float gfixation4 { get; set; }
        public float gfixation5 { get; set; }
        public float gfixation6 { get; set; }
        public float gsaccade1 { get; set; }
        public float gsaccade2 { get; set; }
        public float gsaccade3 { get; set; }
        public float gpath1 { get; set; }
        public float gpath2 { get; set; }
        public float ginteraction1 { get; set; }
        public float ginteraction2 { get; set; }
    }
    public struct FixationStatisticsInfo
    {
        public int starttime;
        public int duration;
        public float posx;
        public float posy;
        public float scrolltopx;
        public float scrolltopy;
        public int endid;
    }
}
