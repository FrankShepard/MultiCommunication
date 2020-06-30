using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiCommunication
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}
		 		
		string serial_name = string.Empty;//串口名
		int serial_baudrate = 0; //串口波特率

		bool[] address_bits_send = new bool[ 36 ];
		bool[] address_bits_received = new bool[ 36 ];
		byte[] datas_send = new byte[ 36 ];
		byte[] datas_received = new byte[ 36 ];
		int send_data_count = 0;
		int received_data_count = 0;

		bool use_multi_communication = true;

		private void CobBaudrate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			ComboBox cob = sender as ComboBox;
			cob.Items.Clear();
			cob.Items.Add( "1200" );
			cob.Items.Add( "2400" );
			cob.Items.Add( "4800" );
			cob.Items.Add( "9600" );
			cob.Items.Add( "19200" );
			cob.Items.Add( "38400" );
			cob.Items.Add( "57600" );
			cob.Items.Add( "115200" );
		}

		private void CobBaudrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cob = sender as ComboBox;
			if (cob.SelectedIndex < 0) {
				serial_baudrate = 0;
				return;
			}

			serial_baudrate = Convert.ToInt32( cob.SelectedItem );
		}

		private void CobSerialport_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			ComboBox cob = sender as ComboBox;
			/*获取当前电脑上的串口集合，并将其显示在cob控件中*/
			string[] SerialPortlist = SerialPort.GetPortNames();
			cob.Items.Clear();
			foreach (string name in SerialPortlist) {
				SerialPort serialPort = new SerialPort( name, 9600, Parity.Mark, 8, StopBits.One );
				try {
					serialPort.Open();
					serialPort.Close();
					cob.Items.Add( name );
				} catch {
					;
				}
			}
		}

		private void CobSerialport_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cob = sender as ComboBox;
			if(cob.SelectedIndex < 0) {
				serial_name = string.Empty;
				return;
			}

			serial_name = cob.SelectedItem.ToString();
		}

		private void CheckBox_KeyDown(object sender, KeyEventArgs e)
		{
			if(!((e.Key == Key.Enter) || (e.Key == Key.Up) || (e.Key == Key.Down) || (e.Key == Key.Tab))) {
				e.Handled = true;
				return;
			}

			if (e.Key == Key.Enter) {
				CheckBox chk = sender as CheckBox;
				if (( bool )chk.IsChecked == false) {
					chk.IsChecked = true;
				} else {
					chk.IsChecked = false;
				}
			}
		}

		private void BtnSend_Click(object sender, RoutedEventArgs e)
		{
			if((serial_name == string.Empty) || (serial_baudrate == 0)) {
				MessageBox.Show( "请选择正常的串口名及波特率" );
				return;
			}

			if((bool)chkUseMulti.IsChecked == false) {
				use_multi_communication = false;
			}else {
				use_multi_communication = true;
			}

			//检查需要下发的数据的相关状态
			for (int index = 1; index <= WrpSend.Children.Count; index += 3) {
				CheckBox checkBox = WrpSend.Children[ index ] as CheckBox;
				if((bool)checkBox.IsChecked == false) {
					address_bits_send[ index / 3 ] = false;
				}else {
					address_bits_send[ index / 3 ] = true;
				}
			}
			//向下发送数据
			for (int index = 2; index <= WrpSend.Children.Count; index += 3) {
				TextBox textBox = WrpSend.Children[ index ] as TextBox;
				if(textBox.Text != string.Empty) {
					send_data_count++;
					//将16进制数据转换为十进制保存
					byte value = 0;
					int high = 0, low = 0;
					if((textBox.Text[0] >= 'A') && (textBox.Text[0] <= 'F')) {
						high = textBox.Text[ 0 ] - 'A' + 10;
					}else if((textBox.Text[0] > '0') && (textBox.Text[0] <= '9')) {
						high = textBox.Text[ 0 ] - '0';
					}

					if ((textBox.Text[ 1 ] >= 'A') && (textBox.Text[ 1 ] <= 'F')) {
						low = textBox.Text[ 1 ] - 'A' + 10;
					} else if ((textBox.Text[ 1 ] > '0') && (textBox.Text[ 1 ] <= '9')) {
						low = textBox.Text[ 1 ] - '0';
					}

					value = Convert.ToByte( 16 * high + low );

					datas_send[ index / 3 ] = value;
				} else {
					break;
				}
			}

			//实际进行下发

		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if(!(((e.Key >= Key.A) && (e.Key <= Key.F)) || ((e.Key >= Key.D0) && (e.Key <= Key.D9)) || ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9)) || (e.Key == Key.Enter) || (e.Key == Key.Back) || (e.Key ==Key.Delete) || (e.Key == Key.Left) || (e.Key == Key.Right) || (e.Key == Key.Tab))) {
				e.Handled = true;
				return;
			}
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if(textBox.Text != string.Empty) {
				string value = textBox.Text.ToUpper();
				if (value.Length == 1) {
					textBox.Text = "0";
					textBox.Text += value;
				} else { 
					textBox.Text = value;
				}
			}
		}
	}
}
