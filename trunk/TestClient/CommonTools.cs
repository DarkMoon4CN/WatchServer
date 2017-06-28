using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    public class CommonTools
    {     
        private static Byte[] Sbox = new Byte[256];
        private static Byte[] InvSbox = new Byte[256];
        private static Byte[][,] w = new Byte[11][,];

        public static void Cipher(ref Byte[] input, Int32 length = 0)
        {
            Byte[] inArr = input;
	        if (length == 0)
	        {
                while (inArr[length] != 0) 
                {
                    length++;
                }
	        }
	        for (Int32 i = 0; i <= length; i += 16)
	        {
                Cipher(ref inArr);
	        }
        }

        private static void Cipher(ref Byte[] input)
        {
	        Byte[,] state = new Byte[4, 4];
	        Int32 i, r, c;

	        for (r = 0; r<4; r++)
	        {
		        for (c = 0; c<4; c++)
		        {
			        state[r, c] = input[c * 4 + r];
		        }
	        }

            AddRoundKey(ref state, w[0]);

	        for (i = 1; i <= 10; i++)
	        {
		        SubBytes(ref state);
		        ShiftRows(ref state);
		        if (i != 10)MixColumns(ref state);
		        AddRoundKey(ref state, w[i]);
	        }

	        for (r = 0; r<4; r++)
	        {
		        for (c = 0; c<4; c++)
		        {
			        input[c * 4 + r] = state[r , c];
		        }
	        }
        }

        private static void AddRoundKey(ref Byte[,] state, Byte[,] k)
        {
	        Int32 r, c;
	        for (c = 0; c<4; c++)
	        {
		        for (r = 0; r<4; r++)
		        {
                    // 按位异或
			        state[r, c] ^= k[r, c];
		        }
	        }
        }

        private static void SubBytes(ref Byte[,] state)
        {
	        int r, c;
	        for (r = 0; r<4; r++)
	        {
		        for (c = 0; c<4; c++)
		        {
			        state[r, c] = Sbox[state[r, c]];
		        }
	        }
        }

        private static void InvSubBytes(ref Byte[,] state)
        {
	        int r, c;
	        for (r = 0; r<4; r++)
	        {
		        for (c = 0; c<4; c++)
		        {
			        state[r, c] = InvSbox[state[r, c]];
		        }
	        }
        }

        private static void ShiftRows(ref Byte[,] state)
        {
            Byte[] t = new Byte[4];
            int r, c;
            for (r = 1; r < 4; r++)
            {
                for (c = 0; c < 4; c++)
                {
                    t[c] = state[r, (c + r) % 4];
                }
                for (c = 0; c < 4; c++)
                {
                    state[r, c] = t[c];
                }
            }
        }

        private static void InvShiftRows(ref Byte[,] state)
        {
	        Byte[] t = new Byte[4];
	        int r, c;
	        for (r = 1; r<4; r++)
	        {
		        for (c = 0; c<4; c++)
		        {
			        t[c] = state[r, (c - r + 4) % 4];
		        }
		        for (c = 0; c<4; c++)
		        {
			        state[r, c] = t[c];
		        }
	        }
        }

        private static void MixColumns(ref Byte[,] state)
        {
	        Byte[] t = new Byte[4];
	        int r, c;
	        for (c = 0; c< 4; c++)
	        {
		        for (r = 0; r<4; r++)
		        {
			        t[r] = state[r , c];
		        }
		        for (r = 0; r<4; r++)
		        {
			        state[r , c] = Convert.ToByte(FFmul(0x02, t[r])
				        ^ FFmul(0x03, t[(r + 1) % 4])
				        ^ FFmul(0x01, t[(r + 2) % 4])
				        ^ FFmul(0x01, t[(r + 3) % 4]));
		        }
	        }
        }

        private static void InvMixColumns(ref Byte[,] state)
        {
            Byte[] t = new Byte[4];
            int r, c;
            for (c = 0; c < 4; c++)
            {
                for (r = 0; r < 4; r++)
                {
                    t[r] = state[r, c];
                }
                for (r = 0; r < 4; r++)
                {
                    state[r, c] = Convert.ToByte(FFmul(0x0e, t[r])
                        ^ FFmul(0x0b, t[(r + 1) % 4])
                        ^ FFmul(0x0d, t[(r + 2) % 4])
                        ^ FFmul(0x09, t[(r + 3) % 4]));
                }
            }
        }

        private static Byte FFmul(Byte a, Byte b)
        {
	        Byte[] bw = new Byte[4];
            Byte res = 0;
	        Int32 i;
	        bw[0] = b;
	        for (i = 1; i<4; i++)
	        {
                bw[i] = (bw[i - 1] << 1) > 255 ? Convert.ToByte((bw[i - 1] << 1) - 256) : Convert.ToByte(bw[i - 1] << 1);
		        if ((bw[i - 1] & 0x80) != 0)
		        {
			        bw[i] ^= 0x1b;
		        }
	        }
	        for (i = 0; i<4; i++)
	        {
		        if (((a >> i) & 0x01) != 0)
		        {
			        res ^= bw[i];
		        }
	        }
	        return res;
        }

        public static bool Setkey(Byte[] key)
        {
            Byte[] sBox =
	        { /*  0    1    2    3    4    5    6    7    8    9    a    b    c    d    e    f */
		        0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76, /*0*/
		        0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0, /*1*/
		        0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15, /*2*/
		        0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75, /*3*/
		        0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84, /*4*/
		        0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf, /*5*/
		        0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8, /*6*/
		        0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2, /*7*/
		        0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73, /*8*/
		        0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb, /*9*/
		        0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79, /*a*/
		        0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08, /*b*/
		        0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a, /*c*/
		        0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e, /*d*/
		        0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf, /*e*/
		        0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16  /*f*/
	        };
            Byte[] invsBox =
	        { /*  0    1    2    3    4    5    6    7    8    9    a    b    c    d    e    f  */
		        0x52, 0x09, 0x6a, 0xd5, 0x30, 0x36, 0xa5, 0x38, 0xbf, 0x40, 0xa3, 0x9e, 0x81, 0xf3, 0xd7, 0xfb, /*0*/
		        0x7c, 0xe3, 0x39, 0x82, 0x9b, 0x2f, 0xff, 0x87, 0x34, 0x8e, 0x43, 0x44, 0xc4, 0xde, 0xe9, 0xcb, /*1*/
		        0x54, 0x7b, 0x94, 0x32, 0xa6, 0xc2, 0x23, 0x3d, 0xee, 0x4c, 0x95, 0x0b, 0x42, 0xfa, 0xc3, 0x4e, /*2*/
		        0x08, 0x2e, 0xa1, 0x66, 0x28, 0xd9, 0x24, 0xb2, 0x76, 0x5b, 0xa2, 0x49, 0x6d, 0x8b, 0xd1, 0x25, /*3*/
		        0x72, 0xf8, 0xf6, 0x64, 0x86, 0x68, 0x98, 0x16, 0xd4, 0xa4, 0x5c, 0xcc, 0x5d, 0x65, 0xb6, 0x92, /*4*/
		        0x6c, 0x70, 0x48, 0x50, 0xfd, 0xed, 0xb9, 0xda, 0x5e, 0x15, 0x46, 0x57, 0xa7, 0x8d, 0x9d, 0x84, /*5*/
		        0x90, 0xd8, 0xab, 0x00, 0x8c, 0xbc, 0xd3, 0x0a, 0xf7, 0xe4, 0x58, 0x05, 0xb8, 0xb3, 0x45, 0x06, /*6*/
		        0xd0, 0x2c, 0x1e, 0x8f, 0xca, 0x3f, 0x0f, 0x02, 0xc1, 0xaf, 0xbd, 0x03, 0x01, 0x13, 0x8a, 0x6b, /*7*/
		        0x3a, 0x91, 0x11, 0x41, 0x4f, 0x67, 0xdc, 0xea, 0x97, 0xf2, 0xcf, 0xce, 0xf0, 0xb4, 0xe6, 0x73, /*8*/
		        0x96, 0xac, 0x74, 0x22, 0xe7, 0xad, 0x35, 0x85, 0xe2, 0xf9, 0x37, 0xe8, 0x1c, 0x75, 0xdf, 0x6e, /*9*/
		        0x47, 0xf1, 0x1a, 0x71, 0x1d, 0x29, 0xc5, 0x89, 0x6f, 0xb7, 0x62, 0x0e, 0xaa, 0x18, 0xbe, 0x1b, /*a*/
		        0xfc, 0x56, 0x3e, 0x4b, 0xc6, 0xd2, 0x79, 0x20, 0x9a, 0xdb, 0xc0, 0xfe, 0x78, 0xcd, 0x5a, 0xf4, /*b*/
		        0x1f, 0xdd, 0xa8, 0x33, 0x88, 0x07, 0xc7, 0x31, 0xb1, 0x12, 0x10, 0x59, 0x27, 0x80, 0xec, 0x5f, /*c*/
		        0x60, 0x51, 0x7f, 0xa9, 0x19, 0xb5, 0x4a, 0x0d, 0x2d, 0xe5, 0x7a, 0x9f, 0x93, 0xc9, 0x9c, 0xef, /*d*/
		        0xa0, 0xe0, 0x3b, 0x4d, 0xae, 0x2a, 0xf5, 0xb0, 0xc8, 0xeb, 0xbb, 0x3c, 0x83, 0x53, 0x99, 0x61, /*e*/
		        0x17, 0x2b, 0x04, 0x7e, 0xba, 0x77, 0xd6, 0x26, 0xe1, 0x69, 0x14, 0x63, 0x55, 0x21, 0x0c, 0x7d  /*f*/
	        };
            Array.Copy(sBox, Sbox, 256);
            Array.Copy(invsBox, InvSbox, 256);
            KeyExpansion(key, w);

            return true;
        }

        private static void KeyExpansion(Byte[] key, Byte[][,] w)
        {
            for (Int32 k = 0; k < 11; k++)
            {
                w[k] = new Byte[4, 4];
            }

            Int32 i, j, r, c;
            Byte[] rc = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36 };
            for (r = 0; r < 4; r++)
            {
                for (c = 0; c < 4; c++)
                {
                    w[0][r, c] = key[r + c * 4];
                }
            }
            for (i = 1; i <= 10; i++)
            {
                for (j = 0; j < 4; j++)
                {
                    Byte[] t = new Byte[4];
                    for (r = 0; r < 4; r++)
                    {
                        t[r] = j != 0 ? w[i][r, j - 1] : w[i - 1][r, 3];
                    }
                    if (j == 0)
                    {
                        Byte temp = t[0];
                        for (r = 0; r < 3; r++)
                        {
                            t[r] = Sbox[t[(r + 1) % 4]];
                        }
                        t[3] = Sbox[temp];
                        t[0] ^= rc[i - 1];
                    }
                    for (r = 0; r < 4; r++)
                    {
                        w[i][r, j] = Convert.ToByte(w[i - 1][r, j] ^ t[r]);
                    }
                }
            }
        }

        public static void InvCipher(ref Byte[] input, Boolean iState)
        {
            Byte[,] state = new Byte[4, 4];
            int i, r, c;

            for (r = 0; r < 4; r++)
            {
                for (c = 0; c < 4; c++)
                {
                    state[r, c] = input[c * 4 + r];
                }
            }

            AddRoundKey(ref state, w[10]);
            for (i = 9; i >= 0; i--)
            {
                InvShiftRows(ref state);
                InvSubBytes(ref state);
                AddRoundKey(ref state, w[i]);
                if (i != 0)
                {
                    InvMixColumns(ref state);
                }
            }

            for (r = 0; r < 4; r++)
            {
                for (c = 0; c < 4; c++)
                {
                    input[c * 4 + r] = state[r, c];
                }
            }
        }

        public static void InvCipher(ref Byte[] input, Int32 length = 0)
        {
            Byte[] inArr = input;
            Int32 i;
            if (length == 0)
            {
                do
                {
                    length++;
                }
                while (inArr[length] == 0);
                inArr = input;
            }
            for (i = 0; i < length; i += 16)
            {
                Cipher(ref inArr);
            }
        }

        public static Byte[] md5(Byte[] str)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                String hash = GetMd5Hash(md5Hash, System.Text.Encoding.Default.GetString(str));
                hash = hash.ToLower();
                return System.Text.Encoding.Default.GetBytes(hash);
            }
        }

        private static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
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