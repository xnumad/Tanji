using Tanji.Helpers;
using Tanji.Services;
using Tanji.Windows.Logger;

namespace Tanji.Windows.Main
{
    public class MainViewModel : ObservableObject, IHaltable
    {
        private readonly LoggerView _loggerView;

        private string _title = "Tanji - Disconnected";
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isAlwaysOnTop = true;
        public bool IsAlwaysOnTop
        {
            get => _isAlwaysOnTop;
            set
            {
                _isAlwaysOnTop = value;
                RaiseOnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            if (App.Master != null)
            {
                _loggerView = new LoggerView();
            }
        }

        public void Halt()
        {
            IsAlwaysOnTop = true;
            Title = "Tanji - Disconnected";
        }
        public void Restore()
        {
            IsAlwaysOnTop = _loggerView.Topmost;
            Title = $"Tanji - Connected[{App.Master.Connection.Remote.EndPoint}]";
        }
    }
}