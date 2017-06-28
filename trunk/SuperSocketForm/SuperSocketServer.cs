using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperSocketForm
{
    public class SuperSocketServer : AppServer<SuperSocketSession, BinaryRequestInfo>
    {
        private SuperSocketForm m_Handler;
        public SuperSocketServer(SuperSocketForm frmHandler) : base(new MyFixedHeaderReceiveFilterFactory()) 
        {
            m_Handler = frmHandler;
        }

        protected override void OnStopped()
        {
            m_Handler.Close();
            base.OnStopped();
        }
    }
}