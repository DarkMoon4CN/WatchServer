using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRV.BLL
{
    public class SQLCommander
    {
        private static readonly object _lockHelper = new object();
        private static SQLCommander _instance = null;

        /// <summary>
        /// 创建UserInfo实例
        /// </summary>
        /// <returns> </returns>
        public static SQLCommander CreateInstance()
        {
            if (_instance == null)
            {
                lock (_lockHelper)
                {
                    if (_instance == null)
                        _instance = new SQLCommander();
                }
            }
            return _instance;
        }

        public Boolean ExeQuery(String cmdTxt)
        {
            var ret = SRV.Data.MySqlHelper.ExecuteNonQuery(cmdTxt);
            if (ret == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}