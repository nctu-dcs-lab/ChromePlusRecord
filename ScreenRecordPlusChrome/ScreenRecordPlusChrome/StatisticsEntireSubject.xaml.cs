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

namespace ScreenRecordPlusChrome
{
    public partial class StatisticsEntireSubject : Window
    {
        #region Variable
        private string _projectName = null;
        #endregion

        public StatisticsEntireSubject(string projectName)
        {
            InitializeComponent();
            InitComponent(projectName);
        }
        private void InitComponent(string projectName)
        {
            if (projectName != null)
            {
                this._projectName = projectName;
                this.Title = "Statistics Entire Subject - " + this._projectName;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Can't find database!","ERROR");
                this.Close();
            }
        }

        #region Button
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            g_statistics_entireSubject.Height = this.ActualHeight - 40;
        }
        #endregion

        public void AddRowData(StatisticsEntireSubjectInfo info){
            dg_statistics_entireSubject_datagrid.Items.Add(info);
        }
    }
    public class StatisticsEntireSubjectInfo{
        public string subject { get; set; }
        public float duration { get; set; }
        public string eyedevice { get; set; }
        public int webpage1 { get; set; }
        public int webpage2 { get; set; }
        public float gfixation1 { get; set; }
        public float gfixation2 { get; set; }
        public float mfixation1 { get; set; }
        public float mfixation2 { get; set; }
    }
}
