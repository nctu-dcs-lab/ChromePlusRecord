   M S B A M L   `   `   `  ����X  TXceed.Wpf.Toolkit, Version=3.2.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f4P:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.ToolkitXceed.Wpf.Toolkit  5   	    ScreenRecordPlusChrome'   !ScreenRecordPlusChrome.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   NwpfTool:clr-namespace:Xceed.Wpf.Toolkit;assembly=Xceed.Wpf.Toolkit     5   	     � Title$  ChromeRecord��5   	   $	��246��6   $	��411��6+    � Icon$ favicon.ico��67   .��5      � $	��217��6   $��Bottom=�6   $��	0,-6,-8,1q�63    � RowDefinitions 5   
   �� 5       � ColumnDefinitions 5
   
   �� 5      .+�5   
   �� $��VideoCodec:��6   $
��Left=�6&   $��	12,25,0,0q�6A   $	��Top=�6T   $��25��6l   $��82��6x   �� 5   
   $��Screens:��6   $
��Left=�6#   $��	38,55,0,0q�6>   $	��Top=�6Q   $��25��6i   $��56��6u   �� 5   
   $��BitRate:��6   $
��Left=�6#   $��
233,25,0,0q�6>   $	��Top=�6R   $��25��6j   $��53��6v   �� 5   
   $
��FPS:��6   $
��Left=�6   $��
253,55,0,0q�6:   $	��Top=�6N   $��25��6f   $��33��6r   �� 5   
   -   5         ��Name$ lb_stopWatch��5      $��00:00:00.0000000��6&   $
��Left=�6A   $	��Top=�6\   $��24(�6t   $��37��6�   $	��212��6�   $��
129,85,0,0q�6�   $��Microsoft YaHei UI)�6�   �� 5   
   $��Save in folder:��6   $
��Left=�6*   $��
12,108,0,0q�6E   $	��Top=�6Y   $��25��6q   $��89��6}   �� 5   
   -   5         ��Name$ cb_VideoCodec��5      $
��Left=�6*   $��	99,29,0,0q�6E   $	��Top=�6X   $	��120��6p   $��21��6|   �� 5   
   -   5        $ cb_screenSelector��5      $
��Left=�6.   $��	99,59,0,0q�6I   $	��Top=�6\   $	��120��6t   $��21��6�   �� 5   
   -   5        $ 
cb_BitRate��5      $
��Left=�6'   $��
291,29,0,0q�6B   $	��Top=�6V   $��85��6n   $��21��6y   �� 5   
   -   5         ��Name$ tb_SaveFolder��5      $
��Left=�6)   $��21��6D   $��
21,137,0,0q�6P    �� TextWrapping$
 Wrap=�6d   $	��D:/��6x   $	��Top=�6�   $	��362��6�   %   Xceed.Wpf.Toolkit.DecimalUpDown  5   
   -   5          Name$ nud_FPS��5       �   �Xceed.Wpf.Toolkit.NumericUpDown`1[[System.Nullable`1[[System.Decimal, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]	   FormatString$	 F0��61   �   �Xceed.Wpf.Toolkit.Primitives.UpDownBase`1[[System.Nullable`1[[System.Decimal, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
   Value$
 10X�6C      	Increment$ 1X�6N      Maximum$ 30X�6\      Minimum$ 10X�6i   $��291,59,28,137q�6v   �� 5   
   -   5        $
 lb_1��5      $��Frames: ��6   $
��Left=�61   $��
12,159,0,0q�6L   $	��Top=�6`   $��25��6x   $	��111��6�   �� 5   
   -   5         ��Name$ bt_Save��5      $
��Stop��6"   $
��Left=�61   $��251,174,0,0q�6L   $	��Top=�6a   $��56��6y   $��30��6�   6�   �� 5   
   -	   5        $ bt_start��5      $��Start��6#   $
��Left=�63   $��327,174,0,0q�6N   $	��Top=�6c   $��56��6{   $��30��6�   6�   5      5      