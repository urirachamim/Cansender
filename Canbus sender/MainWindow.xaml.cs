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

namespace Canbus_sender
{

    
    
    public partial class MainWindow : Window
    {
        private DispatcherTimer sendTimer;
        private const ushort canHandle = PCANBasic.PCAN_USBBUS1;  // PCAN-USB Channel 1 as a constant ushort
        private TPCANBaudrate baudRate = TPCANBaudrate.PCAN_BAUD_500K;
        private bool isSending = false;



        public MainWindow()
        {
            InitializeComponent();
            sendTimer = new DispatcherTimer();
            sendTimer.Interval = TimeSpan.FromMilliseconds(20);
            sendTimer.Tick += SendTimer_Tick;

        }


        private void PopulateUsbPorts()
        {
            List<string> availablePorts = new List<string>();

            // Assuming 1-16 range for PCAN-USB
            for (ushort i = 1; i <= 16; i++)
            {
                TPCANStatus status = PCANBasic.GetStatus((ushort)(PCANBasic.PCAN_USBBUS1 + i - 1));
                if (status == TPCANStatus.PCAN_ERROR_OK)
                {
                    availablePorts.Add($"PCAN-USB {i}");  // Adapter found, add to list
                }
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
                MessageBox.Show("Failed to connect.");
            }
        }

        private Stopwatch stopwatch = new Stopwatch();

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure the input is a valid integer
            if (int.TryParse(CycleTimeTextBox.Text, out int cycleTime) && cycleTime > 0)
            {
                sendTimer.Interval = TimeSpan.FromMilliseconds(cycleTime);

                // Start sending messages if not already started
                if (!isSending)
                {
                    sendTimer.Start();
                    isSending = true;
                    MessageBox.Show("Started sending messages.");
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid cycle time in milliseconds.");
            }
        }



        // Send button click event
        private int messageCount = 0;  // Counter to track number of messages sent

        private void SendTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Create a new CAN message
                TPCANMsg canMessage = new TPCANMsg();
                canMessage.ID = Convert.ToUInt32(MsgIdTextBox.Text, 16);
                canMessage.LEN = 8;
                canMessage.MSGTYPE = TPCANMessageType.PCAN_MESSAGE_STANDARD;

                // Initialize the DATA array with 8 bytes
                canMessage.DATA = new byte[8];
                canMessage.DATA[0] = Convert.ToByte(DataByte0.Text, 16);
                canMessage.DATA[1] = Convert.ToByte(DataByte1.Text, 16);
                canMessage.DATA[2] = Convert.ToByte(DataByte2.Text, 16);
                canMessage.DATA[3] = Convert.ToByte(DataByte3.Text, 16);
                canMessage.DATA[4] = Convert.ToByte(DataByte4.Text, 16);
                canMessage.DATA[5] = Convert.ToByte(DataByte5.Text, 16);
                canMessage.DATA[6] = Convert.ToByte(DataByte6.Text, 16);
                canMessage.DATA[7] = Convert.ToByte(DataByte7.Text, 16);

                // Send the CAN message
                TPCANStatus result = PCANBasic.Write(canHandle, ref canMessage);

                if (result != TPCANStatus.PCAN_ERROR_OK)
                {
                    MessageBox.Show("Failed to send CAN message.");
                    sendTimer.Stop();  // Stop sending if there's an error
                    isSending = false;
                }
                else
                {
                    messageCount++;  // Increment the message count
                                     // Optionally update a UI element to display the count
                                     // MessageBox.Show($"Message sent. Count: {messageCount}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
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

          
            if (result == TPCANStatus.PCAN_ERROR_OK)
            {
               // MessageBox.Show("Disconnected from CAN bus.");
            }
            else
            {
                MessageBox.Show("Failed to disconnect from CAN bus.");
            }

            // Reset baud rate to indicate no selection or default state
            baudRate = TPCANBaudrate.PCAN_BAUD_1M; // or any default value you choose to represent "no selection"

            // Optionally, you can reset other related states or UI elements here
        }

    }
}




