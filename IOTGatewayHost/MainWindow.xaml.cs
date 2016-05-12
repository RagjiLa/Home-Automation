using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using IOTGatewayHost.Business_Logic;

namespace IOTGatewayHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private object _syncLock = new object();
        public MainWindow()
        {
            InitializeComponent();

            WireupTabcontrol(MainContainer, MainContainerHeaders);

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((App)Application.Current).ApiHost.Stop();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Logged += Logger_Logged;
            ((App)Application.Current).ApiHost.Start();
        }

        private void Logger_Logged(object sender, LoggedArgs e)
        {
            lock (_syncLock)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TxtLog.AppendText(e.Message);
                    TxtScrollViewer.ScrollToEnd();
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private static void WireupTabcontrol(TabControl container, ItemsControl headers)
        {
            var tabHeaders = new List<MyHeader>();
            var actionObject = new TabNavigateCommand(container);
            foreach (TabItem tab in container.Items)
            {
                tabHeaders.Add(new MyHeader
                {
                    HeaderText = tab.Header.ToString(),
                    NavigateAction = actionObject,
                    Index = container.Items.IndexOf(tab),
                    Checked = container.Items.IndexOf(tab) == 0
                });
            }
            headers.ItemsSource = tabHeaders;
        }

        public class MyHeader
        {
            public string HeaderText { get; set; }
            public int Index { get; set; }
            public TabNavigateCommand NavigateAction { get; set; }
            public bool Checked { get; set; }
        }

        public class TabNavigateCommand : ICommand
        {
            private readonly TabControl _target;
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                var tabIndex = ((MyHeader)parameter).Index;
                _target.SelectedIndex = tabIndex;

            }

            public TabNavigateCommand(TabControl targetTabControl)
            {
                _target = targetTabControl;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
