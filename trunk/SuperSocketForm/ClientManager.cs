using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Collections.Concurrent;

namespace SuperSocketForm
{
    public class ClientManager
    {
        private Byte[] rand = new Byte[11];

        private Byte[] imei = new Byte[16];

        private SByte[] serverip = new SByte[16];

        public SuperSocketSession Session { get; set; }

        public UInt32 Tag { get; set; }

        public Byte[] Rand { get { return rand; } set { rand = value; } }

        public ByteString Imei { get; set; }

        public Int32 UID { get; set; }

        public Boolean IsLogin { get; set; }

        public Int32 ClassID { get; set; }

        public Int32 SchoolID { get; set; }

        public Int32 Mode { get; set; }

        public String TrueName { get; set; }

        public String ClassName { get; set; }

        public String SchoolName { get; set; }

        public UInt64 UllTimer { get; set; }

        public UInt64 UllActive { get; set; }

        public Int32 nIndex { get; set; }

        public Boolean IsLock { get; set; }

        public SByte[] ServerIP { get { return serverip; } set { serverip = value; } }

        public UInt16 ServerPort { get; set; }
    }
}