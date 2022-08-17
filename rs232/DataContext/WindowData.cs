using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace rs232.DataContext
{
    public class WindowData : INotifyPropertyChanged
    {
        private bool _saveButtonEnabled = false;
        private bool _pingButtonEnabled = true;
        private bool _sendButtonEnabled = true;
        private bool _customSymbolInputEnabled = false;

        public bool saveButtonEnabled
        {
            get
            {
                return _saveButtonEnabled;
            }

            set
            {
                if (value == _saveButtonEnabled)
                    return;

                _saveButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool pingButtonEnabled
        {
            get
            {
                return _pingButtonEnabled;
            }

            set
            {
                if (value == _pingButtonEnabled)
                    return;

                _pingButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool sendButtonEnabled
        {
            get
            {
                return _sendButtonEnabled;
            }

            set
            {
                if (value == _sendButtonEnabled)
                    return;

                _sendButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool customSymbolInputEnabled
        {
            get
            {
                return _customSymbolInputEnabled;
            }

            set
            {
                if (value == _customSymbolInputEnabled)
                    return;

                _customSymbolInputEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
