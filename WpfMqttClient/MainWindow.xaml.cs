using GalaSoft.MvvmLight.Messaging;
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

namespace WpfMqttClient
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            OutputBox.TextChanged += OnOutputBoxTextChanged;
        }

        private void OnOutputBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            OutputBox.ScrollToEnd();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Messenger.Default.Send(new WpfMqttClient.ViewModel.MainViewModel.DoCleanupMessage());
            base.OnClosing(e);
        }
    }
}
