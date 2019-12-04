using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;

namespace JasperUI.Model
{
    public class Mysql
    {
        MySqlConnection conn = null;
        //string StrMySQL = "Server=192.168.0.81;Database=jasper;Uid=leader;Pwd=leader*168;pooling=false;CharSet=utf8;port=3306";
        string StrMySQL = "Server=10.89.164.62;Database=dcdb;Uid=dcu;Pwd=dcudata;pooling=false;CharSet=utf8;port=3306";
        public bool Connect()
        {
            try
            {
                if (conn == null)
                {
                    conn = new MySqlConnection(StrMySQL);
                    conn.Open();
                }
                if (conn.State == ConnectionState.Open)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch 
            {
                return false;
            }
        }
        public bool Connect(string strMySQL)
        {
            StrMySQL = strMySQL;
            try
            {
                if (conn == null)
                {
                    conn = new MySqlConnection(StrMySQL);
                    conn.Open();
                }
                if (conn.State == ConnectionState.Open)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public void DisConnect()
        {
            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
                conn = null;
            }
        }
        public int executeQuery(string stm)
        {
            MySqlCommand cmd = new MySqlCommand(stm, conn);
            int res = cmd.ExecuteNonQuery();
            return res;
        }
        public DataSet Select(string stm)
        {
            DataSet ds = new DataSet();
            MySqlDataAdapter myadp = new MySqlDataAdapter(stm, conn); //适配器 
            myadp.Fill(ds, "table0");
            return ds;
        }
    }
}
