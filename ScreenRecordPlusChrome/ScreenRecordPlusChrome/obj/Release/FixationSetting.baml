   M S B A M L   `   `   `  ���� X  TXceed.Wpf.Toolkit, Version=3.2.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f4P:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.ToolkitXceed.Wpf.Toolkit   ScreenRecordPlusChrome,   &ScreenRecordPlusChrome.FixationSetting   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     NwpfTool:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.Toolkit     -     � Title$  Fixation Setting��$	��340��$	��280��
�������.��� .+��� $��5,5,5,5q�.+��� $
��Wrap=�$��Spatial Threshold��$��14(�$��0,0,0,5q��� 
�������$��2q�$��0,0,0,10q�._��� $��2,2,2,2q�.+��� $
��Wrap=�$��0,0,0,10q�$���~Maximum distance in pixels that a point may vary from the average fixation point and still be considered part of the fixation.����  �� Orientation$ 
Horizontal=�$��0,0,0,5q�$��Center=�.+��� $
��Wrap=�$
��Gaze��$��0,0,10,0q�%   Xceed.Wpf.Toolkit.IntegerUpDown  -     Name$' !iud_fixationSetting_gaze_distance���   �Xceed.Wpf.Toolkit.Primitives.UpDownBase`1[[System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]   Value$ 20X�$��0,0,20,0q�$��60���� $
��Wrap=�$��Mouse��$��0,0,10,0q�  -   $( "iud_fixationSetting_mouse_distance��$ 20X�$��60���� $
��Wrap=�$��Temporal Threshold��$��14(�$��0,0,0,5q��� 
�������$��2q�$��0,0,0,10q�._��� $��2,2,2,2q�.+��� $
��Wrap=�$��0,0,0,10q�$Y��SMinimum duration of consecutive points that can be considered part of the fixation.���� $ 
Horizontal=�$��0,0,0,5q�$��Center=�.+��� $
��Wrap=�$
��Gaze��$��0,0,10,0q�  -   $# iud_fixationSetting_gaze_time��$	 250X�$��0,0,20,0q�$��60���� $
��Wrap=�$��Mouse��$��0,0,10,0q�  -   $$ iud_fixationSetting_mouse_time��$	 250X�$��60��� .+�� -    �Name$# pb_fixationSetting_processbar�� � Value$ 0Q� � Maximum$	 100Q� � Minimum$ 0Q�$��0,0,0,10q�$��20�� � ToolTip �� -   	 ��Name$(	 "tooltip_fixationSetting_processbar��$��0���� -   $#	 tb_fixationSetting_processbar��$	��0/0��$��Center=��� $ 
Horizontal=�$��Center=�.+��� -	   
 ��Name$"
 bt_fixationSetting_calculate��$��	Calculate��$��80��$��0,0,25,0q��� -
   $
 bt_fixationSetting_cancel��$��Cancel��$��80��$��25,0,0,0q�