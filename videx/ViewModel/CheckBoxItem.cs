using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace videx.ViewModel
{
    public class CheckBoxItem : INotifyPropertyChanged
    {

        public ObservableCollection<string> CheckedItems { get; set; }
        public ObservableCollection<CheckBoxItem> CheckBoxItems { get; set; }

        private string _content;
        private bool _isChecked;
        private bool _isEnabled = true;
        private Visibility _visibility = Visibility.Visible;

        public string Content
        {
            get { return _content; }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));

                    OnPropertyChanged(nameof(CheckBoxItems));
                }
            }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    OnPropertyChanged(nameof(Visibility));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
