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

using System.Data.SQLite;
using System.IO;

namespace ScreenRecordPlusChrome
{

    public partial class Database : Window
    {
        #region Variable
        private string _mainDir = null;
        private string _projectName = null;
        private string _DBPath = null;
        private List<string> _subjectName = null;
        private SQLiteConnection _sqliteConnect = null;
        #endregion

        public Database(string mainDir, string projectName, List<string> subjectName)
        {
            InitializeComponent();
            InitComponent(mainDir, projectName, subjectName);
        }
        private void InitComponent(string mainDir, string projectName, List<string> subjectName)
        {
            this._mainDir = mainDir;
            this._projectName = projectName;
            this._DBPath = _mainDir + @"\" + _projectName + @"\Database\" + _projectName;
            this._subjectName = subjectName;
            this.Title = "Database - " + this._projectName;
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
        private void ShowData() {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    foreach(string name in _subjectName){
                        //Rowdata
                        _sqliteCmd.CommandText = "SELECT ID, Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop FROM " + name + "Rawdata ORDER BY ID";
                        var rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read()) {
                            Database_Rawdata rawdata = new Database_Rawdata();
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
                            if (!rdr.IsDBNull(6)) {
                                System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(6));
                                rawdata.ScrollTopX = (float)s.X;
                                rawdata.ScrollTopY = (float)s.Y;
                            }
                            gd_database_rowdata.Items.Add(rawdata);
                        }
                        rdr.Close();

                        //URLEvent
                        _sqliteCmd.CommandText = @"SELECT URLEventID, URL, Keyword, StartTime FROM URLEvent WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read()) {
                            Database_URLEvent urlevent = new Database_URLEvent();
                            urlevent.SubjectName = name;
                            if (!rdr.IsDBNull(0))
                                urlevent.URLID = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1))
                                urlevent.URL = rdr.GetString(1);
                            if (!rdr.IsDBNull(2))
                                urlevent.Keyword = rdr.GetString(2);
                            if (!rdr.IsDBNull(3))
                                urlevent.StartTime = rdr.GetInt32(3);
                            gd_database_urlevent.Items.Add(urlevent);
                        }
                        rdr.Close();

                        //MKEvent
                        _sqliteCmd.CommandText = @"SELECT EventID, EventTime, EventType, EventTask, EventParam FROM MKEvent WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            Database_MKEvent mkevent = new Database_MKEvent();
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
                            gd_database_mkevent.Items.Add(mkevent);
                        }
                        rdr.Close();

                        //GazeFixation
                        _sqliteCmd.CommandText = @"SELECT FID, StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY FROM GazeFixation WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            Database_GazeFixation gazefixation = new Database_GazeFixation();
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
                            gd_database_gazefixation.Items.Add(gazefixation);
                        }
                        rdr.Close();

                        //MouseFixation
                        _sqliteCmd.CommandText = @"SELECT FID, StartTime, Duration, PositionX, PositionY, ScrollTopX, ScrollTopY FROM MouseFixation WHERE SubjectName=@subjectname ORDER BY ID";
                        _sqliteCmd.Parameters.AddWithValue("@subjectname", name);
                        rdr = _sqliteCmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            Database_MouseFixation mousefixation = new Database_MouseFixation();
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
                            gd_database_mousefixation.Items.Add(mousefixation);
                        }
                        rdr.Close();
                        

                    }
                }
                tr.Commit();
            }
        }


        #region Button
        private void Window_Closed(object sender, EventArgs e)
        {
            DBDisconnect();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DBConnect();
            ShowData();
        }
        #endregion


        #region DataGrid Iem
        public class Database_Rawdata {
            public int RID { get; set; }
            public string SubjectName { get; set; }
            public int Time { get; set; }
            public float MousePosX { get; set; }
            public float MousePosY { get; set; }
            public float GazePosX { get; set; }
            public float GazePosY { get; set; }
            public float ScrollTopX { get; set; }
            public float ScrollTopY { get; set; }
        }
        public class Database_MKEvent
        {
            public int MKID { get; set; }
            public string SubjectName { get; set; }
            public int EventTime { get; set; }
            public string EventType { get; set; }
            public string EventTask { get; set; }
            public string EventParam { get; set; }
        }
        public class Database_URLEvent
        {
            public int URLID { get; set; }
            public string SubjectName { get; set; }
            public string URL { get; set; }
            public string Keyword { get; set; }
            public int StartTime { get; set; }
        }
        public class Database_GazeFixation
        {
            public int FID { get; set; }
            public string SubjectName { get; set; }
            public int StartTime { get; set; }
            public int Duration { get; set; }
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float ScrollTopX { get; set; }
            public float ScrollTopY { get; set; }
        }
        public class Database_MouseFixation
        {
            public int FID { get; set; }
            public string SubjectName { get; set; }
            public int StartTime { get; set; }
            public int Duration { get; set; }
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float ScrollTopX { get; set; }
            public float ScrollTopY { get; set; }
        }
        #endregion

        
    }
}
