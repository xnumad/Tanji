using Tanji.Helpers;

namespace Tanji.Services.Toolbox
{
    public class ToolboxViewModel : ObservableObject
    {
        private int _int32Value = int.MaxValue;
        public int Int32Value
        {
            get => _int32Value;
            set
            {
                _int32Value = value;
                RaiseOnPropertyChanged();
            }
        }

        private ushort _uint16Value = ushort.MaxValue;
        public ushort UInt16Value
        {
            get => _uint16Value;
            set
            {
                _uint16Value = value;
                RaiseOnPropertyChanged();
            }
        }
    }
}