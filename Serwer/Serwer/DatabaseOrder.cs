using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace Serwer
{
    class DatabaseOrder
    {
        private static List<string> commits = new List<string>();
        private static List<string> recommits = new List<string>();
        static object commits_locker = new object();
        static object recommits_locker = new object();
        public static SqlConnection DatabaseConnection(SqlConnection _sql)
        {
            try
            {
                _sql.Open();
                return _sql;                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return null;
            }
        }

        public static int CheckUsers(SqlConnection _sql)
        {           
            string ask = "SELECT COUNT(*) FROM Users";
            SqlCommand task = new SqlCommand(ask, _sql);
            SqlDataReader read = task.ExecuteReader();
            read.Read();
            int x = read.GetInt32(0);
            read.Close();

            return x;
        }

        public static bool LogIn(SqlConnection _sql, string l, string p)
        {            
            try
            {               
                string ask = "Select count(*) FROM Users WHERE Login ='" + l + "' AND Password='" + p + "'";
                SqlCommand task = new SqlCommand(ask, _sql);
                SqlDataReader read = task.ExecuteReader();
                read.Read();

                if (read.GetInt32(0) == 1)
                {
                    read.Close();
                    return true;
                }
                else
                {
                    read.Close();
                    return false;
                }              
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        public static int AddUser(SqlConnection _sql, string l, string p)
        {
            int counter = CheckUsers(_sql);

            try
            {
                string ask = "INSERT INTO Users(ID,login,password) VALUES (" + (counter + 1) + ",'" + l + "','" + p + "')";
                SqlCommand task = new SqlCommand(ask, _sql);
                task.ExecuteNonQuery();
                return 0;                
            }
            catch (SqlException)
            {                
                return 1;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return -1;
            }
        }

        public static List<string> DownloadContent(SqlConnection _sql)
        {
            DataTable dt = new DataTable();
            DataRow dw;
            string ask = "SELECT BS.book_ID, BS.bookname, BS.author, BS.publishingdate, BS.quantity, SUM(BB.book_ID) " +
                         "FROM Books BS INNER JOIN BorrowedBooks BB ON BS.book_ID = BB.book_ID " +                         
                         "GROUP BY BS.book_ID, BS.bookname, BS.author, BS.publishingdate, BS.quantity";

            SqlCommand task = new SqlCommand(ask, _sql);
            SqlDataReader read = task.ExecuteReader();
            commits.Clear();
            dt.Load(read);
            
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dw = dt.Rows[i];
                string txt = "CONTENT";
                for (int j = 0; j < dw.ItemArray.Count()-1; j++)
                {
                    if (j < dw.ItemArray.Count() - 2)
                    {
                        txt += ("|" + dw[j].ToString());
                    }
                    else
                    {
                        int x = Int32.Parse(dw[j].ToString());
                        int y = Int32.Parse(dw[j + 1].ToString());
                        if (x <= y)
                        {
                            txt += ("|FALSE");
                        }
                        else
                        {
                            txt += ("|TRUE");
                        }
                    }
                }                
                commits.Add(txt + "|");
                txt = "CONTENT";
            }
            read.Close();

            return commits;
        }

        public static List<string> DownloadReservation(SqlConnection _sql)
        {
            DataTable dt = new DataTable();
            DataRow dw;
            string ask = "SELECT ID, book_ID, user_ID" +
                         "FROM ReservedBooks";
                            
            SqlCommand task = new SqlCommand(ask, _sql);
            SqlDataReader read = task.ExecuteReader();
            recommits.Clear();
            dt.Load(read);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dw = dt.Rows[i];
                string txt = "RESERVE";
                for (int j = 0; j < dw.ItemArray.Count(); j++)
                {
                    txt += ("|" + dw[j].ToString());
                }
                recommits.Add(txt + "|");
                txt = "RESERVE";
            }
            read.Close();

            return recommits;
        }

        public static bool OrderBook(SqlConnection _sql, int _book_ID, string _username)
        {
            int _user_ID = GetUserID(_sql, _username);
            bool _order = CanOrderBook(_sql, _book_ID, _user_ID);

            if (_order)
            {
                string ask = "INSERT INTO BorrowedBooks(book_ID,user_ID,reservationdate,reservationenddate) " +
                             "VALUES (" + _book_ID + ",'" + _user_ID + "','" + DateTime.Now.ToString() + "','" + DateTime.Now.AddMonths(3).ToString() + "')";

                SqlCommand task = new SqlCommand(ask, _sql);
                task.ExecuteNonQuery();

                Monitor.Enter(commits_locker);
                commits = DownloadContent(_sql);
                Monitor.Exit(commits_locker);

                return true;
            }
            else
            {
                ReserveBook(_sql, _book_ID, _user_ID);
                return false;
            }
        }

        private static int GetUserID(SqlConnection _sql, string _username)
        {
            string ask = "SELECT Id FROM Users WHERE login = '" + _username + "'";
            SqlCommand task = new SqlCommand(ask, _sql);
            SqlDataReader read = task.ExecuteReader();
            read.Read();
            try
            {
                int x = read.GetInt32(0);
                read.Close();

                return x;
            }
            catch
            {
                return 0;
            }
        }

        private static bool CanOrderBook(SqlConnection _sql, int _book_ID, int _user_ID)
        {
            string[] roger = null;
            bool result = false;

            string ask = "SELECT COUNT(user_ID)" +
                         "FROM BorrowedBooks WHERE user_ID = '" + _user_ID + "' AND book_id ='" + _book_ID + "'";
            SqlCommand task = new SqlCommand(ask, _sql);
            SqlDataReader read = task.ExecuteReader();
            read.Read();

            int x = read.GetInt32(0);
            read.Close();
            if (x == 0)
            {
                Monitor.Enter(commits_locker);
                foreach (string _book in commits)
                {
                    roger = _book.Split('|');
                    int y = Int32.Parse(roger[1]);

                    if (y == _book_ID)
                    {
                        result = Boolean.Parse(roger[5]);
                        break;
                    }
                }
                Monitor.Exit(commits_locker);                
            }
            return result;
        }

        private static void ReserveBook(SqlConnection _sql, int _book_ID, int _user_ID)
        {
            string ask = "INSERT INTO ReservedBooks(book_ID,user_ID) " +
                         "VALUES (" + _book_ID + ",'" + _user_ID + "')";

            SqlCommand task = new SqlCommand(ask, _sql);
            task.ExecuteNonQuery();
        }

        public static bool ReturnBook(SqlConnection _sql, int _book_ID, string _username)
        {
            int _user_ID = GetUserID(_sql, _username);
            int _reservation_ID = CanReturnBook(_sql, _book_ID, _user_ID);

            if (_reservation_ID > 0)
            {
                string ask = "DELETE FROM BorrowedBooks " +
                             "WHERE reservation_ID = '" + _reservation_ID + "'";

                SqlCommand task = new SqlCommand(ask, _sql);
                task.ExecuteNonQuery();

                Monitor.Enter(commits_locker);
                commits = DownloadContent(_sql);
                Monitor.Exit(commits_locker);

                return true;
            }
            else
            {                
                return false;
            }
        }

        private static int CanReturnBook(SqlConnection _sql, int _book_ID, int _user_ID)
        {
            string ask = "SELECT reservation_ID " +
                         "FROM BorrowedBooks " +
                         "WHERE book_ID = '" + _book_ID + "' AND user_ID = '" + _user_ID + "'";

            SqlCommand task = new SqlCommand(ask, _sql);
            SqlDataReader read = task.ExecuteReader();
            read.Read();
            try
            {
                int x = read.GetInt32(0);
                read.Close();

                return x;
            }
            catch
            {
                return 0;
            }
        }
    }
}
