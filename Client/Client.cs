using Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        IPEndPoint IP; //IP server
        Socket client;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        //Mở kết nối đến server
        void ConnectToServer()
        {
            IP = new IPEndPoint(IPAddress.Parse(txtIPv4.Text), Int32.Parse(txtPort.Text));
            try
            {
                client.Connect(IP);
                SendData(new SocketData(txtShowName.Text, (int)SocketCommand.CONNECT, ""));
                btnConnect.Enabled = false;
            }
            catch
            {
                MessageBox.Show("Không thể connect tới server");
                return;
            }

            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
        }

        //Đóng kết nối đến server
        void CloseConnect()
        {
            client.Close();
        }


        //Gửi tin lên server
        void SendData(object obj)
        {
            client.Send(SerializeData(obj));
        }

        //Nhận tin từ server
        object ReceiveData()
        {
            byte[] byteArr = new byte[1024 * 5000];
            client.Receive(byteArr);
            return DeserializeData(byteArr);
        }


        //Phân mảnh dữ liệu thành mảng byte
        byte[] SerializeData(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            stream.Position = 0;
            bf.Serialize(stream, obj);
            return stream.ToArray();
        }

        //Khôi phục dữ liệu ban đầu
        object DeserializeData(byte[] byteArr)
        {
            MemoryStream stream = new MemoryStream(byteArr);
            BinaryFormatter bf = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            stream.Position = 0;
            return bf.Deserialize(stream);
        }


        /// <summary>
        /// đóng kết nối tới server khi đóng ứng dụng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseConnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client.Connected && txtMessage.Text != "")
            {
                SendData(new SocketData(txtShowName.Text, (int)SocketCommand.MESSAGE, txtMessage.Text));
                AddMessage(GetTimeSend("Bạn", "nói") + txtMessage.Text);
                txtMessage.Text = "";
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            ConnectToServer();
        }

        void AddMessage(string mess)
        {
            Invoke(new Action(() => {
                TextBox txbMess = new TextBox();
                txbMess.Multiline = true;
                txbMess.WordWrap = true;
                txbMess.Width = 455;
                txbMess.Height = 30;
                txbMess.ScrollBars = ScrollBars.Vertical;
                txbMess.BorderStyle = BorderStyle.None;
                txbMess.Text = mess;
                Point p = new Point(0, 0);
                try
                {
                    var lastChild = pnlReceive.Controls[pnlReceive.Controls.Count - 1];
                    p = new Point(0, lastChild.Location.Y + lastChild.Height + 1);
                }
                catch { }
                txbMess.Location = p;
                pnlReceive.Controls.Add(txbMess);
            }));
        }

        void Receive()
        {
            try
            {
                while (true)
                {
                    SocketData data = ReceiveData() as SocketData;
                    switch (data.Command)
                    {
                        case (int)SocketCommand.IMAGE:
                            ReceiveImage(data);
                            break;
                        case (int)SocketCommand.MESSAGE:
                            ReceiveMessage(data);
                            break;
                        case (int)SocketCommand.SOUND:
                            ReceiveSound(data);
                            break;
                        case (int)SocketCommand.FILE:
                            ReceiveFile(data);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch
            {
                CloseConnect();
                btnConnect.Enabled = true;
            }
        }

        private void ReceiveFile(SocketData data)
        {
        }

        private void ReceiveSound(SocketData data)
        {
        }

        void ReceiveImage(SocketData data)
        {
            Invoke(new Action(() => {
                AddMessage(GetTimeSend(data.ShowName, "gửi ảnh"));
                PictureBox picImage = new PictureBox();
                picImage.Size = new Size(64, 64);
                var lastChild = pnlReceive.Controls[pnlReceive.Controls.Count - 1];
                Point p = new Point(0, lastChild.Location.Y + lastChild.Height + 1);
                picImage.Location = p;
                picImage.BorderStyle = BorderStyle.None;
                picImage.SizeMode = PictureBoxSizeMode.StretchImage;
                picImage.Image = (Image)data.Data as Bitmap;
                pnlReceive.Controls.Add(picImage);
            }));
        }

        void ReceiveMessage(SocketData data)
        {
            AddMessage(GetTimeSend(data.ShowName, "nói") + (string)data.Data);
        }

        string GetTimeSend(string SendName, string typeSend)
        {
            return SendName + " đã " + typeSend + " lúc " + DateTime.Now.ToString("HH:mm d/M/yyyy") + ":\n";
        }

        private void btnImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.InitialDirectory = @"D:\ảnh\Phong cảnh & anime\Anime";
            op.Filter = "jpg|*.jpg|png|*.png|All files(*.*)|*.*";
            if (op.ShowDialog() == DialogResult.OK)
            {
                string path = op.FileName;
                Invoke(new Action(() => {
                    AddMessage(GetTimeSend("Server", "gửi ảnh"));
                    PictureBox picImage = new PictureBox();
                    picImage.Size = new Size(64, 64);
                    var lastChild = pnlReceive.Controls[pnlReceive.Controls.Count - 1];
                    Point p = new Point(0, lastChild.Location.Y + lastChild.Height + 1);
                    picImage.Location = p;
                    picImage.BorderStyle = BorderStyle.None;
                    picImage.SizeMode = PictureBoxSizeMode.StretchImage;
                    picImage.Image = Image.FromFile(op.FileName);
                    pnlReceive.Controls.Add(picImage);
                }));
                SendData(new SocketData(txtShowName.Text, (int)SocketCommand.IMAGE, (Bitmap)Image.FromFile(op.FileName)));
            }
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            #region  pre
            OpenFileDialog op = new OpenFileDialog();
            op.InitialDirectory = @"C:\Users\My PC\Desktop";
            op.Filter = ".txt|*.txt|All files(*.*)|*.*";
            if (op.ShowDialog() == DialogResult.OK)
            {
                string path = op.FileName;
                string fileName = path.Substring(path.LastIndexOf('\\') + 1);
                //byte[] fName = Encoding.ASCII.GetBytes(fileName);
                FileData data = new FileData();
                data.FileName = fileName;
                data.Data = FileManager.Instance.ReadFile(path);
                SendData(new SocketData(txtShowName.Text, (int)SocketCommand.FILE, data.Data));
            }
            #endregion
            //SendFile();
        }

        void SendFile()
        {
            using (TcpClient client = new TcpClient())
            {
                client.Connect(new IPEndPoint(IPAddress.Parse(txtIPv4.Text), Int32.Parse(txtPort.Text)));
                NetworkStream stream = client.GetStream();
                OpenFileDialog op = new OpenFileDialog();
                op.InitialDirectory = @"C:\Users\My PC\Desktop";
                op.Filter = ".txt|*.txt|All files(*.*)|*.*";
                if (op.ShowDialog() == DialogResult.OK)
                {
                    string path = op.FileName;
                    string fileName = path.Substring(path.LastIndexOf('\\') + 1);
                    byte[] buffer = new byte[1024 * 5000];
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
