using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SRV.Data
{
    public static class SqlDbHelper
    {
        //SqlConnection Conn = new SqlConnection("server=" + ConfigurationSettings.AppSettings["SqlServer"] + ";uid=" + ConfigurationSettings.AppSettings["SqlUid"] + ";pwd=" + ConfigurationSettings.AppSettings["SqlPwd"] + ";database=" + ConfigurationSettings.AppSettings["SqlDataName"]);

        #region Constants and Fields

        private static string _connectionString = string.Empty;

        #endregion Constants and Fields

        #region Public Properties

        /// <summary>
        /// SQL SERVER连接字符串
        /// </summary>
        public static string ConnectionString
        {
            set
            {
                _connectionString = value;
            }
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                    _connectionString = ConfigurationManager.ConnectionStrings["SMDataConnectString"].ConnectionString;
                return _connectionString;
            }
        }

        #endregion Public Properties

        #region Public Methods and Operators

        public static SqlDataAdapter ExecuteAdapter(string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();
            //using (SqlConnection conn = new SqlConnection(connectionString))
            //{
            var conn = new SqlConnection(ConnectionString);
            // if (conn.State != ConnectionState.Open) conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            if (commandParameters != null)
                foreach (var parm in commandParameters)
                    cmd.Parameters.Add(parm);
            //adapter.Fill(Ds, PagerJian, Pagers, "表名");
            //conn.Close();
            return new SqlDataAdapter(cmd);
            //}
        }

        public static DataSet ExecuteDataSet(SqlCommand cmd, string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteDataSet(cmd, cmdText, CommandType.Text, commandParameters);
        }

        public static DataSet ExecuteDataSet(string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteDataSet(cmdText, CommandType.Text, commandParameters);
        }

        public static DataSet ExecuteDataSet(
            string cmdText, CommandType cmdType, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();
            return ExecuteDataSet(cmd, cmdText, cmdType, commandParameters);
        }

        public static DataSet ExecuteDataSet(SqlCommand cmd, string cmdText, CommandType cmdType, params SqlParameter[] commandParameters)
        {
            var ds = new DataSet();
            using (var conn = new SqlConnection(ConnectionString))
            {
                //SqlConnection conn = new SqlConnection(connectionString);
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                cmd.Connection = conn;
                cmd.CommandText = cmdText;
                cmd.CommandType = cmdType;
                if (commandParameters != null)
                    foreach (var parm in commandParameters)
                    {
                        if (parm != null)
                            cmd.Parameters.Add(parm);
                    }
                var da = new SqlDataAdapter(cmd);
                da.Fill(ds);
            } return ds;
        }

        #region ExecuteNonQuery

        public static int ExecuteNonQuery(string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            using (var conn = new SqlConnection(ConnectionString))
            {
                PrepareCommand(cmd, conn, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static int ExecuteNonQuery(string cmdText, CommandType cmdType, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            using (var conn = new SqlConnection(ConnectionString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static int ExecuteNonQuery(SqlCommand cmd, string cmdText, CommandType cmdType, params SqlParameter[] commandParameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static int ExecuteNonQuery(
            string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            using (var conn = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static int ExecuteNonQuery(
            SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }

        #endregion ExecuteNonQuery

        public static SqlDataReader ExecuteReader(string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();
            var conn = new SqlConnection(ConnectionString);

            // we use a try/catch here because if the method throws an exception we want to close the connection throw code, because no datareader will exist, hence the commandBehaviour.CloseConnection will not work
            try
            {
                PrepareCommand(cmd, conn, cmdText, commandParameters);
                SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        public static SqlDataReader ExecuteReader(
            string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();
            var conn = new SqlConnection(connectionString);

            // we use a try/catch here because if the method throws an exception we want to close the connection throw code, because no datareader will exist, hence the commandBehaviour.CloseConnection will not work
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        #region ExecuteScalar

        public static object ExecuteScalar(string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            using (var conn = new SqlConnection(ConnectionString))
            {
                PrepareCommand(cmd, conn, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static object ExecuteScalar(string cmdText, CommandType cmdType, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            using (var connection = new SqlConnection(ConnectionString))
            {
                PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static object ExecuteScalar(
            string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            using (var connection = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static object ExecuteScalar(
            SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            var cmd = new SqlCommand();

            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }

        #endregion ExecuteScalar

        #endregion Public Methods and Operators

        #region Methods

        /// <summary>
        /// 处理Cmd简化版
        /// </summary>
        /// <param name="cmd"> cmd对象 </param>
        /// <param name="conn"> conn对象 </param>
        /// <param name="cmdText"> 执行cmd字符 </param>
        /// <param name="cmdParms"> 执行cmd参数 </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:检查 SQL 查询是否存在安全漏洞")]
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, string cmdText, SqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            if (cmdParms != null)
                foreach (var parm in cmdParms)
                {
                    if (parm != null)
                        cmd.Parameters.Add(parm);
                }
        }

        private static void PrepareCommand(
            SqlCommand cmd,
            SqlConnection conn,
            SqlTransaction trans,
            CommandType cmdType,
            string cmdText,
            SqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            if (trans != null)
                cmd.Transaction = trans;

            cmd.CommandType = cmdType;

            if (cmdParms != null)
                foreach (var parm in cmdParms)
                {
                    if (parm != null)
                        cmd.Parameters.Add(parm);
                }
        }

        #endregion Methods
    }
}