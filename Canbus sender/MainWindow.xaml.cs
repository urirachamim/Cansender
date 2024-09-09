using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Peak.Can.Basic.BackwardCompatibility;
using Peak.Can.Basic;
using System.Windows.Threading;
using System.Diagnostics;
using System.Timers;


namespace Canbus_sender
{

    
    
    public partial class MainWindow : Window
    {

        private System.Timers.Timer sendTimer;
        private const ushort canHandle = PCANBasic.PCAN_USBBUS1;  // PCAN-USB Channel 1 as a constant ushort
        private TPCANBaudrate baudRate = TPCANBaudrate.PCAN_BAUD_500K;
        private bool isSending = false;



        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            PopulateUsbPorts();

        }



        private void InitializeTimer()
        {
            sendTimer = new System.Timers.Timer();  // Use System.Timers.Timer
            sendTimer.Elapsed += SendTimer_Tick; // Hook the event for each tick
            //sendTimer.AutoReset = true; // Ensure the timer repeats
            //sendTimer.Enabled = false; // Initially disabled
        }

        private void PopulateUsbPorts()
        {
            List<string> availablePorts = new List<string>();

            // Assuming 1-16 range for PCAN-USB
            for (ushort i = 1; i <= 4; i++)
            {
                TPCANStatus status = PCANBasic.GetStatus((ushort)(PCANBasic.PCAN_USBBUS1 + i - 1));
                availablePorts.Add($"PCAN-USB {i}");  // Adapter found, add to list
            }

            UsbPortComboBox.ItemsSource = availablePorts;
            UsbPortComboBox.SelectedIndex = 0; // Optionally set default selection
        }



        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the baud rate based on ComboBox selection
            if (BaudRateComboBox.SelectedItem.ToString() == "500")
                baudRate = TPCANBaudrate.PCAN_BAUD_500K;
            else if (BaudRateComboBox.SelectedItem.ToString() == "250")
                baudRate = TPCANBaudrate.PCAN_BAUD_250K;

            // Initialize the connection to the CAN bus without casting
            TPCANStatus result = PCANBasic.Initialize(canHandle, baudRate);

            // Check the connection status
            if (result == TPCANStatus.PCAN_ERROR_OK)
            {
                ConnectionStatusIndicator.Fill = new SolidColorBrush(Colors.Green); // Change indicator to green
                //MessageBox.Show("Connected to CAN bus.");
            }
            else
            {
                ConnectionStatusIndicator.Fill = new SolidColorBrush(Colors.Red); // Red indicates failure
                MessageBox.Show("Check connections.");
            }
        }





        private Stopwatch stopwatch = new Stopwatch();

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure the input is a valid integer
            if (int.TryParse(CycleTimeTextBox.Text, out int cycleTime) && cycleTime > 0)
            {
                sendTimer.Interval = cycleTime;

                // Start sending messages if not already started
                if (!isSending)
                {

                    sendTimer.Start();
                    isSending = true;
                    //MessageBox.Show("Started sending messages.");
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Please enter a valid cycle time in milliseconds.");
                });
            }
        }


        private int messageCount = 0;  // Counter to track number of messages sent

        string ReadTextBox(TextBox tb)
        {
            string text = "1";
            Dispatcher.Invoke(() =>
            {
                text = tb.Text;
            });
            return text;
        }
        private void SendTimer_Tick(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Create a new CAN message
                TPCANMsg canMessage = new TPCANMsg();
                canMessage.ID = uint.Parse(ReadTextBox(MsgIdTextBox), System.Globalization.NumberStyles.HexNumber);
                canMessage.LEN = 8;  // Data length is always 8 for this example
                canMessage.MSGTYPE = TPCANMessageType.PCAN_MESSAGE_STANDARD;

                // Initialize the DATA array with 8 bytes
                canMessage.DATA = new byte[8];
                canMessage.DATA[0] = Convert.ToByte(ReadTextBox(DataByte0), 16);
                canMessage.DATA[1] = Convert.ToByte(ReadTextBox(DataByte1), 16);
                canMessage.DATA[2] = Convert.ToByte(ReadTextBox(DataByte2), 16);
                canMessage.DATA[3] = Convert.ToByte(ReadTextBox(DataByte3), 16);
                canMessage.DATA[4] = Convert.ToByte(ReadTextBox(DataByte4), 16);
                canMessage.DATA[5] = Convert.ToByte(ReadTextBox(DataByte5), 16);
                canMessage.DATA[6] = Convert.ToByte(ReadTextBox(DataByte6), 16);
                canMessage.DATA[7] = Convert.ToByte(ReadTextBox(DataByte7), 16);

                // Send the CAN message
                TPCANStatus result = PCANBasic.Write(canHandle, ref canMessage);

                if (result != TPCANStatus.PCAN_ERROR_OK)
                {
                    // Use Dispatcher to update the UI safely
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Failed to send CAN message.");
                    });
                    sendTimer.Stop();  // Stop sending if there's an error
                    isSending = false;
                }
                else
                {
                    messageCount++;
                    // Use Dispatcher to update the UI safely
                    Dispatcher.Invoke(() =>
                    {
                        // Update message count label
                        MessageCountLabel.Content = $"Messages Sent: {messageCount}";
                    });
                }
            }
            catch (Exception ex)
            {
                // Use Dispatcher to update the UI safely
                Dispatcher.Invoke(() =>
                {
                    //MessageBox.Show($"Error sending message: {ex.Message}");
                });
                sendTimer.Stop();
                isSending = false;
            }
        }


              


       

        private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
        {
         
            ConnectionStatusIndicator.Fill = new SolidColorBrush(Colors.Red);

           
            sendTimer.Stop();
            isSending = false;

          
            TPCANStatus result = PCANBasic.Uninitialize(canHandle);


          
            baudRate = TPCANBaudrate.PCAN_BAUD_1M; 

           
        }

    }
}




