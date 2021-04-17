using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Serwer
{
    class ClientOrder
    {
        private readonly Socket server_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private Configuration config;
        private byte[] BUFFER;
        private readonly List<Socket> client_sockets = new List<Socket>();
        private List<string> permits = new List<string>();
        private MainWindow MW;
        SqlConnection database_connection;        
        static object permits_locker = new object();


        public ClientOrder(Configuration _c, MainWindow _mw, SqlConnection _dc)
        {
            this.config = _c;
            this.MW = _mw;
            this.database_connection = _dc;
            BUFFER = new byte[config.GetBuffer()];
        }
        
        public void ListenStart()
        {
            server_socket.Bind(new IPEndPoint(IPAddress.Any, config.GetPort()));
            server_socket.Listen(0);
            server_socket.BeginAccept(AcceptCallback, null);
        }

        public void ListenStop()
        {
            server_socket.Close();
            database_connection.Close();
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
            socket.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallback, socket);
            MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client " + socket.RemoteEndPoint + " connected, waiting for request..."); });            
            MW.updateCounterOfActiveUsers(true);
            server_socket.BeginAccept(AcceptCallback, null);
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;
            bool bool_return = false;
            int int_return = 0;
            string _login, _password, _ip;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " forcefully disconnected"); });
                MW.updateCounterOfActiveUsers(false);
                current.Close();
                client_sockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(BUFFER, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            if (text.ToLower() == "exit")
            {
                MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " disconnected"); });
                MW.updateCounterOfActiveUsers(false);
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
                    MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " used unknown commend"); });
                }

                if (roger[0].ToUpper() == "REG" && roger.Length == 3)
                {
                    _login = roger[1];
                    _password = roger[2];
                    _ip = current.RemoteEndPoint.ToString();

                    Task t = new Task(() => { MW.Dispatcher.Invoke(() => { int_return = DatabaseOrder.AddUser(database_connection, _login, _password); }); });
                    MW.tasklist.Add(SingletonSecured.Instance.AddTask(t));
                    t.Start();
                    t.Wait();

                    if (int_return == 0)
                    {
                        MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Registration complied for client: " + _ip + ", username: " + _login); });
                        MW.Dispatcher.Invoke(() => { MW.lbl_AllUsers.Content = int_return.ToString(); });
                        byte[] data = Encoding.ASCII.GetBytes("REG|TRUE");
                        current.Send(data);
                        current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallback, current);
                    }
                    else if (int_return == 1)
                    {
                        MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Duplicated login: " + _login + ", client: " + _ip); });
                        byte[] data = Encoding.ASCII.GetBytes("REG|FALSE");
                        current.Send(data);
                        current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallback, current);
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes("REG|FALSE");
                        current.Send(data);
                        current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallback, current);
                    }
                }
                else if (roger[0].ToUpper() == "LOG" && roger.Length == 3)
                {
                    _login = roger[1];
                    _password = roger[2];
                    _ip = current.RemoteEndPoint.ToString();

                    Task t = new Task(() => { MW.Dispatcher.Invoke(() => { bool_return = DatabaseOrder.LogIn(database_connection, _login, _password); }); });
                    MW.tasklist.Add(SingletonSecured.Instance.AddTask(t));
                    t.Start();
                    t.Wait();

                    if (bool_return)
                    {
                        MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Pomyślnie zalogowano się na użytkownika: " + _login); });                        
                        byte[] data = Encoding.ASCII.GetBytes("LOG|TRUE");
                        current.Send(data);

                        Monitor.Enter(permits_locker);
                        permits.Add(_login + "|" + current.RemoteEndPoint.ToString());
                        Monitor.Exit(permits_locker);

                        current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallbackLoged, current );
                    }
                    else
                    {
                        MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Błędne dane uwierzytelniające z adresu: " + _ip); });
                        byte[] data = Encoding.ASCII.GetBytes("LOG|FALSE");
                        current.Send(data);
                        current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallback, current);
                    }                    
                }
                else
                {
                    current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallback, current);
                }
            }
        }

        private void ReceiveCallbackLoged(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;
            string clientname = "nazwa_uzytkownika";
            string[] connect_data = null;
            bool bool_return=false;

            current.RemoteEndPoint.ToString();

            Monitor.Enter(permits_locker);
            foreach (string x in permits)
            {
                connect_data = x.Split('|');
                if (connect_data[1] == current.RemoteEndPoint.ToString())
                {
                    clientname = connect_data[0];
                    break;
                }
            }
            Monitor.Exit(permits_locker);

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " forcefully disconnected"); });
                MW.updateCounterOfActiveUsers(false);
                client_sockets.Remove(current);
                foreach (string x in permits)
                {
                    if (x == clientname + "|" + current.LocalEndPoint.ToString())
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
                MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " used unknown commend"); });
            }
            if (roger[0].ToUpper() == "EXIT")
            {
                MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " disconnected"); });
                MW.updateCounterOfActiveUsers(false);
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                client_sockets.Remove(current);
                return;
            }
            else if (roger.Length == 2 && roger[0].ToUpper() == "CONTENT" && roger[1].ToUpper() == "ALLBOOKS")
            {
                List<string> library = new List<string>();

                Task t = new Task(() => { MW.Dispatcher.Invoke(() => { library = DatabaseOrder.DownloadContent(database_connection); }); });
                MW.tasklist.Add(SingletonSecured.Instance.AddTask(t));
                t.Start();
                t.Wait();

                foreach (string _book in library)
                {
                    byte[] data = Encoding.ASCII.GetBytes(_book);
                    current.Send(data);
                }
                MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " download library content."); });
                byte[] data_end = Encoding.ASCII.GetBytes("CONTENT|END");
                current.Send(data_end);
                current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallbackLoged, current);
            }
            else if (roger.Length == 2 && roger[0].ToUpper() == "CONTENT" && roger[1].ToUpper() == "MYBOOKS")
            {
                List<string> my_library = new List<string>();

                Task t = new Task(() => { MW.Dispatcher.Invoke(() => { my_library = DatabaseOrder.DownloadMyContent(database_connection, clientname); }); });
                MW.tasklist.Add(SingletonSecured.Instance.AddTask(t));
                t.Start();
                t.Wait();

                foreach (string _book in my_library)
                {
                    byte[] data = Encoding.ASCII.GetBytes(_book);
                    current.Send(data);
                }
                MW.Dispatcher.Invoke(() => { MW.lbx_OperationsList.Items.Add("Client: " + current.RemoteEndPoint + " download library content."); });
                byte[] data_end = Encoding.ASCII.GetBytes("CONTENT|END");
                current.Send(data_end);
                current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallbackLoged, current);
            }
            else if (roger[0].ToUpper() == "ORDER" && roger.Length == 2)
            {
                int x = Int32.Parse(roger[1].ToString());
                Task t = new Task(() => { MW.Dispatcher.Invoke(() => { bool_return = DatabaseOrder.OrderBook(database_connection, x, clientname); }); });
                MW.tasklist.Add(SingletonSecured.Instance.AddTask(t));
                t.Start();
                t.Wait();

                if (bool_return)
                {
                    byte[] data = Encoding.ASCII.GetBytes("ORDER|TRUE");
                    current.Send(data);                    
                }
                else
                {
                    byte[] data = Encoding.ASCII.GetBytes("ORDER|FALSE");
                    current.Send(data);                    
                }
                current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallbackLoged, current);
            }
            else if(roger[0].ToUpper() == "RETURN" && roger.Length == 2)
            {
                int x = Int32.Parse(roger[1].ToString());
                Task t = new Task(() => { MW.Dispatcher.Invoke(() => { bool_return = DatabaseOrder.ReturnBook(database_connection, x, clientname); }); });
                MW.tasklist.Add(SingletonSecured.Instance.AddTask(t));
                t.Start();
                t.Wait();

                if (bool_return)
                {
                    byte[] data = Encoding.ASCII.GetBytes("RETURN|TRUE");
                    current.Send(data);
                }
                else
                {
                    byte[] data = Encoding.ASCII.GetBytes("RETURN|FALSE");
                    current.Send(data);
                }
                current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallbackLoged, current);
            }
            else if (roger[0].ToUpper() == "RESERVE" && roger.Length == 2)
            {
                int x = Int32.Parse(roger[1].ToString());
                Task t = new Task(() => { MW.Dispatcher.Invoke(() => { bool_return = DatabaseOrder.ReserveBook(database_connection, x, clientname); }); });
                MW.tasklist.Add(SingletonSecured.Instance.AddTask(t));
                t.Start();
                t.Wait();

                if (bool_return)
                {
                    byte[] data = Encoding.ASCII.GetBytes("RESERVE|TRUE");
                    current.Send(data);
                }
                else
                {
                    byte[] data = Encoding.ASCII.GetBytes("RESERVE|FALSE");
                    current.Send(data);
                }
                current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallbackLoged, current);
            }            

            current.BeginReceive(BUFFER, 0, config.GetBuffer(), SocketFlags.None, ReceiveCallbackLoged, current);
        }
    }
}
