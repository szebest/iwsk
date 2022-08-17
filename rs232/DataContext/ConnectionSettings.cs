using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Configuration;

namespace rs232.DataContext
{
    public class ConnectionSettings : INotifyPropertyChanged
    {
        private string _comPort;
        private int _baudRate;
        private int _dataBits;
        private string _parity;
        private int _stopBits;
        private string _stopSymbol;
        private string _stopSymbolHex;
        private string _transmissionControl;
        private int _pingTimeout;

        public string comPort
        {
            get
            {
                return _comPort;
            }

            set
            {
                if (value == _comPort)
                    return;

                _comPort = value;
                NotifyPropertyChanged(_comPort.ToString());
            }
        }
        public int baudRate
        {
            get
            {
                return _baudRate;
            }

            set
            {
                if (value == _baudRate)
                    return;

                _baudRate = value;
                NotifyPropertyChanged(_baudRate.ToString());
            }
        }
        public int dataBits
        {
            get
            {
                return _dataBits;
            }

            set
            {
                if (value == _dataBits)
                    return;

                _dataBits = value;
                NotifyPropertyChanged(_dataBits.ToString());
            }
        }
        public string parity
        {
            get
            {
                return _parity;
            }

            set
            {
                if (value == _parity)
                    return;

                _parity = value;
                NotifyPropertyChanged(_parity);
            }
        }
        public int stopBits
        {
            get
            {
                return _stopBits;
            }

            set
            {
                if (value == _stopBits)
                    return;

                _stopBits = value;
                NotifyPropertyChanged(_stopBits.ToString());
            }
        }
        public string stopSymbol
        {
            get
            {
                return _stopSymbol;
            }

            set
            {
                if (value == _stopSymbol)
                    return;

                _stopSymbol = value;
                NotifyPropertyChanged(_stopSymbol);
            }
        }
        public string stopSymbolHex
        {
            get
            {
                return _stopSymbolHex;
            }

            set
            {
                if (value == _stopSymbolHex)
                    return;

                _stopSymbolHex = value;
                NotifyPropertyChanged(_stopSymbolHex);
            }
        }
        public string transmissionControl
        {
            get
            {
                return _transmissionControl;
            }

            set
            {
                if (value == _transmissionControl)
                    return;

                _transmissionControl = value;
                NotifyPropertyChanged(_transmissionControl);
            }
        }
        public int pingTimeout
        {
            get
            {
                return _pingTimeout;
            }

            set
            {
                if (value == _pingTimeout)
                    return;

                _pingTimeout = value;
                NotifyPropertyChanged(_pingTimeout.ToString());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string value, [CallerMemberName] string propertyName = "")
        {
            Settings.AddUpdateAppSettings(propertyName, value);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
