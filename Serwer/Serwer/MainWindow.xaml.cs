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
        private int PORT;
        private int BUFFER_SIZE;
        private byte[] BUFFER;
        BackgroundWorker m_oBackgroundWorker = null;
        SqlConnection database_connection = new SqlConnection(@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = D:\PROJEKTY\PRIR\Serwer\Serwer\_data\db_library.mdf; Integrated Security = True");
        private readonly Socket server_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly List<Socket> client_sockets = new List<Socket>();
        private List<Task> tasklist = new List<Task>();
        private List<string> permits = new List<string>();
        private int users_counter = 0;
        private int active_users = 0;

        public MainWindow()
        {
            ThreadPool.SetMaxThreads(20, 40);
            InitializeComponent();
            DatabaseConnection();            
        }

        private void DatabaseConnection()
        {            
            try
            {                
                database_connection.Open();
                users_counter = CheckUsers();
                lbl_AllUsers.Content = users_counter.ToString();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Main()
        {            
            m_oBackgroundWorker = new BackgroundWorker();
            m_oBackgroundWorker.DoWork += new DoWorkEventHandler(m_oBackgroundWorker_DoWork);            
            m_oBackgroundWorker.RunWorkerAsync(PORT);
        }

        private void m_oBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Setting up server..."); });
            server_socket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            server_socket.Listen(0);
            server_socket.BeginAccept(AcceptCallback, null);            
            Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Server setup complete"); });
            while (true)
            {
                try { Task.WaitAll(tasklist.ToArray()); }
                catch { }
            }
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;            
            try
            {
                socket = server_socket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            client_sockets.Add(socket);
            socket.BeginReceive(BUFFER, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Client " + socket.RemoteEndPoint + " connected, waiting for request..."); });
            updateCounterOfActiveUsers(true);
            server_socket.BeginAccept(AcceptCallback, null);
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;
            bool help = true;
            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " forcefully disconnected"); });
                updateCounterOfActiveUsers(false);                
                current.Close();
                client_sockets.Remove(current);
                return;
            }
            byte[] recBuf = new byte[received];
            Array.Copy(BUFFER, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            if (text.ToLower() == "exit") 
            {
                Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " disconnected"); });
                updateCounterOfActiveUsers(false);                
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                client_sockets.Remove(current);
                return;
            }
            else
            {
                string[] roger = null;
                try
                {
                    roger = text.Split('|');
                }
                catch
                {
                    Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " used unknown commend"); });
                }

                if (roger[0].ToLower() == "registration")
                {
                    int h = users_counter;

                    Task t = new Task(() => { Dispatcher.Invoke(() => { addUser(roger[1], roger[2], current.RemoteEndPoint.ToString()); }); });
                    tasklist.Add(SingletonSecured.Instance.AddTask(t));
                    t.Start();
                    t.Wait();

                    if (users_counter > h)
                    {
                        byte[] data = Encoding.ASCII.GetBytes("registration|correct");
                        current.Send(data);
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("registration|incorrect");
                        current.Send(data);
                    }

                }
                else if (roger[0].ToLower() == "login")
                {
                    Task t = new Task(() => { Dispatcher.Invoke(() => { LogIn(roger[1], roger[2], current.LocalEndPoint.ToString()); }); });
                    tasklist.Add(SingletonSecured.Instance.AddTask(t));
                    t.Start();
                    t.Wait();
                    if (permits.Count > 0)
                    {
                        foreach (string x in permits)
                        {
                            if (x == roger[1] + "|" + current.LocalEndPoint.ToString())
                            {
                                byte[] data = Encoding.ASCII.GetBytes("login|correct");
                                current.Send(data);
                                current.BeginReceive(BUFFER, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallbackLoged, current);
                                help = false;
                                break;
                            }
                            else
                            {
                                byte[] data = Encoding.ASCII.GetBytes("login|incorrect");
                                current.Send(data);
                            }
                        }
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("login|incorrect");
                        current.Send(data);
                    }
                }
                if (help)
                {
                    current.BeginReceive(BUFFER, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
                }
            }
        }

        private void ReceiveCallbackLoged(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;
            string name = "nazwa_pliku";
            string[] connect_data = null;
            foreach (string x in permits)
            {
                connect_data = x.Split('|');
                if (connect_data[1] == current.LocalEndPoint.ToString())
                {
                    name = connect_data[0];
                    break;
                }
            }

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " forcefully disconnected"); });
                updateCounterOfActiveUsers(false);                
                client_sockets.Remove(current);
                foreach (string x in permits)
                {
                    if (x == name + "|" + current.LocalEndPoint.ToString())
                    {
                        permits.Remove(x);
                        break;
                    }
                }
                current.Close();
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(BUFFER, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            string[] roger = null;
            try
            {
                roger = text.Split('|');
            }
            catch
            {
                Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " used unknown commend"); });
            }

            if (roger[0].ToLower() == "exit")
            {
                Dispatcher.Invoke(() => { lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " disconnected"); });
                updateCounterOfActiveUsers(false);
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                client_sockets.Remove(current);
                return;
            }
            //else if (roger[0].ToLower() == "done")
            //{
            //    Task t = new Task(() => { Dispatcher.Invoke(() => { SendToBase(name); }); });
            //    tasklist.Add(SingletonSecured.Instance.AddTask(t));
            //    t.Start();
            //}
            //else if (roger[0].ToLower() == "get")
            //{
            //    Task t = new Task(() => { Dispatcher.Invoke(() => { DownloadPersonalizedData(name, roger[1].ToString(), current.LocalEndPoint.ToString()); }); });
            //    tasklist.Add(SingletonSecured.Instance.AddTask(t));
            //    t.Start();
            //    t.Wait();
            //    string[] value = null;
            //    Monitor.Enter(locker);
            //    foreach (string x in reglist.Reverse<string>())
            //    {
            //        value = x.Split('|');
            //        if (value[4] == current.LocalEndPoint.ToString())
            //        {
            //            byte[] data = Encoding.ASCII.GetBytes("registry|" + value[0] + "|" + value[1] + "|" + value[2] + "|" + value[3]);
            //            current.Send(data);
            //            reglist.Remove(x);
            //            Thread.Sleep(300);
            //        }
            //    }
            //    Monitor.Exit(locker);
            //}
            //else if (roger[0].ToLower() == "dateget")
            //{
            //    Task t = new Task(() => { Dispatcher.Invoke(() => { DownloadAllData(name, current.LocalEndPoint.ToString()); }); });
            //    tasklist.Add(SingletonSecured.Instance.AddTask(t));
            //    t.Start();
            //    t.Wait();
            //    string[] value = null;
            //    Monitor.Enter(locker_2);
            //    foreach (string x in commits.Reverse<string>())
            //    {
            //        value = x.Split('|');
            //        if (value[1] == current.LocalEndPoint.ToString())
            //        {
            //            byte[] data = Encoding.ASCII.GetBytes(value[0]);
            //            current.Send(data);
            //            commits.Remove(x);
            //            Thread.Sleep(300);
            //        }
            //    }
            //    Monitor.Exit(locker_2);
            //}
            //else if (roger[0].ToLower() == "registry")
            //{
            //    Dispatcher.Invoke(() => { lst_spis.Items.Add(text); });

            //    FileStream f = new FileStream(name + ".txt", FileMode.Append, FileAccess.Write);
            //    StreamWriter w = new StreamWriter(f);
            //    string[] reg = null;
            //    try
            //    {
            //        reg = text.Split('|');
            //    }
            //    catch
            //    {
            //        Dispatcher.Invoke(() => { lst_spis.Items.Add("Client reg: " + current.RemoteEndPoint + " error"); });
            //    }
            //    for (int i = 1; i < reg.Length; i++)
            //    {
            //        if (i == 1)
            //        {
            //            w.Write(reg[i]);
            //        }
            //        else
            //        {
            //            w.Write("|" + reg[i]);
            //        }
            //    }
            //    w.Write("\n");
            //    w.Close();
            //    f.Close();
            //}

            current.BeginReceive(BUFFER, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallbackLoged, current);
        }

        private void btn_DefaultOptions_Click(object sender, RoutedEventArgs e)
        {
            tbx_BufforSize.Text = "2048";
            tbx_PortNumber.Text = "8888";
        }

        private void btn_StartWork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string host_name = Dns.GetHostName();
                string my_IP = Dns.GetHostByName(host_name).AddressList[2].ToString();
                int port = Int32.Parse(tbx_PortNumber.Text);
                int buffer = Int32.Parse(tbx_BufforSize.Text);
                PORT = port;
                BUFFER_SIZE = buffer;
                BUFFER = new byte[BUFFER_SIZE];
                tbx_AddressIP.Text = my_IP;
                tbx_AddressIP.IsEnabled = true;
                tbx_PortNumber.IsReadOnly = true;
                tbx_BufforSize.IsReadOnly = true;
                btn_StartWork.IsEnabled = false;
                Main();
            }
            catch
            {
                MessageBox.Show("Wprowadzone dane są nieprawidłowe. Wprowadź nowe lub uzyj ustawień domyślnych.", "Niepowodzenie.", MessageBoxButton.OK);
            }
        }

        private void addUser(string l, string p, string a)
        {
            try
            {
                string ask = "INSERT INTO Users(ID,login,password) VALUES (" + (users_counter + 1) + ",'" + l + "','" + p + "')";
                SqlCommand task = new SqlCommand(ask, database_connection);
                task.ExecuteNonQuery();
                lbx_OperationsList.Items.Add("Registration complied for client: " + a + ", username: " + l);
                lbl_AllUsers.Content = users_counter.ToString();
            }
            catch (SqlException)
            {             
                lbx_OperationsList.Items.Add("Duplicated login: " + l + ", client: " + a);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                users_counter = CheckUsers();
                lbl_AllUsers.Content = users_counter.ToString();
            }
        }

        private int CheckUsers()
        {
            try
            {
                SqlDataReader read;
                int x = 0;
                string ask = "SELECT COUNT(*) FROM Users";
                SqlCommand task = new SqlCommand(ask, database_connection);
                read = task.ExecuteReader();
                read.Read();
                x = read.GetInt32(0);
                read.Close();
                return x;
            }
            catch
            {
                MessageBox.Show("Failed to connect to the database!", "Connection error");
                return 0;
            }
        }

        private void LogIn(string l, string p, string a)
        {
            try
            {
                SqlDataReader read;
                string ask = "Select count(*) FROM Users WHERE Login ='" + l + "' AND Password='" + p + "'";
                SqlCommand task = new SqlCommand(ask, database_connection);
                read = task.ExecuteReader();
                read.Read();
                if (read.GetInt32(0) == 1)
                {
                    lbx_OperationsList.Items.Add("Pomyślnie zalogowano się na użytkownika: " + l);
                    permits.Add(l + "|" + a);
                }
                else
                {
                    lbx_OperationsList.Items.Add("Błędne dane uwierzytelniające z adresu: " + a);
                }
                read.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void updateCounterOfActiveUsers(bool x)
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
                    m_oBackgroundWorker.CancelAsync();
                }
            }
        }
    }
}
