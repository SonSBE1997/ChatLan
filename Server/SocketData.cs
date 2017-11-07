using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    [Serializable]
    public class SocketData
    {
        public string ShowName { get; set; }

        public int Command { get; set; }

        public object Data { get; set; }

        public SocketData(string name, int command, object data)
        {
            this.ShowName = name;
            this.Command = command;
            this.Data = data;
        }

        public SocketData() { }
    }

    public enum SocketCommand
    {
        CONNECT,
        MESSAGE,
        IMAGE,
        SOUND,
        FILE
    }
}
