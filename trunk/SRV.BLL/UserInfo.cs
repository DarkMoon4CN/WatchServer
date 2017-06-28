using MySql.Data.MySqlClient;
using SRV.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRV.BLL
{
    public class UserInfo
    {
        private static readonly object _lockHelper = new object();
        private static UserInfo _instance = null;

        /// <summary>
        /// 创建UserInfo实例
        /// </summary>
        /// <returns> </returns>
        public static UserInfo CreateInstance()
        {
            if (_instance == null)
            {
                lock (_lockHelper)
                {
                    if (_instance == null)
                        _instance = new UserInfo();
                }
            }
            return _instance;
        }


        public User UserLogin(String imei)
        {
            MySqlParameter[] paras = new MySqlParameter[2];
            paras[0] = new MySqlParameter("__imei", imei);
            paras[1] = new MySqlParameter("__rt", 0);
            paras[1].Direction = ParameterDirection.Output; 

            DataTable mTable = SRV.Data.MySqlHelper.ExecuteDataSet(CommandType.StoredProcedure, "sp_user_login", paras).Tables[0];
            User mUser = new User();
            mUser.rt = Convert.ToByte(mTable.Rows[0]["rt"]);
	        if (mUser.rt != 0 && mUser.rt != 1)
	        {
                mUser.userid = Convert.ToUInt32(mTable.Rows[0]["userid"]);
                mUser.username = mTable.Rows[0]["username"].ToString();
                mUser.truename = mTable.Rows[0]["truename"].ToString();
                mUser.classname = mTable.Rows[0]["classname"].ToString();
                mUser.schoolname = mTable.Rows[0]["schoolname"].ToString();
                mUser.classid = Convert.ToInt32(mTable.Rows[0]["classid"]);
                mUser.schoolid = Convert.ToInt32(mTable.Rows[0]["schoolid"]);
                mUser.educode = mTable.Rows[0]["educode"].ToString();
                mUser.sex = (Convert.ToInt32(mTable.Rows[0]["gender"]) == 1) ? true : false;
                mUser.age = Convert.ToByte(mTable.Rows[0]["age"]);
                mUser.height = Convert.ToInt32(mTable.Rows[0]["height"]);
                mUser.weight = Convert.ToInt32(mTable.Rows[0]["weight"]);
                mUser.serverip = mTable.Rows[0]["ip"].ToString();
                mUser.serverport = Convert.ToUInt16(mTable.Rows[0]["port"]);
                mUser.sportmode = Convert.ToInt32(mTable.Rows[0]["sportmode"]);
	        }
	        return mUser;
        }


        public Boolean UpdateUserOnlineInfo(UInt32 batteryLevel, UInt32 uid)
        {
            String cmdTxt = String.Format("update tuserstatusinfo set keepalive='{0}',BatteryLevel={1},online=1 where userid={2}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), batteryLevel, uid);
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

        public Boolean UpdateUserStatus(Int32 UserID, Boolean bIsLogin)
        {
	        try
	        {
		        String cmdTxt = String.Format("update tuserstatusinfo set online={0} where userid={1}", bIsLogin, UserID);        
		        SRV.Data.MySqlHelper.ExecuteNonQuery(cmdTxt);
	        }
	        catch (Exception ex) {
		        return false;
	        }
	        return true;
        }


        public Boolean UpdateUserSportModeInfo(Int32 modeType, Int32 workMode, UInt32 uid)
        {
            String cmdTxt = String.Empty;
            if (modeType == 0)
            {
                cmdTxt = String.Format("update tuserstatusinfo set sportmode={0} where userid={1}", workMode, uid);
            } 
            else
            {
                cmdTxt = String.Format("update tuserstatusinfo set OldSportMode=SportMode,sportmode={0} where userid={1}", workMode, uid);
            }
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

        public Boolean InsertLowRate(UInt32 uid, UInt32 heartrate, TIME_INFO timeInfo)
        {
            String cmdTxt = String.Format("insert into trecordlowrate(userid,recordtime,heartrate,CullTime) values({0},now(),'{1}','{2}-{3}-{4} {5}:{6}:{7}');",
                                uid, heartrate,
                                timeInfo.Year, timeInfo.Month,
                                timeInfo.Day, timeInfo.Hour,
                                timeInfo.Minute, timeInfo.Sec);
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

        public Boolean InitWatchOnlineStatus()
        {
            String cmdTxt = String.Format("update tuserstatusinfo set online=0");
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