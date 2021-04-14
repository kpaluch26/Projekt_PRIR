using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Windows;

namespace Serwer
{
    class DatabaseOrder
    {
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

        public static bool LogIn(SqlConnection _sql, string l, string p, string a)
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

        public static int addUser(SqlConnection _sql, string l, string p, string a)
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
    }
}
