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

namespace IOTGatewayHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WireupTabcontrol(MainContainer, MainContainerHeaders);

            for (int t = 0; t < 500; t++)
            {
                TxtLog.Text += t + Environment.NewLine;
            }
            
            //TxtLog.Text += "Ragji" + Environment.NewLine;
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
    }
}
