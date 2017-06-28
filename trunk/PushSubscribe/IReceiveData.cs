using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PushSubscribe
{
    [ServiceContract]
    public interface IReceiveData
    {
        [OperationContract(IsOneWay = true)]
        void Receive(string userid, DateTime culltime, string kind, string value, int mode);

        /// <summary>
        /// 指令下发（Socket）
        /// </summary>
        /// <param name="studentid">学生ID</param>
        /// <param name="sportmode">运动模式</param>
        /// <param name="sportmode">班级编号</param>
        /// <param name="tableName">临时表名</param>
        /// <param name="isClose">下发结束标记，只用于关闭下发时，最后一条数据设定成true</param>
        [OperationContract(IsOneWay = true)]
        void SendSportMode(string studentid, int sportmode, int classid, string tableName, bool isClose);
    }
}