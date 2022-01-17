using Newtonsoft.Json.Linq;
using System.Text;

namespace ChromeNativeMessagingHost.Messages
{
    public class ServerCommunication
    {
        private readonly Stream _Stream;
        private readonly UnicodeEncoding _StreamEncoding;

        public ServerCommunication(Stream stream)
        {
            _Stream = stream;
            _StreamEncoding = new UnicodeEncoding();
        }

        public string ReadMessage()
        {
            int length = _Stream.ReadByte() * 256;
            length += _Stream.ReadByte();
            byte[] buffer = new byte[length];
            _Stream.Read(buffer, 0, length);

            return _StreamEncoding.GetString(buffer);
        }

        public JObject ReadMessageAsJObject()
        {
            int length = _Stream.ReadByte() * 256;
            length += _Stream.ReadByte();
            byte[] buffer = new byte[length];
            _Stream.Read(buffer, 0, length);

            string msg = _StreamEncoding.GetString(buffer);

            return JObject.Parse(msg);
        }

        public int SendMessage(string outString)
        {
            byte[] buffer = _StreamEncoding.GetBytes(outString);
            int length = buffer.Length;
            if (length > UInt16.MaxValue)
            {
                length = (int)UInt16.MaxValue;
            }
            _Stream.WriteByte((byte)(length / 256));
            _Stream.WriteByte((byte)(length & 255));
            _Stream.Write(buffer, 0, length);
            _Stream.Flush();

            return buffer.Length + 2;
        }
    }
}
