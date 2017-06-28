using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PushSubscribe
{
    public class PushData: IPushData
    {
        public void Push(string  userid,string  culltime,string kind,string value,int mode)
        {
            Utilities.GetLoger().Info("befor SubscribeList"+userid);
            if (SubscribeManager.SubscribeList.Contains(userid))
            {
                var config = Utilities.GetConfiguration();
                var serviceurl = config.AppSettings.Settings["PushServiceURL"].Value;
                try
                {
                    using (ChannelFactory<IReceiveData> channelFactory = new ChannelFactory<IReceiveData>(new BasicHttpBinding(), serviceurl + "/WCF/MonitoringHub.svc"))
                    {
                        IReceiveData proxy = channelFactory.CreateChannel();
                        Utilities.GetLoger().Info("befor Receive" + userid);
                        proxy.Receive(userid, DateTime.Parse(culltime),kind,value,mode);

                    }
                }
                catch (Exception ex)
                {
                    Utilities.GetLoger().Error(ex.Message);
                }
            }
        }
    }
}
