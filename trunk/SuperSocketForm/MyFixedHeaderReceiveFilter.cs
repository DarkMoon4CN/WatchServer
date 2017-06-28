using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.Common;
using SuperSocket.Facility.Protocol;

namespace SuperSocketForm
{
    public class MyFixedHeaderReceiveFilter : FixedHeaderReceiveFilter<BinaryRequestInfo>
    {
        public MyFixedHeaderReceiveFilter()
            : base(4)
        {

        }

        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            if (header[offset] == '@')
            {
                return (int)header[offset + 2] * 256 + (int)header[offset + 3] - 4;
            }
            else
            {
                return (int)header[offset + 2] + (int)header[offset + 3] * 256 - 4;
            }
        }

        protected override BinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {
            Byte[] packLength = new Byte[2];
            Array.ConstrainedCopy(header.Array, header.Offset + 2, packLength, 0, 2);
            if (BitConverter.IsLittleEndian && Encoding.Default.GetString(header.Array, header.Offset, 2) == "@@")
            {
                Array.Reverse(packLength);
            }
            return new BinaryRequestInfo(Encoding.Default.GetString(header.Array, header.Offset, 2) + "|" + BitConverter.ToInt16(packLength, 0).ToString(), bodyBuffer.CloneRange(offset, length));
        }
    }
}