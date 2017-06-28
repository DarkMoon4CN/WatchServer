using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Protobuf;
using System.Collections;
using System.Collections.Concurrent;
using SuperSocketForm.Utility;
using log4net;
using log4net.Config;
using System.Reflection;
using SRV.Model;
using System.Configuration;
using SRV.BLL;
using System.Runtime.InteropServices;
using PushSubscribe;
using System.Threading;
using SuperSocket.SocketEngine;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;

namespace SuperSocketForm
{
    public partial class SuperSocketForm : Form
    {
        private const Int32 SQL_BUF_NUM = 5000;
        private const Int32 TIME_CONN_TIME_OUT = 10 * 60 * 1000;
        private const Int32 TIMES_CMD_RET = 600;
        private const Int32 TIMES_SP_RET = 10;
        private const Int32 RECVLEN = 4096;
        private SuperSocketServer appServer;
        private static ConcurrentDictionary<String, ClientManager> m_SessionDic;
        private static ConcurrentDictionary<Int32, String> m_DBTableDic;
        private static List<ClientManager> m_CommandList;
        private static List<ClientManager> m_SendCmdList;
        private static SuperSocket.SocketBase.Logging.ILog logger;
        private static Int32 m_nLog = 0;
        private static Int32 m_nErrCode = 0;
        private static ServerParam m_ServerInfo;
        private static Boolean[] m_pbIsExist = new Boolean[SQL_BUF_NUM];
        private static Boolean m_bIsStartDeal = false;
        private static List<String> m_pSql;
        private static readonly object _lockHelper = new object();
        private static readonly object _lockLogger = new object();
        private static System.Timers.Timer m_DBTimer;
        private static System.Timers.Timer m_ConnTimer;
        private static System.Timers.Timer m_CommandTimer;
        private static System.Timers.Timer m_SrvParamTimer;
        private static Thread m_DBThread;
        public static BindingList<String> m_SrvLogger;
        private static System.Timers.Timer m_SrvLoggerTimer;
        private String loggerStr = String.Empty;
        private static List<RecvInfo> m_RecvInfo;
        private static Int32 m_iRecvCount = 0;
        private delegate void AppendItemDelegate(String str);
        private static AppendItemDelegate appendItemDelegate;
        private delegate void RemoveItemDelegate(Int32 op);
        private static RemoveItemDelegate removeItemDelegate;
        private delegate void comboxBind();
        private delegate string getcomboxSelected();
        private static WCFHost s;

        [DllImport("kernel32")]
        static extern ulong GetTickCount64();

        public SuperSocketForm()
        {

            InitializeComponent();
            //设置aes加密算法密钥
            Byte[] key =
            {
                0x2b, 0x7e, 0x15, 0xc6,
                0x2f, 0x3e, 0xd2, 0xa2,
                0xab, 0xfa, 0x15, 0xb8,
                0x39, 0x5f, 0x4f, 0x31
            };
            CommonTools.Setkey(key);
            m_pSql = new List<String>(SQL_BUF_NUM);
            m_SrvLogger = new BindingList<String>();
            listBoxLogger.DataSource = m_SrvLogger;
            m_RecvInfo = new List<RecvInfo>(RECVLEN);
            appendItemDelegate = new AppendItemDelegate(AppendItem);
            removeItemDelegate = new RemoveItemDelegate(RemoveItem);
            buttonClose.Enabled = false;

            //初始化数据库信息
            m_ServerInfo = new ServerParam();
            m_ServerInfo.cbDBKind = (DBType)Properties.Settings.Default.kind;
            m_ServerInfo.wDBPort = Properties.Settings.Default.port;
            m_ServerInfo.szDBHost = Properties.Settings.Default.host;
            m_ServerInfo.szDBUser = Properties.Settings.Default.user;
            m_ServerInfo.szDBPass = Properties.Settings.Default.pass;
            m_ServerInfo.szDBName = Properties.Settings.Default.name;

            //初始化登陆服务器信息
            m_ServerInfo.szLoginIp = Properties.Settings.Default.srvip;
            m_ServerInfo.wLoginPort = Properties.Settings.Default.port;
            m_ServerInfo.bIsLogin = (1 == Convert.ToInt32(ConfigurationManager.AppSettings["loginserver"])) ? true : false;
            m_ServerInfo.bIsAnswer = (1 == Properties.Settings.Default.bIsAnswer) ? true : false;
            m_nLog = Convert.ToInt32(ConfigurationManager.AppSettings["log"]);

            //初始化接口服务器信息
            m_ServerInfo.wWebPort = Properties.Settings.Default.webport;

            appServer = new SuperSocketServer(this);
            appServer.NewSessionConnected += appServer_NewSessionConnected;
            appServer.SessionClosed += appServer_SessionClosed;
            appServer.NewRequestReceived += appServer_NewRequestReceived;
            SuperSocketSetup(appServer);

            m_SessionDic = new ConcurrentDictionary<String, ClientManager>();
            m_CommandList = new List<ClientManager>();
            m_SendCmdList = new List<ClientManager>();
            m_DBTableDic = new ConcurrentDictionary<Int32, String>();

            //开始批量写入数据库
            m_DBTimer = new System.Timers.Timer();
            m_DBTimer.Interval = 10 * 1000;
            m_DBTimer.Elapsed += m_DBTimer_Elapsed;

            //批量写入线程
            m_DBThread = new Thread(DBBatMethod);

            s = new WCFHost();
            s.Start();

            //服务端启动时将所有手表的登陆状态设置为离线
            UserInfo.CreateInstance().InitWatchOnlineStatus();

            //设置超时检测定时器
            m_ConnTimer = new System.Timers.Timer();
            m_ConnTimer.Interval = 60 * 1000;
            m_ConnTimer.Elapsed += m_ConnTimer_Elapsed;

            //设置命令下发检测定时器
            m_CommandTimer = new System.Timers.Timer();
            m_CommandTimer.Interval = 10 * 1000;
            m_CommandTimer.Elapsed += m_CommandTimer_Elapsed;

            //设置服务器参数检测定时器
            m_SrvParamTimer = new System.Timers.Timer();
            m_SrvParamTimer.Interval = 20 * 1000;
            m_SrvParamTimer.Elapsed += m_SrvParamTimer_Elapsed;

            //清理log显示框过多的显示记录
            m_SrvLoggerTimer = new System.Timers.Timer();
            m_SrvLoggerTimer.Interval = 10 * 60 * 1000;
            m_SrvLoggerTimer.Elapsed += m_SrvLoggerTimer_Elapsed;

            // buttonStartSrv_Click(null, null);
        }

        private static void m_SrvLoggerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lockLogger)
            {
                if (m_SrvLogger.Count > 1000)
                {
                    while (m_SrvLogger.Count > 400)
                    {
                        listBoxLogger.Invoke(removeItemDelegate, 0);
                    }

                    listBoxLogger.Invoke(removeItemDelegate, 1);
                }
            }
        }

        private void AppendItem(String str)
        {
            lock (_lockLogger)
            {
                m_SrvLogger.Add(str);
                listBoxLogger.SetSelected(listBoxLogger.Items.Count - 1, true);
                listBoxLogger.ClearSelected();
            }
        }

        private void RemoveItem(Int32 op)
        {
            if (op == 0)
            {
                m_SrvLogger.RemoveAt(0);
            } 
            else
            {
                listBoxLogger.SetSelected(listBoxLogger.Items.Count - 1, true);
                listBoxLogger.ClearSelected();
            }   
        }

        private static void m_SrvParamTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (((ICollection)m_SendCmdList).SyncRoot)
            {
                for (int i = m_SendCmdList.Count - 1; i >= 0; i--)
                {
                    if (m_SendCmdList[i].nIndex > TIMES_SP_RET - 1)
                    {
                        //超过最大重发次数
                        UInt64 ulCurr = GetTickCount64();

                        var query = m_SessionDic.Where(p => p.Value.UID == m_SendCmdList[i].UID).FirstOrDefault();
                        if (!query.Equals(default(KeyValuePair<String, ClientManager>)) && 3 * 60 * 1000 > ulCurr - query.Value.UllActive && query.Value.IsLogin)
                        {
                            //更新用户登陆状态
//                             UserInfo.CreateInstance().UpdateUserStatus(query.Value.UID, false);
//                             String str = String.Format("{0}_用户:{1}已断开连接\n", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), query.Value.UID);
//                             listBoxLogger.Invoke(appendItemDelegate, str);
//                             if (2 == m_nLog) logger.Info(str);
                            query.Value.Session.Close(SuperSocket.SocketBase.CloseReason.TimeOut);
                        }
                        query.Value.IsLock = true;

                        m_SendCmdList.RemoveAt(i);
                        continue;
                    }

                    //是否已重新登陆
                    var query01 = m_SessionDic.Where(p => p.Value.UID == m_SendCmdList[i].UID).FirstOrDefault();
                    if (query01.Equals(default(KeyValuePair<String, ClientManager>)))
                    {
                        // query01.Value.IsLock = true;
                        m_SendCmdList.RemoveAt(i);
                        continue;
                    }

                    //重发命令,重发次数加1
                    if ((60 > m_SendCmdList[i].nIndex) || (60 < m_SendCmdList[i].nIndex && 0 == m_SendCmdList[i].nIndex % 10))
                    {
                        SendLoginServerParam(query01.Value.Session, m_SendCmdList[i].UID, m_SendCmdList[i].ServerIP, m_SendCmdList[i].ServerPort);
                    }            
                    m_SendCmdList[i].nIndex++;
                }
            }
        }

        private static void m_CommandTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (((ICollection)m_CommandList).SyncRoot)
            {
                for (Int32 i = m_CommandList.Count - 1; i >= 0; i--)
                {
                    if (m_CommandList[i].nIndex > TIMES_CMD_RET - 1)
                    {
                        //超过最大重发次数
                        UInt64 ulCurr = GetTickCount64();

                        var query = m_SessionDic.Where(p => p.Value.UID == m_CommandList[i].UID).FirstOrDefault();
                        if (!query.Equals(default(KeyValuePair<String, ClientManager>)) && 3 * 60 * 1000 > ulCurr - query.Value.UllActive && query.Value.IsLogin)
                        {
                            //更新用户登陆状态
//                             UserInfo.CreateInstance().UpdateUserStatus(query.Value.UID, false);
//                             String str = String.Format("{0}_用户:{1}已断开连接\n", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), query.Value.UID);
//                             listBoxLogger.Invoke(appendItemDelegate, str);
//                             if (2 == m_nLog) logger.Info(str);
                            query.Value.Session.Close(SuperSocket.SocketBase.CloseReason.TimeOut);
                        }

                        m_CommandList.RemoveAt(i);
                        continue;
                    }

                    //重发命令,重发次数加1
                    SendCommand(m_CommandList[i].UID, m_CommandList[i].Mode);
                    m_CommandList[i].nIndex++;
                }
            }
        }

        private static void m_ConnTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UInt64 ulCurr = GetTickCount64();
            foreach (KeyValuePair<String, ClientManager> kPair in m_SessionDic)
            {
                if (TIME_CONN_TIME_OUT < ulCurr - kPair.Value.UllTimer && kPair.Value.IsLogin)
                {
                    String str;
                    str = String.Format("用户userid = {0} 掉线__时间{1}\n", kPair.Value.UID, DateTime.Now.ToString("yy-MM-dd HH:mm:ss"));
                    listBoxLogger.Invoke(appendItemDelegate, str);
                    if (2 == m_nLog) logger.Info(str);

                    //更新用户登陆状态
                    UserInfo.CreateInstance().UpdateUserStatus(kPair.Value.UID, false);
                    str = String.Format("{0}_用户:{1}已断开连接\n", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), kPair.Value.UID);
                    listBoxLogger.Invoke(appendItemDelegate, str);
                    if (2 == m_nLog) logger.Info(str);
                    kPair.Value.Session.Close(SuperSocket.SocketBase.CloseReason.TimeOut);
                }
            }
        }

        private static void m_DBTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (((ICollection)m_pSql).SyncRoot)
            {
                if (0 < m_pSql.Count)
                {
                    m_bIsStartDeal = true;
                }
            }
        }

        private void DBBatMethod()
        {
            int i = 0; ;
            while (true)                //阻塞线程
            {
                try
                {
                    lock (((ICollection)m_pSql).SyncRoot)
                    {
                        if (m_bIsStartDeal && 0 < m_pSql.Count)      //判断是否可以开始批量写入并且待写入sql语句条数大于0
                        {
                            for (i = 0; i < m_pSql.Count; i++)               //遍历命令缓存区
                            {
                                //执行数据库查询
                                SQLCommander.CreateInstance().ExeQuery(m_pSql[i]);
                            }
                            m_pSql.Clear();                            //置空缓存区
                            m_bIsStartDeal = false;			           //关闭批量写入
                        }
                    }
                    //睡眠1毫秒
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    m_nErrCode = 1000;
                    logger.Error(String.Format("严重错误:数据处理失败!错误号:{0}__{1}", m_nErrCode, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                    continue;
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            const Int32 WM_SYSCOMMAND = 0x0112;
            const Int32 SC_CLOSE = 0xf060;
            const Int32 SC_MINIMIZE = 0xf020;
            // const Int32 SC_MAXIMIZE = 0xf030;
            if (m.Msg == WM_SYSCOMMAND)
            {
                if (m.WParam.ToInt32() == SC_CLOSE)
                {
                    this.Hide();
                    this.ShowInTaskbar = false;
                    if (this.WindowState == FormWindowState.Maximized)
                    {
                        this.Hide();
                    }
                    notifyIcon.Visible = true;
                    notifyIcon.BalloonTipTitle = "迈动健康运动手表数据服务器";
                    notifyIcon.BalloonTipText = "需要运动手表数据服务时请来这里打开我:)";
                    notifyIcon.ShowBalloonTip(2000);
                    return;
                }
                else if (m.WParam.ToInt32() == SC_MINIMIZE)
                {
                    this.Opacity = this.Opacity == 0 ? 1 : 0;
                    this.ShowInTaskbar = true;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        static void appServer_SessionClosed(SuperSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            ClientManager obj;
            m_SessionDic.TryRemove(session.SessionID, out obj);
            logger.Info("Session ID为" + session.SessionID + "的连接已经关闭。");
            // m_SrvLogger.Add(String.Format("{0} 客户端 {1} 已断开连接!", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), session.RemoteEndPoint.Address));
            listBoxLogger.Invoke(appendItemDelegate, String.Format("{0} 客户端 {1} 已断开连接!", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), session.RemoteEndPoint.Address));
            TestClientBind();
        }

        static void appServer_NewSessionConnected(SuperSocketSession session)
        {
            ClientManager tManager = new ClientManager();
            tManager.Session = session;
            m_SessionDic.TryAdd(session.SessionID, tManager);
            m_SessionDic[session.SessionID].UllTimer = GetTickCount64();
            logger.Info("有客户端连接到服务器，连接Session ID: " + session.SessionID);
            // m_SrvLogger.Add(String.Format("{0} 客户端 {1} 连接成功!", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), session.RemoteEndPoint.Address));
            listBoxLogger.Invoke(appendItemDelegate, String.Format("{0} 客户端 {1} 连接成功!", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), session.RemoteEndPoint.Address));
            TestClientBind();
        }

        static void appServer_NewRequestReceived(SuperSocketSession session, SuperSocket.SocketBase.Protocol.BinaryRequestInfo requestInfo)
        {
            // 对应C++服务器里soap服务完成的那一部分功能
            if (requestInfo.Key.Split('|')[0] == "##")
            {
                String[] uidList = Encoding.UTF8.GetString(requestInfo.Body).Split('|')[0].Split(',');
                Int32 iMode = Convert.ToInt32(Encoding.UTF8.GetString(requestInfo.Body).Split('|')[1]);
                Int32 cid = Convert.ToInt32(Encoding.UTF8.GetString(requestInfo.Body).Split('|')[2]);
                String tableName = Encoding.UTF8.GetString(requestInfo.Body).Split('|')[3];
                Boolean isClose = Convert.ToBoolean(Encoding.UTF8.GetString(requestInfo.Body).Split('|')[4]);
                if (isClose)
                {
                    m_DBTableDic.TryRemove(cid, out tableName);
                }
                else
                {
                    m_DBTableDic.TryAdd(cid, tableName);
                }
                for (Int32 i = 0; i < uidList.Length; i++)
                {
                    try
                    {
                        SendCommand(Convert.ToInt32(uidList[i]), iMode, true);
                    }
                    catch (System.Exception ex)
                    {
                        logger.Error(String.Format("{0}_严重错误:命令下发失败_uid={1} mode={2}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), uidList[i], iMode));
                    }
                }

                session.Close(SuperSocket.SocketBase.CloseReason.ServerClosing);
                return;
            }

            Byte[] charArr = new Byte[16];
            GPB_DEV2SRV tInfo = GPB_DEV2SRV.Parser.ParseFrom(requestInfo.Body);
            GPB_SRV2DEV bInfo = new GPB_SRV2DEV();
            bInfo.UID = 0;
            Random rdm = new Random();
            String str, tmp;
            String ch = String.Empty;
            Char[] wch = new Char[65535];
            Int32[] iWorkMode = new Int32[256];
            Byte[] mccmnc = new Byte[17];
            Byte[] lac = new Byte[17];
            UInt64 seq = 0;
            //判断userid,当用户已经登陆时判断服务器保存的uid和数据包中的uid是否相等
            if (m_SessionDic[session.SessionID].IsLogin && m_SessionDic[session.SessionID].UID != tInfo.UID)
            {
                logger.Error(String.Format("{0}_登陆错误:uid不符 服务器存储uid:{1} 客户端发送包uid:{2}  客户端IP:{3}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), m_SessionDic[session.SessionID].UID, tInfo.UID, session.RemoteEndPoint.Address.ToString()));
                session.Close(SuperSocket.SocketBase.CloseReason.ProtocolError);
                return;
            }

            if (m_SessionDic[session.SessionID].IsLogin)
            {
                UInt64 ulCurr = GetTickCount64();
                m_SessionDic[session.SessionID].UllActive = ulCurr;
            }

            #region 敲门
            if (tInfo.KnockReq != null)
            {

                SrvLogHelper("errcode=" + StatusCode.KnockStart);

                //数据操作
                byte[] respArr = KnockReq(tInfo, bInfo, session, requestInfo);

                //发送数据
                session.Send(respArr, 0, respArr.Length);

                listBoxLogger.Invoke(appendItemDelegate, String.Format("登陆标志(tag):{0}    协议版本:{1}  当前用户数量:{2}", tInfo.KnockReq.Tag, tInfo.KnockReq.PBVer, m_SessionDic.Count));

                SrvLogHelper("errcode=" + StatusCode.KnockEnd);
            }
            #endregion

            #region 登陆
            if (tInfo.LoginReq != null)
            {
                SrvLogHelper("errcode=" + StatusCode.LoginStart);
                // 判断tag是否相符
                if (tInfo.LoginReq.Tag != m_SessionDic[session.SessionID].Tag)
                {
                    String ipstr = session.RemoteEndPoint.Address.ToString();
                    logger.Error("登陆错误:登陆标志(tag)不符,客户端IP: " + ipstr);
                    session.Close(SuperSocket.SocketBase.CloseReason.ProtocolError);
                    return;
                }
                byte[] respArr = LoginReq(tInfo, bInfo, session, requestInfo);

                if (respArr == null)
                {
                    return;
                }
                session.Send(respArr, 0, respArr.Length);
                SrvLogHelper("errcode=" + StatusCode.LoginEnd);
            }
            #endregion

            #region 保活
            if (tInfo.KeepAlive != null && m_SessionDic[session.SessionID].IsLogin)
            {
                SrvLogHelper("errcode=" + StatusCode.KeepAliveStart);
                byte[] respArr = KeepAlive(tInfo, bInfo, session, requestInfo, ref seq);
                session.Send(respArr, 0, respArr.Length);
                SrvLogHelper("errcode=" + StatusCode.KeepAliveEnd);
            }
            #endregion

            #region 轨迹点
            if (0 < tInfo.TrackSend.Count && m_SessionDic[session.SessionID].IsLogin)  //判断数据包中是否包含定位信息
            {
                SrvLogHelper("errcode=" + StatusCode.TrackStart);
                byte[] respArr = TrackSend(tInfo, bInfo, session, requestInfo, ref iWorkMode);
                session.Send(respArr, 0, respArr.Length);
            }
            #endregion 轨迹点

            if (0 < tInfo.HeartRateSend.Count && m_SessionDic[session.SessionID].IsLogin)           //判断数据包中是否包含心率信息
            {
                m_nErrCode = 50000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());
                int n = tInfo.HeartRateSend.Count;
                str = String.Format("心率数据共：{0}条_数据包大小:{1}_{2}%n", n, Convert.ToInt16(requestInfo.Key.Split('|')[1]) - 4, DateTime.Now.ToString("yy-MM-dd HH:mm:ss"));
                listBoxLogger.Invoke(appendItemDelegate, str);
                if (2 == m_nLog) logger.Info(str);

                String sql = String.Empty;
                for (int i = 0; i < tInfo.HeartRateSend.Count; i++)
                {
                    try
                    {
                        String tmp01 = String.Empty;
                        String uch = String.Empty;
                        String mch = String.Empty;

                        if (1 == tInfo.HeartRateSend[i].Label)
                        {
                            m_nErrCode = 51000;
                            if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                            mch = String.Format("insert into trecordlowrate(userid,recordtime,heartrate,CullTime) values({0},now(),'{1}','{2}-{3}-{4} {5}:{6}:{7}');",
                                m_SessionDic[session.SessionID].UID, tInfo.HeartRateSend[i].HeartRate,
                                tInfo.HeartRateSend[i].CurrTime.Year, tInfo.HeartRateSend[i].CurrTime.Month,
                                tInfo.HeartRateSend[i].CurrTime.Day, tInfo.HeartRateSend[i].CurrTime.Hour,
                                tInfo.HeartRateSend[i].CurrTime.Minute, tInfo.HeartRateSend[i].CurrTime.Sec);
                            sql = mch;
                            if (2 == m_nLog) logger.Error(String.Format("sql:{0}_{1}", sql, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                            UserInfo.CreateInstance().InsertLowRate(Convert.ToUInt32(m_SessionDic[session.SessionID].UID), tInfo.HeartRateSend[i].HeartRate, tInfo.HeartRateSend[i].CurrTime);
                            listBoxLogger.Invoke(appendItemDelegate, sql);

                            bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                            bInfo.HeartRateResult = new HEART_RATE_RESULT();
                            bInfo.HeartRateResult.Result = CONN_ERR.Success;
                            Byte[] respArr = SrvPack(bInfo);
                            session.Send(respArr, 0, respArr.Length);
                            return;
                        }
                        m_nErrCode = 52000;
                        if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                        uch = String.Format("{0}-{1}-{2} {3}:{4}:{5}", tInfo.HeartRateSend[i].CurrTime.Year, tInfo.HeartRateSend[i].CurrTime.Month,
                            tInfo.HeartRateSend[i].CurrTime.Day, tInfo.HeartRateSend[i].CurrTime.Hour,
                            tInfo.HeartRateSend[i].CurrTime.Minute, tInfo.HeartRateSend[i].CurrTime.Sec);
                        String tableName = String.Empty;
                        if (m_DBTableDic.TryGetValue(m_SessionDic[session.SessionID].ClassID, out tableName))
                        {
                            if (String.IsNullOrWhiteSpace(tableName))
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'heartrate','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});",
                                m_SessionDic[session.SessionID].UID, tInfo.HeartRateSend[i].HeartRate,
                                tInfo.HeartRateSend[i].CurrTime.Year, tInfo.HeartRateSend[i].CurrTime.Month,
                                tInfo.HeartRateSend[i].CurrTime.Day, tInfo.HeartRateSend[i].CurrTime.Hour,
                                tInfo.HeartRateSend[i].CurrTime.Minute, tInfo.HeartRateSend[i].CurrTime.Sec, iWorkMode[i],
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.HeartRateSend[i].Sequence);
                            }
                            else
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'heartrate','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});insert into " + tableName + "(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'heartrate','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});",
                                m_SessionDic[session.SessionID].UID, tInfo.HeartRateSend[i].HeartRate,
                                tInfo.HeartRateSend[i].CurrTime.Year, tInfo.HeartRateSend[i].CurrTime.Month,
                                tInfo.HeartRateSend[i].CurrTime.Day, tInfo.HeartRateSend[i].CurrTime.Hour,
                                tInfo.HeartRateSend[i].CurrTime.Minute, tInfo.HeartRateSend[i].CurrTime.Sec, iWorkMode[i],
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.HeartRateSend[i].Sequence);
                            }
                        }
                        else
                        {
                            mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'heartrate','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});",
                                m_SessionDic[session.SessionID].UID, tInfo.HeartRateSend[i].HeartRate,
                                tInfo.HeartRateSend[i].CurrTime.Year, tInfo.HeartRateSend[i].CurrTime.Month,
                                tInfo.HeartRateSend[i].CurrTime.Day, tInfo.HeartRateSend[i].CurrTime.Hour,
                                tInfo.HeartRateSend[i].CurrTime.Minute, tInfo.HeartRateSend[i].CurrTime.Sec, iWorkMode[i],
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.HeartRateSend[i].Sequence);
                        }
                        sql = mch;
                        if (2 == m_nLog) logger.Info(String.Format("sql:{0}_{1}", sql, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                        lock (((ICollection)m_pSql).SyncRoot)
                        {
                            // for (; m_nSqlCount < SQL_BUF_NUM; m_nSqlCount++) if (!m_pbIsExist[m_nSqlCount]) break;
                            m_pSql.Add(sql);
                            // m_pbIsExist[m_nSqlCount++] = true;
                        }

                        if (4 < iWorkMode[i])      //实时模式,推送数据
                        {
                            m_nErrCode = 53000;
                            if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                            //推送消息
                            PushData p = new PushData();
                            String stm = uch;
                            uch = m_SessionDic[session.SessionID].UID.ToString();
                            String userid = uch;
                            uch = tInfo.HeartRateSend[i].HeartRate.ToString();
                            String heartrate = uch;
                            String kind = "heardrate";
                            tmp = userid;
                            try
                            {
                                p.Push(userid, stm, kind, heartrate, iWorkMode[i]);
                                if (1 == m_nLog) logger.Info(String.Format("推送消息_uid={0}_{1}", userid, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                            }
                            catch (Exception ex)
                            {
                                logger.Error(String.Format("推送消息失败_uid={0} mode={1}_{2}", userid, iWorkMode[i], DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("接收心率数据不全。");
                    }
                }
                m_nErrCode = 54000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                //大数据包回复确认信息
                if (m_ServerInfo.bIsAnswer || n > 1)
                {
                    bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                    bInfo.HeartRateResult = new HEART_RATE_RESULT();
                    bInfo.HeartRateResult.Result = CONN_ERR.Success;
                    Byte[] respArr01 = SrvPack(bInfo);
                    session.Send(respArr01, 0, respArr01.Length);
                }

                m_nErrCode = 55000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());
                str = String.Format("心率数据共{0}条,接收成功", n);
                listBoxLogger.Invoke(appendItemDelegate, str);

                if (2 == m_nLog) logger.Info(str);
            }

            if (0 < tInfo.StepsCurrent.Count && m_SessionDic[session.SessionID].IsLogin)            //判断数据包中是否包含计步信息
            {
                int n = tInfo.StepsCurrent.Count;
                str = String.Format("计步数据共：{0}条_数据包大小:{1}_{2}\n", n, Convert.ToInt16(requestInfo.Key.Split('|')[1]) - 4, DateTime.Now.ToString("yy-MM-dd HH:mm:ss"));
                listBoxLogger.Invoke(appendItemDelegate, str);

                if (2 == m_nLog) logger.Info(str);
                m_nErrCode = 60000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                String sql = String.Empty;
                String mch = String.Empty;
                for (Int32 i = 0; i < n; i++)
                {
                    try
                    {
                        String tableName = String.Empty;
                        if (m_DBTableDic.TryGetValue(m_SessionDic[session.SessionID].ClassID, out tableName))
                        {
                            if (String.IsNullOrWhiteSpace(tableName))
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'steps','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});",
                                m_SessionDic[session.SessionID].UID, tInfo.StepsCurrent[i].StepsCurrent,
                                tInfo.StepsCurrent[i].CurrTime.Year, tInfo.StepsCurrent[i].CurrTime.Month,
                                tInfo.StepsCurrent[i].CurrTime.Day, tInfo.StepsCurrent[i].CurrTime.Hour,
                                tInfo.StepsCurrent[i].CurrTime.Minute, tInfo.StepsCurrent[i].CurrTime.Sec, iWorkMode[i],
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.StepsCurrent[i].Sequence);
                            }
                            else
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'steps','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});insert into " + tableName + "(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'steps','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});",
                                m_SessionDic[session.SessionID].UID, tInfo.StepsCurrent[i].StepsCurrent,
                                tInfo.StepsCurrent[i].CurrTime.Year, tInfo.StepsCurrent[i].CurrTime.Month,
                                tInfo.StepsCurrent[i].CurrTime.Day, tInfo.StepsCurrent[i].CurrTime.Hour,
                                tInfo.StepsCurrent[i].CurrTime.Minute, tInfo.StepsCurrent[i].CurrTime.Sec, iWorkMode[i],
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.StepsCurrent[i].Sequence);
                            }
                        }
                        else
                        {
                            mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{11},now(),'steps','{1}','{2}-{3}-{4} {5}:{6}:{7}',{8},{9},{10});",
                                m_SessionDic[session.SessionID].UID, tInfo.StepsCurrent[i].StepsCurrent,
                                tInfo.StepsCurrent[i].CurrTime.Year, tInfo.StepsCurrent[i].CurrTime.Month,
                                tInfo.StepsCurrent[i].CurrTime.Day, tInfo.StepsCurrent[i].CurrTime.Hour,
                                tInfo.StepsCurrent[i].CurrTime.Minute, tInfo.StepsCurrent[i].CurrTime.Sec, iWorkMode[i],
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.StepsCurrent[i].Sequence);
                        }
                        sql = mch;
                        if (2 == m_nLog) logger.Info(String.Format("sql:{0}_{1}\n", sql, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                        lock (((ICollection)m_pSql).SyncRoot)
                        {
                            // for (; m_nSqlCount < SQL_BUF_NUM; m_nSqlCount++) if (!m_pbIsExist[m_nSqlCount]) break;
                            m_pSql.Add(sql);
                            // m_pbIsExist[m_nSqlCount++] = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("接收计步数据不全");
                    }
                }
                m_nErrCode = 61000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                //大数据包回复确认信息
                if (m_ServerInfo.bIsAnswer || n > 1)
                {
                    bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                    bInfo.StepResult = new STEP_RESULT();
                    bInfo.StepResult.Result = CONN_ERR.Success;
                    Byte[] respArr = SrvPack(bInfo);
                    session.Send(respArr, 0, respArr.Length);
                }

                m_nErrCode = 62000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());
                str = String.Format("计步数据共{0}条,接收成功\n", n);
                listBoxLogger.Invoke(appendItemDelegate, str);
            }

            if (0 < tInfo.HrStepMode.Count && m_SessionDic[session.SessionID].IsLogin)              //判断数据包中是否包含心率计步信息
            {
                int n = tInfo.HrStepMode.Count;
                str = String.Format("计步心率数据共：{0}条_数据包大小:{1}_{2}\n", n, Convert.ToInt16(requestInfo.Key.Split('|')[1]) - 4, DateTime.Now.ToString("yy-MM-dd HH:mm:ss"));
                listBoxLogger.Invoke(appendItemDelegate, str);
                if (2 == m_nLog) logger.Info(str);
                m_nErrCode = 100000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                String sql = String.Empty;
                String mch = String.Empty;
                for (int i = 0; i < n; i++)
                {
                    try
                    {
                        //检测是否已收到此数据包
                        if (!CheckRecv(m_SessionDic[session.SessionID].UID, tInfo.HrStepMode[i].Sequence))
                        {
                            break;
                        }

                        String uch = String.Empty;
                        iWorkMode[i] = Convert.ToInt32(tInfo.HrStepMode[i].Mode);
                        if (iWorkMode[i] != m_SessionDic[session.SessionID].Mode)
                        {
                            m_SessionDic[session.SessionID].Mode = iWorkMode[i];
                            if (5 > iWorkMode[i])
                            {
                                // 非实时模式
                                UserInfo.CreateInstance().UpdateUserSportModeInfo(0, iWorkMode[i], Convert.ToUInt32(m_SessionDic[session.SessionID].UID));
                            }
                            // 从非实时模式进入实时模式
                            else if (4 < iWorkMode[i] && 5 > m_SessionDic[session.SessionID].Mode)
                            {
                                UserInfo.CreateInstance().UpdateUserSportModeInfo(1, iWorkMode[i], Convert.ToUInt32(m_SessionDic[session.SessionID].UID));
                            }

                            lock (((ICollection)m_CommandList).SyncRoot)
                            {
                                Int32 qIndex = m_CommandList.FindIndex(o => o.UID == m_SessionDic[session.SessionID].UID);
                                if (qIndex != -1)
                                {
                                    //当命令队列中的工作模式和当前模式相等时删除队列中的元素
                                    if (m_CommandList[qIndex].Mode == iWorkMode[i])
                                    {
                                        m_CommandList.RemoveAt(qIndex);
                                    }
                                }
                            }
                        }

                        uch = String.Format("{0}-{1}-{2} {3}:{4}:{5}", tInfo.HrStepMode[i].CurrTime.Year, tInfo.HrStepMode[i].CurrTime.Month,
                            tInfo.HrStepMode[i].CurrTime.Day, tInfo.HrStepMode[i].CurrTime.Hour,
                            tInfo.HrStepMode[i].CurrTime.Minute, tInfo.HrStepMode[i].CurrTime.Sec);
                        String tableName = String.Empty;
                        if (m_DBTableDic.TryGetValue(m_SessionDic[session.SessionID].ClassID, out tableName))
                        {
                            if (String.IsNullOrWhiteSpace(tableName))
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'steps','{1}','{2}',{3},{4},{5});",
                                m_SessionDic[session.SessionID].UID, tInfo.HrStepMode[i].Steps,
                                uch, Convert.ToInt32(tInfo.HrStepMode[i].Mode),
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.HrStepMode[i].Sequence);
                            }
                            else
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'steps','{1}','{2}',{3},{4},{5});insert into " + tableName + "(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'steps','{1}','{2}',{3},{4},{5});",
                                m_SessionDic[session.SessionID].UID, tInfo.HrStepMode[i].Steps,
                                uch, Convert.ToInt32(tInfo.HrStepMode[i].Mode),
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.HrStepMode[i].Sequence);
                            }
                        }
                        else
                        {
                            mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'steps','{1}','{2}',{3},{4},{5});",
                                m_SessionDic[session.SessionID].UID, tInfo.HrStepMode[i].Steps,
                                uch, Convert.ToInt32(tInfo.HrStepMode[i].Mode),
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.HrStepMode[i].Sequence);
                        }
                        sql = mch;
                        if (2 == m_nLog) logger.Info(String.Format("sql:{0}_{1}\n", sql, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                        lock (((ICollection)m_pSql).SyncRoot)
                        {
                            // for (; m_nSqlCount < SQL_BUF_NUM; m_nSqlCount++) if (!m_pbIsExist[m_nSqlCount]) break;
                            m_pSql.Add(sql);
                            // m_pbIsExist[m_nSqlCount++] = true;
                        }
                        tableName = String.Empty;
                        if (m_DBTableDic.TryGetValue(m_SessionDic[session.SessionID].ClassID, out tableName))
                        {
                            if (String.IsNullOrWhiteSpace(tableName))
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'heartrate','{1}','{2}',{3},{4},{5});",
                                m_SessionDic[session.SessionID].UID, tInfo.HrStepMode[i].HeartRate,
                                uch, Convert.ToInt32(tInfo.HrStepMode[i].Mode),
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].ClassID, tInfo.HrStepMode[i].Sequence);
                            }
                            else
                            {
                                mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'heartrate','{1}','{2}',{3},{4},{5});insert into " + tableName + "(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'heartrate','{1}','{2}',{3},{4},{5});",
                                m_SessionDic[session.SessionID].UID, tInfo.HrStepMode[i].HeartRate,
                                uch, Convert.ToInt32(tInfo.HrStepMode[i].Mode),
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].ClassID, tInfo.HrStepMode[i].Sequence);
                            }
                        }
                        else
                        {
                            mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{6},now(),'heartrate','{1}','{2}',{3},{4},{5});",
                                m_SessionDic[session.SessionID].UID, tInfo.HrStepMode[i].HeartRate,
                                uch, Convert.ToInt32(tInfo.HrStepMode[i].Mode),
                                m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].ClassID, tInfo.HrStepMode[i].Sequence);
                        }
                        sql = mch;
                        if (2 == m_nLog) logger.Info(String.Format("sql:{0}_{1}\n", sql, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                        lock (((ICollection)m_pSql).SyncRoot)
                        {
                            // for (; m_nSqlCount < SQL_BUF_NUM; m_nSqlCount++) if (!m_pbIsExist[m_nSqlCount]) break;
                            m_pSql.Add(sql);
                            // m_pbIsExist[m_nSqlCount++] = true;
                        }
                        if (4 < iWorkMode[i])
                        {
                            m_nErrCode = 101000;
                            if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                            //推送消息
                            PushData p = new PushData();
                            String stm = uch;
                            uch = m_SessionDic[session.SessionID].UID.ToString();
                            String userid = uch;
                            uch = tInfo.HrStepMode[i].HeartRate.ToString();
                            String heartrate = uch;
                            String kind = "heardrate";
                            tmp = userid;
                            try
                            {
                                p.Push(userid, stm, kind, heartrate, iWorkMode[i]);
                                if (1 == m_nLog) logger.Info(String.Format("推送消息_uid={0}_{1}\n", userid, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                            }
                            catch (Exception ex)
                            {
                                logger.Error(String.Format("推送消息失败_uid={0} mode={1}_{2}\n", userid, iWorkMode[i], DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.Error("接收心率计步数据不全\n");
                    }
                }
                m_nErrCode = 102000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                //大数据包回复确认信息
                if (m_ServerInfo.bIsAnswer || n > 1)
                {
                    bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                    bInfo.HrStepModeResult = new global::SRV.Model.HR_STEP_MODE_RESULT();
                    bInfo.HrStepModeResult.Result = CONN_ERR.Success;
                    Byte[] respArr = SrvPack(bInfo);
                    session.Send(respArr, 0, respArr.Length);
                }

                m_nErrCode = 63000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());
                str = String.Format("计步心率数据共{0}条,接收成功\n", n);
                listBoxLogger.Invoke(appendItemDelegate, str);
                if (2 == m_nLog) logger.Info(str);
            }
            else if (tInfo.AlbsReq != null && m_SessionDic[session.SessionID].IsLogin)          //判断数据包中是否包含辅助定位信息
            {
                m_nErrCode = 70000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                bInfo.AlbsRsp = new global::SRV.Model.ALBS_RSP();

                Byte[] temp = new Byte[16];

                Array.ConstrainedCopy(tInfo.AlbsReq.CenterCell.Mccmnc.ToByteArray(), 0, mccmnc, 0, tInfo.AlbsReq.CenterCell.Mccmnc.ToByteArray().Length);
                CommonTools.InvCipher(ref mccmnc, true);
                Array.ConstrainedCopy(tInfo.AlbsReq.CenterCell.LAC.ToByteArray(), 0, lac, 0, tInfo.AlbsReq.CenterCell.LAC.ToByteArray().Length);
                CommonTools.InvCipher(ref lac, true);
                int nlac = (byte)lac[0] + (byte)lac[1] * 256;
                Buffer.BlockCopy(tInfo.AlbsReq.CenterCell.CELLID.ToByteArray(), 0, temp, 0, tInfo.AlbsReq.CenterCell.CELLID.ToByteArray().Length);
                CommonTools.InvCipher(ref temp, true);
                int ncell = (byte)temp[0] + (byte)temp[1] * 256;
                ch = String.Format("数据包类型:PT_ALBS_REQ  CenterCell=mccmnc:{0} LAC:{1} CELL_ID:{2} Rssi:{3}  ", Encoding.Default.GetString(mccmnc),
                    nlac, ncell, tInfo.AlbsReq.CenterCell.Rssi);
                str = ch;
                listBoxLogger.Invoke(appendItemDelegate, str);

                bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                bInfo.AlbsRsp.Result = CONN_ERR.Success;
                bInfo.AlbsRsp.AlbsData = ByteString.CopyFrom("kkk3333333333", Encoding.Default);
                Byte[] respArr = SrvPack(bInfo);
                session.Send(respArr, 0, respArr.Length);
                m_nErrCode = 71000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());
            }
            else if (tInfo.DevNotifySend != null && m_SessionDic[session.SessionID].IsLogin)                //判断数据包中是否包含设备通知信息
            {
                m_nErrCode = 80000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                bInfo.DevNotifyResult = new global::SRV.Model.DEV_NOTIFY_RESULT();

                if (tInfo.DevNotifySend.UserInfoReq != 0)
                {
                    m_nErrCode = 81000;
                    if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                    //返回成功信息
                    bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                    bInfo.DevNotifyResult.Result = CONN_ERR.Success;
                    bInfo.DevNotifyResult.Tag = tInfo.DevNotifySend.Tag;
                    Byte[] respArr = SrvPack(bInfo);
                    session.Send(respArr, 0, respArr.Length);

                    //下发用户信息
                    User pUserInfo = UserInfo.CreateInstance().UserLogin(System.Text.Encoding.Default.GetString(m_SessionDic[session.SessionID].Imei.ToByteArray()).TrimEnd('\0'));

                    bInfo = new GPB_SRV2DEV();
                    bInfo.UID = pUserInfo.userid;
                    bInfo.OrderNotify = new global::SRV.Model.ORDER_NOTIFY();
                    bInfo.OrderNotify.Tag = 12;
                    bInfo.OrderNotify.Userinfo = new USER_INFO();
                    bInfo.OrderNotify.Userinfo.Name = ByteString.CopyFrom(System.Text.Encoding.Unicode.GetBytes(pUserInfo.truename));
                    bInfo.OrderNotify.Userinfo.Sex = pUserInfo.sex ? SEX.Male : SEX.Girl;
                    bInfo.OrderNotify.Userinfo.Classroom = ByteString.CopyFrom(pUserInfo.classname, Encoding.Unicode);
                    bInfo.OrderNotify.Userinfo.School = ByteString.CopyFrom(pUserInfo.schoolname, Encoding.Unicode);
                    bInfo.OrderNotify.Userinfo.EducationId = ByteString.CopyFrom(pUserInfo.educode, Encoding.Unicode);
                    bInfo.OrderNotify.Userinfo.Height = pUserInfo.height;
                    bInfo.OrderNotify.Userinfo.Weight = pUserInfo.weight;
                    bInfo.OrderNotify.Userinfo.Age = pUserInfo.age;
                    str = String.Format("{0}_{1}_下发用户信息", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), pUserInfo.truename);
                    listBoxLogger.Invoke(appendItemDelegate, str);
                    if (2 == m_nLog) logger.Info(str);
                }
                else
                {
                    m_nErrCode = 82000;
                    if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                    ch = String.Format("数据包类型:PT_DEV_NOTIFY_SEND  Tag={0}  charge_ind={1}  Steps_meet_target={2}", tInfo.DevNotifySend.Tag,
                        tInfo.DevNotifySend.ChargeInd, tInfo.DevNotifySend.StepsMeetTarget);
                    str = ch;
                    listBoxLogger.Invoke(appendItemDelegate, str);

                    bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                    bInfo.DevNotifyResult = new global::SRV.Model.DEV_NOTIFY_RESULT();
                    bInfo.DevNotifyResult.Result = CONN_ERR.Success;
                    bInfo.DevNotifyResult.Tag = 1123;
                }
                Byte[] respArr01 = SrvPack(bInfo);
                session.Send(respArr01, 0, respArr01.Length);
                m_nErrCode = 83000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());
            }
            else if (tInfo.OrderResp != null && m_SessionDic[session.SessionID].IsLogin)            //判断数据包中是否包含指令下发信息
            {
                m_nErrCode = 90000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                UInt32 tag = tInfo.OrderResp.Tag;
                int result = Convert.ToInt32(tInfo.OrderResp.Result);
                ch = String.Format("数据包类型:PT_ORDER_RESP  Tag={0}  result={1} ", tag,
                    tInfo.OrderResp.Result);
                str = ch;
                listBoxLogger.Invoke(appendItemDelegate, str);
                if (2 == m_nLog) logger.Info(str);

                //解除命令下发锁定
                if (1 == result)
                {
                    m_SessionDic[session.SessionID].IsLock = false;
                }

                //设置工作模式
                Int32 qIndex = m_CommandList.FindIndex(o => o.UID == m_SessionDic[session.SessionID].UID);

                if (qIndex != -1 && 9 == tag && 1 == result)
                {
                    m_nErrCode = 91000;
                    if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());

                    if (m_SessionDic[session.SessionID].Mode != m_CommandList[qIndex].Mode)
                    {
                        if (UserInfo.CreateInstance().UpdateUserSportModeInfo(1, m_CommandList[qIndex].Mode, Convert.ToUInt32(m_CommandList[qIndex].UID)))
                        {
                            m_SessionDic[session.SessionID].Mode = m_CommandList[qIndex].Mode;
                        }
                        else
                        {
                            listBoxLogger.Invoke(appendItemDelegate, String.Format("update tuserstatusinfo set OldSportMode=SportMode,sportmode={0} where userid={1}", m_CommandList[qIndex].Mode, Convert.ToUInt32(m_CommandList[qIndex].UID)));
                        }
                        str = String.Format("{0}_用户:{1} 工作模式切换为{2}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), m_SessionDic[session.SessionID].TrueName, m_CommandList[qIndex].Mode);
                        if (2 == m_nLog) logger.Info(str);
                        listBoxLogger.Invoke(appendItemDelegate, str);
                    }

                    lock (((ICollection)m_CommandList).SyncRoot)
                    {
                        m_CommandList.RemoveAt(qIndex);
                    }

                    if (1 == m_nLog) logger.Info(String.Format("更新新工作模式成功 userid={0} mode={1}_{2}\n", m_SessionDic[session.SessionID].UID, m_SessionDic[session.SessionID].Mode, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                }

                //下发登陆服务器参数
                if (2 == tag && 1 == result)            //下发登陆服务器参数成功                                                                                                                                      
                {
                    if (2 == m_nLog) logger.Info(String.Format("{0}_下发登陆服务器参数成功_uid={1}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), m_SessionDic[session.SessionID].UID));

                    lock (((ICollection)m_SendCmdList).SyncRoot)
                    {
                        if (m_SendCmdList.FindIndex(o => o.UID == m_SessionDic[session.SessionID].UID) != -1)
                        {
                            m_SendCmdList.RemoveAt(m_SendCmdList.FindIndex(o => o.UID == m_SessionDic[session.SessionID].UID));
                        }
                    }
                }

                //手表参数恢复到出厂设置状态
                if (10 == tag && 1 == result)           //下发恢复到出厂设置成功
                {
                    if (2 == m_nLog) logger.Info(String.Format("{0}_手表参数恢复到出厂设置状态_uid={1}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), m_SessionDic[session.SessionID].UID));
                    session.Close(SuperSocket.SocketBase.CloseReason.ServerClosing);
                }

                m_nErrCode = 92000;
                if (1 == m_nLog) logger.Error("errcode=" + m_nErrCode.ToString());
            }
        }

        private static byte[] SrvPack(GPB_SRV2DEV obj)
        {
            Int32 length = obj.CalculateSize();
            byte[] s_Content = obj.ToByteArray();
            byte[] s_Total = new byte[s_Content.Length + 4];
            s_Total[0] = Convert.ToByte('@');
            s_Total[1] = Convert.ToByte('@');
            Byte[] packLength = BitConverter.GetBytes(Convert.ToInt16(s_Content.Length + 4));
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(packLength);
            }
            System.Buffer.BlockCopy(packLength, 0, s_Total, 2, 2);
            System.Buffer.BlockCopy(s_Content, 0, s_Total, 4, s_Content.Length);
            return s_Total;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "服务正在运行，您确定关闭服务器？", "关闭服务器", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                m_DBTimer.Stop();
                m_ConnTimer.Stop();
                m_CommandTimer.Stop();
                m_SrvParamTimer.Stop();
                m_SrvLoggerTimer.Stop();
                m_DBThread.Abort();
                m_DBThread.Join();
                loggerStr = String.Format("服务器已关闭。");
                logger.Info("服务器正在关闭中...");
                m_SrvLogger.Add(loggerStr);
                foreach (SuperSocketSession session in appServer.GetAllSessions())
                {
                    session.Close(SuperSocket.SocketBase.CloseReason.ServerClosing);
                }
                notifyIcon.Visible = false;
                appServer.Stop();
            }
        }

        private void buttonSendCommand_Click(object sender, EventArgs e)
        {
            try
            {
                //指令下发
                string sessionID = (string)clientListcombox.Invoke(new getcomboxSelected(D_sessionID));
                string mtype = (string)clientListcombox.Invoke(new getcomboxSelected(D_getMethodType));
                TestSend(sessionID, Convert.ToInt32(mtype));
            }
            catch (Exception ex)
            {
                MessageBox.Show("目标客户端未选中！！！选中后继续！");
                return;
            }
        }

        private void buttonStartSrv_Click(object sender, EventArgs e)
        {
            appServer.Start();
            logger = appServer.Logger;
            m_DBTimer.Start();
            m_ConnTimer.Start();
            m_CommandTimer.Start();
            m_SrvParamTimer.Start();
            m_SrvLoggerTimer.Start();
            m_DBThread.Start();
            loggerStr = String.Format("服务器已经启动，开始监听本机{0}端口...", m_ServerInfo.wLoginPort);
            logger.Info(loggerStr);
            m_SrvLogger.Add(loggerStr);
            listBoxLogger.ClearSelected();
            logger.Info(String.Format("程序启动..._{0}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
            buttonStartSrv.Enabled = false;
            buttonClose.Enabled = true;
            ToolStripMenuItemClose.Enabled = true;
        }

        /// <summary>
        /// 命令下发
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="mode"></param>
        /// <param name="isAdd"></param>
        /// <returns></returns>
        private static Int32 SendCommand(Int32 userid, Int32 mode, Boolean isAdd = false)
        {
            var query = m_SessionDic.Where(o => o.Value.UID == userid).FirstOrDefault();
            ClientManager obj = new ClientManager();
            //手表未登陆或工作模式超出范围[1,6]
            if (query.Equals(default(KeyValuePair<String, ClientManager>)) || 1 > mode || 6 < mode)
            {
                if (1 == m_nLog) logger.Error(String.Format("命令下发失败!userid={0},mode={1} {2}", userid, mode, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                return -1;
            }

            GPB_SRV2DEV bInfo = new GPB_SRV2DEV();
            bInfo.UID = Convert.ToUInt32(userid);
            bInfo.OrderNotify = new global::SRV.Model.ORDER_NOTIFY();
            bInfo.OrderNotify.Tag = 9;
            bInfo.OrderNotify.WorkingMode = (WORKING_MODE)mode;

            try
            {
                var query01 = m_SendCmdList.Where(o => o.UID == userid).FirstOrDefault();

                if (query01 != null && query.Value.IsLock && mode == query01.Mode && isAdd)
                {
                    if (1 == m_nLog) logger.Error(String.Format("{0}_命令下发失败,原因:已锁定!userid={1},mode={2} ", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), userid, mode));
                    return -1;
                }
            }
            catch (System.Exception ex)
            {
            	// To do.
            }

            SuperSocketSession session = query.Value.Session;
            Byte[] respArr = SrvPack(bInfo);
            session.Send(respArr, 0, respArr.Length);
            query.Value.IsLock = true;

            if (2 == m_nLog) logger.Error(String.Format("命令下发成功!userid = {0}, mode = {1} {2}", userid, mode, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));

            if (isAdd)
            {
                var query02 = m_SessionDic.Where(o => o.Key == session.SessionID).FirstOrDefault();
                //锁定命令下发
                if (!query02.Equals(default(KeyValuePair<String, ClientManager>)))
                {
                    query02.Value.IsLock = true;
                }
                lock (((ICollection)m_CommandList).SyncRoot)
                {
                    //加入命令队列
                    m_CommandList.RemoveAll(o => o.UID == userid);
                    ClientManager obj01 = new ClientManager();
                    obj01.UID = userid;
                    obj01.Mode = mode;
                    m_CommandList.Add(obj01); 
                }
            }

            return 0;
        }

        /// <summary>
        /// 下发登陆服务器参数
        /// </summary>
        /// <param name="s"></param>
        /// <param name="uid"></param>
        /// <param name="serverip"></param>
        /// <param name="serverport"></param>
        /// <param name="isAdd"></param>
        /// <returns></returns>
        private static Boolean SendLoginServerParam(SuperSocketSession s, Int32 uid, SByte[] serverip, UInt16 serverport, Boolean isAdd = false)
        {
            var query = m_SessionDic.Where(p => p.Value.UID == uid).FirstOrDefault();
            if (query.Value.IsLock && isAdd)
            {
                if (1 == m_nLog) logger.Error(String.Format("{0}_服务器参数命令下发失败,原因:已锁定!userid={1}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), uid));
                return false;
            }

            GPB_SRV2DEV bInfo = new GPB_SRV2DEV();
            Byte[] ch = new Byte[17];
            Byte[] ch1 = new Byte[17];

            bInfo.UID = Convert.ToUInt32(uid);
            bInfo.OrderNotify = new global::SRV.Model.ORDER_NOTIFY();
            bInfo.OrderNotify.Tag = 2;
            bInfo.OrderNotify.ServerParm = new SERVER_PARM_INFO();
            Buffer.BlockCopy(serverip, 0, ch, 0, 16);
            CommonTools.Cipher(ref ch, true);
            ch[16] = 0;
            bInfo.OrderNotify.ServerParm.Address = ByteString.CopyFrom(ch);
            ch1 = BitConverter.GetBytes(serverport);
            CommonTools.Cipher(ref ch1, true);
            bInfo.OrderNotify.ServerParm.Port = ByteString.CopyFrom(ch1);
            Byte[] respArr = SrvPack(bInfo);
            s.Send(respArr, 0, respArr.Length);
            String str = String.Empty, imei = String.Empty, name = String.Empty;
            str = Encoding.Unicode.GetString(ch);
            if (query.Equals(default(KeyValuePair<String, ClientManager>))) return false;
            imei = Encoding.Unicode.GetString(query.Value.Imei.ToByteArray());
            if (2 == m_nLog) logger.Info(String.Format("{0}_服务器参数下发__({1},{2})_IP:{3}  Port:{4}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), uid, imei, str, serverport));

            //锁定命令下发
            if (isAdd)
            {
                var query02 = m_SessionDic.Where(o => o.Key == s.SessionID).FirstOrDefault();
                //锁定命令下发
                if (!query02.Equals(default(KeyValuePair<String, ClientManager>)))
                {
                    query02.Value.IsLock = true;
                }
                lock (((ICollection)m_SendCmdList).SyncRoot)
                {
                    //添加命令队列元素
                    m_SendCmdList.RemoveAll(o => o.UID == uid);
                    ClientManager obj01 = new ClientManager();
                    obj01.UID = uid;
                    obj01.ServerIP = serverip;
                    obj01.ServerPort = serverport;
                    m_SendCmdList.Add(obj01);
                }
            }

            return true;
        }

        /// <summary>
        /// 获取已收到的数据的最大序列号
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        private static UInt64 GetMaxIndex(Int32 uid)
        {
            var result = m_RecvInfo.Where(p => p.UID == uid).FirstOrDefault();
            return result == null ? 0 : result.BaseIndex;
        }

        /// <summary>
        /// 检测已收到的数据
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static Boolean CheckRecv(Int32 uid, UInt64 index)
        {

            //生活模式直接录入数据库
            if (0 == index) return true;

            //查找元素
            int m = m_RecvInfo.FindIndex(o => o.UID == uid);

            //没有找到元素
            if (-1 == m)
            {
                RecvInfo m_Info = new RecvInfo();

                m_Info.UID = uid;
                m_Info.UllRecv[0] = index;
                m_Info.BaseIndex = index;
                m_Info.N = 1;
                m_RecvInfo.Add(m_Info);
                m_iRecvCount++;
                return true;
            }

            //判断数据包是否有效
            if (index == m_RecvInfo[m].BaseIndex + 1 || 1 == index)
            {
                m_RecvInfo[m].BaseIndex = index;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 用于log日志帮助
        /// </summary>
        /// <returns>用于Srv服务程序日志服务</returns>
        private static void SrvLogHelper(dynamic msg, int m_nLog = 1)
        {

            switch (m_nLog)
            {
                case 1:
                    logger.Error(msg); break;
                case 2:
                    logger.Info(msg); break;
            }
        }

        #region SuperSocket 配置选项
        /// <summary>
        ///  superSocket 配置选项
        /// </summary>
        /// <param name="appServer">appServer</param>
        /// <returns></returns>
        private SuperSocketServer SuperSocketSetup(SuperSocketServer appServer)
        {
            var serverConfig = new ServerConfig
            {
                Name = "SuperSocketForm",//服务器实例的名称
                ServerType = "AgileServer.Socket.TelnetServer, AgileServer.Socket",
                Ip = "Any",//Any - 所有的IPv4地址 IPv6Any - 所有的IPv6地址
                Mode = SuperSocket.SocketBase.SocketMode.Tcp,//服务器运行的模式, Tcp (默认) 或者 Udp
                Port = Convert.ToInt32(numericUpDownPort.Value),//服务器监听的端口
                SendingQueueSize = 10,//发送队列最大长度, 默认值为5
                MaxConnectionNumber = 10000,//可允许连接的最大连接数
                LogCommand = false,//是否记录命令执行的记录
                LogBasicSessionActivity = false,//是否记录session的基本活动，如连接和断开
                LogAllSocketException = false,//是否记录所有Socket异常和错误
                MaxRequestLength = 1024 * 10,//最大允许的请求长度，默认值为1024
                //TextEncoding = "UTF-8",//文本的默认编码，默认值是 ASCII
                KeepAliveTime = 6000,//网络连接正常情况下的keep alive数据的发送间隔, 默认值为 600, 单位为秒
                KeepAliveInterval = 600,//Keep alive失败之后, keep alive探测包的发送间隔，默认值为 60, 单位为秒
                ClearIdleSession = false, // 是否定时清空空闲会话，默认值是 false;
                ClearIdleSessionInterval = 120//: 清空空闲会话的时间间隔, 默认值是120, 单位为秒;
            };

            var rootConfig = new RootConfig()
            {
                MaxWorkingThreads = 1000,//线程池最大工作线程数量
                MinWorkingThreads = 10,// 线程池最小工作线程数量;
                MaxCompletionPortThreads = 1000,//线程池最大完成端口线程数量;
                MinCompletionPortThreads = 10,// 线程池最小完成端口线程数量;
                DisablePerformanceDataCollector = true,// 是否禁用性能数据采集;
                PerformanceDataCollectInterval = 60,// 性能数据采集频率 (单位为秒, 默认值: 60);
                Isolation = SuperSocket.SocketBase.IsolationMode.AppDomain// 服务器实例隔离级别

            };
            if (appServer == null)
            {
                return null;
            }
            else if (!appServer.Setup(rootConfig, serverConfig))
            {
                return null;
            }
            return appServer;
        }
        #endregion

        #region  敲门程序处理
        /// <summary>
        /// 敲门程序处理 
        /// </summary>
        /// <param name="tInfo">请求实体</param>
        /// <param name="bInfo">响应实体</param>
        /// <param name="session">Socket Session</param>
        /// <param name="requestInfo">Socket Request</param>
        private static byte[] KnockReq(GPB_DEV2SRV tInfo, GPB_SRV2DEV bInfo, SuperSocketSession session, SuperSocket.SocketBase.Protocol.BinaryRequestInfo requestInfo)
        {
            Byte[] charArr = new Byte[16];
            Random rdm = new Random();
            //生成由随机数字组成并且长度为10的字条串
            for (Int32 i = 0; i < 10; i++)
            {
                charArr[i] = Convert.ToByte(48 + rdm.Next(10));
            }
            m_SessionDic[session.SessionID].Tag = tInfo.KnockReq.Tag;
            Array.ConstrainedCopy(charArr, 0, m_SessionDic[session.SessionID].Rand, 0, m_SessionDic[session.SessionID].Rand.Length);
            CommonTools.Cipher(ref charArr);    //aes加密
            bInfo.KnockResp = new KNOCK_RESP();
            bInfo.KnockResp.Tag = tInfo.KnockReq.Tag;               //设置tag
            bInfo.KnockResp.EncryptRandomNumber = ByteString.CopyFrom(charArr);
            Byte[] respArr = SrvPack(bInfo);

            return respArr;
        }
        #endregion

        #region  登陆程序处理
        /// <summary>
        /// 登陆程序处理
        /// </summary>
        /// <param name="tInfo">请求实体</param>
        /// <param name="bInfo">响应实体</param>
        /// <param name="session">Socket Session</param>
        /// <param name="requestInfo">Socket Request</param>
        /// <returns></returns>
        private static byte[] LoginReq(GPB_DEV2SRV tInfo, GPB_SRV2DEV bInfo, SuperSocketSession session, SuperSocket.SocketBase.Protocol.BinaryRequestInfo requestInfo)
        {
            string ch = string.Empty;
            string str = string.Empty;
            Byte[] respArr;
            Byte[] szimei = new Byte[16];
            Byte[] rnd = new Byte[133];
            Byte[] ret = new Byte[33];
            Byte[] ip = new Byte[16];
            Array.ConstrainedCopy(tInfo.LoginReq.IMEI.ToByteArray(), 0, szimei, 0, tInfo.LoginReq.IMEI.ToByteArray().Length);
            CommonTools.InvCipher(ref szimei, true);  //解密aes

            m_SessionDic[session.SessionID].Imei = ByteString.CopyFrom(szimei);
            Array.ConstrainedCopy(m_SessionDic[session.SessionID].Rand, 0, rnd, 0, 10);
            Array.ConstrainedCopy(szimei, 0, rnd, 10, 16);
            Byte[] rnd01 = CommonTools.md5(rnd);
            rnd = new Byte[133];
            Array.ConstrainedCopy(rnd01, 0, rnd, 0, rnd01.Length);
            Array.ConstrainedCopy(CommonTools.Hex2ASC(tInfo.LoginReq.EncryptNumberResult.ToByteArray(), 16), 0, ret, 0, 33);
            if (!System.Text.Encoding.Default.GetString(rnd).TrimEnd('\0').Equals(System.Text.Encoding.Default.GetString(ret).TrimEnd('\0')))
            {
                String ipstr = session.RemoteEndPoint.Address.ToString();
                logger.Error("登陆错误:加密验证失败,客户端IP: " + ipstr);
                session.Close(SuperSocket.SocketBase.CloseReason.ProtocolError);
                return null;
            }
            ch = String.Format("数据包类型:PT_LOGIN_REQ  Tag={0}  EncryptNumberResult={1}  IMEI={2}", tInfo.LoginReq.Tag, System.Text.Encoding.Default.GetString(rnd), System.Text.Encoding.Default.GetString(szimei));
            str = ch;
            listBoxLogger.Invoke(appendItemDelegate, str);

            if (System.Text.Encoding.Default.GetString(szimei).TrimEnd('\0').Length == 0)           //如果imei码长度为0
            {
                m_SrvLogger.Add(String.Format("{0}_imei码不能为空。", DateTime.Now.ToString("yy-MM-dd HH:mm:ss")));
                SrvLogHelper(String.Format("{0}_imei码不能为空。", DateTime.Now.ToString("yy-MM-dd HH:mm:ss")), 2);
                session.Close(SuperSocket.SocketBase.CloseReason.ProtocolError);
                return null;
            }
            //从数据库中获取用户信息
            User pUserInfo = UserInfo.CreateInstance().UserLogin(System.Text.Encoding.Default.GetString(szimei).TrimEnd('\0'));

            SrvLogHelper("errcode=" + StatusCode.UserLoginDB);

            //获取用户信息失败
            if (0 == pUserInfo.rt || 1 == pUserInfo.rt)
            {
                SrvLogHelper("errcode=" + StatusCode.UserLoginOP);

                ch = String.Format("不存在的imei号:{0},客户端IP {1} {2}-{3}-{4} {5}:{6}:{7}\r\n", System.Text.Encoding.Default.GetString(szimei), session.RemoteEndPoint.Address.ToString(),
                    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                logger.Warn(ch);
                str = ch;
                listBoxLogger.Invoke(appendItemDelegate, str);

                bInfo.UID = 0;
                bInfo.LoginResp = new LOGIN_RESP();
                bInfo.LoginResp.Tag = tInfo.LoginReq.Tag;
                bInfo.LoginResp.Result = CONN_ERR.Invaliddev;
            }
            else if (2 == pUserInfo.rt)      //获取用户信息成功                         
            {
                SrvLogHelper("errcode=" + StatusCode.UserLoginOP2);

                if (!m_ServerInfo.bIsLogin)     //判断服务器是否为验证服务器,否则将作为登陆服务器
                {
                    //初始化服务器参数,IP地址和端口号
                    Byte[] ch01 = new Byte[16];
                    bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                    bInfo.OrderNotify = new global::SRV.Model.ORDER_NOTIFY();
                    bInfo.OrderNotify.Tag = 2;
                    bInfo.OrderNotify.ServerParm = new SERVER_PARM_INFO();
                    Array.ConstrainedCopy(System.Text.Encoding.Default.GetBytes(pUserInfo.serverip), 0, ch01, 0, System.Text.Encoding.Default.GetByteCount(pUserInfo.serverip));

                    CommonTools.Cipher(ref ch01);
                    bInfo.OrderNotify.ServerParm.Address = ByteString.CopyFrom(ch01);
                    ch01 = new Byte[16];
                    Array.ConstrainedCopy(BitConverter.GetBytes(pUserInfo.serverport), 0, ch01, 0, BitConverter.GetBytes(pUserInfo.serverport).Length);
                    CommonTools.Cipher(ref ch01);
                    bInfo.OrderNotify.ServerParm.Port = ByteString.CopyFrom(ch01);
                    respArr = SrvPack(bInfo);
                    return respArr;
                }

                var query = m_SessionDic.Where(p => p.Key == session.SessionID).FirstOrDefault();
                if (query.Equals(default(KeyValuePair<String, ClientManager>)))
                {
                    session.Close(SuperSocket.SocketBase.CloseReason.ProtocolError);
                    return null;
                }

                //保存用户信息
                m_SessionDic[session.SessionID].UID = Convert.ToInt32(pUserInfo.userid);
                m_SessionDic[session.SessionID].IsLogin = true;
                m_SessionDic[session.SessionID].ClassID = pUserInfo.classid;
                m_SessionDic[session.SessionID].SchoolID = pUserInfo.schoolid;
                m_SessionDic[session.SessionID].TrueName = pUserInfo.truename;
                m_SessionDic[session.SessionID].Mode = pUserInfo.sportmode;
                m_SessionDic[session.SessionID].ClassName = pUserInfo.classname;
                m_SessionDic[session.SessionID].SchoolName = pUserInfo.schoolname;

                ch = String.Format("用户:{0} 登陆成功!", pUserInfo.truename);
                str = ch;
                listBoxLogger.Invoke(appendItemDelegate, str);
                logger.Info(String.Format("{0}_{1}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), str));

                //发送数据
                bInfo.UID = pUserInfo.userid;
                bInfo.LoginResp = new LOGIN_RESP();
                bInfo.LoginResp.Tag = tInfo.LoginReq.Tag;
                bInfo.LoginResp.Result = CONN_ERR.Success;
                bInfo.LoginResp.UID = pUserInfo.userid;
                bInfo.LoginResp.GMTTime = new TIME_INFO();
                bInfo.LoginResp.GMTTime.Year = Convert.ToUInt32(DateTime.Now.Year);
                bInfo.LoginResp.GMTTime.Month = Convert.ToUInt32(DateTime.Now.Month);
                bInfo.LoginResp.GMTTime.Day = Convert.ToUInt32(DateTime.Now.Day);
                bInfo.LoginResp.GMTTime.Hour = Convert.ToUInt32(DateTime.Now.Hour);
                bInfo.LoginResp.GMTTime.Minute = Convert.ToUInt32(DateTime.Now.Minute);
                bInfo.LoginResp.GMTTime.Sec = Convert.ToUInt32(DateTime.Now.Second);
            }
            byte[] respArr01 = SrvPack(bInfo);
            return respArr01;
        }
        #endregion

        #region 保活程序处理
        /// <summary>
        /// 保活信息处理
        /// </summary>
        /// <param name="tInfo">请求实体</param>
        /// <param name="bInfo">响应实体</param>
        /// <param name="session">Socket Session</param>
        /// <param name="requestInfo">Socket Request</param>
        /// <returns></returns>
        private static byte[] KeepAlive(GPB_DEV2SRV tInfo, GPB_SRV2DEV bInfo, SuperSocketSession session, SuperSocket.SocketBase.Protocol.BinaryRequestInfo requestInfo, ref UInt64 seq)
        {
            string ch = string.Empty;
            string str = string.Empty;
            seq = GetMaxIndex(m_SessionDic[session.SessionID].UID);
            ch = String.Format("数据包类型:KEEP_ALIVE_INFO  Battery_level={0}  Counter={1}  uid={2}_seq={3}__", tInfo.KeepAlive.BatteryLevel, tInfo.KeepAlive.Counter, m_SessionDic[session.SessionID].UID, seq);
            str = ch + DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
            listBoxLogger.Invoke(appendItemDelegate, str);

            // 更新计时器
            m_SessionDic[session.SessionID].UllTimer = GetTickCount64();

            // 更新连接用户在线状态
            UserInfo.CreateInstance().UpdateUserOnlineInfo(tInfo.KeepAlive.BatteryLevel, Convert.ToUInt32(m_SessionDic[session.SessionID].UID));

            bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
            bInfo.KeepAliveResp = new KEEP_ALIVE_RSP();
            bInfo.KeepAliveResp.SequenceAck = seq;
            bInfo.KeepAliveResp.Result = CONN_ERR.Success;
            Byte[] respArr = SrvPack(bInfo);
            return respArr;
        }
        #endregion

        #region 轨迹点程序处理
        /// <summary>
        /// 轨迹点程序处理
        /// </summary>
        /// <param name="tInfo">请求实体</param>
        /// <param name="bInfo">响应实体</param>
        /// <param name="session">Socket Session</param>
        /// <param name="requestInfo">Socket Request</param>
        /// <returns></returns>
        private static byte[] TrackSend(GPB_DEV2SRV tInfo, GPB_SRV2DEV bInfo, SuperSocketSession session, SuperSocket.SocketBase.Protocol.BinaryRequestInfo requestInfo, ref Int32[] iWorkMode)
        {
            string ch = string.Empty;
            string str = string.Empty;
            Int32 n = tInfo.TrackSend.Count;
            SByte[] lgn = new SByte[16];
            SByte[] lat = new SByte[16];
            Byte[] lgn01 = new Byte[16];
            Byte[] lat01 = new Byte[16];
            String mch = String.Empty;
            str = String.Format("轨迹点数据共{0}条_数据包大小:{1}_{2}\n", n, Convert.ToInt16(requestInfo.Key.Split('|')[1]) - 4, DateTime.Now.ToString("yy-MM-dd HH:mm:ss"));
            listBoxLogger.Invoke(appendItemDelegate, str);

            SrvLogHelper(str, 2);

            String sql = String.Empty;
            for (Int32 i = 0; i < tInfo.TrackSend.Count; i++)
            {
                SrvLogHelper("errcode=" + StatusCode.TrackWork);
                try
                {
                    iWorkMode[i] = Convert.ToInt32(tInfo.TrackSend[i].WorkingMode);
                    if (iWorkMode[i] != m_SessionDic[session.SessionID].Mode)
                    {
                        m_SessionDic[session.SessionID].Mode = iWorkMode[i];
                        if (5 > iWorkMode[i])
                        {
                            // 非实时模式
                            UserInfo.CreateInstance().UpdateUserSportModeInfo(0, iWorkMode[i], Convert.ToUInt32(m_SessionDic[session.SessionID].UID));
                        }
                        // 从非实时模式进入实时模式
                        else if (4 < iWorkMode[i] && 5 > m_SessionDic[session.SessionID].Mode)
                        {
                            UserInfo.CreateInstance().UpdateUserSportModeInfo(1, iWorkMode[i], Convert.ToUInt32(m_SessionDic[session.SessionID].UID));
                        }
                        lock (((ICollection)m_CommandList).SyncRoot)
                        {
                            Int32 qIndex = m_CommandList.FindIndex(o => o.UID == m_SessionDic[session.SessionID].UID);
                            if (qIndex != -1)
                            {
                                //当命令队列中的工作模式和当前模式相等时删除队列中的元素
                                if (m_CommandList[qIndex].Mode == iWorkMode[i])
                                {
                                    m_CommandList.RemoveAt(qIndex);
                                }
                            }
                        }
                    }
                    Array.ConstrainedCopy(tInfo.TrackSend[i].GpsInfo.Lng.ToByteArray(), 0, lgn01, 0, tInfo.TrackSend[i].GpsInfo.Lng.ToByteArray().Length);
                    Array.ConstrainedCopy(tInfo.TrackSend[i].GpsInfo.Lat.ToByteArray(), 0, lat01, 0, tInfo.TrackSend[i].GpsInfo.Lat.ToByteArray().Length);
                    CommonTools.InvCipher(ref lgn01, true);
                    CommonTools.InvCipher(ref lat01, true);
                    Buffer.BlockCopy(lgn01, 0, lgn, 0, 16);
                    Buffer.BlockCopy(lat01, 0, lat, 0, 16);
                    //检测字符串
                    Boolean isEnd = false, isInvalid = false;
                    for (Int32 ii = 0; ii < 16; ii++)
                    {
                        //判断字符串是否已结束
                        if (0 == lgn[ii])
                        {
                            isEnd = true;
                            break;
                        }
                        //解密出来的经纬度只允许包含"."和数字,其他字符非法
                        if ((46 > lgn[ii] || 57 < lgn[ii]) && ii > 5)
                        {
                            isInvalid = true;
                            break;
                        }
                    }
                    if (!isEnd) lgn[15] = 0;
                    isEnd = false;
                    for (Int32 ii = 0; ii < 16; ii++)
                    {
                        //判断字符串是否已结束
                        if (0 == lat[ii])
                        {
                            isEnd = true;
                            break;
                        }
                        //解密出来的经纬度只允许包含"."和数字,其他字符非法
                        if (46 > lat[ii] || 57 < lat[ii] && ii > 5)
                        {
                            isInvalid = true;
                            break;
                        }
                        lat[15] = 0;
                    }
                    if (!isEnd) lat[15] = 0;

                    //长度大于11或包含非法字符时设置为-1
                    String longitude = String.Empty;
                    String latitude = String.Empty;
                    unsafe
                    {
                        fixed (sbyte* p1 = lgn)
                        {
                            fixed (sbyte* p2 = lat)
                            {
                                longitude = new String(p1);
                                latitude = new String(p2);
                                if (11 < longitude.Length || 11 < latitude.Length || isInvalid)
                                {
                                    longitude = "-1";
                                    latitude = "-1";
                                }
                            }
                        }
                    }
                    String tableName = String.Empty;
                    if (m_DBTableDic.TryGetValue(m_SessionDic[session.SessionID].ClassID, out tableName))
                    {
                        if (String.IsNullOrWhiteSpace(tableName))
                        {
                            mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{12},now(),'track','{1},{2}','{3}-{4}-{5} {6}:{7}:{8}',{9},{10},{11});",
                            m_SessionDic[session.SessionID].UID, longitude, latitude,
                            tInfo.TrackSend[i].CurrTime.Year, tInfo.TrackSend[i].CurrTime.Month,
                            tInfo.TrackSend[i].CurrTime.Day, tInfo.TrackSend[i].CurrTime.Hour,
                            tInfo.TrackSend[i].CurrTime.Minute, tInfo.TrackSend[i].CurrTime.Sec, iWorkMode[i],
                            m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.TrackSend[i].Sequence);
                        }
                        else
                        {
                            mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{12},now(),'track','{1},{2}','{3}-{4}-{5} {6}:{7}:{8}',{9},{10},{11});insert into " + tableName + "(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{12},now(),'track','{1},{2}','{3}-{4}-{5} {6}:{7}:{8}',{9},{10},{11});",
                            m_SessionDic[session.SessionID].UID, longitude, latitude,
                            tInfo.TrackSend[i].CurrTime.Year, tInfo.TrackSend[i].CurrTime.Month,
                            tInfo.TrackSend[i].CurrTime.Day, tInfo.TrackSend[i].CurrTime.Hour,
                            tInfo.TrackSend[i].CurrTime.Minute, tInfo.TrackSend[i].CurrTime.Sec, iWorkMode[i],
                            m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.TrackSend[i].Sequence);
                        }
                    }
                    else
                    {
                        mch = String.Format("insert into trecordsportinfo(userid,PackInx,recordtime,kind,value,CullTime,mode,classid,schoolid) values({0},{12},now(),'track','{1},{2}','{3}-{4}-{5} {6}:{7}:{8}',{9},{10},{11});",
                            m_SessionDic[session.SessionID].UID, longitude, latitude,
                            tInfo.TrackSend[i].CurrTime.Year, tInfo.TrackSend[i].CurrTime.Month,
                            tInfo.TrackSend[i].CurrTime.Day, tInfo.TrackSend[i].CurrTime.Hour,
                            tInfo.TrackSend[i].CurrTime.Minute, tInfo.TrackSend[i].CurrTime.Sec, iWorkMode[i],
                            m_SessionDic[session.SessionID].ClassID, m_SessionDic[session.SessionID].SchoolID, tInfo.TrackSend[i].Sequence);
                    }
                    sql = mch;

                    SrvLogHelper(String.Format("sql:{0}_{1}", sql, DateTime.Now.ToString("yy-MM-dd HH:mm:ss")), 2);

                    lock (((ICollection)m_pSql).SyncRoot)
                    {
                        //for (; m_nSqlCount < SQL_BUF_NUM; m_nSqlCount++)
                        //{
                        //检测元素中是否已存在sql语句
                        //if (!m_pbIsExist[m_nSqlCount]) break;
                        //}
                        m_pSql.Add(sql);
                        //m_pbIsExist[m_nSqlCount] = true;
                        //m_nSqlCount++;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("接收轨迹点不全" + ex.Message);
                }
            }

            SrvLogHelper("errcode=" + StatusCode.TrackDB);

            str = String.Format("轨迹点数据共{0}条,接收成功", n);
            listBoxLogger.Invoke(appendItemDelegate, str);
            SrvLogHelper(str, 2);
            //大数据包回复确认信息
            if (m_ServerInfo.bIsAnswer || n > 1)
            {
                bInfo.UID = Convert.ToUInt32(m_SessionDic[session.SessionID].UID);
                bInfo.TrackResult = new TRACK_RESULT();
                bInfo.TrackResult.Result = CONN_ERR.Success;
                Byte[] respArr = SrvPack(bInfo);

                return respArr;
            }
            return null;
        }
        #endregion

        #region 模拟服务器端发送数据块
        private static void TestClientBind()
        {
            clientListcombox.Invoke(new comboxBind(D_clientBind));
            methodListcombox.Invoke(new comboxBind(D_methodBind));
        }

        private static void D_clientBind()
        {
            //绑定socket客户端数据源
            BindingSource bs = new BindingSource();
            bs.DataSource = m_SessionDic;
            clientListcombox.DataSource = bs;
            clientListcombox.DisplayMember = "Key";
            clientListcombox.ValueMember = "Key";
        }

        private static void D_methodBind()
        {
            //绑定发送方法数据源
            Dictionary<int, string> dict = new Dictionary<int, string>();
            dict.Add(1, "KnockReq (敲门)");
            dict.Add(2, "LoginReq (登陆)");
            dict.Add(3, "KeepAlive(保活)");
            dict.Add(4, "TrackSend(轨迹点)");
            dict.Add(5, "HeartRate(心率)");
            dict.Add(6, "Step(计步)");
            dict.Add(7, "HrStepMode(心率计步)");
            dict.Add(8, "Albs(辅助定位)");
            dict.Add(9, "DevNotify(设备通知)");
            dict.Add(10, "Order(指令下发)");
            BindingSource bds = new BindingSource();
            bds.DataSource = dict;
            methodListcombox.DataSource = bds;
            methodListcombox.DisplayMember = "Value";
            methodListcombox.ValueMember = "Key";
        }

        private string D_sessionID()
        {
            if (clientListcombox.Items.Count == 0)
            {
                return null;
            }
            return clientListcombox.SelectedValue.ToString().Trim();
        }

        private string D_getMethodType()
        {
            return methodListcombox.SelectedValue.ToString().Trim();
        }
        private static void TestSend(string sessionID, int methodKey = 1)
        {
            var session = m_SessionDic[sessionID].Session;
            GPB_SRV2DEV bInfo = new GPB_SRV2DEV();
            #region 测试变量
            int testUid = 0;
            int testTag = 0;
            Byte[] testCharArr = new Byte[16];
            testCharArr[0] = 49;
            testCharArr[1] = 50;
            testCharArr[2] = 51;
            testCharArr[3] = 52;
            testCharArr[4] = 53;
            testCharArr[5] = 53;
            testCharArr[6] = 52;
            testCharArr[7] = 51;
            testCharArr[8] = 50;
            testCharArr[9] = 49;
            string msg = string.Empty;
            #endregion

            #region 测试数据封装
            switch (methodKey)
            {
                case 1:
                    msg = "敲门程序";
                    bInfo.UID = 0;
                    bInfo.KnockResp = new KNOCK_RESP();
                    bInfo.KnockResp.Tag = 42;
                    CommonTools.Cipher(ref testCharArr);
                    bInfo.KnockResp.EncryptRandomNumber = ByteString.CopyFrom(testCharArr);
                    break;
                case 2:
                    msg = "登陆程序";
                    bInfo.LoginResp = new LOGIN_RESP();
                    bInfo.LoginResp.Tag = (uint)testTag;
                    bInfo.LoginResp.Result = CONN_ERR.Success;
                    bInfo.UID = 0;
                    bInfo.LoginResp.GMTTime = new TIME_INFO();
                    bInfo.LoginResp.GMTTime.Year = Convert.ToUInt32(DateTime.Now.Year);
                    bInfo.LoginResp.GMTTime.Month = Convert.ToUInt32(DateTime.Now.Month);
                    bInfo.LoginResp.GMTTime.Day = Convert.ToUInt32(DateTime.Now.Day);
                    bInfo.LoginResp.GMTTime.Hour = Convert.ToUInt32(DateTime.Now.Hour);
                    bInfo.LoginResp.GMTTime.Minute = Convert.ToUInt32(DateTime.Now.Minute);
                    bInfo.LoginResp.GMTTime.Sec = Convert.ToUInt32(DateTime.Now.Second);
                    break;
                case 3:
                    msg = "保活程序";
                    bInfo.UID = 0;
                    bInfo.KeepAliveResp = new KEEP_ALIVE_RSP();
                    bInfo.KeepAliveResp.SequenceAck = 654564646465;
                    bInfo.KeepAliveResp.Result = CONN_ERR.Success;
                    break;
                case 4:
                    msg = "轨迹点程序";
                    bInfo.UID = (uint)testUid;
                    bInfo.TrackResult = new TRACK_RESULT();
                    bInfo.TrackResult.Result = CONN_ERR.Success;
                    break;
                case 5:
                    msg = "心率值程序";
                    bInfo.UID = (uint)testUid;
                    bInfo.HeartRateResult = new HEART_RATE_RESULT();
                    bInfo.HeartRateResult.Result = CONN_ERR.Success;
                    break;
                case 6:
                    msg = "计步程序";
                    bInfo.UID = (uint)testUid;
                    bInfo.StepResult = new STEP_RESULT();
                    bInfo.StepResult.Result = CONN_ERR.Success;
                    break;
                case 7:
                    msg = "心率计步程序";
                    bInfo.UID = (uint)testUid;
                    bInfo.HrStepModeResult = new global::SRV.Model.HR_STEP_MODE_RESULT();
                    bInfo.HrStepModeResult.Result = CONN_ERR.Success;
                    break;
                case 8:
                    msg = "辅助定位程序";
                    bInfo.UID = (uint)testUid;
                    bInfo.AlbsRsp = new global::SRV.Model.ALBS_RSP();
                    bInfo.AlbsRsp.Result = CONN_ERR.Success;
                    bInfo.AlbsRsp.AlbsData = ByteString.CopyFrom("kkk3333333333", Encoding.Default);
                    break;
                case 9:
                    msg = "设备通知程序";
                    bInfo.UID = (uint)testUid;
                    bInfo.DevNotifyResult = new global::SRV.Model.DEV_NOTIFY_RESULT();
                    bInfo.DevNotifyResult.Result = CONN_ERR.Success;
                    bInfo.DevNotifyResult.Tag = 1123;
                    break;
                case 10:
                    msg = "指令下发程序";
                    break;
            }
            #endregion
            byte[] respArr = SrvPack(bInfo);
            //发送数据
            session.Send(respArr, 0, respArr.Length);
            listBoxLogger.Invoke(appendItemDelegate, String.Format("客户端：{0}, 接口：{1}, 已模拟数据发送成功！", sessionID, msg));
        }
        #endregion

        private void SuperSocketForm_Load(object sender, EventArgs e)
        {
            notifyIcon.Visible = true;
        }

        private void ToolStripMenuItemClose_Click(object sender, EventArgs e)
        {
            if (buttonClose.Enabled)
            {
                buttonClose_Click(null, null);
            }
            else
            {
                notifyIcon.Visible = false;
                this.Close();
            }
        }

        private void ToolStripMenuItemShow_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Opacity = 1;
                (sender as NotifyIcon).Visible = true;
                this.Show();
                this.Activate();
                this.ShowInTaskbar = true;
            }
        }
    }
}