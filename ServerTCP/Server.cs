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
using System.Xml;

namespace ServerTCP
{
    public partial class Server : Form
    {
        TcpListener listener; //Socket lắng nghe
        List<TcpClient> lsClient = new List<TcpClient>(); //Danh sách 
        const int bufferSize = 1024 * 5000; //Kích thước của mảng byte lưu byte[] nhận

        //Hàm tạo
        public Server()
        {
            InitializeComponent();
            this.FormClosing += (sender, e) => { DisposeServer(); };
        }

        // Nút tạo server
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (CreateServer())
            {
                MessageBox.Show("Tạo server thành công!");
                btnConnect.Enabled = false;
            }
            else MessageBox.Show("Tạo server thất bại!");
        }

        //Tạo server
        bool CreateServer()
        {
            listener = new TcpListener(IPAddress.Any, Int32.Parse(txtPort.Text));
            listener.Start();

            try
            {
                Thread lsnr = new Thread(() => {
                    try
                    {
                        while (true)
                        {
                            TcpClient client = listener.AcceptTcpClient();
                            lsClient.Add(client);
                            Thread receive = new Thread(Receive);
                            receive.IsBackground = true;
                            receive.Start(client);
                        }
                    }
                    catch
                    {
                        //listener = new TcpListener(IPAddress.Any, Int32.Parse(txtPort.Text));
                    }
                });
                lsnr.IsBackground = true;
                lsnr.Start();
            }
            catch
            {
                DisposeServer();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Đóng server khi đóng form
        /// </summary>
        void DisposeServer()
        {
            if (listener != null) listener.Stop();
            btnConnect.Enabled = true;
        }


        /// <summary>
        /// Nhận dữ liệu từ 1 client và gửi cho các client còn lại
        /// </summary>
        /// <param name="ob"></param>
        void Receive(object ob)
        {
            TcpClient client = ob as TcpClient;
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[bufferSize];
                    NetworkStream stream = client.GetStream();
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

                    Thread.Sleep(100);
                    foreach (TcpClient item in lsClient)
                    {
                        if (item.Client != client.Client && data.Command != (int)SocketCommand.CONNECT)
                        {
                            NetworkStream streamClient = item.GetStream();
                            streamClient.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
            catch
            {
                AddMessage(client.Client.LocalEndPoint.ToString().Split(':')[0] + " Disconnected");
                lsClient.Remove(client);
                client.Close();
            }
        }

        /// <summary>
        /// Server gửi mess
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (lsClient.Count > 0 && txtMessage.Text != "")
            {
                foreach (TcpClient item in lsClient)
                {
                    SendData(item.GetStream(), new SocketData("Server", (int)SocketCommand.MESSAGE, txtMessage.Text));
                    AddMessage(GetTimeSend("Bạn", "nói") + txtMessage.Text);
                    txtMessage.Text = "";
                }
            }
        }

        /// <summary>
        /// Server gửi ảnh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendAnh_Click(object sender, EventArgs e)
        {
            if (lsClient.Count > 0)
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
                    foreach (TcpClient item in lsClient)
                    {
                        SendData(item.GetStream(), new SocketData("Server", (int)SocketCommand.IMAGE, (Bitmap)Image.FromFile(op.FileName)));
                    }

                }
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
                Point p = new Point(0, 0);
                try
                {
                    var lastChild = pnlReceive.Controls[pnlReceive.Controls.Count - 1];
                    p = new Point(0, lastChild.Location.Y + lastChild.Height + 1);
                }
                catch { }
                picImage.Location = p;
                picImage.BorderStyle = BorderStyle.None;
                picImage.SizeMode = PictureBoxSizeMode.StretchImage;
                picImage.Image = (Image)data.Data as Bitmap;
                pnlReceive.Controls.Add(picImage);
            }));

        }

        /// <summary>
        /// Nhận tin nhắn từ client
        /// </summary>
        /// <param name="data"></param>
        void ReceiveMessage(SocketData data)
        {
            AddMessage(GetTimeSend(data.ShowName, "nói"));
            AddMessage((string)data.Data);
        }


        /// <summary>
        /// Tạo đối tượng hiển thị lên form
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
        /// Lấy thời gian gửi và nhận
        /// </summary>
        /// <param name="SendName">Tên người gửi/nhận</param>
        /// <param name="typeSend">Loại dữ liệu gửi</param>
        /// <returns></returns>
        string GetTimeSend(string SendName, string typeSend)
        {
            return SendName + " đã " + typeSend + " lúc " + DateTime.Now.ToString("HH:mm d/M/yyyy") + ":\n";
        }

        /// <summary>
        /// Gửi dữ liệu theo luồng từng client
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ob"></param>
        void SendData(NetworkStream stream, SocketData ob)
        {
            try
            {
                byte[] sendBuffer = SerializeData(ob);
                stream.Write(sendBuffer, 0, sendBuffer.Length);
            }
            catch { }
        }

        /// <summary>
        /// Phân rã dữ liệu thành mảng byte
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
        /// Khôi phục dữ liệu gốc
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
