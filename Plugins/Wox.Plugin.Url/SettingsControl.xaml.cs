using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wox.Plugin.Url
{
    /// <summary>
    /// SettingsControl.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private Settings _settings;
        public SettingsControl(Settings settings)
        {
            InitializeComponent();
            _settings = settings;
        }

        private void OnApplyBTClick(object sender, RoutedEventArgs e)
        {
            _settings.BrowserPath = browserPathBox.Text;
        }
    }
}
