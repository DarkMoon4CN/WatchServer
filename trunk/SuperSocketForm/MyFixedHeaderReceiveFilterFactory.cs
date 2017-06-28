using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperSocketForm
{
    public class MyFixedHeaderReceiveFilterFactory : IReceiveFilterFactory<BinaryRequestInfo>
    {
        public IReceiveFilter<BinaryRequestInfo> CreateFilter(SuperSocket.SocketBase.IAppServer appServer, SuperSocket.SocketBase.IAppSession appSession, System.Net.IPEndPoint remoteEndPoint)
        {
            return new MyFixedHeaderReceiveFilter();
        }
    }
}
