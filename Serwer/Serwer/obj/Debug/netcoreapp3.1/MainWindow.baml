   M S B A M L   `   `   `  ����A  =Serwer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:SerwerSerwer  5   	       Serwer.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   &localclr-namespace:Serwer     5   	   b ^MaterialDesignThemes.Wpf, Version=4.0.0.2422, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5   	   -   5           MaterialDesignBody#���   5	   	   $��Regular"�5
   	   $��13(�5   	   &   System.Windows.Media.TextOptions    TextFormattingMode$  Ideal=�5   	      TextRenderingMode$
 Auto=�5   	     MaterialDesignFont#���  5   	    � Title$ Serwer��5   	   $	��440��6   $	��800��6%    � 
ResizeMode$ CanMinimize=�61    � Icon$ /server.png��6J    � WindowStartupLocation$ CenterScreen=�6]   ���	#FF403E3E
�������.��5      � .+�5   
   �� $��Ustawienia serwera��6   $��16,16,16,16q�6/   $	��365��6D   $	��220��6Q   $	��Top=�6]   $
��Left=�6u     MaterialDesignGroupBox#���  6�   .  (MaterialDesignThemes.Wpf.ColorZoneAssist   Mode$ SecondaryMid=�6�   .��5      � .+�5      �� -   5         ��Name$ tbx_AddressIP��5      $��	10,10,0,0q�61   )  #MaterialDesignThemes.Wpf.HintAssist   Hint$ AdresIP��6D   .  (MaterialDesignThemes.Wpf.TextFieldAssist	   TextBoxViewMargin$	 1 0 1 0q�6m   
   FloatingScale$
 1Q�6�    ! MaterialDesignOutlinedTextBox#��[ 6�   $��20(�6   � Cursor$ IBeam��6  $��60��6(  $	��180��64  $
��Left=�6@  $	��Top=�6[     
Background �	#FF403E3E   
Foreground �	#FFAEEA00 �� 
CaretBrush �	#FFAEEA00 �� SelectionBrush �	#FFAEEA00 �� SelectionTextBrush �	#FFAEEA00   UnderlineBrush �	#FFAEEA00 �� 
IsReadOnly$
 True��6`  ${�False��6r  �� 5      -   5        $ tbx_PortNumber��5      $��	10,85,0,0q�62   $
 Port��6E   $	 1 0 1 0q�6k   $
 1Q�6�   #��[ 6�   $��20(�6	  $ IBeam��6  $��60��6&  $	��180��62  $
��Left=�6>  $	��Top=�6Y   �	#FF403E3E �	#FFAEEA00 �	#FFAEEA00 �	#FFAEEA00 �	#FFAEEA00 �	#FFAEEA006$  �� 5      -   5        $ tbx_BufforSize��5      $��
10,160,0,0q�62   $ Buffor��6F   $	 1 0 1 0q�6n   $
 1Q�6�   #��[ 6�   $��20(�6  $ IBeam��6  $��60��6)  $	��180��65  $
��Left=�6A  $	��Top=�6\   �	#FF403E3E �	#FFAEEA00 �	#FFAEEA00 �	#FFAEEA00 �	#FFAEEA00 �	#FFAEEA006'  �� 5      -   5         ��Name$ btn_DefaultOptions��5      $��
10,235,0,0q�65    $  MaterialDesignRaisedAccentButton#��[ 6I    � ToolTip$5 /Wczytaj domyślne ustawienia z pamięci serwera��6�   +  %MaterialDesignThemes.Wpf.RippleAssist   Feedback �	#DD000000$��Ustawienia domyślne��6�   $��30��6
  $	��180��6  $
��Left=�6"  $	��Top=�6=  6U  �� 5      -   5        $ btn_StartWork��5      $��
10,280,0,0q�60   #��[ 6D   $# Rozpocznij działanie serwera��6~    �	#DD000000$��Start��6�   $��30��6�   $	��180��6�   $
��Left=�6�   $	��Top=�6  61  5      5      �� 5   
   $!��Informacje o użytkownikach��6   $��268,16,16,16q�67   $	��110��6M   $	��500��6Z   $	��Top=�6f   $
��Left=�6~   #���  6�   $ SecondaryMid=�6�   .��5      � .+�5      �� $.��(Ilość zarejestrowanych użytkowników:��6   $	��Top=�6H   $��30��6`   
�������$	��230��6   $
��Left=�6�   �� 5      $'��!Ilość aktywnych użytkowników:��6   $��0,30,0,0q�6@   $	��Top=�6S   $��30��6k   
�������$	��190��6�   $
��Left=�6�   �� 5      -   5         ��Name$ lbl_AllUsers��5      $�� ��6.   $��	230,0,0,0q�69   $	��Top=�6M   $��30��6e   
�������$
��Left=�6�   �� 5      -   5        $ lbl_ActiveUsers��5      $��0��61   $��
190,30,0,0q�6=   $	��Top=�6R   $��30��6j   
�������$
��Left=�6�   5       5!      �� 5"   
   $��Historia operacji��6   $��268,148,16,16q�6.   $	��233��6E   $	��500��6R   $	��Top=�6^   $
��Left=�6v   #���  6�   $ SecondaryMid=�6�   .��5#      �� -	   5         ��Name$ lbx_OperationsList��5#        MaterialDesignListBox#��[ 63   $��12(�6b   
�������6p   5$      5%      5&      