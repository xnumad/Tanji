using System.Windows.Input;
using System.Windows.Controls;

using Tanji.Services.Modules.Models;

namespace Tanji.Services.Modules
{
    public partial class ModulesView : UserControl
    {
        public ModulesView()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var module = (((ListViewItem)sender).Content as ModuleInfo);
            module.Initialize();

            e.Handled = true;
        }
    }
}