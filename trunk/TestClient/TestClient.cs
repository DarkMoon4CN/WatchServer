using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Protobuf;
using Google;
using SRV.Model;

namespace TestClient
{
    public partial class TestClient : Form
    {
        private Socket clientSocket;
        private IPAddress ip;
        private static byte[] result = new byte[1024];
        public TestClient()
        {
            InitializeComponent();
            //设置aes加密算法密钥
            Byte[] key =
	        {
                0x2b, 0x7e, 0x15, 0xc6,
		        0x2f, 0x3e, 0xd2, 0xa2,
		        0xab, 0xfa, 0x15, 0xb8,
		        0x39, 0x5f, 0x4f, 0x31
	        };
            CommonTools.Setkey(key);
            comboBox1.SelectedIndex = 0;
            ip = IPAddress.Parse("192.168.0.102");
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // clientSocket.Connect(new IPEndPoint(ip, 5015));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!clientSocket.Connected)
                {
                    //配置服务器IP与端口
                    clientSocket.Connect(new IPEndPoint(ip, 5015));
                    if (clientSocket.Connected)
                    {
                        MessageBox.Show("连接服务器成功");
                    }
                }                
            }
            catch
            {
                MessageBox.Show("连接服务器失败，请按回车键退出！");
                return;
            }

            //通过 clientSocket 发送数据
            for (int i = 0; i < 1; i++)
            {
                try
                {
                    // Thread.Sleep(1000);    //等待1秒钟
                    GPB_DEV2SRV m_Info = new GPB_DEV2SRV();
                    m_Info.UID = 0;
                    m_Info.KnockReq = new KNOCK_REQ();
                    m_Info.KnockReq.PBVer = DEV_SERVER_VER_.DevServerVer;
                    // m_Info.KnockReq.Tag = Convert.ToUInt32((new Random()).Next());
                    m_Info.KnockReq.Tag = 42;

//                     m_Info.Name = ByteString.CopyFrom(textBox1.Text, Encoding.Unicode);
//                     m_Info.Sex = comboBox1.SelectedIndex == 0 ? SEX.Male : SEX.Girl;
//                     m_Info.Age = Convert.ToInt32(numericUpDown2.Value);
//                     m_Info.Height = Convert.ToInt32(numericUpDown3.Value);
//                     m_Info.Weight = Convert.ToInt32(numericUpDown4.Value);;
//                     m_Info.Classroom = ByteString.CopyFrom(textBox2.Text, Encoding.Unicode);
//                     m_Info.School = ByteString.CopyFrom(textBox3.Text, Encoding.Unicode);
//                     m_Info.EducationId = ByteString.CopyFrom(textBox4.Text, Encoding.Unicode);

                    byte[] s_Content = m_Info.ToByteArray();
                    byte[] s_Total = new byte[s_Content.Length + 4];
                    s_Total[0] = Convert.ToByte('@');
                    s_Total[1] = Convert.ToByte('@');
                    Byte[] packLength = BitConverter.GetBytes(Convert.ToInt16(s_Content.Length + 4));
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(packLength);
                    }
                    System.Buffer.BlockCopy(packLength, 0, s_Total, 2, 2);
                    System.Buffer.BlockCopy(s_Content, 0, s_Total, 4, s_Content.Length);
                    clientSocket.Send(s_Total);
                    // MessageBox.Show("向服务器发送消息：" + i.ToString());

                    Int32 receiveLength = clientSocket.Receive(result);
                    byte[] fArr = new byte[receiveLength - 4];
                    Array.Copy(result, 4, fArr, 0, fArr.Length);
                    GPB_SRV2DEV bInfo;
                    bInfo = GPB_SRV2DEV.Parser.ParseFrom(fArr);
                    Byte[] enArr = bInfo.KnockResp.EncryptRandomNumber.ToByteArray();
                    CommonTools.InvCipher(ref enArr, true);
                }
                catch(Exception ex)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    break;
                }
            }
            Console.WriteLine("发送完毕，按回车键退出");
            Console.ReadLine();
        }

        private static byte[] CltPack(GPB_DEV2SRV obj)
        {
            byte[] s_Content = obj.ToByteArray();
            byte[] s_Total = new byte[s_Content.Length + 4];
            s_Total[0] = Convert.ToByte('@');
            s_Total[1] = Convert.ToByte('@');
            System.Buffer.BlockCopy(BitConverter.GetBytes(Convert.ToInt16(s_Content.Length + 4)), 0, s_Total, 2, 2);
            System.Buffer.BlockCopy(s_Content, 0, s_Total, 4, s_Content.Length);
            return s_Total;
        }
    }
}