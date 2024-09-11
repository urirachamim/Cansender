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

    // IMPROVE DISCONECTIO 
    //ADD AN OPTION TO CHOSES THE BIT TO RUN THE ALIVE COUNTER 
    // ADD NICE DESIGNE 
    // ADD A COUNT LABLE TO THE ALIVE COUNTER 
    //RELEASE THE VERSION TO THE TEEM 
    
    public partial class MainWindow : Window

    {
        private int selectedDataByte = 7;
        private bool isAliveCounterEnabled = false;
        private System.Timers.Timer sendTimer;
        private const ushort canHandle = PCANBasic.PCAN_USBBUS1;  // PCAN-USB Channel 1 as a constant ushort
        private TPCANBaudrate baudRate = TPCANBaudrate.PCAN_BAUD_500K;
        private bool isSending = false;
        private int[] bitCounters = new int[8];  // Counters for each bit (0-7)
        private bool[] bitEnabled = new bool[8]; // Track if live counter is enabled for each bit (based on checkbox)


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


        private void DataByteSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataByteSelectionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Get the selected data byte (Tag contains the byte number 0-7)
                selectedDataByte = int.Parse(selectedItem.Tag.ToString());
            }
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

        private void InitializeBaudRateComboBox()

        {

            BaudRateComboBox.Items.Clear();
            BaudRateComboBox.Items.Add("500");
            BaudRateComboBox.Items.Add("250");

            // Set default baud rate to 500 kbps
            BaudRateComboBox.SelectedItem = "500";


        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

            if (BaudRateComboBox.SelectedItem == null)
            {
                // Display a message if no baud rate is selected
                MessageBox.Show("Please select a baud rate before connecting.", "Baud Rate Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Exit the method to prevent further execution
            }

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


        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the input text is a digit
            e.Handled = !IsTextNumeric(e.Text);
        }

        private bool IsTextNumeric(string text)
        {
            // Use a regular expression to check if the input is a number
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^[0-9]+$");
        }

        // Allow only letters and numbers
        private void myTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[A-Za-z0-9]$"))
            {
                // Prevent the character from being added
                e.Handled = true;
            }
        }

        private void myTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            int caretIndex = textBox.CaretIndex;



            string inputText = textBox.Text.ToUpper();

            textBox.Text = inputText;

            textBox.CaretIndex = caretIndex;
        }



        private Stopwatch stopwatch = new Stopwatch();
        private bool isConnected = false;
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

            
         if(isConnected)
            {
                
                MessageBox.Show("Please connect to the CAN bus before sending messages.");
                return;

            }

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
                    MessageBox.Show("Set baud rate.");
                });
            }
        }



        private void STOPButton_Click(object sender, RoutedEventArgs e)
        {

            if (isSending)

            {
                sendTimer.Stop();
                isSending = false;


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



        private void AliveCounterCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Enable the alive counter
            ToggleAliveCounter(true);
            

        }

        private void AliveCounterCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable the alive counter
            ToggleAliveCounter(false);
        }

        private void ToggleAliveCounter(bool enable)
        {
            isAliveCounterEnabled = enable;
        }





        int aliveCounter = 0;
        private void SendTimer_Tick(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Create a new CAN message
                TPCANMsg canMessage = new TPCANMsg();
                canMessage.ID = uint.Parse(ReadTextBox(MsgIdTextBox), System.Globalization.NumberStyles.HexNumber);
                canMessage.LEN = 8;  // Data length is always 8 for this example
                canMessage.MSGTYPE = TPCANMessageType.PCAN_MESSAGE_EXTENDED;

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



                if (isAliveCounterEnabled)
                {
                    // Apply the alive counter to the selected data byte (from 0 to 7)
                    canMessage.DATA[selectedDataByte] = (byte)(aliveCounter % 16);  // Set alive counter (0-15)
                    aliveCounter++;  // Increment the alive counter for the next cycle

                    Dispatcher.Invoke(() =>
                    {
                        Counter_mod16.Content = $"Alive Counter: {aliveCounter % 16}";
                    });

                }
                else
                {
                    // Set the value from the TextBox when the alive counter is not enabled
                    switch (selectedDataByte)
                    {
                        case 0: canMessage.DATA[0] = Convert.ToByte(ReadTextBox(DataByte0), 16); break;
                        case 1: canMessage.DATA[1] = Convert.ToByte(ReadTextBox(DataByte1), 16); break;
                        case 2: canMessage.DATA[2] = Convert.ToByte(ReadTextBox(DataByte2), 16); break;
                        case 3: canMessage.DATA[3] = Convert.ToByte(ReadTextBox(DataByte3), 16); break;
                        case 4: canMessage.DATA[4] = Convert.ToByte(ReadTextBox(DataByte4), 16); break;
                        case 5: canMessage.DATA[5] = Convert.ToByte(ReadTextBox(DataByte5), 16); break;
                        case 6: canMessage.DATA[6] = Convert.ToByte(ReadTextBox(DataByte6), 16); break;
                        case 7: canMessage.DATA[7] = Convert.ToByte(ReadTextBox(DataByte7), 16); break;
                    }
                }


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
                        // Update message count labels
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

        private string ReadTextBox(TPCANMsg canMessage)
        {
            throw new NotImplementedException();
        }

        private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
        {
         
            ConnectionStatusIndicator.Fill = new SolidColorBrush(Colors.Red);


           
            sendTimer.Stop();
            isSending = false;

            TPCANStatus result = PCANBasic.Uninitialize(canHandle);
            if (result != TPCANStatus.PCAN_ERROR_OK)
            {
                MessageBox.Show($"Error disconnecting: {result}");

            }

            BaudRateComboBox.Items.Clear();
            BaudRateComboBox.Items.Add("500");
            BaudRateComboBox.Items.Add("250");
            BaudRateComboBox.SelectedIndex = -1; // Clear the selection so the user can pick a new one

            // Enable the connect button and other controls if needed
            ConnectButton.IsEnabled = true;
            BaudRateComboBox.IsEnabled = true;
            UsbPortComboBox.IsEnabled = true;

 

        }

       
    }

    
}




