using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushSubscribe
{
    public class WCFHost: IWCFHost
    {
        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                var config = Utilities.GetConfiguration();
                var serviceurl = "http://localhost:" + config.AppSettings.Settings["SubscribeServicePort"].Value;
                try
                {
                    using (ServiceHost host = new ServiceHost(typeof(StudentSubscribe)))
                    {
                        host.Closed += Host_Closed;
                        host.AddServiceEndpoint(typeof(ISubscribe),
                            new BasicHttpBinding(), new Uri(serviceurl + "/StudentSubscribeService"));

                        if (host.State != CommunicationState.Opening)
                            host.Open();
                        Utilities.GetLoger().Info("订阅列表服务启动成功！");
                        // Console.Read();
                        Thread.Sleep(Timeout.Infinite);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.GetLoger().Error(ex.Message);
                }
            });
        }

        private void Host_Closed(object sender, EventArgs e)
        {
            Utilities.GetLoger().Error("错误的停止！");
        }
    }
}