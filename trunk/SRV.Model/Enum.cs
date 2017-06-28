using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRV.Model
{

    /// <summary>
    /// 运行状态码
    /// </summary>
    public enum StatusCode
    {
        /// <summary>
        /// 敲门程序 记录开始码
        /// </summary>
        KnockStart= 10000,

        /// <summary>
        /// 敲门程序 记录结束码
        /// </summary>
        KnockEnd= 11000,

        /// <summary>
        /// 登陆程序 记录开始码
        /// </summary>
        LoginStart= 20000,

        /// <summary>
        /// 用户数据操作
        /// </summary>
        UserLoginDB= 21000,

        /// <summary>
        /// 用户信息操作  pUserInfo.rt == 1 or 0
        /// </summary>
        UserLoginOP = 21100,

        /// <summary>
        /// 用户信息操作  pUserInfo.rt ==2 
        /// </summary>
        UserLoginOP2 = 21200,

        /// <summary>
        /// 登陆程序 记录结束码
        /// </summary>
        LoginEnd = 21300,

        /// <summary>
        /// 保活程序 记录开始码
        /// </summary>
        KeepAliveStart= 30000,

        /// <summary>
        /// 保活程序  记录结束码
        /// </summary>
        KeepAliveEnd= 31000,

        /// <summary>
        /// 轨迹点程序 记录开始码
        /// </summary>
        TrackStart=40000,

        /// <summary>
        /// 轨迹点 工作模式
        /// </summary>
        TrackWork=41000,

        /// <summary>
        /// 轨迹点数据操作 
        /// </summary>
        TrackDB =42000,
        /// <summary>
        /// 轨迹点程序 记录结束码
        /// </summary>
        TrackEnd =43000,

    }
}
