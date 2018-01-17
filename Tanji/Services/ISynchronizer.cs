using Sulakore.Habbo;

namespace Tanji.Services
{
    public interface ISynchronizer
    {
        void Synchronize(HGame game);
        void Synchronize(HGameData gameData);
    }
}