﻿#pragma checksum "..\..\..\ExportVideo.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "646F33F1B036F42A32F56C375FD10184F3B400DC"
//------------------------------------------------------------------------------
// <auto-generated>
//     這段程式碼是由工具產生的。
//     執行階段版本:4.0.30319.42000
//
//     對這個檔案所做的變更可能會造成錯誤的行為，而且如果重新產生程式碼，
//     變更將會遺失。
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace ScreenRecordPlusChrome {
    
    
    /// <summary>
    /// ExportVideo
    /// </summary>
    public partial class ExportVideo : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 11 "..\..\..\ExportVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cb_exportVideo_codec;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\..\ExportVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tb_exportVideo_savefolder;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\..\ExportVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button bt_exportVideo_browser;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\ExportVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ProgressBar pb_exportVideo_processbar;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\..\ExportVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock tooltip_exportVideo_processbar;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\..\ExportVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button bt_exportVideo_export;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\..\ExportVideo.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button bt_cancelExportVideo_export;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ScreenRecordPlusChrome;component/exportvideo.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\ExportVideo.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 4 "..\..\..\ExportVideo.xaml"
            ((ScreenRecordPlusChrome.ExportVideo)(target)).Closed += new System.EventHandler(this.Window_Closed);
            
            #line default
            #line hidden
            return;
            case 2:
            this.cb_exportVideo_codec = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 3:
            this.tb_exportVideo_savefolder = ((System.Windows.Controls.TextBox)(target));
            return;
            case 4:
            this.bt_exportVideo_browser = ((System.Windows.Controls.Button)(target));
            
            #line 16 "..\..\..\ExportVideo.xaml"
            this.bt_exportVideo_browser.Click += new System.Windows.RoutedEventHandler(this.bt_exportVideo_browser_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.pb_exportVideo_processbar = ((System.Windows.Controls.ProgressBar)(target));
            return;
            case 6:
            this.tooltip_exportVideo_processbar = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 7:
            this.bt_exportVideo_export = ((System.Windows.Controls.Button)(target));
            
            #line 31 "..\..\..\ExportVideo.xaml"
            this.bt_exportVideo_export.Click += new System.Windows.RoutedEventHandler(this.bt_exportVideo_export_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.bt_cancelExportVideo_export = ((System.Windows.Controls.Button)(target));
            
            #line 32 "..\..\..\ExportVideo.xaml"
            this.bt_cancelExportVideo_export.Click += new System.Windows.RoutedEventHandler(this.bt_cancelExportVideo_export_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

