using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PushSubscribe
{
   public  class Utilities
    {
        public static ILog GetLoger()
        {
            var path = getconfigpath();
            var logCfg = new FileInfo(path);
            XmlConfigurator.ConfigureAndWatch(logCfg);
            return  LogManager.GetLogger(typeof(WCFHost));
        }

        public static Configuration GetConfiguration()
        {
            var path = getconfigpath();
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = path;
            return  ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }

        private static string getconfigpath()
        {
            Assembly myAssembly = Assembly.GetEntryAssembly();
            string path = myAssembly.Location;
            path= Directory.GetParent(path).FullName;
            return Path.Combine(path, "App.config");
        }
    }
}
