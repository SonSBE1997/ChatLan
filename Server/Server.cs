using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{

    public partial class Form1 : Form
    {

        IPEndPoint IP; //IP server
        Socket server;
        List<Socket> lsClient; //danh sách các client

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        //Tạo server
        bool CreateServer()
        {
            lsClient = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Any, Int32.Parse(txtPort.Text));
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            server.Bind(IP);
            server.Listen(100);

            try
            {
                Thread listen = new Thread(() => {
                    try
                    {
                        while (true)
                        {
                            Socket client = server.Accept();
                            lsClient.Add(client);

                            try
                            {
                                Thread receive = new Thread(Receive);
                                receive.IsBackground = true;
                                receive.Start(client);
                            }
                            catch { }
                        }
                    }
                    catch
                    {
                        IP = new IPEndPoint(IPAddress.Any, Int32.Parse("3005"));
                        server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);
                    }
                });
                listen.IsBackground = true;
                listen.Start();
            }
            catch
            {
                CloseConnect();
                return false;
            }

            //TcpListener lsn = new TcpListener(IP);
            //Invoke(new Action(() => {
            //    Thread thread = new Thread(() => {
            //        lsn.Start();
            //        TcpClient client = lsn.AcceptTcpClient();
            //        Thread receiveFile = new Thread(ReceiveFileNetStream);
            //        receiveFile.IsBackground = true;
            //        receiveFile.Start(client);
            //    });
            //}));

            return true;
        }

        void ReceiveFileNetStream(object ob)
        {
            TcpClient client = ob as TcpClient;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024 * 5000];
            stream.Read(buffer, 0, buffer.Length);

            SaveFileDialog savefile = new SaveFileDialog();
            savefile.InitialDirectory = @"d:\ảnh\phong cảnh & anime\anime";
            savefile.Filter = "txt|*.txt|all files(*.*)|*.*";
            if (savefile.ShowDialog() == DialogResult.OK)
            {
                string path = savefile.FileName;
                FileManager.Instance.WriteFile(path, buffer);
            }
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

        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    SocketData data = ReceiveData(client) as SocketData;
                    switch (data.Command)
                    {
                        case (int)SocketCommand.IMAGE:
                            ReceiveImage(data);
                            break;
                        case (int)SocketCommand.MESSAGE:
                            ReceiveMessage(data);
                            break;
                        case (int)SocketCommand.SOUND:
                            break;
                        case (int)SocketCommand.FILE:
                            ReceiveFile(data);
                            break;
                        default:
                            AddMessage(data.ShowName + " Connected");
                            break;
                    }
                    Thread.Sleep(100);
                    foreach (Socket item in lsClient)
                    {
                        if (item != client && data.Command != (int)SocketCommand.CONNECT)
                            SendData(item, data);
                    }
                }
            }
            catch
            {
                AddMessage(client.LocalEndPoint.ToString().Split(':')[0] + " Disconnected");
                lsClient.Remove(client);
                client.Close();
            }
        }

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

        void ReceiveMessage(SocketData data)
        {
            AddMessage(GetTimeSend(data.ShowName, "nói"));
            AddMessage((string)data.Data);
        }

        void ReceiveFile(SocketData data)
        {
            #region region1
            Invoke(new Action(() => {
                AddMessage(GetTimeSend(data.ShowName, "gửi 1 file"));
                TextBox txbfile = new TextBox();
                txbfile.Width = 455;
                txbfile.BorderStyle = BorderStyle.None;
                txbfile.Text = (data.Data as FileData).FileName;
                Point p = new Point(0, 0);
                try
                {
                    var lastchild = pnlReceive.Controls[pnlReceive.Controls.Count - 1];
                    p = new Point(0, lastchild.Location.Y + lastchild.Height + 1);
                }
                catch { }
                txbfile.Location = p;
                txbfile.Click += (sender, e) => {
                    SaveFileDialog savefile = new SaveFileDialog();
                    savefile.InitialDirectory = @"d:\ảnh\phong cảnh & anime\anime";
                    savefile.Filter = "txt|*.txt|all files(*.*)|*.*";
                    if (savefile.ShowDialog() == DialogResult.OK)
                    {
                        string path = savefile.FileName;
                        FileManager.Instance.WriteFile(path, (data.Data as FileData).Data);
                    }
                };
                pnlReceive.Controls.Add(txbfile);
            }));
            #endregion
        }

        // đóng server
        void CloseConnect()
        {
            server.Close();
            btnConnect.Enabled = true;
        }

        //Gửi tin lên đến client
        void SendData(Socket client, object obj)
        {
            client.Send(SerializeData(obj));
        }

        // Nhận dữ liệu về
        object ReceiveData(Socket client)
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
            bf.Serialize(stream, obj);
            return stream.ToArray();
        }

        //Khôi phục dữ liệu ban đầu
        object DeserializeData(byte[] byteArr)
        {
            MemoryStream stream = new MemoryStream(byteArr);
            BinaryFormatter bf = new BinaryFormatter();
            stream.Position = 0;
            return bf.Deserialize(stream);
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseConnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != "")
            {
                foreach (Socket item in lsClient)
                    SendData(item, new SocketData("Server", (int)SocketCommand.MESSAGE, txtMessage.Text));
                AddMessage(GetTimeSend("Server", "nói") + txtMessage.Text);
                txtMessage.Text = "";
            }
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
                    picImage.Image = Image.FromFile(op.FileName);
                    pnlReceive.Controls.Add(picImage);
                }));

                foreach (Socket item in lsClient)
                {
                    SendData(item, new SocketData("Server", (int)SocketCommand.IMAGE, (Bitmap)Image.FromFile(op.FileName)));
                }
            }
        }

        string GetTimeSend(string SendName, string typeSend)
        {
            return SendName + " đã " + typeSend + " lúc " + DateTime.Now.ToString("HH:mm d/M/yyyy") + ":\r\n";
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (CreateServer())
            {
                MessageBox.Show("Tạo server thành công!");
                btnConnect.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
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

                foreach (Socket item in lsClient)
                {
                    SendData(item, new SocketData("Server", (int)SocketCommand.IMAGE, (Bitmap)Image.FromFile(op.FileName)));
                }
            }
        }
    }
}