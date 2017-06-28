using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRV.Model
{
    public enum DBType
    {
        dtMSSQL = 1,
        dtMYSQL = 2
    };

    public class ServerParam
    {
        //服务器配置
        public String szLoginIp;				    //服务器IP地址
        public UInt16 wLoginPort;					//服务器端口号
        public Boolean bIsLogin;					//是否为登陆服务器
        public Boolean bIsAnswer;								//是否回复单个数据包

        //数据库连接信息
        public String szDBHost;							//主机名称
        public String szDBUser;							//用户帐号
        public String szDBPass;							//用户密码
        public String szDBName;							//数据库名
        public UInt16 wDBPort;								//端口号码
        public DBType cbDBKind;							//数据库类型
        public UInt16 wCmdInx;								//命令下发索引
        public Int32 userid;									//用户ID
        public Int32 mode;									//运动模式

        //接口服务配置
        public UInt16 wWebPort;								//webservice端口
    }
}