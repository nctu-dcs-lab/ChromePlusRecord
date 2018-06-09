using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public partial class ExportDatabase : Window
    {
        #region Variables
        private BackgroundWorker _bgWorker = new BackgroundWorker();
        private SQLiteConnection _sqliteConnect = null;
        private string _mainDir = "";
        private string _projectName = "";
        private List<string> _subjectNames = null;
        private string _DBPath = null;
        private string _saveFolder = "";
        private bool _closePending = false;
        #endregion

        public ExportDatabase(string mainDir, string projectName, List<string> subjectNames)
        {
            InitializeComponent();
            InitComponent(mainDir, projectName, subjectNames);
        }
        private void InitComponent(string mainDir, string projectName, List<string> subjectNames)
        {
            _mainDir = mainDir;
            _projectName = projectName;
            _subjectNames = subjectNames;
            _DBPath = _mainDir + @"\" + _projectName + @"\Database\" + _projectName;
            _saveFolder = _mainDir + @"\" + _projectName + @"\DatabaseInfo";
            tb_exportDatabase_saveFolder.Text = _saveFolder;
            bt_exportDatabase_cancel.IsEnabled = false;
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
            else
            {
                System.Windows.Forms.MessageBox.Show("Can't find database!", "ERROR");
                this.Close();
            }
        }
        private void DBDisconnect()
        {
            try
            {
                if (_sqliteConnect != null)
                {
                    _sqliteConnect.Close();
                    _sqliteConnect = null;
                }
            }
            catch (Exception e) { }
        }

        #region Button
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_DBPath))
            {
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
            _closePending = true;
            DBDisconnect();
        }

        private void bt_exportDatabase_browse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _saveFolder = fbd.SelectedPath + @"\DatabaseInfo";
                tb_exportDatabase_saveFolder.Text = _saveFolder;
            }
            fbd.Dispose();
        }
        private void bt_exportDatabase_export_Click(object sender, RoutedEventArgs e)
        {
            bt_exportDatabase_export.IsEnabled = false;
            bt_exportDatabase_cancel.IsEnabled = true;
            if (!Directory.Exists(_saveFolder))
                Directory.CreateDirectory(_saveFolder);
            if (!_bgWorker.IsBusy)
                _bgWorker.RunWorkerAsync();
        }
        private void bt_exportDatabase_cancel_Click(object sender, RoutedEventArgs e)
        {
            _bgWorker.CancelAsync();
        }
        #endregion

        #region BackgroundWorker
        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            ExportData(worker, e, _saveFolder);
        }

        void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pb_exportDatabase_programbar.Value = e.ProgressPercentage;
            tooltip_exportDatabase_pragrambar.Text = e.ProgressPercentage.ToString();
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bt_exportDatabase_export.IsEnabled = true;
            bt_exportDatabase_cancel.IsEnabled = false;
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
            pb_exportDatabase_programbar.Value = 0;
            tooltip_exportDatabase_pragrambar.Text = "0";
        }
        #endregion

        private void ExportData(BackgroundWorker worker, DoWorkEventArgs e, string saveFolder)
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    int subjectid = 0;
                    StreamWriter sw = null;
                    foreach (string name in _subjectNames)
                    {
                        //Create dir
                        string fileroot = saveFolder + @"\" + name;
                        if (!Directory.Exists(fileroot))
                            Directory.CreateDirectory(fileroot);

                        //Rowdata
                        _sqliteCmd.CommandText = "SELECT ID, Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop FROM " + name + "Rawdata ORDER BY ID";
                        var rdr = _sqliteCmd.ExecuteReader();
                        if (File.Exists(fileroot + @"\Rowdata.csv"))
                            File.Delete(fileroot + @"\Rowdata.csv");
                        sw = new StreamWriter(fileroot + @"\Rowdata.csv", true);
                        sw.WriteLine("RID,SubjectName,Time(ms),MousePosX(px),MousePosY(px),GazePosX(px),GazePosY(px),ScrollTopX(px),ScrollTopY(px)");
                        while (rdr.Read())
                        {
                            Database.Database_Rawdata rawdata = new Database.Database_Rawdata();
                            rawdata.SubjectName = name;
                            if (!rdr.IsDBNull(0))
                                rawdata.RID = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                rawdata.Time = rdr.GetInt32(1);
                            if (!rdr.IsDBNull(2))
                                rawdata.MousePosX = rdr.GetFloat(2);
                            if (!rdr.IsDBNull(3))
                                rawdata.MousePosY = rdr.GetFloat(3);
                            if (!rdr.IsDBNull(4))
                                rawdata.GazePosX = rdr.GetFloat(4);
                            if (!rdr.IsDBNull(5))
                                rawdata.GazePosY = rdr.GetFloat(5);
                            if (!rdr.IsDBNull(6))
                            {
                                System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(6));
                                rawdata.ScrollTopX = (float)s.X;
                                rawdata.ScrollTopY = (float)s.Y;
                            }
                            //save file
                            sw.WriteLine(rawdata.RID + "," + rawdata.SubjectName + "," + rawdata.Time + "," + rawdata.MousePosX + "," + rawdata.MousePosY + "," + rawdata.GazePosX + "," + rawdata.GazePosY + "," + rawdata.ScrollTopX + "," + rawdata.ScrollTopY);
                            //canel event
                            if (worker.CancellationPending || _closePending)
                            {
                                if (sw != null) {
                                    sw.Close();
                                    sw = null;
                                }
                                rdr.Close();
                                e.Cancel = true;
                                return;
                            }
                        }
                        sw.Close();
                        sw = null;
                        rdr.Close();

                        //URLEvent
                        _sqliteCmd.CommandText = @"SELECT URLEventID, URL, Keyword, StartTime FROM URLEvent WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        if (File.Exists(fileroot + @"\URLEvent.csv"))
                            File.Delete(fileroot + @"\URLEvent.csv");
                        sw = new StreamWriter(fileroot + @"\URLEvent.csv", true);
                        sw.WriteLine("URLID,SubjectName,URL,Keyword,StartTime(ms)");
                        while (rdr.Read())
                        {
                            Database.Database_URLEvent urlevent = new Database.Database_URLEvent();
                            urlevent.SubjectName = name;
                            if (!rdr.IsDBNull(0))
                                urlevent.URLID = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                urlevent.URL = rdr.GetString(1);
                            if (!rdr.IsDBNull(2))
                                urlevent.Keyword = rdr.GetString(2);
                            if (!rdr.IsDBNull(3))
                                urlevent.StartTime = rdr.GetInt32(3);
                            //save file
                            sw.WriteLine(urlevent.URLID + "," + urlevent.SubjectName + "," + urlevent.URL + "," + urlevent.Keyword + "," + urlevent.StartTime);
                            //canel event
                            if (worker.CancellationPending || _closePending)
                            {
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
                        sw.Close();
                        sw = null;
                        rdr.Close();

                        //MKEvent
                        _sqliteCmd.CommandText = @"SELECT EventID, EventTime, EventType, EventTask, EventParam FROM MKEvent WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        if (File.Exists(fileroot + @"\MKEvent.csv"))
                            File.Delete(fileroot + @"\MKEvent.csv");
                        sw = new StreamWriter(fileroot + @"\MKEvent.csv", true);
                        sw.WriteLine("MKID,SubjectName,EventTime(ms),EventType,EventTask,EventParam");
                        while (rdr.Read())
                        {
                            Database.Database_MKEvent mkevent = new Database.Database_MKEvent();
                            mkevent.SubjectName = name;
                            if (!rdr.IsDBNull(0))
                                mkevent.MKID = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                mkevent.EventTime = rdr.GetInt32(1);
                            if (!rdr.IsDBNull(2))
                                mkevent.EventType = rdr.GetString(2);
                            if (!rdr.IsDBNull(3))
                                mkevent.EventTask = rdr.GetString(3);
                            if (!rdr.IsDBNull(4))
                                mkevent.EventParam = rdr.GetString(4);
                            //save file
                            sw.WriteLine(mkevent.MKID + "," + mkevent.SubjectName + "," + mkevent.EventTime + "," + mkevent.EventType + "," + mkevent.EventTask + ",\"" + mkevent.EventParam + "\"");
                            //canel event
                            if (worker.CancellationPending || _closePending)
                            {
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
                        sw.Close();
                        sw = null;
                        rdr.Close();

                        //GazeFixation
                        _sqliteCmd.CommandText = @"SELECT FID, StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY FROM GazeFixation WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        if (File.Exists(fileroot + @"\GazeFixation.csv"))
                            File.Delete(fileroot + @"\GazeFixation.csv");
                        sw = new StreamWriter(fileroot + @"\GazeFixation.csv", true);
                        sw.WriteLine("FID,SubjectName,StartTime(ms),Duration(ms),PositionX(px),PositionY(px),ScrollTopX(px),ScrollTopY(px)");
                        while (rdr.Read())
                        {
                            Database.Database_GazeFixation gazefixation = new Database.Database_GazeFixation();
                            gazefixation.SubjectName = name;
                            if (!rdr.IsDBNull(0))
                                gazefixation.FID = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                gazefixation.StartTime = rdr.GetInt32(1);
                            if (!rdr.IsDBNull(2))
                                gazefixation.Duration = rdr.GetInt32(2);
                            if (!rdr.IsDBNull(3))
                                gazefixation.PositionX = rdr.GetFloat(3);
                            if (!rdr.IsDBNull(4))
                                gazefixation.PositionY = rdr.GetFloat(4);
                            if (!rdr.IsDBNull(5))
                                gazefixation.ScrollTopX = rdr.GetFloat(5);
                            if (!rdr.IsDBNull(6))
                                gazefixation.ScrollTopY = rdr.GetFloat(6);
                            //save file
                            sw.WriteLine(gazefixation.FID + "," + gazefixation.SubjectName + "," + gazefixation.StartTime + "," + gazefixation.Duration + "," + gazefixation.PositionX + "," + gazefixation.PositionY + "," + gazefixation.ScrollTopX + "," + gazefixation.ScrollTopY);
                            //canel event
                            if (worker.CancellationPending || _closePending)
                            {
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
                        sw.Close();
                        sw = null;
                        rdr.Close();

                        //MouseFixation
                        _sqliteCmd.CommandText = @"SELECT FID, StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY FROM MouseFixation WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        if (File.Exists(fileroot + @"\MouseFixation.csv"))
                            File.Delete(fileroot + @"\MouseFixation.csv");
                        sw = new StreamWriter(fileroot + @"\MouseFixation.csv", true);
                        sw.WriteLine("FID,SubjectName,StartTime(ms),Duration(ms),PositionX(px),PositionY(px),ScrollTopX(px),ScrollTopY(px)");
                        while (rdr.Read())
                        {
                            Database.Database_MouseFixation mousefixation = new Database.Database_MouseFixation();
                            mousefixation.SubjectName = name;
                            if (!rdr.IsDBNull(0))
                                mousefixation.FID = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                mousefixation.StartTime = rdr.GetInt32(1);
                            if (!rdr.IsDBNull(2))
                                mousefixation.Duration = rdr.GetInt32(2);
                            if (!rdr.IsDBNull(3))
                                mousefixation.PositionX = rdr.GetFloat(3);
                            if (!rdr.IsDBNull(4))
                                mousefixation.PositionY = rdr.GetFloat(4);
                            if (!rdr.IsDBNull(5))
                                mousefixation.ScrollTopX = rdr.GetFloat(5);
                            if (!rdr.IsDBNull(6))
                                mousefixation.ScrollTopY = rdr.GetFloat(6);
                            //save file
                            sw.WriteLine(mousefixation.FID + "," + mousefixation.SubjectName + "," + mousefixation.StartTime + "," + mousefixation.Duration + "," + mousefixation.PositionX + "," + mousefixation.PositionY + "," + mousefixation.ScrollTopX + "," + mousefixation.ScrollTopY);
                            //canel event
                            if (worker.CancellationPending || _closePending)
                            {
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
                        sw.Close();
                        sw = null;
                        rdr.Close();

                        //Updata Processbar
                        subjectid++;
                        if (worker.CancellationPending || _closePending)
                        {
                            e.Cancel = true;
                            return;
                        }
                        else
                        {
                            worker.ReportProgress(100 * subjectid / _subjectNames.Count);
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                }
                tr.Commit();
            }
        }

    }
}
