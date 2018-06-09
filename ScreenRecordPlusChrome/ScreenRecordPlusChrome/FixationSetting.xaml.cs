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

    public partial class FixationSetting : Window
    {
        public enum FixationMethod
        {
            Distance_Dispersion = 0,
            Salvucci_IDT = 1,
            Centroid_Distance = 2,
        }

        #region Variables
        private BackgroundWorker _bgWorker = new BackgroundWorker();
        private bool _closePending = false;
        private int _previousProgrambar = 0;
        private int _programbarID = 0;

        private string _DBPath;
        private List<string> _subjectNames = null;
        private SQLiteConnection _sqliteConnect = null;
        private int _gazeDurationThreshold;
        private int _gazeSpatialThreshold;
        private int _mouseDurationThreshold;
        private int _mouseSpatialThreshold;
        private bool isCalculateGaze;
        private bool isCalculateMouse;
        private bool _isAbandonBrowserToolbar;
        private int _BrowserToolbarH;
        private FixationMethod _FixationMethod = FixationMethod.Distance_Dispersion;
        #endregion

        public FixationSetting(string dbPath, List<string> subjectNames)
        {
            InitializeComponent();

            InitComponent(dbPath, subjectNames);
        }
        private void InitComponent(string dbPath, List<string> subjectNames)
        {
            if (subjectNames == null || subjectNames.Count == 0)
            {
                MessageBox.Show("Please select the subject", "ERROR");
                this.Close();
            }
            else
            {
                tb_fixationSetting_processbar.Text = "0/" + subjectNames.Count;
                bt_fixationSetting_cancel.IsEnabled = false;
                List<string> algorithmName = new List<string>();
                algorithmName.Add("Distance_Dispersion");
                algorithmName.Add("Salvucci_IDT");
                algorithmName.Add("Centroid_Distance");
                cb_fixationSetting_algorithm.ItemsSource = algorithmName;
                cb_fixationSetting_algorithm.SelectedIndex = 0;
                //Set Variables
                _DBPath = dbPath;
                _subjectNames = subjectNames;

                //DB 
                DBConnect();

                //Set BackgroundWorker
                _bgWorker.WorkerReportsProgress = true;
                _bgWorker.WorkerSupportsCancellation = true;
                _bgWorker.DoWork += bgWorker_DoWork;
                _bgWorker.ProgressChanged += bgWorker_ProgressChanged;
                _bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            CloseInterface();
        }
        private void CloseInterface()
        {
            if (_bgWorker.IsBusy)
                _closePending = true;
            DBDisconnect();
        }
        private void DBConnect()
        {
            if (File.Exists(_DBPath))
            {
                try
                {
                    _sqliteConnect = new SQLiteConnection("Data source=" + _DBPath);
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
            if (_sqliteConnect != null)
            {
                try
                {
                    _sqliteConnect.Close();
                    _sqliteConnect = null;
                }
                catch (Exception e) { }
            }
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

        #region Buttin
        private void bt_fixationSetting_calculate_Click(object sender, RoutedEventArgs e)
        {
            bt_fixationSetting_calculate.IsEnabled = false;
            bt_fixationSetting_cancel.IsEnabled = true;
            _gazeDurationThreshold = (int)iud_fixationSetting_gaze_time.Value;
            _gazeSpatialThreshold = (int)iud_fixationSetting_gaze_distance.Value;
            _mouseDurationThreshold = (int)iud_fixationSetting_mouse_time.Value;
            _mouseSpatialThreshold = (int)iud_fixationSetting_mouse_distance.Value;
            isCalculateGaze = (bool)cb_fixationSetting_cal_gaze.IsChecked;
            isCalculateMouse = (bool)cb_fixationSetting_cal_mouse.IsChecked;
            _isAbandonBrowserToolbar = (bool)cb_fixationSetting_browserToolbar.IsChecked;
            _BrowserToolbarH = (int)iud_fixationSetting_browserToolbarH.Value;

            if (!_bgWorker.IsBusy)
                _bgWorker.RunWorkerAsync();
        }
        private void bt_fixationSetting_cancel_Click(object sender, RoutedEventArgs e)
        {
            _bgWorker.CancelAsync();
        }
        private void cb_fixationSetting_algorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_fixationSetting_algorithm.SelectedIndex == 0) {
                _FixationMethod = FixationMethod.Distance_Dispersion;
                tb_fixationSetting_spatialThreshold.Text = "Maximum distance from one point to the other in the fixation. (px)";
            }
            else if (cb_fixationSetting_algorithm.SelectedIndex == 1) {
                _FixationMethod = FixationMethod.Salvucci_IDT;
                tb_fixationSetting_spatialThreshold.Text = "Value of maximal horizontal distance plus the maximal vertical distance of the fixation. (px)";
            }
            else if (cb_fixationSetting_algorithm.SelectedIndex == 2) {
                _FixationMethod = FixationMethod.Centroid_Distance;
                tb_fixationSetting_spatialThreshold.Text = "Maximum distance that a point may vary from the average fixation point and still be considered part of the fixation. (px)";
            }
        }
        #endregion

        #region BackgroundWorker
        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            foreach (string username in _subjectNames)
            {
                List<int> urlstartrid = GetAllURLStartRID(username);
                if (urlstartrid != null && urlstartrid.Count > 1)
                    CalculateWithURL(worker, e, username, _FixationMethod, _gazeDurationThreshold, _gazeSpatialThreshold, _mouseDurationThreshold, _mouseSpatialThreshold, urlstartrid, isCalculateGaze, isCalculateMouse);
                else
                    Calculate(worker, e, username, _FixationMethod, _gazeDurationThreshold, _gazeSpatialThreshold, _mouseDurationThreshold, _mouseSpatialThreshold, isCalculateGaze, isCalculateMouse);
            }
        }

        void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (_previousProgrambar > e.ProgressPercentage)
            {
                _programbarID++;
                tb_fixationSetting_processbar.Text = _programbarID + "/" + _subjectNames.Count;
            }
            _previousProgrambar = e.ProgressPercentage;
            pb_fixationSetting_processbar.Value = e.ProgressPercentage;
            tooltip_fixationSetting_processbar.Text = e.ProgressPercentage.ToString();
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bt_fixationSetting_calculate.IsEnabled = true;
            bt_fixationSetting_cancel.IsEnabled = false;
            tb_fixationSetting_processbar.Text = _subjectNames.Count + "/" + _subjectNames.Count;
            if (e.Cancelled)
            {
                MessageBox.Show("Task has been canceled.", "INFO");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Task error! " + e.Error.ToString(), "ERROR");
            }
            else
            {
                MessageBox.Show("Task finished!", "INFO");
            }
            pb_fixationSetting_processbar.Value = 0;
            tooltip_fixationSetting_processbar.Text = "0";
            _previousProgrambar = 0;
            _programbarID = 0;
            tb_fixationSetting_processbar.Text = "0/" + _subjectNames.Count;
        }
        #endregion

        public void Calculate(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, FixationMethod method, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, bool calGaze, bool calMouse)
        {
            //create fixation table if it's not existed
            //remove data
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS MouseFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS GazeFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM MouseFixation WHERE SubjectName = '" + SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM GazeFixation WHERE SubjectName = '" + SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
            }

            if (method == FixationMethod.Distance_Dispersion)
            {
                Distance_Dispersion_Method(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse);
            }
            else if (method == FixationMethod.Centroid_Distance)
            {
                Centroid_Distance_Method(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse);
            }
            else if (method == FixationMethod.Salvucci_IDT)
            {
                Salvucci_IDT_Method(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse);
            }
        }
        public void CalculateWithURL(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, FixationMethod method, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, List<int> URLRID, bool calGaze, bool calMouse)
        {
            //create fixation table if it's not existed
            //remove data
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS MouseFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS GazeFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM MouseFixation WHERE SubjectName = '" + SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM GazeFixation WHERE SubjectName = '" + SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
            }

            if (method == FixationMethod.Distance_Dispersion)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    Tuple<int, int> fid = new Tuple<int, int>(1, 1);
                    for (int i = 0; i < URLRID.Count - 1; i++)
                    {
                        if (fid.Item1 != -1 && fid.Item2 != -1)
                        {
                            fid = Distance_Dispersion_Method_WithURL(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse, URLRID[i], URLRID[i + 1], fid.Item1, fid.Item2);
                        }
                    }
                    if (fid.Item1 != -1 && fid.Item2 != -1)
                    {
                        Distance_Dispersion_Method_WithURL(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse, URLRID[URLRID.Count - 1], -1, fid.Item1, fid.Item2);
                    }
                    tr.Commit();
                }
            }
            else if (method == FixationMethod.Centroid_Distance)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    Tuple<int, int> fid = new Tuple<int, int>(1, 1);
                    for (int i = 0; i < URLRID.Count - 1; i++)
                    {
                        if (fid.Item1 != -1 && fid.Item2 != -1)
                        {
                            fid = Centroid_Distance_Method_WithURL(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse, URLRID[i], URLRID[i + 1], fid.Item1, fid.Item2);
                        }
                    }
                    if (fid.Item1 != -1 && fid.Item2 != -1)
                    {
                        Centroid_Distance_Method_WithURL(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse, URLRID[URLRID.Count - 1], -1, fid.Item1, fid.Item2);
                    }
                    tr.Commit();
                }
            }
            else if (method == FixationMethod.Salvucci_IDT)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    Tuple<int, int> fid = new Tuple<int, int>(1, 1);
                    for (int i = 0; i < URLRID.Count - 1; i++)
                    {
                        if (fid.Item1 != -1 && fid.Item2 != -1)
                        {
                            fid = Salvucci_IDT_Method_WithURL(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse, URLRID[i], URLRID[i + 1], fid.Item1, fid.Item2);
                        }
                    }
                    if (fid.Item1 != -1 && fid.Item2 != -1)
                    {
                        Salvucci_IDT_Method_WithURL(worker, e, SubjectName, GDthreshold, GSthreshold, MDthreshold, MSthreshold, calGaze, calMouse, URLRID[URLRID.Count - 1], -1, fid.Item1, fid.Item2);
                    }
                    tr.Commit();
                }
            }
        }

        //Each point in that fixation must be no further than some threshold (dmax) from every other point. 
        private void Distance_Dispersion_Method(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, bool calGaze, bool calMouse)
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                //Get total row to program bar
                long totalRowForProgrambar = 1;
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = "SELECT COUNT(*) FROM " + SubjectName + "Rawdata";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0))
                            totalRowForProgrambar = rdr.GetInt32(0);
                    }
                    rdr.Close();
                }
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    long RowIDForProgrambar = 0;
                    int mfid = 1;
                    int gfid = 1;
                    List<CalculationItem> Mitems = new List<CalculationItem>();
                    List<CalculationItem> Gitems = new List<CalculationItem>();
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata ORDER BY ID";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        CalculationItem mouse = new CalculationItem();
                        CalculationItem gaze = new CalculationItem();

                        if (!rdr.IsDBNull(5))
                        {
                            System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                            mouse._scrolltop.X = s.X;
                            mouse._scrolltop.Y = s.Y;
                            gaze._scrolltop.X = s.X;
                            gaze._scrolltop.Y = s.Y;
                        }
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            mouse._id = rdr.GetInt32(6);
                            mouse._time = rdr.GetInt32(0);
                            mouse._item.X = rdr.GetFloat(1);
                            mouse._item.Y = rdr.GetFloat(2);
                        }
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            gaze._id = rdr.GetInt32(6);
                            gaze._time = rdr.GetInt32(0);
                            gaze._item.X = rdr.GetFloat(3);
                            gaze._item.Y = rdr.GetFloat(4);
                        }

                        #region Mouse
                        if (calMouse)
                        {
                            //Current point has value
                            if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                            {
                                //Candidate fixation has points
                                if (Mitems.Count != 0)
                                {
                                    int outRangeIndex = -1;
                                    for (int i = 0; i < Mitems.Count; i++)
                                    {
                                        //Web position
                                        if ((((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y)) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y))) > (MSthreshold * MSthreshold))
                                        {
                                            outRangeIndex = i;
                                        }
                                    }
                                    //Distance of current point and certain point in fixation is out of threshold
                                    if (outRangeIndex != -1)
                                    {
                                        //Time of candidate fixation is longer than threshold
                                        //Save fixation into DB, Clear candidate fixation to 0 
                                        if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                                        {
                                            double centerX = Mitems.Average(center => center._item.X);
                                            double centerY = Mitems.Average(center => center._item.Y);
                                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                            {
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    mfid++;
                                                }
                                            }
                                            Mitems.Clear();
                                        }
                                        //Remove 0 to last outRangeIndex from candidate fixation 
                                        else
                                        {
                                            Mitems.RemoveRange(0, outRangeIndex + 1);
                                        }
                                    }
                                }
                                //Add current point in fixation
                                Mitems.Add(mouse);
                            }
                            else
                            {
                                //Candidate fixation has points
                                if (Mitems.Count != 0)
                                {
                                    //Time of candidate fixation is longer than threshold
                                    //Save fixation into DB
                                    if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                                    {
                                        double centerX = Mitems.Average(center => center._item.X);
                                        double centerY = Mitems.Average(center => center._item.Y);
                                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                        {
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                mfid++;
                                            }
                                        }
                                    }
                                    //Clear candidate fixation to 0 
                                    Mitems.Clear();
                                }
                            }
                        }
                        #endregion

                        #region Gaze
                        if (calGaze)
                        {
                            //Current point has value
                            if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                            {
                                //Candidate fixation has points
                                if (Gitems.Count != 0)
                                {
                                    int outRangeIndex = -1;
                                    for (int i = 0; i < Gitems.Count; i++)
                                    {
                                        if ((((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y)) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y))) > (GSthreshold * GSthreshold))
                                        {
                                            outRangeIndex = i;
                                        }
                                    }
                                    //Distance of current point and certain point in fixation is out of threshold
                                    if (outRangeIndex != -1)
                                    {
                                        //Time of candidate fixation is longer than threshold
                                        //Save fixation into DB, Clear candidate fixation to 0 
                                        if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                                        {
                                            double centerX = Gitems.Average(center => center._item.X);
                                            double centerY = Gitems.Average(center => center._item.Y);
                                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                            {
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    gfid++;
                                                }
                                            }
                                            Gitems.Clear();
                                        }
                                        //Remove 0 to last outRangeIndex from candidate fixation 
                                        else
                                        {
                                            Gitems.RemoveRange(0, outRangeIndex + 1);
                                        }
                                    }
                                }
                                //Add current point in fixation
                                Gitems.Add(gaze);
                            }
                            else
                            {
                                //Candidate fixation has points
                                if (Gitems.Count != 0)
                                {
                                    //Time of candidate fixation is longer than threshold
                                    //Save fixation into DB, Clear candidate fixation to 0 
                                    if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                                    {
                                        double centerX = Gitems.Average(center => center._item.X);
                                        double centerY = Gitems.Average(center => center._item.Y);
                                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                        {
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                gfid++;
                                            }
                                        }
                                    }
                                    Gitems.Clear();
                                }
                            }
                        }
                        #endregion

                        //Update Program Bar
                        RowIDForProgrambar++;
                        if (worker.CancellationPending || _closePending)
                        {
                            //clear
                            rdr.Close();

                            e.Cancel = true;
                            return;
                        }
                        else
                        {
                            worker.ReportProgress((int)(100 * RowIDForProgrambar / totalRowForProgrambar));
                            //System.Threading.Thread.Sleep(10);
                        }
                    }

                    #region Last_fixation
                    //The last fixation
                    //Save fixation into DB
                    if (calMouse)
                    {
                        if (Mitems.Count != 0)
                        {
                            if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                            {
                                double centerX = Mitems.Average(center => center._item.X);
                                double centerY = Mitems.Average(center => center._item.Y);
                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                {
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        mfid++;
                                    }
                                }
                            }
                        }
                        Mitems.Clear();
                    }

                    if (calGaze)
                    {
                        if (Gitems.Count != 0)
                        {
                            if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                            {
                                double centerX = Gitems.Average(center => center._item.X);
                                double centerY = Gitems.Average(center => center._item.Y);
                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                {
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        gfid++;
                                    }
                                }
                            }
                        }
                        Gitems.Clear();
                    }
                    #endregion

                    rdr.Close();
                }
                tr.Commit();
            }
        }
        private Tuple<int, int> Distance_Dispersion_Method_WithURL(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, bool calGaze, bool calMouse, int startID, int endID, int mfid, int gfid)
        {
            //Get total row to program bar
            long totalRowForProgrambar = 1;
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                _sqliteCmd.CommandText = "SELECT COUNT(*) FROM " + SubjectName + "Rawdata";
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (!rdr.IsDBNull(0))
                        totalRowForProgrambar = rdr.GetInt32(0);
                }
                rdr.Close();
            }
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                long RowIDForProgrambar = 0;
                List<CalculationItem> Mitems = new List<CalculationItem>();
                List<CalculationItem> Gitems = new List<CalculationItem>();
                if (endID == -1)
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata WHERE ID>=@startid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                }
                else
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata WHERE ID>=@startid AND ID<@endid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                    _sqliteCmd.Parameters.AddWithValue("@endid", endID);
                }
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    CalculationItem mouse = new CalculationItem();
                    CalculationItem gaze = new CalculationItem();

                    if (!rdr.IsDBNull(5))
                    {
                        System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                        mouse._scrolltop.X = s.X;
                        mouse._scrolltop.Y = s.Y;
                        gaze._scrolltop.X = s.X;
                        gaze._scrolltop.Y = s.Y;
                    }
                    if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        mouse._id = rdr.GetInt32(6);
                        mouse._time = rdr.GetInt32(0);
                        mouse._item.X = rdr.GetFloat(1);
                        mouse._item.Y = rdr.GetFloat(2);
                    }
                    if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        gaze._id = rdr.GetInt32(6);
                        gaze._time = rdr.GetInt32(0);
                        gaze._item.X = rdr.GetFloat(3);
                        gaze._item.Y = rdr.GetFloat(4);
                    }

                    #region Mouse
                    if (calMouse)
                    {
                        //Current point has value
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            //Candidate fixation has points
                            if (Mitems.Count != 0)
                            {
                                int outRangeIndex = -1;
                                for (int i = 0; i < Mitems.Count; i++)
                                {
                                    //Web position
                                    if ((((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y)) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y))) > (MSthreshold * MSthreshold))
                                    {
                                        outRangeIndex = i;
                                    }
                                }
                                //Distance of current point and certain point in fixation is out of threshold
                                if (outRangeIndex != -1)
                                {
                                    //Time of candidate fixation is longer than threshold
                                    //Save fixation into DB, Clear candidate fixation to 0 
                                    if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                                    {
                                        double centerX = Mitems.Average(center => center._item.X);
                                        double centerY = Mitems.Average(center => center._item.Y);
                                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                        {
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                mfid++;
                                            }
                                        }
                                        Mitems.Clear();
                                    }
                                    //Remove 0 to last outRangeIndex from candidate fixation 
                                    else
                                    {
                                        Mitems.RemoveRange(0, outRangeIndex + 1);
                                    }
                                }
                            }
                            //Add current point in fixation
                            Mitems.Add(mouse);
                        }
                        else
                        {
                            //Candidate fixation has points
                            if (Mitems.Count != 0)
                            {
                                //Time of candidate fixation is longer than threshold
                                //Save fixation into DB
                                if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                                {
                                    double centerX = Mitems.Average(center => center._item.X);
                                    double centerY = Mitems.Average(center => center._item.Y);
                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                    {
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            mfid++;
                                        }
                                    }
                                }
                                //Clear candidate fixation to 0 
                                Mitems.Clear();
                            }
                        }
                    }
                    #endregion

                    #region Gaze
                    if (calGaze)
                    {
                        //Current point has value
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            //Candidate fixation has points
                            if (Gitems.Count != 0)
                            {
                                int outRangeIndex = -1;
                                for (int i = 0; i < Gitems.Count; i++)
                                {
                                    if ((((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y)) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y))) > (GSthreshold * GSthreshold))
                                    {
                                        outRangeIndex = i;
                                    }
                                }
                                //Distance of current point and certain point in fixation is out of threshold
                                if (outRangeIndex != -1)
                                {
                                    //Time of candidate fixation is longer than threshold
                                    //Save fixation into DB, Clear candidate fixation to 0 
                                    if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                                    {
                                        double centerX = Gitems.Average(center => center._item.X);
                                        double centerY = Gitems.Average(center => center._item.Y);
                                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                        {
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                gfid++;
                                            }
                                        }
                                        Gitems.Clear();
                                    }
                                    //Remove 0 to last outRangeIndex from candidate fixation 
                                    else
                                    {
                                        Gitems.RemoveRange(0, outRangeIndex + 1);
                                    }
                                }
                            }
                            //Add current point in fixation
                            Gitems.Add(gaze);
                        }
                        else
                        {
                            //Candidate fixation has points
                            if (Gitems.Count != 0)
                            {
                                //Time of candidate fixation is longer than threshold
                                //Save fixation into DB, Clear candidate fixation to 0 
                                if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                                {
                                    double centerX = Gitems.Average(center => center._item.X);
                                    double centerY = Gitems.Average(center => center._item.Y);
                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                    {
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            gfid++;
                                        }
                                    }
                                }
                                Gitems.Clear();
                            }
                        }
                    }
                    #endregion

                    //Update Program Bar
                    RowIDForProgrambar++;
                    if (worker.CancellationPending || _closePending)
                    {
                        //clear
                        rdr.Close();

                        e.Cancel = true;
                        return new Tuple<int, int>(-1, -1);
                    }
                    else
                    {
                        worker.ReportProgress((int)(100 * (startID + RowIDForProgrambar) / totalRowForProgrambar));
                        //System.Threading.Thread.Sleep(10);
                    }
                }

                #region Last_fixation
                //The last fixation
                //Save fixation into DB
                if (calMouse)
                {
                    if (Mitems.Count != 0)
                    {
                        if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                        {
                            double centerX = Mitems.Average(center => center._item.X);
                            double centerY = Mitems.Average(center => center._item.Y);
                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                            {
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    mfid++;
                                }
                            }
                        }
                    }
                    Mitems.Clear();
                }

                if (calGaze)
                {
                    if (Gitems.Count != 0)
                    {
                        if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                        {
                            double centerX = Gitems.Average(center => center._item.X);
                            double centerY = Gitems.Average(center => center._item.Y);
                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                            {
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    gfid++;
                                }
                            }
                        }
                    }
                    Gitems.Clear();
                }
                #endregion

                rdr.Close();
            }
            return new Tuple<int, int>(mfid, gfid);
        }

        //Each point has a standard deviation of distance from the centroid not exceeding some threshold
        private void Centroid_Distance_Method(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, bool calGaze, bool calMouse)
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                //Get total row to program bar
                long totalRowForProgrambar = 1;
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = "SELECT COUNT(*) FROM " + SubjectName + "Rawdata";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0))
                            totalRowForProgrambar = rdr.GetInt32(0);
                    }
                    rdr.Close();
                }
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    long RowIDForProgrambar = 0;
                    int mfid = 1;
                    int gfid = 1;
                    List<CalculationItem> Mitems = new List<CalculationItem>();
                    List<CalculationItem> Gitems = new List<CalculationItem>();
                    bool isMFixation = false;
                    bool isGFixation = false;
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata ORDER BY ID";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        CalculationItem mouse = new CalculationItem();
                        CalculationItem gaze = new CalculationItem();

                        if (!rdr.IsDBNull(5))
                        {
                            System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                            mouse._scrolltop.X = s.X;
                            mouse._scrolltop.Y = s.Y;
                            gaze._scrolltop.X = s.X;
                            gaze._scrolltop.Y = s.Y;
                        }
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            mouse._id = rdr.GetInt32(6);
                            mouse._time = rdr.GetInt32(0);
                            mouse._item.X = rdr.GetFloat(1);
                            mouse._item.Y = rdr.GetFloat(2);
                        }
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            gaze._id = rdr.GetInt32(6);
                            gaze._time = rdr.GetInt32(0);
                            gaze._item.X = rdr.GetFloat(3);
                            gaze._item.Y = rdr.GetFloat(4);
                        }

                        #region Mouse
                        if (calMouse)
                        {
                            //Current point has value
                            if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                            {
                                //Candidate fixation has points
                                if (Mitems.Count != 0)
                                {
                                    //Time of candidate fixation >= threshold
                                    if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                                    {
                                        if (!isMFixation)
                                        {
                                            //Calculate center and distances to center
                                            double Cx = Mitems.Average(center => center._item.X + center._scrolltop.X);
                                            double Cy = Mitems.Average(center => center._item.Y + center._scrolltop.Y);
                                            isMFixation = true;
                                            //int outRangeIndex = -1;
                                            for (int i = 0; i < Mitems.Count; i++)
                                            {
                                                if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) > MSthreshold * MSthreshold)
                                                {
                                                    isMFixation = false;
                                                    //outRangeIndex = i;
                                                    break;
                                                }
                                            }
                                            if (isMFixation)
                                            {
                                                //Calculate center and distances to center
                                                double cx = (Mitems.Sum(center => center._item.X + center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (double)(Mitems.Count + 1);
                                                double cy = (Mitems.Sum(center => center._item.Y + center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (double)(Mitems.Count + 1);
                                                bool expand = true;
                                                for (int i = 0; i < Mitems.Count; i++)
                                                {
                                                    if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                                    {
                                                        expand = false;
                                                        break;
                                                    }
                                                }
                                                if (expand)
                                                {
                                                    if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                                    {
                                                        expand = false;
                                                    }
                                                }
                                                if (!expand)
                                                {
                                                    //Save fixation into DB
                                                    if (isMFixation)
                                                    {
                                                        double centerX = Mitems.Average(center => center._item.X);
                                                        double centerY = Mitems.Average(center => center._item.Y);
                                                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                        {
                                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                            {
                                                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                                _sqliteCmd2.ExecuteNonQuery();
                                                                mfid++;
                                                            }
                                                        }
                                                    }
                                                    //Clear candidate fixation to 0 
                                                    Mitems.Clear();
                                                    isMFixation = false;
                                                }

                                            }
                                            else
                                            {
                                                //Mitems.RemoveRange(0, outRangeIndex + 1);
                                                Mitems.RemoveRange(0, 1);
                                            }

                                        }
                                        else
                                        {
                                            //Calculate center and distances to center
                                            double cx = (Mitems.Sum(center => center._item.X + center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (double)(Mitems.Count + 1);
                                            double cy = (Mitems.Sum(center => center._item.Y + center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (double)(Mitems.Count + 1);
                                            bool expand = true;
                                            for (int i = 0; i < Mitems.Count; i++)
                                            {
                                                if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                                {
                                                    expand = false;
                                                    break;
                                                }
                                            }
                                            if (expand)
                                            {
                                                if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                                {
                                                    expand = false;
                                                }
                                            }
                                            if (!expand)
                                            {
                                                //Save fixation into DB
                                                if (isMFixation)
                                                {
                                                    double centerX = Mitems.Average(center => center._item.X);
                                                    double centerY = Mitems.Average(center => center._item.Y);
                                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                    {
                                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                        {
                                                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                            _sqliteCmd2.ExecuteNonQuery();
                                                            mfid++;
                                                        }
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Mitems.Clear();
                                                isMFixation = false;
                                            }

                                        }
                                    }

                                }
                                //Add current point in fixation
                                Mitems.Add(mouse);

                            }
                            else
                            {
                                //IF has fixation, save it into DB
                                if (isMFixation)
                                {
                                    double centerX = Mitems.Average(center => center._item.X);
                                    double centerY = Mitems.Average(center => center._item.Y);
                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                    {
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            mfid++;
                                        }
                                    }
                                }
                                //Clear candidate fixation to 0 
                                Mitems.Clear();
                                isMFixation = false;
                            }
                        }
                        #endregion

                        #region Gaze
                        if (calGaze)
                        {
                            //Current point has value
                            if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                            {
                                //Candidate fixation has points
                                if (Gitems.Count != 0)
                                {
                                    //Time of candidate fixation >= threshold
                                    if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                                    {
                                        if (!isGFixation)
                                        {
                                            //Calculate center and distances to center
                                            double Cx = Gitems.Average(center => center._item.X + center._scrolltop.X);
                                            double Cy = Gitems.Average(center => center._item.Y + center._scrolltop.Y);
                                            isGFixation = true;
                                            //int outRangeIndex = -1;
                                            for (int i = 0; i < Gitems.Count; i++)
                                            {
                                                if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) > GSthreshold * GSthreshold)
                                                {
                                                    isGFixation = false;
                                                    //outRangeIndex = i;
                                                    break;
                                                }
                                            }
                                            if (isGFixation)
                                            {
                                                //Calculate center and distances to center
                                                double cx = (Gitems.Sum(center => center._item.X + center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                                double cy = (Gitems.Sum(center => center._item.Y + center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                                bool expand = true;
                                                for (int i = 0; i < Gitems.Count; i++)
                                                {
                                                    if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                                    {
                                                        expand = false;
                                                        break;
                                                    }
                                                }
                                                if (expand)
                                                {
                                                    if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                                    {
                                                        expand = false;
                                                    }
                                                }
                                                if (!expand)
                                                {
                                                    //Save fixation into DB
                                                    if (isGFixation)
                                                    {
                                                        double centerX = Gitems.Average(center => center._item.X);
                                                        double centerY = Gitems.Average(center => center._item.Y);
                                                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                        {
                                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                            {
                                                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                                _sqliteCmd2.ExecuteNonQuery();
                                                                gfid++;
                                                            }
                                                        }
                                                    }
                                                    //Clear candidate fixation to 0 
                                                    Gitems.Clear();
                                                    isGFixation = false;
                                                }

                                            }
                                            else
                                            {
                                                //Gitems.RemoveRange(0, outRangeIndex + 1);
                                                Gitems.RemoveRange(0, 1);
                                            }

                                        }
                                        else
                                        {
                                            //Calculate center and distances to center
                                            double cx = (Gitems.Sum(center => center._item.X + center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                            double cy = (Gitems.Sum(center => center._item.Y + center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                            bool expand = true;
                                            for (int i = 0; i < Gitems.Count; i++)
                                            {
                                                if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                                {
                                                    expand = false;
                                                    break;
                                                }
                                            }
                                            if (expand)
                                            {
                                                if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                                {
                                                    expand = false;
                                                }
                                            }
                                            if (!expand)
                                            {
                                                //Save fixation into DB
                                                if (isGFixation)
                                                {
                                                    double centerX = Gitems.Average(center => center._item.X);
                                                    double centerY = Gitems.Average(center => center._item.Y);
                                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                    {
                                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                        {
                                                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                            _sqliteCmd2.ExecuteNonQuery();
                                                            gfid++;
                                                        }
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Gitems.Clear();
                                                isGFixation = false;
                                            }

                                        }
                                    }

                                }
                                //Add current point in fixation
                                Gitems.Add(gaze);

                            }
                            else
                            {
                                //IF has fixation, save it into DB
                                if (isGFixation)
                                {
                                    double centerX = Gitems.Average(center => center._item.X);
                                    double centerY = Gitems.Average(center => center._item.Y);
                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                    {
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            gfid++;
                                        }
                                    }
                                }
                                //Clear candidate fixation to 0 
                                Gitems.Clear();
                                isGFixation = false;
                            }
                        }
                        #endregion

                        //Update Program Bar
                        RowIDForProgrambar++;
                        if (worker.CancellationPending || _closePending)
                        {
                            //clear
                            rdr.Close();

                            e.Cancel = true;
                            return;
                        }
                        else
                        {
                            worker.ReportProgress((int)(100 * RowIDForProgrambar / totalRowForProgrambar));
                            //System.Threading.Thread.Sleep(10);
                        }
                    }

                    #region Last_fixation
                    //The last fixation
                    if (calMouse)
                    {
                        //Save fixation into DB
                        if (isMFixation)
                        {
                            double centerX = Mitems.Average(center => center._item.X);
                            double centerY = Mitems.Average(center => center._item.Y);
                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                            {
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    mfid++;
                                }
                            }
                        }
                        //Clear candidate fixation to 0 
                        Mitems.Clear();
                        isMFixation = false;
                    }

                    if (calGaze)
                    {
                        //Save fixation into DB
                        if (isGFixation)
                        {
                            double centerX = Gitems.Average(center => center._item.X);
                            double centerY = Gitems.Average(center => center._item.Y);
                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                            {
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    gfid++;
                                }
                            }
                        }
                        //Clear candidate fixation to 0 
                        Gitems.Clear();
                        isGFixation = false;
                    }
                    #endregion

                    rdr.Close();
                }
                tr.Commit();
            }
        }
        private Tuple<int, int> Centroid_Distance_Method_WithURL(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, bool calGaze, bool calMouse, int startID, int endID, int mfid, int gfid)
        {
            //Get total row to program bar
            long totalRowForProgrambar = 1;
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                _sqliteCmd.CommandText = "SELECT COUNT(*) FROM " + SubjectName + "Rawdata";
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (!rdr.IsDBNull(0))
                        totalRowForProgrambar = rdr.GetInt32(0);
                }
                rdr.Close();
            }
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                long RowIDForProgrambar = 0;
                List<CalculationItem> Mitems = new List<CalculationItem>();
                List<CalculationItem> Gitems = new List<CalculationItem>();
                bool isMFixation = false;
                bool isGFixation = false;
                if (endID == -1)
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata WHERE ID>=@startid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                }
                else
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata WHERE ID>=@startid AND ID<@endid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                    _sqliteCmd.Parameters.AddWithValue("@endid", endID);
                }
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    CalculationItem mouse = new CalculationItem();
                    CalculationItem gaze = new CalculationItem();

                    if (!rdr.IsDBNull(5))
                    {
                        System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                        mouse._scrolltop.X = s.X;
                        mouse._scrolltop.Y = s.Y;
                        gaze._scrolltop.X = s.X;
                        gaze._scrolltop.Y = s.Y;
                    }
                    if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        mouse._id = rdr.GetInt32(6);
                        mouse._time = rdr.GetInt32(0);
                        mouse._item.X = rdr.GetFloat(1);
                        mouse._item.Y = rdr.GetFloat(2);
                    }
                    if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        gaze._id = rdr.GetInt32(6);
                        gaze._time = rdr.GetInt32(0);
                        gaze._item.X = rdr.GetFloat(3);
                        gaze._item.Y = rdr.GetFloat(4);
                    }

                    #region Mouse
                    if (calMouse)
                    {
                        //Current point has value
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            //Candidate fixation has points
                            if (Mitems.Count != 0)
                            {
                                //Time of candidate fixation >= threshold
                                if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold)
                                {
                                    if (!isMFixation)
                                    {
                                        //Calculate center and distances to center
                                        double Cx = Mitems.Average(center => center._item.X + center._scrolltop.X);
                                        double Cy = Mitems.Average(center => center._item.Y + center._scrolltop.Y);
                                        isMFixation = true;
                                        //int outRangeIndex = -1;
                                        for (int i = 0; i < Mitems.Count; i++)
                                        {
                                            if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) > MSthreshold * MSthreshold)
                                            {
                                                isMFixation = false;
                                                //outRangeIndex = i;
                                                break;
                                            }
                                        }
                                        if (isMFixation)
                                        {
                                            //Calculate center and distances to center
                                            double cx = (Mitems.Sum(center => center._item.X + center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (Mitems.Count + 1);
                                            double cy = (Mitems.Sum(center => center._item.Y + center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (Mitems.Count + 1);
                                            bool expand = true;
                                            for (int i = 0; i < Mitems.Count; i++)
                                            {
                                                if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                                {
                                                    expand = false;
                                                    break;
                                                }
                                            }
                                            if (expand)
                                            {
                                                if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                                {
                                                    expand = false;
                                                }
                                            }
                                            if (!expand)
                                            {
                                                //Save fixation into DB
                                                if (isMFixation)
                                                {
                                                    double centerX = Mitems.Average(center => center._item.X);
                                                    double centerY = Mitems.Average(center => center._item.Y);
                                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                    {
                                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                        {
                                                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                            _sqliteCmd2.ExecuteNonQuery();
                                                            mfid++;
                                                        }
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Mitems.Clear();
                                                isMFixation = false;
                                            }

                                        }
                                        else
                                        {
                                            //Mitems.RemoveRange(0, outRangeIndex + 1);
                                            Mitems.RemoveRange(0, 1);
                                        }

                                    }
                                    else
                                    {
                                        //Calculate center and distances to center
                                        double cx = (Mitems.Sum(center => center._item.X + center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (Mitems.Count + 1);
                                        double cy = (Mitems.Sum(center => center._item.Y + center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (Mitems.Count + 1);
                                        bool expand = true;
                                        for (int i = 0; i < Mitems.Count; i++)
                                        {
                                            if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                            {
                                                expand = false;
                                                break;
                                            }
                                        }
                                        if (expand)
                                        {
                                            if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > MSthreshold * MSthreshold)
                                            {
                                                expand = false;
                                            }
                                        }
                                        if (!expand)
                                        {
                                            //Save fixation into DB
                                            if (isMFixation)
                                            {
                                                double centerX = Mitems.Average(center => center._item.X);
                                                double centerY = Mitems.Average(center => center._item.Y);
                                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                {
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        mfid++;
                                                    }
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Mitems.Clear();
                                            isMFixation = false;
                                        }

                                    }
                                }

                            }
                            //Add current point in fixation
                            Mitems.Add(mouse);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isMFixation)
                            {
                                double centerX = Mitems.Average(center => center._item.X);
                                double centerY = Mitems.Average(center => center._item.Y);
                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                {
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        mfid++;
                                    }
                                }
                            }
                            //Clear candidate fixation to 0 
                            Mitems.Clear();
                            isMFixation = false;
                        }
                    }
                    #endregion

                    #region Gaze
                    if (calGaze)
                    {
                        //Current point has value
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            //Candidate fixation has points
                            if (Gitems.Count != 0)
                            {
                                //Time of candidate fixation >= threshold
                                if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                                {
                                    if (!isGFixation)
                                    {
                                        //Calculate center and distances to center
                                        double Cx = Gitems.Average(center => center._item.X + center._scrolltop.X);
                                        double Cy = Gitems.Average(center => center._item.Y + center._scrolltop.Y);
                                        isGFixation = true;
                                        //int outRangeIndex = -1;
                                        for (int i = 0; i < Gitems.Count; i++)
                                        {
                                            if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) > GSthreshold * GSthreshold)
                                            {
                                                isGFixation = false;
                                                //outRangeIndex = i;
                                                break;
                                            }
                                        }
                                        if (isGFixation)
                                        {
                                            //Calculate center and distances to center
                                            double cx = (Gitems.Sum(center => center._item.X + center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                            double cy = (Gitems.Sum(center => center._item.Y + center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                            bool expand = true;
                                            for (int i = 0; i < Gitems.Count; i++)
                                            {
                                                if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                                {
                                                    expand = false;
                                                    break;
                                                }
                                            }
                                            if (expand)
                                            {
                                                if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                                {
                                                    expand = false;
                                                }
                                            }
                                            if (!expand)
                                            {
                                                //Save fixation into DB
                                                if (isGFixation)
                                                {
                                                    double centerX = Gitems.Average(center => center._item.X);
                                                    double centerY = Gitems.Average(center => center._item.Y);
                                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                    {
                                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                        {
                                                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                            _sqliteCmd2.ExecuteNonQuery();
                                                            gfid++;
                                                        }
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Gitems.Clear();
                                                isGFixation = false;
                                            }

                                        }
                                        else
                                        {
                                            //Gitems.RemoveRange(0, outRangeIndex + 1);
                                            Gitems.RemoveRange(0, 1);
                                        }

                                    }
                                    else
                                    {
                                        //Calculate center and distances to center
                                        double cx = (Gitems.Sum(center => center._item.X + center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                        double cy = (Gitems.Sum(center => center._item.Y + center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                        bool expand = true;
                                        for (int i = 0; i < Gitems.Count; i++)
                                        {
                                            if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                            {
                                                expand = false;
                                                break;
                                            }
                                        }
                                        if (expand)
                                        {
                                            if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > GSthreshold * GSthreshold)
                                            {
                                                expand = false;
                                            }
                                        }
                                        if (!expand)
                                        {
                                            //Save fixation into DB
                                            if (isGFixation)
                                            {
                                                double centerX = Gitems.Average(center => center._item.X);
                                                double centerY = Gitems.Average(center => center._item.Y);
                                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                {
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        gfid++;
                                                    }
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Gitems.Clear();
                                            isGFixation = false;
                                        }

                                    }
                                }

                            }
                            //Add current point in fixation
                            Gitems.Add(gaze);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isGFixation)
                            {
                                double centerX = Gitems.Average(center => center._item.X);
                                double centerY = Gitems.Average(center => center._item.Y);
                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                {
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        gfid++;
                                    }
                                }
                            }
                            //Clear candidate fixation to 0 
                            Gitems.Clear();
                            isGFixation = false;
                        }
                    }
                    #endregion

                    //Update Program Bar
                    RowIDForProgrambar++;
                    if (worker.CancellationPending || _closePending)
                    {
                        //clear
                        rdr.Close();

                        e.Cancel = true;
                        return new Tuple<int, int>(-1, -1);
                    }
                    else
                    {
                        worker.ReportProgress((int)(100 * (startID + RowIDForProgrambar) / totalRowForProgrambar));
                        //System.Threading.Thread.Sleep(10);
                    }
                }

                #region Last_fixation
                //The last fixation
                if (calMouse)
                {
                    //Save fixation into DB
                    if (isMFixation)
                    {
                        double centerX = Mitems.Average(center => center._item.X);
                        double centerY = Mitems.Average(center => center._item.Y);
                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                        {
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                mfid++;
                            }
                        }
                    }
                    //Clear candidate fixation to 0 
                    Mitems.Clear();
                    isMFixation = false;
                }

                if (calGaze)
                {
                    //Save fixation into DB
                    if (isGFixation)
                    {
                        double centerX = Gitems.Average(center => center._item.X);
                        double centerY = Gitems.Average(center => center._item.Y);
                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                        {
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                gfid++;
                            }
                        }
                    }
                    //Clear candidate fixation to 0 
                    Gitems.Clear();
                    isGFixation = false;
                }
                #endregion

                rdr.Close();
            }
            return new Tuple<int, int>(mfid, gfid);
        }

        //The maximal horizontal distance plus the maximal vertical distance is less than some threshold
        private void Salvucci_IDT_Method(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, bool calGaze, bool calMouse)
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                //Get total row to program bar
                long totalRowForProgrambar = 1;
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    _sqliteCmd.CommandText = "SELECT COUNT(*) FROM " + SubjectName + "Rawdata";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0))
                            totalRowForProgrambar = rdr.GetInt32(0);
                    }
                    rdr.Close();
                }
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    long RowIDForProgrambar = 0;
                    int mfid = 1;
                    int gfid = 1;
                    List<CalculationItem> Mitems = new List<CalculationItem>();
                    List<CalculationItem> Gitems = new List<CalculationItem>();
                    bool isMFixation = false;
                    bool isGFixation = false;
                    System.Windows.Point MdispersionMax = new System.Windows.Point(); //{max_x, max_y}
                    System.Windows.Point MdispersionMin = new System.Windows.Point(); //{min_x, min_y}
                    System.Windows.Point GdispersionMax = new System.Windows.Point();
                    System.Windows.Point GdispersionMin = new System.Windows.Point();
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata ORDER BY ID";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        CalculationItem mouse = new CalculationItem();
                        CalculationItem gaze = new CalculationItem();

                        if (!rdr.IsDBNull(5))
                        {
                            System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                            mouse._scrolltop.X = s.X;
                            mouse._scrolltop.Y = s.Y;
                            gaze._scrolltop.X = s.X;
                            gaze._scrolltop.Y = s.Y;
                        }
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            mouse._id = rdr.GetInt32(6);
                            mouse._time = rdr.GetInt32(0);
                            mouse._item.X = rdr.GetFloat(1);
                            mouse._item.Y = rdr.GetFloat(2);
                        }
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            gaze._id = rdr.GetInt32(6);
                            gaze._time = rdr.GetInt32(0);
                            gaze._item.X = rdr.GetFloat(3);
                            gaze._item.Y = rdr.GetFloat(4);
                        }

                        #region Mouse
                        if (calMouse) {
                            //Current point has value
                            if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2)) {
                                //Candidate fixation has points
                                if (Mitems.Count != 0) {
                                    //Time of candidate fixation >= threshold
                                    if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold) {
                                        if (!isMFixation) {
                                            //Calculate dispersion
                                            MdispersionMax.X = Mitems.Max(p => p._scrolltop.X + p._item.X);
                                            MdispersionMax.Y = Mitems.Max(p => p._scrolltop.Y + p._item.Y);
                                            MdispersionMin.X = Mitems.Min(p => p._scrolltop.X + p._item.X);
                                            MdispersionMin.Y = Mitems.Min(p => p._scrolltop.Y + p._item.Y);
                                            //x_distance + y_distance <= threshold
                                            if ((MdispersionMax.X - MdispersionMin.X) + (MdispersionMax.Y - MdispersionMin.Y) <= MSthreshold)
                                                isMFixation = true;
                                            else
                                                //remove first of mitem
                                                Mitems.RemoveRange(0, 1);
                                        }
                                        if (isMFixation) {
                                            //tmp = Find dispersion between mouse and dispersion
                                            System.Windows.Point tmpMax = new System.Windows.Point(Math.Max((mouse._scrolltop.X + mouse._item.X), MdispersionMax.X), Math.Max((mouse._scrolltop.Y + mouse._item.Y), MdispersionMax.Y));
                                            System.Windows.Point tmpMin = new System.Windows.Point(Math.Min((mouse._scrolltop.X + mouse._item.X), MdispersionMin.X), Math.Min((mouse._scrolltop.Y + mouse._item.Y), MdispersionMin.Y));
                                            //x_distance + y_distance <= threshold
                                            if ((tmpMax.X - tmpMin.X) + (tmpMax.Y - tmpMin.Y) <= MSthreshold) {
                                                MdispersionMax = tmpMax;
                                                MdispersionMin = tmpMin;
                                            }
                                            else {
                                                //Save fixation into DB
                                                double centerX = Mitems.Average(center => center._item.X);
                                                double centerY = Mitems.Average(center => center._item.Y);
                                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                {
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        mfid++;
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Mitems.Clear();
                                                isMFixation = false;
                                            }

                                        }
                                    }
                                }
                                //Add current point in fixation
                                Mitems.Add(mouse);

                            }
                            else
                            {
                                if (isMFixation)
                                {
                                    //Save fixation into DB
                                    double centerX = Mitems.Average(center => center._item.X);
                                    double centerY = Mitems.Average(center => center._item.Y);
                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                    {
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            mfid++;
                                        }
                                    }
                                }
                                //Clear candidate fixation to 0 
                                Mitems.Clear();
                                isMFixation = false;
                            }
                        }
                        #endregion

                        #region Gaze
                        if (calGaze) {
                            //Current point has value
                            if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4)) {
                                //Candidate fixation has points
                                if (Gitems.Count != 0) {
                                    //Time of candidate fixation >= threshold
                                    if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold)
                                    {
                                        if (!isGFixation)
                                        {
                                            //Calculate dispersion
                                            GdispersionMax.X = Gitems.Max(p => p._scrolltop.X + p._item.X);
                                            GdispersionMax.Y = Gitems.Max(p => p._scrolltop.Y + p._item.Y);
                                            GdispersionMin.X = Gitems.Min(p => p._scrolltop.X + p._item.X);
                                            GdispersionMin.Y = Gitems.Min(p => p._scrolltop.Y + p._item.Y);
                                            //x_distance + y_distance <= threshold
                                            if ((GdispersionMax.X - GdispersionMin.X) + (GdispersionMax.Y - GdispersionMin.Y) <= GSthreshold)
                                                isGFixation = true;
                                            else
                                                //remove first of mitem
                                                Gitems.RemoveRange(0, 1);
                                        }
                                        if (isGFixation)
                                        {
                                            //tmp = Find dispersion between gaze and dispersion
                                            System.Windows.Point tmpMax = new System.Windows.Point(Math.Max((gaze._scrolltop.X + gaze._item.X), GdispersionMax.X), Math.Max((gaze._scrolltop.Y + gaze._item.Y), GdispersionMax.Y));
                                            System.Windows.Point tmpMin = new System.Windows.Point(Math.Min((gaze._scrolltop.X + gaze._item.X), GdispersionMin.X), Math.Min((gaze._scrolltop.Y + gaze._item.Y), GdispersionMin.Y));
                                            //x_distance + y_distance <= threshold
                                            if ((tmpMax.X - tmpMin.X) + (tmpMax.Y - tmpMin.Y) <= GSthreshold)
                                            {
                                                GdispersionMax = tmpMax;
                                                GdispersionMin = tmpMin;
                                            }
                                            else
                                            {
                                                //Save fixation into DB
                                                double centerX = Gitems.Average(center => center._item.X);
                                                double centerY = Gitems.Average(center => center._item.Y);
                                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                                {
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        gfid++;
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Gitems.Clear();
                                                isGFixation = false;
                                            }

                                        }
                                    }
                                }
                                //Add current point in fixation
                                Gitems.Add(gaze);

                            }
                            else
                            {
                                if (isGFixation)
                                {
                                    //Save fixation into DB
                                    double centerX = Gitems.Average(center => center._item.X);
                                    double centerY = Gitems.Average(center => center._item.Y);
                                    if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                    {
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            gfid++;
                                        }
                                    }
                                }
                                //Clear candidate fixation to 0 
                                Gitems.Clear();
                                isGFixation = false;
                            }
                        }
                        #endregion

                        //Update Program Bar
                        RowIDForProgrambar++;
                        if (worker.CancellationPending || _closePending)
                        {
                            //clear
                            rdr.Close();

                            e.Cancel = true;
                            return;
                        }
                        else
                        {
                            worker.ReportProgress((int)(100 * RowIDForProgrambar / totalRowForProgrambar));
                            //System.Threading.Thread.Sleep(10);
                        }
                    }

                    #region Last_fixation
                    //The last fixation
                    if (calMouse)
                    {
                        //Save fixation into DB
                        if (isMFixation)
                        {
                            double centerX = Mitems.Average(center => center._item.X);
                            double centerY = Mitems.Average(center => center._item.Y);
                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                            {
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    mfid++;
                                }
                            }
                        }
                        //Clear candidate fixation to 0 
                        Mitems.Clear();
                        isMFixation = false;
                    }

                    if (calGaze)
                    {
                        //Save fixation into DB
                        if (isGFixation)
                        {
                            double centerX = Gitems.Average(center => center._item.X);
                            double centerY = Gitems.Average(center => center._item.Y);
                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                            {
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    gfid++;
                                }
                            }
                        }
                        //Clear candidate fixation to 0 
                        Gitems.Clear();
                        isGFixation = false;
                    }
                    #endregion

                    rdr.Close();
                }
                tr.Commit();
            }
        }
        private Tuple<int, int> Salvucci_IDT_Method_WithURL(BackgroundWorker worker, DoWorkEventArgs e, string SubjectName, int GDthreshold, int GSthreshold, int MDthreshold, int MSthreshold, bool calGaze, bool calMouse, int startID, int endID, int mfid, int gfid)
        {
            //Get total row to program bar
            long totalRowForProgrambar = 1;
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                _sqliteCmd.CommandText = "SELECT COUNT(*) FROM " + SubjectName + "Rawdata";
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (!rdr.IsDBNull(0))
                        totalRowForProgrambar = rdr.GetInt32(0);
                }
                rdr.Close();
            }
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                long RowIDForProgrambar = 0;
                List<CalculationItem> Mitems = new List<CalculationItem>();
                List<CalculationItem> Gitems = new List<CalculationItem>();
                bool isMFixation = false;
                bool isGFixation = false;
                System.Windows.Point MdispersionMax = new System.Windows.Point(); //{max_x, max_y}
                System.Windows.Point MdispersionMin = new System.Windows.Point(); //{min_x, min_y}
                System.Windows.Point GdispersionMax = new System.Windows.Point();
                System.Windows.Point GdispersionMin = new System.Windows.Point();
                if (endID == -1)
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata WHERE ID>=@startid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                }
                else
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + SubjectName + "Rawdata WHERE ID>=@startid AND ID<@endid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                    _sqliteCmd.Parameters.AddWithValue("@endid", endID);
                }
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    CalculationItem mouse = new CalculationItem();
                    CalculationItem gaze = new CalculationItem();

                    if (!rdr.IsDBNull(5))
                    {
                        System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                        mouse._scrolltop.X = s.X;
                        mouse._scrolltop.Y = s.Y;
                        gaze._scrolltop.X = s.X;
                        gaze._scrolltop.Y = s.Y;
                    }
                    if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        mouse._id = rdr.GetInt32(6);
                        mouse._time = rdr.GetInt32(0);
                        mouse._item.X = rdr.GetFloat(1);
                        mouse._item.Y = rdr.GetFloat(2);
                    }
                    if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        gaze._id = rdr.GetInt32(6);
                        gaze._time = rdr.GetInt32(0);
                        gaze._item.X = rdr.GetFloat(3);
                        gaze._item.Y = rdr.GetFloat(4);
                    }

                    #region Mouse
                    if (calMouse) {
                        //Current point has value
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2)) {
                            //Candidate fixation has points
                            if (Mitems.Count != 0) {
                                //Time of candidate fixation >= threshold
                                if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= MDthreshold) {
                                    if (!isMFixation) {
                                        //Calculate dispersion
                                        MdispersionMax.X = Mitems.Max(p => p._scrolltop.X + p._item.X);
                                        MdispersionMax.Y = Mitems.Max(p => p._scrolltop.Y + p._item.Y);
                                        MdispersionMin.X = Mitems.Min(p => p._scrolltop.X + p._item.X);
                                        MdispersionMin.Y = Mitems.Min(p => p._scrolltop.Y + p._item.Y);
                                        //x_distance + y_distance <= threshold
                                        if ((MdispersionMax.X - MdispersionMin.X) + (MdispersionMax.Y - MdispersionMin.Y) <= MSthreshold)
                                            isMFixation = true;
                                        else
                                            //remove first of mitem
                                            Mitems.RemoveRange(0, 1);
                                    }
                                    if (isMFixation) {
                                        //tmp = Find dispersion between mouse and dispersion
                                        System.Windows.Point tmpMax = new System.Windows.Point(Math.Max((mouse._scrolltop.X + mouse._item.X), MdispersionMax.X), Math.Max((mouse._scrolltop.Y + mouse._item.Y), MdispersionMax.Y));
                                        System.Windows.Point tmpMin = new System.Windows.Point(Math.Min((mouse._scrolltop.X + mouse._item.X), MdispersionMin.X), Math.Min((mouse._scrolltop.Y + mouse._item.Y), MdispersionMin.Y));
                                        //x_distance + y_distance <= threshold
                                        if ((tmpMax.X - tmpMin.X) + (tmpMax.Y - tmpMin.Y) <= MSthreshold) {
                                            MdispersionMax = tmpMax;
                                            MdispersionMin = tmpMin;
                                        }
                                        else {
                                            //Save fixation into DB
                                            double centerX = Mitems.Average(center => center._item.X);
                                            double centerY = Mitems.Average(center => center._item.Y);
                                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                            {
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    mfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Mitems.Clear();
                                            isMFixation = false;
                                        }
                                    }
                                }
                            }
                            //Add current point in fixation
                            Mitems.Add(mouse);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isMFixation)
                            {
                                double centerX = Mitems.Average(center => center._item.X);
                                double centerY = Mitems.Average(center => center._item.Y);
                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                {
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        mfid++;
                                    }
                                }
                            }
                            //Clear candidate fixation to 0 
                            Mitems.Clear();
                            isMFixation = false;
                        }
                    }
                    #endregion

                    #region Gaze
                    if (calGaze) {
                        //Current point has value
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4)) {
                            //Candidate fixation has points
                            if (Gitems.Count != 0) {
                                //Time of candidate fixation >= threshold
                                if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= GDthreshold) {
                                    if (!isGFixation) {
                                        //Calculate dispersion
                                        GdispersionMax.X = Gitems.Max(p => p._scrolltop.X + p._item.X);
                                        GdispersionMax.Y = Gitems.Max(p => p._scrolltop.Y + p._item.Y);
                                        GdispersionMin.X = Gitems.Min(p => p._scrolltop.X + p._item.X);
                                        GdispersionMin.Y = Gitems.Min(p => p._scrolltop.Y + p._item.Y);
                                        //x_distance + y_distance <= threshold
                                        if ((GdispersionMax.X - GdispersionMin.X) + (GdispersionMax.Y - GdispersionMin.Y) <= GSthreshold)
                                            isGFixation = true;
                                        else
                                            //remove first of mitem
                                            Gitems.RemoveRange(0, 1);
                                    }
                                    if (isGFixation) {
                                        //tmp = Find dispersion between gaze and dispersion
                                        System.Windows.Point tmpMax = new System.Windows.Point(Math.Max((gaze._scrolltop.X + gaze._item.X), GdispersionMax.X), Math.Max((gaze._scrolltop.Y + gaze._item.Y), GdispersionMax.Y));
                                        System.Windows.Point tmpMin = new System.Windows.Point(Math.Min((gaze._scrolltop.X + gaze._item.X), GdispersionMin.X), Math.Min((gaze._scrolltop.Y + gaze._item.Y), GdispersionMin.Y));
                                        //x_distance + y_distance <= threshold
                                        if ((tmpMax.X - tmpMin.X) + (tmpMax.Y - tmpMin.Y) <= GSthreshold) {
                                            GdispersionMax = tmpMax;
                                            GdispersionMin = tmpMin;
                                        }
                                        else
                                        {
                                            //Save fixation into DB
                                            double centerX = Gitems.Average(center => center._item.X);
                                            double centerY = Gitems.Average(center => center._item.Y);
                                            if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                            {
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    gfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Gitems.Clear();
                                            isGFixation = false;
                                        }
                                    }
                                }
                            }
                            //Add current point in fixation
                            Gitems.Add(gaze);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isGFixation)
                            {
                                double centerX = Gitems.Average(center => center._item.X);
                                double centerY = Gitems.Average(center => center._item.Y);
                                if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                                {
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        gfid++;
                                    }
                                }
                            }
                            //Clear candidate fixation to 0 
                            Gitems.Clear();
                            isGFixation = false;
                        }
                    }
                    #endregion

                    //Update Program Bar
                    RowIDForProgrambar++;
                    if (worker.CancellationPending || _closePending)
                    {
                        //clear
                        rdr.Close();

                        e.Cancel = true;
                        return new Tuple<int, int>(-1, -1);
                    }
                    else
                    {
                        worker.ReportProgress((int)(100 * (startID + RowIDForProgrambar) / totalRowForProgrambar));
                        //System.Threading.Thread.Sleep(10);
                    }
                }

                #region Last_fixation
                //The last fixation
                if (calMouse)
                {
                    //Save fixation into DB
                    if (isMFixation)
                    {
                        double centerX = Mitems.Average(center => center._item.X);
                        double centerY = Mitems.Average(center => center._item.Y);
                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                        {
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                mfid++;
                            }
                        }
                    }
                    //Clear candidate fixation to 0 
                    Mitems.Clear();
                    isMFixation = false;
                }

                if (calGaze)
                {
                    //Save fixation into DB
                    if (isGFixation)
                    {
                        double centerX = Gitems.Average(center => center._item.X);
                        double centerY = Gitems.Average(center => center._item.Y);
                        if ((_isAbandonBrowserToolbar && centerY > _BrowserToolbarH) || !_isAbandonBrowserToolbar)
                        {
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                gfid++;
                            }
                        }
                    }
                    //Clear candidate fixation to 0 
                    Gitems.Clear();
                    isGFixation = false;
                }
                #endregion

                rdr.Close();
            }
            return new Tuple<int, int>(mfid, gfid);
        }

        private class CalculationItem
        {
            public int _id;
            public int _time;
            public System.Windows.Point _item;
            public System.Windows.Point _scrolltop;
            public CalculationItem()
            {
                _id = -1;
                _time = -1;
                _item = new System.Windows.Point(-1, -1);
                _scrolltop = new System.Windows.Point(0, 0);
            }
        }

        







    }
}
