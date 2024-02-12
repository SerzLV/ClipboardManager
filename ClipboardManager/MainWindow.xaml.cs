using System.ComponentModel;
using System.Windows;
using MahApps.Metro.Controls;

namespace ClipboardManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        MainWindowViewModel _mwvm = new MainWindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _mwvm;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowClipboardManager = new Helper.ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += _mwvm.ClipboardContextChange;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            this._mwvm.SaveCommand.Execute(null);
        }
    }
}