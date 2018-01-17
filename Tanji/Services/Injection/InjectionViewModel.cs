using System.Threading.Tasks;
using System.Collections.ObjectModel;

using Tanji.Helpers;

namespace Tanji.Services.Injection
{
    public class InjectionViewModel : ObservableObject
    {
        public AsyncCommand SendToClientCommand { get; }
        public AsyncCommand SendToServerCommand { get; }

        public ObservableCollection<string> PacketSignatures { get; }

        private string _packetSignature;
        public string PacketSignature
        {
            get => _packetSignature;
            set
            {
                _packetSignature = value;
                RaiseOnPropertyChanged();
            }
        }

        public InjectionViewModel()
        {
            PacketSignatures = new ObservableCollection<string>();
            SendToServerCommand = new AsyncCommand(SendToServerAsync, CanSendData);
            SendToClientCommand = new AsyncCommand(SendToClientAsync, CanSendData);
        }

        private bool CanSendData(object obj)
        {
            if (string.IsNullOrWhiteSpace(PacketSignature))
            {
                return false;
            }
            return (App.Master?.Connection.IsConnected ?? true);
        }
        private async Task SendToClientAsync(object arg)
        {
            AddSignature(PacketSignature);
            await App.Master.Connection.SendToClientAsync(PacketSignature);
        }
        private async Task SendToServerAsync(object arg)
        {
            AddSignature(PacketSignature);
            await App.Master.Connection.SendToServerAsync(PacketSignature);
        }

        private void AddSignature(string signature)
        {
            if (!PacketSignatures.Contains(signature))
            {
                PacketSignatures.Add(signature);
            }
        }
    }
}