using System.ComponentModel;
using System.Windows.Threading;
using System.Runtime.CompilerServices;

using Tanji.Services;

namespace Tanji.Helpers
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public Dispatcher Dispatcher { get; }

        public ObservableObject()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            if (App.Master != null)
            {
                var haltable = (this as IHaltable);
                if (haltable != null)
                {
                    App.Master.AddHaltable(haltable);
                }
                var receiver = (this as IReceiver);
                if (receiver != null)
                {
                    App.Master.AddReceiver(receiver);
                }
                var synchronizer = (this as ISynchronizer);
                if (synchronizer != null)
                {
                    App.Master.AddSynchronizer(synchronizer);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        protected void RaiseOnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}