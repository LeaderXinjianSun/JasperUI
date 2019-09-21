using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;

namespace JasperUI.Model
{
    public class Oracle
    {
        private string m_server_name;
        private string m_id;
        private string m_pwd;
        private bool m_connect_state;
        public OleDbConnection oledbConn = null;
        public Oracle(string ServerName, string ID, string PWD)
        {
            m_server_name = ServerName;
            m_id = ID;
            m_pwd = PWD;
            m_connect_state = false;
            connect();
        }
        public bool connect()
        {
            try
            {
                string str1 = "Provider=MSDAORA.1" +
                    ";Data Source=" + m_server_name +
                    ";User Id=" + m_id +
                    ";Password=" + m_pwd +
                    ";Persist Security Info=False";

                if (oledbConn == null)
                {
                    m_connect_state = false;
                    oledbConn = new OleDbConnection(str1);
                    oledbConn.Open();
                    if (oledbConn.State == ConnectionState.Open)
                    {
                        m_connect_state = true;
                    }
                }
                else
                {
                    if (oledbConn.State != ConnectionState.Open)
                    {
                        oledbConn.Open();
                        if (oledbConn.State == ConnectionState.Open)
                        {
                            m_connect_state = true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return m_connect_state;
        }
        public void disconnect()
        {
            if (oledbConn != null)
            {
                oledbConn.Close();
                oledbConn.Dispose();
                oledbConn = null;
                m_connect_state = false;
            }
        }
        public bool isConnect()
        {
            return m_connect_state;
        }
        public DataSet executeQuery(string strSQL)
        {
            DataSet da = new DataSet();
            try
            {
                OleDbDataAdapter sda = new OleDbDataAdapter(strSQL, oledbConn);
                int m = sda.Fill(da);
                sda.Dispose();
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return da;
        }
        public string OraclDateTime()
        {
            try
            {
                DataSet da = executeQuery("select to_char(SYSDATE,'YYYY-MM-DD HH24:MI:SS') sDate FROM DUAL");
                setLocalTime(da.Tables[0].Rows[0][0].ToString());
                return da.Tables[0].Rows[0][0].ToString();
            }
            catch (Exception)
            {
                throw;
            }

        }
        private void setLocalTime(string strDateTime)
        {
            DateTimeUtility.SYSTEMTIME st = new DateTimeUtility.SYSTEMTIME();
            DateTime dt = Convert.ToDateTime(strDateTime);
            st.FromDateTime(dt);
            DateTimeUtility.SetLocalTime(ref st);
        }
    }
}
