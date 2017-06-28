using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SuperSocketForm.Utility
{
    public class MD5Encryption
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public struct MD5_CTX
        {
            // ULONG[2]              
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = System.Runtime.InteropServices.UnmanagedType.U4)]
            public uint[] i;

            // ULONG[4]              
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.U4)]
            public uint[] buf;

            // unsigned char[64]              
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] @in;

            // unsigned char[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] digest;
        }

        [DllImport("advapi32.dll")]
        public static extern void MD5Init(ref MD5_CTX context);
        [DllImport("advapi32.dll")]
        public static extern void MD5Update(ref MD5_CTX context, Byte[] input, Int32 inlen);
        [DllImport("advapi32.dll")]
        public static extern void MD5Final(ref MD5_CTX context);

        public static Byte[] MD5Encrypt(Byte[] str)
        {
            MD5_CTX ctx = new MD5_CTX();
            Int32 len = str.Length;
            MD5Init(ref ctx);
            MD5Update(ref ctx, str, len);
            MD5Final(ref ctx);

            return Hex2ASC(ctx.digest, 16);
        }

        public static Byte[] MD5Hex(Byte[] str)
        {
            MD5_CTX ctx = new MD5_CTX();
            Int32 len = str.Length;
            MD5Init(ref ctx);
            MD5Update(ref ctx, str, len);
            MD5Final(ref ctx);

            return ctx.digest;
        }

        public static Byte[] Hex2ASC(Byte[] Hex, int Len)
        {
            Byte[] ASC = new Byte[4096 * 2];
            Int32 i;

            for (i = 0; i < Len; i++)
            {
                ASC[i * 2] = Convert.ToByte("0123456789abcdef"[Hex[i] >> 4]);
                ASC[i * 2 + 1] = Convert.ToByte("0123456789abcdef"[Hex[i] & 0x0F]);
            }
            ASC[i * 2] = 0;
            return ASC;
        }
    }
}