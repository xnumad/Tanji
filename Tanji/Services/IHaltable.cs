using System.Windows.Threading;

namespace Tanji.Services
{
    public interface IHaltable
    {
        Dispatcher Dispatcher { get; }

        void Halt();
        void Restore();
    }
}