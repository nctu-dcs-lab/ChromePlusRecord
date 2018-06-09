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

    public partial class DatabaseSelecter : Window
    {
        #region Variable
        private string _mainDir = null;
        private string _projectName = null;
        private string _DBPath = null;
        private SQLiteConnection _sqliteConnect = null;
        #endregion

        public DatabaseSelecter(string mainDir, string projectName)
        {
            InitializeComponent();

            InitComponent(mainDir, projectName);
        }
        private void InitComponent(string mainDir, string projectName)
        {
            this._mainDir = mainDir;
            this._projectName = projectName;
            this._DBPath = _mainDir + @"\" + _projectName + @"\Database\" + _projectName;
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
                catch {
                    System.Windows.Forms.MessageBox.Show("Can't open database!", "ERROR");
                    this.Close();
                }
            }
            else {
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

        #region Button
        private void Window_Closed(object sender, EventArgs e)
        {
            DBDisconnect();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DBConnect();
            this.clb_databaseSelecter_subjectname.ItemsSource = GetAllSubjectName();
            DBDisconnect();
        }
        private void bt_databaseSelecter_go_Click(object sender, RoutedEventArgs e)
        {
            if (clb_databaseSelecter_subjectname.SelectedItems.Count > 0)
            {
                new Database(_mainDir, _projectName, clb_databaseSelecter_subjectname.SelectedItems.OfType<string>().ToList()).Show();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please choose subjects!", "ERROR");
            }
        }
        private void bt_databaseSelecter_export_Click(object sender, RoutedEventArgs e)
        {
            if (clb_databaseSelecter_subjectname.SelectedItems.Count > 0)
            {
                new ExportDatabase(_mainDir, _projectName, clb_databaseSelecter_subjectname.SelectedItems.OfType<string>().ToList()).Show();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please choose subjects!", "ERROR");
            }
        }
        #endregion

        

        
    }
}
