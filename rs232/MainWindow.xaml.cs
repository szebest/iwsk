using rs232.DataContext;
using rs232.EnumTypes;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;

namespace rs232
{
    public class ComboBoxItem<T>
    {
        public int Id { get; set; }
        public T Value { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowContext windowContext;

        private SerialPort _usedPort;
        private string PING_MESSAGE = "PING";
        private string PONG_MESSAGE = "PONG";
        private bool isWaitingForDSR = false;
        private bool isWaitingForPong = false;
        private CancellationTokenSource dsrTaskCancelToken = null;
        private CancellationTokenSource pingTaskCancelToken = null;
        private Stopwatch pingSw = null;

        private int _numValue = 0;

        public int NumValue
        {
            get { return _numValue; }
            set
            {
                _numValue = value;
                txtNum.Text = value.ToString();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            _usedPort = new SerialPort();
            _usedPort.Encoding = Encoding.GetEncoding(28591);
            _usedPort.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);

            windowContext = new MainWindowContext
            {
                ConnectionSettings = new ConnectionSettings(),
                WindowData = new WindowData()
            };

            DataContext = windowContext;

            pingSw = new Stopwatch();

            var savedTimeout = Settings.GetAppSettings("pingTimeout");
            NumValue = savedTimeout?.Length > 0 ? int.Parse(savedTimeout) : 1000;

            var comPorts = new List<ComboBoxItem<string>>();
            foreach (var (value, index) in SerialPort.GetPortNames().Select((value, i) => (value, i)))
                comPorts.Add(new ComboBoxItem<string> { Id = index, Value = value});
            portCbx.ItemsSource = comPorts;
            var portSetting = Settings.GetAppSettings("comPort");
            portCbx.SelectedIndex = comPorts.Find((item) => item.Value == portSetting)?.Id ?? 0;

            var baudRates = new List<ComboBoxItem<int>> 
            {
                new ComboBoxItem<int> { Id = 0, Value = 110 },
                new ComboBoxItem<int> { Id = 1, Value = 300 },
                new ComboBoxItem<int> { Id = 2, Value = 600 },
                new ComboBoxItem<int> { Id = 3, Value = 1200 },
                new ComboBoxItem<int> { Id = 4, Value = 2400 },
                new ComboBoxItem<int> { Id = 5, Value = 4800 },
                new ComboBoxItem<int> { Id = 6, Value = 9600 },
                new ComboBoxItem<int> { Id = 7, Value = 14400 },
                new ComboBoxItem<int> { Id = 8, Value = 19200 },
                new ComboBoxItem<int> { Id = 9, Value = 38400 },
                new ComboBoxItem<int> { Id = 10, Value = 57600 },
                new ComboBoxItem<int> { Id = 11, Value = 115200 },
            };
            baudCbx.ItemsSource = baudRates;
            var baudSetting = int.Parse(Settings.GetAppSettings("baudRate") ?? "0");
            baudCbx.SelectedIndex = baudRates.Find((item) => item.Value == baudSetting)?.Id ?? 0;

            var dataBits = new List<ComboBoxItem<int>>
            {
                new ComboBoxItem<int> { Id = 0, Value = 7 },
                new ComboBoxItem<int> { Id = 1, Value = 8 },
            };
            dataBitsCbx.ItemsSource = dataBits;
            var dataBitsSetting = int.Parse(Settings.GetAppSettings("dataBits") ?? "0");
            dataBitsCbx.SelectedIndex = dataBits.Find((item) => item.Value == dataBitsSetting)?.Id ?? 0;

            var parityTypes = new List<ComboBoxItem<string>>();
            foreach (int index in Enum.GetValues(typeof(ParityEnum)))
                parityTypes.Add(new ComboBoxItem<string> { Id = index, Value = ((ParityEnum)index).ToString() });
            parityCbx.ItemsSource = parityTypes;
            var paritySetting = Settings.GetAppSettings("parity");
            parityCbx.SelectedIndex = parityTypes.Find((item) => item.Value == paritySetting)?.Id ?? 0;

            var stopBits = new List<ComboBoxItem<int>>()
            {
                new ComboBoxItem<int> { Id = 0, Value = 1 },
                new ComboBoxItem<int> { Id = 1, Value = 2 },
            };
            stopBitsCbx.ItemsSource = stopBits;
            var stopBitsSetting = int.Parse(Settings.GetAppSettings("stopBits") ?? "0");
            stopBitsCbx.SelectedIndex = stopBits.Find((item) => item.Value == stopBitsSetting)?.Id ?? 0;

            var stopSymbolTypes = new List<ComboBoxItem<string>>();
            foreach (int index in Enum.GetValues(typeof(StopSymbolEnum)))
                stopSymbolTypes.Add(new ComboBoxItem<string> { Id = index, Value = ((StopSymbolEnum)index).ToString() });
            stopSymbolCbx.ItemsSource = stopSymbolTypes;
            var stopSymbolSetting = Settings.GetAppSettings("stopSymbol");
            var found = stopSymbolTypes.FindIndex((item) => item.Value == stopSymbolSetting);
            stopSymbolCbx.SelectedIndex = found >= 0 ? found : 0;

            var transmissionControlTypes = new List<ComboBoxItem<string>>();
            foreach (int index in Enum.GetValues(typeof(DataIOControlEnum)))
                transmissionControlTypes.Add(new ComboBoxItem<string> { Id = index, Value = ((DataIOControlEnum)index).ToString() });
            transmissionControlCbx.ItemsSource = transmissionControlTypes;
            var transmissionControlSetting = Settings.GetAppSettings("transmissionControl");
            transmissionControlCbx.SelectedIndex = transmissionControlTypes.Find((item) => item.Value == transmissionControlSetting)?.Id ?? 0;

            if (((ComboBoxItem<string>)stopSymbolCbx.SelectedItem).Value == StopSymbolEnum.Custom.ToString())
                customStopSymbolInput.Text = Settings.GetAppSettings("stopSymbolHex");

            saveCurrentSelectedSettings();

            windowContext.WindowData.saveButtonEnabled = false;
        }

        private void sendTimer_Elapsed()
        {
            if (isWaitingForDSR)
            {
                Dispatcher.Invoke(() =>
                {
                    WriteLine($"Brak odpowiedzi od urządzenia", MessageEnum.Error);

                    windowContext.WindowData.sendButtonEnabled = true;
                });
            }
        }

        private void pingTimer_Elapsed()
        {
            if (isWaitingForPong)
            {
                windowContext.WindowData.pingButtonEnabled = true;
                isWaitingForPong = false;

                Dispatcher.Invoke(() =>
                {
                    WriteLine($"Brak odpowiedzi na ping w przeciągu {windowContext.ConnectionSettings.pingTimeout}ms", MessageEnum.Error);
                });
            }
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                long ms = 0;
                if (isWaitingForPong)
                {
                    pingSw.Stop();
                    ms = pingSw.ElapsedMilliseconds;
                }
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadLine();

                Dispatcher.Invoke(() =>
                {
                    bool write = true;

                    if (indata == PING_MESSAGE)
                    {
                        Task.Run(() => SendMessage(PONG_MESSAGE));
                    }
                    else if (indata == PONG_MESSAGE)
                    {
                        if (isWaitingForPong)
                        {
                            isWaitingForPong = false;
                            windowContext.WindowData.pingButtonEnabled = true;
                            Dispatcher.Invoke(() =>
                            {
                                pingTaskCancelToken?.Cancel();
                            });

                            indata += " " + ms + " ms";
                        }
                        else
                        {
                            write = false;
                        }
                    }

                    if (write)
                    {
                        WriteLine(indata, MessageEnum.Received);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void FormDataChangedHandler(object sender, SelectionChangedEventArgs e)
        {
            var senderComboBox = (ComboBox)sender;
            if (senderComboBox?.Name == "stopSymbolCbx")
            {
                windowContext.WindowData.customSymbolInputEnabled = ((ComboBoxItem<string>)senderComboBox.SelectedItem).Value == StopSymbolEnum.Custom.ToString();
            }
            if (windowContext == null)
                return;
            windowContext.WindowData.saveButtonEnabled = true;
        }

        private void saveCurrentSelectedSettings()
        {
            windowContext.ConnectionSettings.comPort = ((ComboBoxItem<string>)portCbx.SelectedItem).Value;
            windowContext.ConnectionSettings.baudRate = ((ComboBoxItem<int>)baudCbx.SelectedItem).Value;
            windowContext.ConnectionSettings.dataBits = ((ComboBoxItem<int>)dataBitsCbx.SelectedItem).Value;
            windowContext.ConnectionSettings.parity = ((ComboBoxItem<string>)parityCbx.SelectedItem).Value;
            windowContext.ConnectionSettings.stopBits = ((ComboBoxItem<int>)stopBitsCbx.SelectedItem).Value;
            windowContext.ConnectionSettings.stopSymbol = ((ComboBoxItem<string>)stopSymbolCbx.SelectedItem).Value;
            windowContext.ConnectionSettings.transmissionControl = ((ComboBoxItem<string>)transmissionControlCbx.SelectedItem).Value;
            windowContext.ConnectionSettings.pingTimeout = NumValue;

            var stopSymbol = windowContext.ConnectionSettings.stopSymbol == StopSymbolEnum.Custom.ToString() ? customStopSymbolInput.Text : Convert.ToString(((ComboBoxItem<string>)stopSymbolCbx.SelectedItem).Id, 16).ToUpper();
            windowContext.ConnectionSettings.stopSymbolHex = !string.IsNullOrEmpty(stopSymbol) ? (stopSymbol.Length % 2 != 0 ? "0" + stopSymbol : stopSymbol) : "00"; // Default stop symbol

            UpdatePortValues();
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            saveCurrentSelectedSettings();

            windowContext.WindowData.saveButtonEnabled = false;
        }

        private void UpdatePortValues()
        {
            if (_usedPort.IsOpen) _usedPort.Close();
            _usedPort.PortName = windowContext.ConnectionSettings.comPort;
            _usedPort.Parity = (Parity)Enum.Parse(typeof(ParityEnum), windowContext.ConnectionSettings.parity);
            _usedPort.StopBits = windowContext.ConnectionSettings.stopBits == 1 ? StopBits.One : StopBits.Two;
            _usedPort.BaudRate = windowContext.ConnectionSettings.baudRate;
            _usedPort.DataBits = windowContext.ConnectionSettings.dataBits;
            _usedPort.NewLine = HexadecimalToASCII(windowContext.ConnectionSettings.stopSymbolHex);
            switch ((DataIOControlEnum)Enum.Parse(typeof(DataIOControlEnum), windowContext.ConnectionSettings.transmissionControl))
            {
                case DataIOControlEnum.None:
                    _usedPort.Handshake = Handshake.None;
                    break;
                case DataIOControlEnum.RTS_CTS:
                    _usedPort.Handshake = Handshake.RequestToSend;
                    break;
                case DataIOControlEnum.DTR_DSR:
                    _usedPort.Handshake = Handshake.None;
                    _usedPort.DtrEnable = true;
                    break;
                case DataIOControlEnum.Program:
                    _usedPort.Handshake = Handshake.XOnXOff;
                    break;
            }
            _usedPort.ReadTimeout = 10000;
            _usedPort.WriteTimeout = 10000;
            try
            {
                _usedPort.Open();
                windowContext.WindowData.sendButtonEnabled = true;
                windowContext.WindowData.pingButtonEnabled = true;
            }
            catch(Exception e)
            {
                WriteLine($"Port {windowContext.ConnectionSettings.comPort} jest zajęty", MessageEnum.Error);
                _usedPort.Close();
                windowContext.WindowData.sendButtonEnabled = false;
                windowContext.WindowData.pingButtonEnabled = false;
            }
        }

        private void sendDataBtn_Click(object sender, RoutedEventArgs e)
        {
            windowContext.WindowData.sendButtonEnabled = false;

            string text = DataToBeSent.Text;

            Task.Run(() => SendMessage(text));

            DataToBeSent.Text = "";
        }

        private void pingBtn_Click(object sender, RoutedEventArgs e)
        {
            windowContext.WindowData.pingButtonEnabled = false;
            isWaitingForPong = true;

            pingTaskCancelToken = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                pingSw = Stopwatch.StartNew();
                Thread.Sleep(NumValue);
                pingTimer_Elapsed();
            }, pingTaskCancelToken.Token);

            Task.Run(() => SendMessage(PING_MESSAGE));
        }

        private void WriteLine(string message, MessageEnum type)
        {
            string prefix = type switch
            {
                MessageEnum.Sent => "(Wysłano): ",
                MessageEnum.Received => "(Odebrano): ",
                MessageEnum.Error => "(Error): ",
                _ => "(Nieznany typ): "
            };

            Dispatcher.Invoke(() =>
            {
                TextRange prefixTextRange = new TextRange(InfoDataBox.Document.ContentEnd, InfoDataBox.Document.ContentEnd)
                {
                    Text = prefix
                };
                prefixTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, type switch
                {
                    MessageEnum.Sent => Brushes.LightBlue,
                    MessageEnum.Received => Brushes.LightGreen,
                    MessageEnum.Error => Brushes.Red,
                    _ => Brushes.Red
                });

                TextRange contentMessage = new TextRange(InfoDataBox.Document.ContentEnd, InfoDataBox.Document.ContentEnd)
                {
                    Text = message + '\n'
                };
                contentMessage.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);

                InfoDataBox.ScrollToEnd();
            });
        }

        private void SendMessage(string message)
        {
            if (windowContext.ConnectionSettings.transmissionControl == DataIOControlEnum.DTR_DSR.ToString())
            {
                _usedPort.DtrEnable = false;
                isWaitingForDSR = true;
                dsrTaskCancelToken = new CancellationTokenSource();
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(1000);
                    sendTimer_Elapsed();
                }, dsrTaskCancelToken.Token);

                while (isWaitingForDSR)
                {
                    if (_usedPort.DsrHolding)
                    {
                        try
                        {
                            _usedPort.WriteLine(message);
                            WriteLine(message, MessageEnum.Sent);
                            Dispatcher.Invoke(() =>
                            {
                                dsrTaskCancelToken?.Cancel();
                            });
                        }
                        catch (Exception e)
                        {
                            WriteLine("Nie udało się wysłać wiadomości", MessageEnum.Error);
                        }

                        break;
                    }
                }

                windowContext.WindowData.sendButtonEnabled = true;
                _usedPort.DtrEnable = true;
                isWaitingForDSR = false;
            }
            else
            {
                try
                {
                    _usedPort.WriteLine(message);
                    WriteLine(message, MessageEnum.Sent);
                }
                catch (Exception e)
                {
                    WriteLine("Nie udało się wysłać wiadomości", MessageEnum.Error);
                }

                windowContext.WindowData.sendButtonEnabled = true;
            }
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            NumValue++;
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            NumValue--;
        }

        private void txtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtNum == null)
            {
                return;
            }

            FormDataChangedHandler(null, null);

            if (!int.TryParse(txtNum.Text, out _numValue))
                txtNum.Text = _numValue.ToString();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            InfoDataBox.Document.Blocks.Clear();
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int hexNumber;
            e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);

            this.FormDataChangedHandler(null, null);
        }

        private static string HexadecimalToASCII(string hex)
        {
            string ascii = string.Empty;

            for (int i = 0; i < hex.Length; i += 2)
            {
                ascii += (char)HexadecimalToDecimal(hex.Substring(i, 2));
            }

            return ascii;
        }

        private static int HexadecimalToDecimal(string hex)
        {
            hex = hex.ToUpper();

            int hexLength = hex.Length;
            double dec = 0;

            for (int i = 0; i < hexLength; ++i)
            {
                byte b = (byte)hex[i];

                if (b >= 48 && b <= 57)
                    b -= 48;
                else if (b >= 65 && b <= 70)
                    b -= 55;

                dec += b * Math.Pow(16, ((hexLength - i) - 1));
            }

            return (int)dec;
        }
    }
}
