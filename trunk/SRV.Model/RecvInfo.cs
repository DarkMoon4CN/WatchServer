using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRV.Model
{
    public class RecvInfo
    {
        private const Int32 RECVLEN = 4096;
        private Boolean[] bRecvIndex = new Boolean[RECVLEN];	    //已收到数据包的索引
        private UInt64[] ullRecv = new UInt64[RECVLEN];			    //已收到数据包的索引
	    
        public Int32 UID { get; set; }								//用户ID
	    public Int32 N { get; set; }								//最大计数
        public UInt64 BaseIndex { get; set; }					    //数组bRecvIndex的基数

        public Boolean[] BRecvIndex { get { return bRecvIndex; } set { bRecvIndex = value; } }

        public UInt64[] UllRecv { get { return ullRecv; } set { ullRecv = value; } }
    }
}