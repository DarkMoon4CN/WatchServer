using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRV.Data
{
    /// <summary>

    /// 基于MySQL的数据层基类

    /// </summary>

    /// <remarks>

    /// 参考于MS Petshop 4.0

    /// </remarks>

    public abstract class MySqlHelper
    {
        #region 数据库连接字符串

        public static readonly string DbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SMDataConnectString"].ConnectionString;

        //public static readonly string DBConnectionString = "Server=localhost;DataBase=menagerie;Uid=de.cel;Pwd=de.cel";   //在MySql中localhost好像不能用"."代替，我试了一下，会出错。

        #endregion 数据库连接字符串

        #region PrepareCommand

        /// <summary>

        /// Command预处理

        /// </summary>

        /// <param name="conn"> MySqlConnection对象 </param>

        /// <param name="trans"> MySqlTransaction对象，可为null </param>

        /// <param name="cmd"> MySqlCommand对象 </param>

        /// <param name="cmdType"> CommandType，存储过程或命令行 </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组，可为null </param>

        private static void PrepareCommand(MySqlConnection conn, MySqlTransaction trans, MySqlCommand cmd, CommandType cmdType, string cmdText, MySqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)

                conn.Open();

            cmd.Connection = conn;

            cmd.CommandText = cmdText;

            if (trans != null)

                cmd.Transaction = trans;

            cmd.CommandType = cmdType;

            if (cmdParms != null)
            {
                foreach (MySqlParameter parm in cmdParms)

                    cmd.Parameters.Add(parm);
            }
        }

        private static void PrepareCommand(MySqlConnection conn, MySqlTransaction trans, MySqlCommand cmd, CommandType cmdType, string cmdText)
        {
            if (conn.State != ConnectionState.Open)

                conn.Open();

            cmd.Connection = conn;

            cmd.CommandText = cmdText;

            if (trans != null)

                cmd.Transaction = trans;

            cmd.CommandType = cmdType;

            //if (cmdParms != null)
            //{
            //    foreach (MySqlParameter parm in cmdParms)

            //        cmd.Parameters.Add(parm);
            //}
        }

        #endregion PrepareCommand

        #region ExecuteNonQuery

        /// <summary>

        /// 执行命令

        /// </summary>

        /// <param name="connectionString"> 数据库连接字符串 </param>

        /// <param name="cmdType"> 命令类型（存储过程或SQL语句） </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组 </param>

        /// <returns> 返回受引响的记录行数 </returns>

        public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                PrepareCommand(conn, null, cmd, cmdType, cmdText, cmdParms);

                int val = cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();

                return val;
            }
        }

        /// <summary>
        /// 执行带事物的sql
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="trans"></param>
        /// <param name="cmdType"></param>
        /// <param name="cmdText"></param>
        /// <param name="cmdParms"></param>
        /// <owner>lihuan </owner>
        /// <returns></returns>
        public static int ExecuteNonQuery(MySqlConnection conn, MySqlTransaction trans, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            PrepareCommand(conn, trans, cmd, cmdType, cmdText, cmdParms);

            int val = cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();

            return val;
        }

        public static int ExecuteNonQuery(MySqlConnection conn, MySqlTransaction trans, CommandType cmdType, string cmdText)
        {
            MySqlCommand cmd = new MySqlCommand();

            PrepareCommand(conn, trans, cmd, cmdType, cmdText);

            int val = cmd.ExecuteNonQuery();

            //cmd.Parameters.Clear();

            return val;
        }

        /// <summary>

        /// 执行命令

        /// </summary>

        /// <param name="conn"> Connection对象 </param>

        /// <param name="cmdType"> 命令类型（存储过程或SQL语句） </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组 </param>

        /// <returns> 返回受引响的记录行数 </returns>

        public static int ExecuteNonQuery(MySqlConnection conn, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            PrepareCommand(conn, null, cmd, cmdType, cmdText, cmdParms);

            int val = cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();

            return val;
        }

        public static int ExecuteNonQuery(CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteNonQuery(DbConnectionString, cmdType, cmdText, cmdParms);
        }

        public static int ExecuteNonQuery(string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteNonQuery(CommandType.Text, cmdText, cmdParms);
        }

        public static int ExecuteNonQuery(string cmdText)
        {
            return ExecuteNonQuery(cmdText, null);
        }

        /// <summary>

        /// 执行事务

        /// </summary>

        /// <param name="trans"> MySqlTransaction对象 </param>

        /// <param name="cmdType"> 命令类型（存储过程或SQL语句） </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组 </param>

        /// <returns> 返回受引响的记录行数 </returns>

        public static int ExecuteNonQuery(MySqlTransaction trans, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            PrepareCommand(trans.Connection, trans, cmd, cmdType, cmdText, cmdParms);

            int val = cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();

            return val;
        }

        #endregion ExecuteNonQuery

        #region ExecuteScalar

        /// <summary>

        /// 执行命令，返回第一行第一列的值

        /// </summary>

        /// <param name="connectionString"> 数据库连接字符串 </param>

        /// <param name="cmdType"> 命令类型（存储过程或SQL语句） </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组 </param>

        /// <returns> 返回Object对象 </returns>

        public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                PrepareCommand(connection, null, cmd, cmdType, cmdText, cmdParms);

                object val = cmd.ExecuteScalar();

                cmd.Parameters.Clear();

                return val;
            }
        }

        public static object ExecuteScalar(CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteScalar(DbConnectionString, cmdType, cmdText, cmdParms);
        }

        public static object ExecuteScalar(string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteScalar(CommandType.Text, cmdText, cmdParms);
        }

        public static object ExecuteScalar(string cmdText)
        {
            return ExecuteScalar(cmdText, null);
        }

        /// <summary>

        /// 执行命令，返回第一行第一列的值

        /// </summary>

        /// <param name="connectionString"> 数据库连接字符串 </param>

        /// <param name="cmdType"> 命令类型（存储过程或SQL语句） </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组 </param>

        /// <returns> 返回Object对象 </returns>

        public static object ExecuteScalar(MySqlConnection conn, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            PrepareCommand(conn, null, cmd, cmdType, cmdText, cmdParms);

            object val = cmd.ExecuteScalar();

            cmd.Parameters.Clear();

            return val;
        }

        #endregion ExecuteScalar

        #region ExecuteReader

        /// <summary>

        /// 执行命令或存储过程，返回MySqlDataReader对象

        /// 注意MySqlDataReader对象使用完后必须Close以释放MySqlConnection资源

        /// </summary>

        /// <param name="connectionString"> 数据库连接字符串 </param>

        /// <param name="cmdType"> 命令类型（存储过程或SQL语句） </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组 </param>

        /// <returns> </returns>

        public static MySqlDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            MySqlConnection conn = new MySqlConnection(connectionString);

            try
            {
                PrepareCommand(conn, null, cmd, cmdType, cmdText, cmdParms);

                MySqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                cmd.Parameters.Clear();

                return dr;
            }
            catch
            {
                conn.Close();

                throw;
            }
        }

        public static MySqlDataReader ExecuteReader(CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteReader(DbConnectionString, cmdType, cmdText, cmdParms);
        }

        public static MySqlDataReader ExecuteReader(string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteReader(CommandType.Text, cmdText, cmdParms);
        }

        public static MySqlDataReader ExecuteReader(string cmdText)
        {
            return ExecuteReader(CommandType.Text, cmdText, null);
        }

        #endregion ExecuteReader

        #region ExecuteDataSet

        /// <summary>

        /// 执行命令或存储过程，返回DataSet对象

        /// </summary>

        /// <param name="connectionString"> 数据库连接字符串 </param>

        /// <param name="cmdType"> 命令类型(存储过程或SQL语句) </param>

        /// <param name="cmdText"> SQL语句或存储过程名 </param>

        /// <param name="cmdParms"> MySqlCommand参数数组(可为null值) </param>

        /// <returns> </returns>

        public static DataSet ExecuteDataSet(string connectionString, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                PrepareCommand(conn, null, cmd, cmdType, cmdText, cmdParms);

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);

                DataSet ds = new DataSet();

                da.Fill(ds);

                return ds;
            }
        }

        public static DataSet ExecuteDataSet(MySqlConnection conn, MySqlTransaction trans, CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            MySqlCommand cmd = new MySqlCommand();

            PrepareCommand(conn, trans, cmd, cmdType, cmdText, cmdParms);

            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            DataSet ds = new DataSet();

            da.Fill(ds, "table");

            return ds;
        }

        public static DataSet ExecuteDataSet(CommandType cmdType, string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteDataSet(DbConnectionString, cmdType, cmdText, cmdParms);
        }

        public static DataSet ExecuteDataSet(string cmdText, params MySqlParameter[] cmdParms)
        {
            return ExecuteDataSet(DbConnectionString, CommandType.Text, cmdText, cmdParms);
        }

        public static DataSet ExecuteDataSet(string cmdText)
        {
            return ExecuteDataSet(cmdText, null);
        }

        #endregion ExecuteDataSet
    }//end class
}