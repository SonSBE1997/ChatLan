using System;
using System.IO;
using System.Text;

namespace Server
{
    public class FileManager
    {
        private static FileManager instance;

        public static FileManager Instance
        {
            get
            {
                if (instance == null) instance = new FileManager();
                return instance;
            }

            private set
            {
                instance = value;
            }
        }

        private FileManager() { }

        public byte[] ReadFile(string fileName)
        {
            byte[] buffer = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                if (fs.CanRead)
                {
                    buffer = new byte[1024 * 5000];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                }
                fs.Flush();
                fs.Close();
            }
            return buffer;
        }

        public void WriteFile(string fileName, byte[] buffer)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                if (fs.CanWrite)
                {
                    fs.Write(buffer, 0, buffer.Length);
                }
                fs.Flush();
                fs.Close();
            }
        }


    }
}
