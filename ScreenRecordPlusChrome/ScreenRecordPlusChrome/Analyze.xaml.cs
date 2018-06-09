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

using System.Windows.Forms;
using System.IO;

namespace ScreenRecordPlusChrome
{
    /// <summary>
    /// Analyze.xaml 的互動邏輯
    /// </summary>
    public partial class Analyze : Window
    {
        private string _MainDir;
        private List<string> _projectName;

        public Analyze(string mainDir)
        {
            InitializeComponent();
            if (mainDir != null || mainDir != "") {
                this.tb_analyze_SaveFolder.Text = mainDir + @"\MoniChrome";
            }
            _MainDir = this.tb_analyze_SaveFolder.Text;
        }

        //Replay Module
        private void bt_analyze_replay_Click(object sender, RoutedEventArgs e)
        {
            new Replay(_MainDir, cb_analyze_projectName.SelectedValue.ToString()).Show();
        }
        //Statistics Module
        private void bt_analyze_statistics_Click(object sender, RoutedEventArgs e)
        {
            new Statistics(_MainDir, cb_analyze_projectName.SelectedValue.ToString()).Show();
        }
        //Database Module
        private void bt_analyze_database_Click(object sender, RoutedEventArgs e)
        {
            new DatabaseSelecter(_MainDir, cb_analyze_projectName.SelectedValue.ToString()).Show();
        }

        #region Analysis Path
        private void bt_analyze_browse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.tb_analyze_SaveFolder.Text = fbd.SelectedPath;
                _MainDir = this.tb_analyze_SaveFolder.Text;
            }
            fbd.Dispose();
        }

        private List<string> CheckProjectDir(string maindir) {
            List<string> projectName = new List<string>();
            if (Directory.Exists(maindir)) {
                foreach (var d in Directory.GetDirectories(maindir))
                {
                    var dirName = new DirectoryInfo(d).Name;
                    if (File.Exists(d + @"\Database\" + dirName)) { 
                        projectName.Add(dirName);
                    }
                }
            }
            return projectName;
        }

        private void tb_analyze_SaveFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                _projectName = CheckProjectDir(_MainDir);
                this.cb_analyze_projectName.ItemsSource = _projectName;
                this.cb_analyze_projectName.SelectedIndex = 0;
            }));
        }
        #endregion

        

        

        
    }
}
