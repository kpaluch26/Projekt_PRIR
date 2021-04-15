using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Net;

namespace Serwer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        Configuration config;
        BackgroundWorker m_oBackgroundWorker = null;
        SqlConnection database_connection = new SqlConnection(@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = D:\PROJEKTY\PRIR\Serwer\Serwer\_data\db_library.mdf; Integrated Security = True");        
        public List<Task> tasklist = new List<Task>();       
        private int users_counter = 0;
        private int active_users = 0;

        public MainWindow()
        {
            ThreadPool.SetMaxThreads(20, 40);
            InitializeComponent();

            database_connection = DatabaseOrder.DatabaseConnection(database_connection);
            if (database_connection != null)
            {
                users_counter = DatabaseOrder.CheckUsers(database_connection);
                lbl_AllUsers.Content = users_counter.ToString();
            }
            else
            {
                this.Close();
            }
        }

        private void Main()
        {            
            m_oBackgroundWorker = new BackgroundWorker();
            m_oBackgroundWorker.DoWork += new DoWorkEventHandler(m_oBackgroundWorker_DoWork);            
            m_oBackgroundWorker.RunWorkerAsync(config.GetPort());
        }

        private void m_oBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Setting up server..."); });
            ClientOrder client = new ClientOrder(config, this, database_connection);
            client.ListenStart();
            Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Server setup complete"); });
            while (true)
            {
                try { Task.WaitAll(tasklist.ToArray()); }
                catch { }

                if (m_oBackgroundWorker.CancellationPending)
                {
                    client.ListenStop();
                    e.Cancel = true;
                    return;
                }
            }
        }

        
        private void btn_DefaultOptions_Click(object sender, RoutedEventArgs e)
        {
            tbx_BufforSize.Text = "512";
            tbx_PortNumber.Text = "7564";
        }

        private void btn_StartWork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string host_name = Dns.GetHostName();
                string my_IP = Dns.GetHostByName(host_name).AddressList[1].ToString();
                int port = Int32.Parse(tbx_PortNumber.Text);
                int buffer = Int32.Parse(tbx_BufforSize.Text);

                config = new Configuration(port, buffer, my_IP);
                
                tbx_AddressIP.Text = my_IP;
                tbx_AddressIP.IsEnabled = true;
                tbx_PortNumber.IsReadOnly = true;
                tbx_BufforSize.IsReadOnly = true;
                btn_StartWork.IsEnabled = false;
                btn_DefaultOptions.IsEnabled = false;

                Main();
            }
            catch
            {               
                MessageBox.Show("Wprowadzone dane są nieprawidłowe. Wprowadź nowe lub uzyj ustawień domyślnych.", "Niepowodzenie.", MessageBoxButton.OK);
            }
        }

        public void updateCounterOfActiveUsers(bool x)
        {
            if (x)
            {
                Dispatcher.Invoke(() => { lbl_ActiveUsers.Content = ++active_users; });
            }
            else
            {
                Dispatcher.Invoke(() => { lbl_ActiveUsers.Content = --active_users; });
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Czy na pewno chcesz zamknąć aplikację serwera?", "Serwer", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) { e.Cancel = true; }
            else
            {
                database_connection.Close();
                if (m_oBackgroundWorker != null && m_oBackgroundWorker.IsBusy)
                {
                    //m_oBackgroundWorker.CancelAsync();
                }
            }
        }
    }
}
