using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail
{
    public static class StreamExtensions
    {
        public static void Write(this Stream clientStream, string strMessage)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = encoder.GetBytes(strMessage + "\r\n");
            Write(clientStream, buffer);
        }
        public static void Write(this Stream clientStream, byte[] buffer)
        {
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }

        public static string Read(this Stream clientStream)
        {
            byte[] messageBytes = new byte[8192];
            int bytesRead = 0;
            //NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            bytesRead = clientStream.Read(messageBytes, 0, 8192);
            string strMessage = encoder.GetString(messageBytes, 0, bytesRead);
            return strMessage;
        }

    }
}
