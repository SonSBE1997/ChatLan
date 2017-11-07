using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    [Serializable]
    public class FileData
    {
        public string FileName { get; set; }

        [NonSerialized]
        private byte[] data;

        public byte[] Data { get; set; }
    }
}
