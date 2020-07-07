using System;
using System.IO.Ports;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;

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
		

		bool use_multi_communication = true;
		bool use_modbus_crc16 = false;
		bool send_completed = false;
		int last_count = 0;

		//定时器，用于循环显示串口接收到的数据
		System.Timers.Timer timer;

		SerialPort serialPort;

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
			if(timer != null) {
				timer.Enabled = false; //暂时不使用定时器循环检查
			}
			if(serialPort != null) {
				if (serialPort.IsOpen) {
					serialPort.Close();
				}
			}
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

			if ((bool)chkUseMulti.IsChecked == false) {
				use_multi_communication = false;
			}else {
				use_multi_communication = true;
			}

			if((bool)chkUseModbusCRC16.IsChecked == false) {
				use_modbus_crc16 = false;
			} else {
				use_modbus_crc16 = true;
			}

			int send_data_count = 0;

			//获取需要下发的数据及数量
			for (int index = 2; index <= WrpSend.Children.Count; index += 3) {
				TextBox textBox = WrpSend.Children[ index ] as TextBox;
				if(textBox.Text != string.Empty) {					
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
					send_data_count++;
				} else {
					break;
				}
			}

			if (use_modbus_crc16) {//CRC16的额外两个字节
				ushort crc_value = ModBusCRC16( ref datas_send, send_data_count );
				TextBox textBox;
				if (( bool )chkLowByteFirst.IsChecked) {
					datas_send[ send_data_count ] = ( byte )(crc_value);
					textBox = WrpSend.Children[ 3 * send_data_count + 2 ] as TextBox;
					textBox.Text = datas_send[ send_data_count ].ToString( "X2" );
					datas_send[ ++send_data_count ] = ( byte )((crc_value & 0xFF00) >> 8);
					textBox = WrpSend.Children[ 3 * send_data_count + 2 ] as TextBox;
					textBox.Text = datas_send[ send_data_count ].ToString( "X2" );
					send_data_count++;
				} else {
					datas_send[ send_data_count ] = ( byte )((crc_value & 0xFF00) >> 8);
					textBox = WrpSend.Children[ 3 * send_data_count +2 ] as TextBox;
					textBox.Text = datas_send[ send_data_count ].ToString( "X2" );
					datas_send[++ send_data_count ] = ( byte )(crc_value);
					textBox = WrpSend.Children[ 3 * send_data_count +2 ] as TextBox;
					textBox.Text = datas_send[ send_data_count ].ToString( "X2" );
					send_data_count++;
				}
			}

			//检查需要下发的数据的相关状态
			int address_count = 0;
			for ( int index = 1 ; index <= WrpSend.Children.Count ; index += 3 ) {
				CheckBox checkBox = WrpSend.Children [ index ] as CheckBox;
				if ( ( bool ) checkBox.IsChecked == false ) {
					address_bits_send [ index / 3 ] = false;
				} else {
					address_bits_send [ index / 3 ] = true;
				}
				if(++address_count >= send_data_count ) {
					break;
				}
			}
			//实际进行下发,串口校验模式需要根据地址位与否进行反复设置
			try {
				if (timer != null) {  //新发送数据时，需要重新初始化串口，暂停对串口数据的循环检测
					timer.Enabled = false;
				}
				//先正常关闭串口
				if( serialPort != null) {
					if (serialPort.IsOpen) {
						serialPort.Close();
					}
				}

				bool use_mark = false; //是否为地址字节的标志
				Parity last_parity = Parity.None;
				if (use_multi_communication) {
					if (address_bits_send[ 0 ]) {
						last_parity = Parity.Mark;
						use_mark = true;
					} else {
						last_parity = Parity.Space;
						use_mark = false;
					}
				}
				serialPort = new SerialPort ( serial_name, serial_baudrate, last_parity, 8, StopBits.One );				
				serialPort.Open();

				if (use_multi_communication) {
					int index = 0;
					int same_parity_count = 0;
					do {
						if(address_bits_send[index] == use_mark) {
							same_parity_count++;
						} else {
							byte[] send_temp = new byte[ same_parity_count ];
							Buffer.BlockCopy( datas_send, index - 1, send_temp, 0, same_parity_count );
							serialPort.Write( send_temp, 0, same_parity_count );
							same_parity_count = 1;
							use_mark = address_bits_send[ index ];
							if(use_mark == false) {
								serialPort.Close();
								serialPort.Parity = Parity.Space;
							} else {
								serialPort.Close();
								serialPort.Parity = Parity.Mark;
							}
							serialPort.Open();
						}
					} while (++index < send_data_count);

					if (same_parity_count == send_data_count) {
						serialPort.Write( datas_send, 0, same_parity_count );
					} else {
						//最后一次地址标志与否的变化的数据尚未发送，在此处发送
						byte[] send_temp = new byte[ same_parity_count ];
						Buffer.BlockCopy( datas_send, send_data_count - same_parity_count , send_temp, 0, same_parity_count );
						serialPort.Write( send_temp, 0, same_parity_count);
					}

				} else {
					serialPort.Write( datas_send, 0, send_data_count );
				}
				send_completed = true; //标记发送完成，需要在接下来的100ms时间内获取回码

				if (timer == null) {
					//开启定时器，用于实时刷新进度条、测试环节、测试项、测试值
					timer = new System.Timers.Timer( 10 );   //实例化Timer类，设置间隔时间单位毫秒
					timer.Elapsed += new ElapsedEventHandler( UpdateWork ); //到达时间的时候执行事件；     
					timer.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；     
					timer.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件； 
				} else {
					timer.Enabled = true; //允许重新使能定时器检测
				}

			} catch (Exception ex){
				MessageBox.Show ( "串口发送过程中出现问题，请重新发送\r\n" + ex.ToString() );
			}
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

		#region -- 定时器操作

		private delegate void dlg_ReceivedDataShow( byte[] showed_value, int visable_index_max);

		private void ReceivedDataShow(byte[] showed_value, int visable_index_max)
		{
			int index = 0;
			for(index = 0;index < showed_value.Length; index++) {
				if(index < visable_index_max) {
					TextBox textBox = WrpReceived.Children[ 3 * index + 2 ] as TextBox;
					textBox.Text = showed_value[index].ToString( "X2" );

					WrpReceived.Children[ 3 * index ].Visibility = Visibility.Visible;
					WrpReceived.Children[ 3 * index + 1 ].Visibility = Visibility.Visible;
					WrpReceived.Children[ 3 * index + 2 ].Visibility = Visibility.Visible;
				} else {
					WrpReceived.Children[ 3 * index ].Visibility = Visibility.Hidden;
					WrpReceived.Children[ 3 * index + 1 ].Visibility = Visibility.Hidden;
					WrpReceived.Children[ 3 * index + 2 ].Visibility = Visibility.Hidden;
				}
			}
		}

		/// <summary>
		/// 定时器中执行委托用于显示实时情况
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateWork(object sender, ElapsedEventArgs e)
		{
			//			if( send_completed ) {
			//int count = 0;
			//do {
			//	Thread.Sleep ( 5 );
			//} while ( ( serialPort.BytesToRead == 0 ) && ( ++count < 20 ) );

			//last_count = serialPort.BytesToRead;
			//while ( ( serialPort.BytesToRead != last_count ) && ( ++count < 20 ) ) {
			//	Thread.Sleep ( 5 );
			//	last_count = serialPort.BytesToRead;
			//}

			if ((serialPort.BytesToRead != last_count) || (serialPort.BytesToRead == 0)) {
				last_count = serialPort.BytesToRead;
				return;
			}
			int received_data_count = Math.Min( serialPort.BytesToRead, datas_received.Length );
			last_count = 0;
			//刷新显示
			serialPort.Read( datas_received, 0, received_data_count );
			serialPort.ReadExisting();
			Dispatcher.Invoke( new dlg_ReceivedDataShow( ReceivedDataShow ), datas_received, received_data_count );			
			//			}
		}

		#endregion


		ushort ModBusCRC16(ref byte[] cmd, int len)
		{
			ushort i, j, tmp, CRC16;

			CRC16 = 0xFFFF;             //CRC寄存器初始值
			for (i = 0; i < len; i++) {
				CRC16 ^= cmd[ i ];
				for (j = 0; j < 8; j++) {
					tmp = ( ushort )(CRC16 & 0x0001);
					CRC16 >>= 1;
					if (tmp == 1) {
						CRC16 ^= 0xA001;    //异或多项式
					}
				}
			}
			return CRC16;
		}
	}
}
