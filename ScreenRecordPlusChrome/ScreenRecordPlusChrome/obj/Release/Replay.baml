   M S B A M L   `   `   `  ���� Q  MScreenRecordPlusChrome, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null?$clr-namespace:ScreenRecordPlusChromeScreenRecordPlusChrome  X TXceed.Wpf.Toolkit, Version=3.2.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f4P:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.ToolkitXceed.Wpf.Toolkit O KCustomWPFColorPicker, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullY@clr-namespace:CustomWPFColorPicker;assembly=CustomWPFColorPickerCustomWPFColorPicker #    ScreenRecordPlusChrome.Replay   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     3as$clr-namespace:ScreenRecordPlusChrome     NwpfTool:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.Toolkit     aCustomWPFColorPicker@clr-namespace:CustomWPFColorPicker;assembly=CustomWPFColorPicker       � Title$  Replay��$	��600��$	��680��$	��230�� � 	Resources  �� MergedDictionaries �� Source��  �� $G A/ScreenRecordPlusChrome;component/Resource/Style/SliderStyle.xaml?��� $G A/ScreenRecordPlusChrome;component/Resource/Style/ButtonStyle.xaml?�%    .��� -    �Name$ g_replay_mainGrid��.+��� .+�� -   $ g_replay_topController�����	#FFE6E6E6$��45��$	��Top=�.+���  �� Orientation$ 
Horizontal=�.+��� -    ��Name$ cb_replay_videoName��$
��Left=�$	��Top=�$	��134��$��	10,10,0,0q��� $��Speed:��$
��Left=�$	��Top=�$��5,7,0,0q�$��14(�$  Xceed.Wpf.Toolkit.DoubleUpDown  -     Name$ dud_replay_speed���  �Xceed.Wpf.Toolkit.NumericUpDown`1[[System.Nullable`1[[System.Double, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]   FormatString$ F1���  �Xceed.Wpf.Toolkit.Primitives.UpDownBase`1[[System.Nullable`1[[System.Double, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]	   Value$		 1.0X�
   	Increment$
 .1X�   Maximum$	 2.0X�   Minimum$	 0.1X�$��5,10,0,0q�$��19��$
��Left=�$��70��$	��Top=��� -    ��Name$ bt_replay_sound��$	��♬��   SoundButtonStyle#���   $
��Left=�$��30��$��10,7,0,0q�$	��Top=�$��30��$��0q���Z�  � ToolTip �� 
����ح�$��2,2,2,2q�._��� .+��� $
��Bold"�$��Video Sound :���� $��Mute or unmute.���� -   $ bt_replay_calculate��$��F��#���   $
��Left=�$��10,7,0,0q�$��30��$��30��$	��Top=�$��0q���Z�  �� 
����ح�$��2,2,2,2q�._��� .+��� $
��Bold"�$��Calculate Fixations :���� $)��#Calculate fixations of the subject.���� -   $ bt_replay_exportVideo��$��E��#���   $
��Left=�$��10,7,0,0q�$��30��$��30��$	��Top=�$��0q���Z�  �� 
����ح�$��2,2,2,2q�._��� .+��� $
��Bold"�$��Export :���� $Z��TExport the video of the subject and add theinformation with the style you selected.��� -   $ g_replay_toolbar��$��60��.+�`�  `� 
Background
 �   �.�c�  c� Band$ 1�� c� 	BandIndex$ 1��$
��Left=�$��30��$	��Top=����	#FFDAE7F5.@��� -	    ��Name$ 	img_mouse��$��23��$��3,1,0,0q�$	��Top=�$��25��$ ��Resource\Picture\mouse.png���� �� -
   $ bt_toolbar_mouse_track��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $ ��Resource\Picture\track.png�� �� .+��� $��Mouse track���� -   $! bt_toolbar_mouse_track_type��$��S��$��25��$��3,1,0,0q�$	��Top=�$��25��
���   �$��16(�
�������$
��Bold"� �� .+��� $>��8S: Track move when scrollD: Track disappear when scroll���� $ 
Horizontal=�$��2,1,0,0q�.+�1  +CustomWPFColorPicker.ColorPickerControlView  -     Name$( "bt_toolbar_mouse_track_colorpicker��$��23��   CurrentColor$	 Red���� -    ��Name$( "tb_toolbar_mouse_track_colorpicker��$��1(� �� 
IsReadOnly . $y�Hidden=�$��5������  �� ElementName$( "bt_toolbar_mouse_track_colorpicker�� �� Path$ CurrentColor�%  Xceed.Wpf.Toolkit.IntegerUpDown  -     Name$' !iud_toolbar_mouse_track_linewidth��$��0,1,0,0q�$��23��$��40���  �Xceed.Wpf.Toolkit.Primitives.UpDownBase`1[[System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]   Minimum$ 1X�   Maximum$ 10X�   Value$ 3X��� �� -   $ bt_toolbar_mouse_cursor��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $!��Resource\Picture\cursor.png�� �� .+��� $��Mouse cursor���� -   $$ bt_toolbar_mouse_cursor_circle��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $!��Resource\Picture\circle.png�� �� .+��� $$��Mouse cursor with circle shape���� $ 
Horizontal=�$��2,1,0,0q�.+�  -   $0 *bt_toolbar_mouse_cursor_circle_colorpicker��$��23��$	 Red���� -   $0 *tb_toolbar_mouse_cursor_circle_colorpicker��$��1(� . $y�Hidden=�$��5������ $0 *bt_toolbar_mouse_cursor_circle_colorpicker��$ CurrentColor�  -   $/ )iud_toolbar_mouse_cursor_circle_linewidth��$��0,1,0,0q�$��23��$��40��$ 1X�$ 10X�$ 3X��� �� -   $ bt_toolbar_mouse_fixation��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $��Resource\Picture\ring.png�� �� .+��� $��Mouse fixation���� $ 
Horizontal=�$��2,1,0,0q�.+�  -   $+ %bt_toolbar_mouse_fixation_colorpicker��$��23��$	 Red���� -   $+ %tb_toolbar_mouse_fixation_colorpicker��$��1(� . $y�Hidden=�$��5������ $+ %bt_toolbar_mouse_fixation_colorpicker��$ CurrentColor�  -   $* $iud_toolbar_mouse_fixation_linewidth��$��0,1,0,0q�$��23��$��40��$ 1X�$ 10X�$ 3X�  -   $% iud_toolbar_mouse_fixation_rate��$��5,1,0,0q�$��23��$��40��$ 1X�$ 9X�$ 5X�
������� �� .+��� $G��AFixation circle will become biggerwhen this value becomes higher���� -   $ bt_toolbar_mouse_scanpath��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $#��Resource\Picture\scanpath.png�� �� .+��� $��
Mouse path���� $ 
Horizontal=�$��2,1,0,0q�.+�  -   $+ %bt_toolbar_mouse_scanpath_colorpicker��$��23��$	 Red���� -   $+ %tb_toolbar_mouse_scanpath_colorpicker��$��1(� . $y�Hidden=�$��5������ $+ %bt_toolbar_mouse_scanpath_colorpicker��$ CurrentColor�  -   $* $iud_toolbar_mouse_scanpath_linewidth��$��0,1,0,0q�$��23��$��40��$ 1X�$ 10X�$ 3X��� -   $! bt_toolbar_mouse_fixationID��$��ID��$��15(����#3D3D3D$��25��$��3,1,0,0q�$	��Top=�$��25��
������� �� .+��� $��Mouse fixation ID���� $ 
Horizontal=�$��2,1,0,0q�.+�  -   $- 'bt_toolbar_mouse_fixationID_colorpicker��$��23��$	 Red���� -   $- 'tb_toolbar_mouse_fixationID_colorpicker��$��1(� . $y�Hidden=�$��5������ $- 'bt_toolbar_mouse_fixationID_colorpicker��$ CurrentColor�c� $ 2��$ 1��$
��Left=�$��30��$	��Top=����	#FFDAE7F5.@��� -    $ img_eye��$��25��$��3,1,0,0q�$	��Top=�$��25��$��Resource\Picture\eye.png���� �� -!   $ bt_toolbar_eye_track��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $ ��Resource\Picture\track.png�� �� .+��� $��
Gaze track���� -"   $ bt_toolbar_eye_track_type��$��S��$��25��$��3,1,0,0q�$	��Top=�$��25��
���   �$��16(�
�������$
��Bold"� �� .+��� $>��8S: Track move when scrollD: Track disappear when scroll���� $ 
Horizontal=�$��2,1,0,0q�.+�  -#   $&  bt_toolbar_eye_track_colorpicker��$��23��$
 Blue���� -$   $&  tb_toolbar_eye_track_colorpicker��$��1(� . $y�Hidden=�$��5������ $&  bt_toolbar_eye_track_colorpicker��$ CurrentColor�  -%   $% iud_toolbar_eye_track_linewidth��$��0,1,0,0q�$��23��$��40��$ 1X�$ 10X�$ 3X��� �� -&   $ bt_toolbar_eye_cursor��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $!��Resource\Picture\cursor.png�� �� .+��� $��Gaze cursor���� -'   $" bt_toolbar_eye_cursor_circle��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $!��Resource\Picture\circle.png�� �� .+��� $#��Gaze cursor with circle shape���� $ 
Horizontal=�$��2,1,0,0q�.+�  -(   $. (bt_toolbar_eye_cursor_circle_colorpicker��$��23��$
 Blue���� -)   $. (tb_toolbar_eye_cursor_circle_colorpicker��$��1(� . $y�Hidden=�$��5������ $. (bt_toolbar_eye_cursor_circle_colorpicker��$ CurrentColor�  -*   $- 'iud_toolbar_eye_cursor_circle_linewidth��$��0,1,0,0q�$��23��$��40��$ 1X�$ 10X�$ 3X��� �� -+   $ bt_toolbar_eye_fixation��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $��Resource\Picture\ring.png�� �� .+��� $��Gaze fixation���� $ 
Horizontal=�$��2,1,0,0q�.+�  -,   $) #bt_toolbar_eye_fixation_colorpicker��$��23��$
 Blue���� --   $) #tb_toolbar_eye_fixation_colorpicker��$��1(� . $y�Hidden=�$��5������ $) #bt_toolbar_eye_fixation_colorpicker��$ CurrentColor�  -.   $( "iud_toolbar_eye_fixation_linewidth��$��0,1,0,0q�$��23��$��40��$ 1X�$ 10X�$ 3X�  -/   $# iud_toolbar_eye_fixation_rate��$��5,1,0,0q�$��23��$��40��$ 1X�$ 9X�$ 5X�
������� �� .+��� $G��AFixation circle will become biggerwhen this value becomes higher���� -0   $ bt_toolbar_eye_scanpath��$��25��$��3,1,0,0q�$	��Top=�$��25��
�������.���� $#��Resource\Picture\scanpath.png�� �� .+��� $��	Gaze path���� $ 
Horizontal=�$��2,1,0,0q�.+�  -1   $) #bt_toolbar_eye_scanpath_colorpicker��$��23��$
 Blue���� -2   $) #tb_toolbar_eye_scanpath_colorpicker��$��1(� . $y�Hidden=�$��5������ $) #bt_toolbar_eye_scanpath_colorpicker��$ CurrentColor�  -3   $( "iud_toolbar_eye_scanpath_linewidth��$��0,1,0,0q�$��23��$��40��$ 1X�$ 10X�$ 3X��� -4   $ bt_toolbar_eye_fixationID��$��ID��$��15(����#3D3D3D$��25��$��3,1,0,0q�$	��Top=�$��25��
������� �� .+��� $��Gaze fixation ID���� $ 
Horizontal=�$��2,1,0,0q�.+�  -5   $+ %bt_toolbar_eye_fixationID_colorpicker��$��23��$
 Blue���� -6   $+ %tb_toolbar_eye_fixationID_colorpicker��$��1(� . $y�Hidden=�$��5������ $+ %bt_toolbar_eye_fixationID_colorpicker��$ CurrentColor�� -7   $ g_replay_window��
���   �.+�7  1System.Windows.Forms.Integration.WindowsFormsHost  -8     Name$ wfh_replay_window��� -9   $ g_replay_url��$��30��
���   �.+��� $ 
Horizontal=�.+��� -:   $ tb_replay_urlname��$�� ��
���   � . 
�������$��20(�
���   ����� *ActualWidth+$ g_replay_url��� -;   $ g_replay_bottomController��$��45�����	#FFD4D4D4$��Bottom=�.+��� $ 
Horizontal=�$
��Left=�$	��672��.+��� -<   $ bt_replay_play��$	��▶��  StartButtonStyle#���  $��30��$��30��$��10,0,0,0q��� -=   $ bt_replay_stop��$	��■��  StopButtonStyle#��[ $��30��$��30��$��10,0,0,0q��� ->   $ bt_replay_pagebackward��$	��◁��  BackwardButtonStyle#��[ $��30��$��30��$��10,0,0,0q� �� .+��� $��Next webpage���� -?   $ bt_replay_pageforward��$	��▷��  ForwardButtonStyle#��[ $��30��$��30��$��10,0,0,0q� �� .+��� $��Previos webpage��� $	��327��.+��� -@    ��Name$ s_replay_timeBar��$��10,8,0,0q� � Value$ 0Q�  � Maximum$  10Q�! � Minimum$! 0Q�" �� IsSnapToTickEnabled" . # �� TickFrequency$# 1Q�$ � SmallChange$$ 1Q�% � LargeChange$% 5Q�& �� AutoToolTipPlacement$& TopLeft=�' �� AutoToolTipPrecision$' 1���� -A   ( ��Name$( lb_replay_currenttime��$��00:00:00��$��70��$��	10,10,0,0q�$��15(��� -B   $( lb_replay_totaltime��$��/��$��80��$��	-8,10,0,0q�$��15(�