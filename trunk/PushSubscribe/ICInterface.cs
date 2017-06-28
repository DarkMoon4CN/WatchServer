using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PushSubscribe
{
    public interface IWCFHost
    {
        void Start();
    }

    public interface IPushData
    {
        void Push(string userid, string culltime, string kind, string value, int mode);
    }
}