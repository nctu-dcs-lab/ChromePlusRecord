   M S B A M L   `   `   `  ���� X  TXceed.Wpf.Toolkit, Version=3.2.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f4P:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.ToolkitXceed.Wpf.Toolkit   ScreenRecordPlusChrome'   !ScreenRecordPlusChrome.Statistics   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     NwpfTool:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.Toolkit     -     � Title$  
Statistics�� � Icon$ main.ico��$	��447��$	��790��$	��790��$	��330�� � 	Resources  �� MergedDictionaries �� Source��  �� $G A/ScreenRecordPlusChrome;component/Resource/Style/ButtonStyle.xaml?�%    .��� ���	#FFC1C1C1.+��� .+�� -    �Name$ g_statistics_top��$	��230��.+��� -    ��Name$ tc_statistics_main��$
��Left=�$	��217��$	��760��$��5,10,0,0q�$	��Top=�.@��� $��Subject��.��� ���	#FFE5E5E5.+���  �� Orientation$ 
Horizontal=�$��5,15,5,5q�.+��� 
�������$��2q�$	��210��._�� .+��� $��5,15,5,5q�.+�$   Xceed.Wpf.Toolkit.CheckListBox  -     Name$  clb_statistics_subjectname��$	��120���� -   	 ��Name$$	 bt_statistics_subject_analysis��$��Entire Subject Analysis��$��0,5,0,0q����	#FFC1C1C1
 � ToolTip
 �� 
����ح�$��2,2,2,2q�._��� .+��� $
��Bold"�$��Calculate Information :���� $I��CCalculate information of entire subjectand show it on other table.���� $��Subjects�����	#FFE5E5E5$��15,3,0,0q�$��28��$��57��$	��Top=�$
��Left=��� $
��Gaze��.��� ���	#FFE5E5E5.+��� $ 
Horizontal=�$��5,15,5,5q�.+��� 
�������$��2q�$	��200��._�� .+��� $��5,15,5,5q�.+��� -    ��Name$" cb_statistics_gaze_fixation1��$��fixation (count)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $$��The number of fixations(count)���� -   $" cb_statistics_gaze_fixation2��$��fixation (count/s)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $@��:The number of fixations / Duration of the webpage(count/s)���� -   $" cb_statistics_gaze_fixation3��$!��fixation duration mean (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $"��Average of fixation time(ms)���� -	   $" cb_statistics_gaze_fixation4��$#��fixation duration median (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $!��Median of fixation time(ms)���� -
   $" cb_statistics_gaze_fixation5��$��fixation duration (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $��Total fixation time(ms)���� -   $" cb_statistics_gaze_fixation6��$!��fixation/URL duration ratio��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $3��-Total fixation time / Duration of the webpage���� 
�������$��2q�$	��185��$��5,0,0,0q�._�� .+��� $��5,15,5,5q�.+��� -   $! cb_statistics_gaze_saccade1��$��avg. saccade length (px)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $.��(Path length / The number of saccades(px)���� -   $! cb_statistics_gaze_saccade2��$ ��avg. saccade duration (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $K��EAverage of saccade time(ms)[Saccade time: Path time - Fixation time]���� -   $! cb_statistics_gaze_saccade3��$��avg. saccade rate (px/s)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $g��aAverage of saccade rate(px/s)[Saccade rate: Saccade length / Saccade time between two fixations]���� 
�������$��2q�$	��140��$��5,0,0,0q�._�� .+��� $��5,15,5,5q�.+��� -   $ cb_statistics_gaze_path1��$��path length (px)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $/��)Total direct length between fixations(px)���� -   $ cb_statistics_gaze_path2��$��path duration (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $3��-Time from first fixation to last fixation(ms)���� 
�������$��2q�$	��205��$��5,0,0,0q�._�� .+��� $��5,15,5,5q�.+��� -   $% cb_statistics_gaze_interaction1��$)��#fixations until first click (count)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $(��"Fixations until first click(count)���� -   $% cb_statistics_gaze_interaction2��$#��avg. gaze/mouse distance (px)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $E��?Average of direct length between gaze point and mouse point(px)���� $��Gaze fixations�����	#FFE5E5E5$��15,3,0,0q�$��28��$��90��$	��Top=�$
��Left=��� $��Gaze saccades�����	#FFE5E5E5$��	220,3,0,0q�$��28��$��95��$	��Top=�$
��Left=��� $��	Gaze path�����	#FFE5E5E5$��	410,3,0,0q�$��28��$��68��$	��Top=�$
��Left=��� $��Gaze Mouse interaction�����	#FFE5E5E5$��	555,3,0,0q�$��28��$	��145��$	��Top=�$
��Left=��� $��Mouse��.��� ���	#FFE5E5E5.+��� $ 
Horizontal=�$��5,15,5,5q�.+��� 
�������$��2q�$	��200��._�� .+��� $��5,15,5,5q�.+��� -   $# cb_statistics_mouse_fixation1��$��fixation (count)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $$��The number of fixations(count)���� -   $# cb_statistics_mouse_fixation2��$��fixation (count/s)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $@��:The number of fixations / Duration of the webpage(count/s)���� -   $# cb_statistics_mouse_fixation3��$!��fixation duration mean (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $"��Average of fixation time(ms)���� -   $# cb_statistics_mouse_fixation4��$#��fixation duration median (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $!��Median of fixation time(ms)���� -   $# cb_statistics_mouse_fixation5��$��fixation duration (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $��Total fixation time(ms)���� -   $# cb_statistics_mouse_fixation6��$!��fixation/URL duration ratio��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $3��-Total fixation time / Duration of the webpage���� 
�������$��2q�$	��185��$��5,0,0,0q�._�� .+��� $��5,15,5,5q�.+��� -   $" cb_statistics_mouse_saccade1��$��avg. saccade length (px)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $.��(Path length / The number of saccades(px)���� -   $" cb_statistics_mouse_saccade2��$ ��avg. saccade duration (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $K��EAverage of saccade time(ms)[Saccade time: Path time - Fixation time]���� -   $" cb_statistics_mouse_saccade3��$��avg. saccade rate (px/s)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $g��aAverage of saccade rate(px/s)[Saccade rate: Saccade length / Saccade time between two fixations]���� 
�������$��2q�$	��140��$��5,0,0,0q�._�� .+��� $��5,15,5,5q�.+��� -   $ cb_statistics_mouse_path1��$��path length (px)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $/��)Total direct length between fixations(px)���� -   $ cb_statistics_mouse_path2��$��path duration (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $3��-Time from first fixation to last fixation(ms)���� 
�������$��2q�$	��205��$��5,0,0,0q�._�� .+��� .+��� $��5,15,5,5q�$ 
Horizontal=�
�������.+��� -   $! cb_statistics_mouse_click_1��$��L��$
��Left=�$	��Top=�$��15,5,0,5q�
 �� .+��� $��Left mouse button���� -   $! cb_statistics_mouse_click_2��$��R��$
��Left=�$	��Top=�$��35,5,0,5q�
 �� .+��� $��Right mouse button���� -    $! cb_statistics_mouse_click_3��$��M��$
��Left=�$	��Top=�$��35,5,0,5q�
 �� .+��� $��Middle mouse button���� $��5,0,5,5q�.+��� -!   $" cb_statistics_mouse_clicks_1��$��click (count)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $!��The number of clicks(count)���� -"   $" cb_statistics_mouse_clicks_2��$��click rate (count/s)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $=��7The number of clicks / Duration of the webpage(count/s)���� -#   $" cb_statistics_mouse_clicks_3��$)��#fixations until first click (count)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $(��"Fixations until first click(count)���� -$   $" cb_statistics_mouse_clicks_4��$!��time until first click (ms)��$
��Left=�$	��Top=�$��0,5,0,5q�
 �� .+��� $ ��Time until first click(ms)���� $��Mouse fixations�����	#FFE5E5E5$��15,3,0,0q�$��28��$	��100��$	��Top=�$
��Left=��� $��Mouse saccades�����	#FFE5E5E5$��	220,3,0,0q�$��28��$	��105��$	��Top=�$
��Left=��� $��
Mouse path�����	#FFE5E5E5$��	410,3,0,0q�$��28��$��78��$	��Top=�$
��Left=��� $��Mouse clicks�����	#FFE5E5E5$��	555,3,0,0q�$��28��$��83��$	��Top=�$
��Left=��� -%   $	 bt_statistics_export��$��E��   FixationCalButtonStyle#���   $
��Left=�$��	630,5,0,0q�$��25��$��25��$	��Top=�$��0q���Z� 
 �� 
����ح�$��2,2,2,2q�._��� .+��� $
��Bold"�$��Export :���� $����Export all information on selected subjectsto text files. You can select 'merge' to mergeentire subject information to URL information.���� -&   $%	 bt_statistics_calculatefixation��$��F��#���   $
��Left=�$��	660,5,0,0q�$��25��$��25��$	��Top=�$��0q���Z� 
 �� 
����ح�$��2,2,2,2q�._��� .+��� $
��Bold"�$��Calculate Fixations :���� $/��)Calculate fixations of selected subjects.���� -'   $	 bt_statistics_calculate��$��	Calculate��$��65��$��24��$
��Left=�$	��Top=�$��	690,5,0,0q�
�������
�����F�
�������
 �� 
����ح�$��2,2,2,2q�._��� .+��� $
��Bold"�$��Calculate Information :���� $L��FCalculate information of selected subjectsand show it on table below.��� -(   $ g_statistics_bottom��$	��180��.+�&   System.Windows.Controls.DataGrid  -)     Name$ dg_statistics_datagrid��$
��Left=�$	��760��$��5,0,0,0q�$	��Top=�   RowHeaderWidth$ 20Q�   ColumnHeaderHeight$ 50Q�   CanUserResizeRows .     CanUserAddRows .     CanUserDeleteRows .     
IsReadOnly . ���� *ActualHeight+ �� ElementName$ g_statistics_bottom��   Columns 0  *System.Windows.Controls.DataGridTextColumn  -*   dgtc_statistics_subject��,  &System.Windows.Controls.DataGridColumn   Header$ Subject��   Width5  /System.Windows.Controls.DataGridLengthConverter$ 60 1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *	subject+  -+   dgtc_statistics_id��$ 	WebpageID��$ 80  �� *id+5  /System.Windows.Controls.DataGridHyperlinkColumn  -,   dgtc_statistics_webpage��$ Webpage��$ 70  �� *	webpage+  --   dgtc_statistics_keyword��$ Keyword��$ 60  �� *	keyword+  -.   "dgtc_statistics_gaze_fixation1��$ Gaze:Fixation (count)��$	 110    
Visibility$ Hidden=� �� *
gfixation1+  -/   "dgtc_statistics_gaze_fixation2��$ Gaze:Fixation (count/s)��$	 110 $ Hidden=� �� *
gfixation2+  -0   "dgtc_statistics_gaze_fixation3��$' !Gaze:Fixation Duration Mean (ms)��$	 110 $ Hidden=� �� *
gfixation3+  -1   "dgtc_statistics_gaze_fixation4��$) #Gaze:Fixation Duration Median (ms)��$	 110 $ Hidden=� �� *
gfixation4+  -2   "dgtc_statistics_gaze_fixation5��$" Gaze:Fixation Duration (ms)��$	 110 $ Hidden=� �� *
gfixation5+  -3   "dgtc_statistics_gaze_fixation6��$0 *Gaze:Fixation Duration/URL Duration Ratio��$	 110 $ Hidden=� �� *
gfixation6+  -4   !dgtc_statistics_gaze_saccade1��$' !Gaze:Average Saccade Length (px)��$	 110 $ Hidden=� �� *	gsaccade1+  -5   !dgtc_statistics_gaze_saccade2��$) #Gaze:Average Saccade Duration (ms)��$	 110 $ Hidden=� �� *	gsaccade2+  -6   !dgtc_statistics_gaze_saccade3��$' !Gaze:Average Saccade Rate (px/s)��$	 110 $ Hidden=� �� *	gsaccade3+  -7   dgtc_statistics_gaze_path1��$ Gaze:Path Length (px)��$	 110 $ Hidden=� �� *gpath1+  -8   dgtc_statistics_gaze_path2��$ Gaze:Path Duration (ms)��$	 110 $ Hidden=� �� *gpath2+  -9   %!dgtc_statistics_gaze_interaction1��$ Gaze:FUFC (count)��$	 110 $ Hidden=� �� *ginteraction1+  -:   %!dgtc_statistics_gaze_interaction2��$, &Gaze:Average Gaze Mouse Distance (px)��$	 110 $ Hidden=� �� *ginteraction2+  -;   #dgtc_statistics_mouse_fixation1��$ Mouse:Fixation (count)��$	 110 $ Hidden=� �� *
mfixation1+  -<   #dgtc_statistics_mouse_fixation2��$ Mouse:Fixation (count/s)��$	 110 $ Hidden=� �� *
mfixation2+  -=   #dgtc_statistics_mouse_fixation3��$( "Mouse:Fixation Duration Mean (ms)��$	 110 $ Hidden=� �� *
mfixation3+  ->   #dgtc_statistics_mouse_fixation4��$* $Mouse:Fixation Duration Median (ms)��$	 110 $ Hidden=� �� *
mfixation4+  -?   #dgtc_statistics_mouse_fixation5��$# Mouse:Fixation Duration (ms)��$	 110 $ Hidden=� �� *
mfixation5+  -@   #dgtc_statistics_mouse_fixation6��$1 +Mouse:Fixation Duration/URL Duration Ratio��$	 110 $ Hidden=� �� *
mfixation6+  -A   "dgtc_statistics_mouse_saccade1��$( "Mouse:Average Saccade Length (px)��$	 110 $ Hidden=� �� *	msaccade1+  -B   "dgtc_statistics_mouse_saccade2��$* $Mouse:Average Saccade Duration (ms)��$	 110 $ Hidden=� �� *	msaccade2+  -C   "dgtc_statistics_mouse_saccade3��$( "Mouse:Average Saccade Rate (px/s)��$	 110 $ Hidden=� �� *	msaccade3+  -D   dgtc_statistics_mouse_path1��$ Mouse:Path Length (px)��$	 110 $ Hidden=� �� *mpath1+  -E   dgtc_statistics_mouse_path2��$ Mouse:Path Duration (ms)��$	 110 $ Hidden=� �� *mpath2+  -F   !dgtc_statistics_mouse_lclick1��$ Mouse:L Click (count)��$	 110 $ Hidden=� �� *
mlclick1+  -G   !dgtc_statistics_mouse_lclick2��$# Mouse:L Click Rate (count/s)��$	 110 $ Hidden=� �� *
mlclick2+  -H   !dgtc_statistics_mouse_lclick3��$ Mouse:L FUFC (count)��$	 110 $ Hidden=� �� *
mlclick3+  -I   !dgtc_statistics_mouse_lclick4��$ Mouse:L TUFC (ms)��$	 110 $ Hidden=� �� *
mlclick4+  -J   !dgtc_statistics_mouse_rclick1��$ Mouse:R Click (count)��$	 110 $ Hidden=� �� *
mrclick1+  -K   !dgtc_statistics_mouse_rclick2��$# Mouse:R Click Rate (count/s)��$	 110 $ Hidden=� �� *
mrclick2+  -L   !dgtc_statistics_mouse_rclick3��$ Mouse:R FUFC (count)��$	 110 $ Hidden=� �� *
mrclick3+  -M   !dgtc_statistics_mouse_rclick4��$ Mouse:R TUFC (ms)��$	 110 $ Hidden=� �� *
mrclick4+  -N   !dgtc_statistics_mouse_mclick1��$ Mouse:M Click (count)��$	 110 $ Hidden=� �� *
mmclick1+  -O   !dgtc_statistics_mouse_mclick2��$# Mouse:M Click Rate (count/s)��$	 110 $ Hidden=� �� *
mmclick2+  -P   !dgtc_statistics_mouse_mclick3��$ Mouse:M FUFC (count)��$	 110 $ Hidden=� �� *
mmclick3+  -Q   !dgtc_statistics_mouse_mclick4��$ Mouse:M TUFC (ms)��$	 110 $ Hidden=� �� *
mmclick4+