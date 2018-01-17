using System.Windows;
using System.ComponentModel;

namespace Tanji.Windows.Logger
{
    public partial class LoggerView : Window
    {
        public LoggerView()
        {
            InitializeComponent();

            var vm = (LoggerViewModel)DataContext;
            Activated += vm.LoggerActivated;
            Closing += vm.LoggerClosing;
        }
        
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;

            base.OnClosing(e);
        }
    }
}