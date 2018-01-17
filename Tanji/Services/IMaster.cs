using Tanji.Network;

using Sulakore.Habbo;
using Sulakore.Modules;

namespace Tanji.Services
{
    public interface IMaster : IInstaller
    {
        new HGame Game { get; set; }
        new HConnection Connection { get; }

        void AddReceiver(IReceiver receiver);
        void AddHaltable(IHaltable haltable);
        void AddSynchronizer(ISynchronizer synchronizer);

        void Synchronize(HGame game);
        void Synchronize(HGameData gameData);
    }
}