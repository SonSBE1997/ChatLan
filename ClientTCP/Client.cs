using ServerTCP;
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

namespace ClientTCP
{
    public partial class Client : Form
    {
        TcpClient client = new TcpClient(); //Socket Client
        IPEndPoint IP; // Host server
        NetworkStream stream = null; //Luồng dữ liệu client
        const int bufferSize = 1024 * 5000; //Kích thước tối đa của mảng byte gửi/nhận

        /// <summary>
        /// Hàm tạo
        /// </summary>
        public Client()
        {
            InitializeComponent();
            this.FormClosing += (sender, e) => { Disconnect(); };
        }

        /// <summary>
        /// Nút kết nối đến server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            ConnectToServer();
        }

        /// <summary>
        /// Kết nối với server và mở luồng lắng nghe từ server
        /// </summary>
        void ConnectToServer()
        {
            IP = new IPEndPoint(IPAddress.Parse(txtIPv4.Text), Convert.ToInt32(txtPort.Text));
            try
            {
                client.Connect(IP);
                btnConnect.Enabled = false;
                stream = client.GetStream();
                SendData(new SocketData(txtShowName.Text, (int)SocketCommand.CONNECT, ""));
                try
                {
                    Thread thrReceive = new Thread(Receive);
                    thrReceive.IsBackground = true;
                    thrReceive.Start();
                }
                catch
                {
                    stream.Close();
                    client.Close();
                }
            }
            catch
            {
                MessageBox.Show("Không thể connect tới server");
                Disconnect();
            }

        }

        /// <summary>
        /// Đóng kết nối với server khi đóng form
        /// </summary>
        void Disconnect()
        {
            if (client.Connected)
            {
                stream.Close();
                client.Close();
            }
            btnConnect.Enabled = true;
        }

        /// <summary>
        /// Gửi dữ liệu - SocketData
        /// </summary>
        /// <param name="ob"></param>
        void SendData(SocketData ob)
        {
            try
            {
                byte[] sendBuffer = SerializeData(ob);
                stream.Write(sendBuffer, 0, sendBuffer.Length);
            }
            catch { }
        }
       
        /// <summary>
        /// Gửi tin nhắn đến server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client.Connected && txtMessage.Text != "")
            {
                SendData(new SocketData(txtShowName.Text, (int)SocketCommand.MESSAGE, txtMessage.Text));
                AddMessage(GetTimeSend("Bạn", "nói") + txtMessage.Text);
                txtMessage.Text = "";
            }
        }

        /// <summary>
        /// Gửi ảnh đến server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendAnh_Click(object sender, EventArgs e)
        {
            if (client.Connected)
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
        }

        /// <summary>
        /// Nhận dữ liệu trở về
        /// </summary>
        void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[bufferSize];
                    int sizeDataReceive = stream.Read(buffer, 0, buffer.Length);

                    SocketData data = DeserializeData(buffer);

                    switch (data.Command)
                    {
                        case (int)SocketCommand.IMAGE:
                            ReceiveImage(data);
                            break;
                        case (int)SocketCommand.MESSAGE:
                            ReceiveMessage(data);
                            break;
                        default:
                            AddMessage(data.ShowName + " Connected");
                            break;
                    }
                }
            }
            catch
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Nhận ảnh
        /// </summary>
        /// <param name="data"></param>
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

        /// <summary>
        /// Nhận tin nhắn
        /// </summary>
        /// <param name="data"></param>
        void ReceiveMessage(SocketData data)
        {
            AddMessage(GetTimeSend(data.ShowName, "nói") + (string)data.Data);
        }

        /// <summary>
        /// Đổ dữ liệu lên form
        /// </summary>
        /// <param name="mess"></param>
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

        /// <summary>
        /// Lấy thời gian gửi/nhận dữ liệu
        /// </summary>
        /// <param name="SendName"></param>
        /// <param name="typeSend"></param>
        /// <returns></returns>
        string GetTimeSend(string SendName, string typeSend)
        {
            return SendName + " đã " + typeSend + " lúc " + DateTime.Now.ToString("HH:mm d/M/yyyy") + ":\n";
        }


        /// <summary>
        /// Phân rã dữ liệu gửi đi
        /// </summary>
        /// <param name="ob"></param>
        /// <returns></returns>
        byte[] SerializeData(SocketData ob)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            ms.Position = 0;
            bf.Serialize(ms, ob);
            return ms.ToArray();
        }

        /// <summary>
        /// Khôi phục dữ liệu ban đầu
        /// </summary>
        /// <param name="receiveBuffer"></param>
        /// <returns></returns>
        SocketData DeserializeData(byte[] receiveBuffer)
        {
            MemoryStream stream = new MemoryStream(receiveBuffer);
            BinaryFormatter bf = new BinaryFormatter();
            stream.Position = 0;
            SocketData data = new SocketData();
            data = (SocketData)bf.Deserialize(stream);
            return data;
        }
    }
}