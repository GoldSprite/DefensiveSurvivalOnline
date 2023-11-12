using System.Collections.Generic;
using System.IO;

namespace Unity.Sync.Relay.Message
{
    public class IOBuffer
    {
        private MemoryStream ms = new MemoryStream();

        public IOBuffer()
        {
        }
        
        public void Dispose()
        {
            ms.Dispose();
        }
        
        // 纯bytes 数组读入
        
        public List<RelayEvent> read(byte[] buf, int dataSize)
        {

            List<RelayEvent> result = new List<RelayEvent>();
            if (dataSize == 0)
            {
                return result;
            }
            
            this.ms.Write(buf, 0, dataSize);
            
            ms.Seek(0, SeekOrigin.Begin);
            
            while (ms.Length - ms.Position >= RelayBaseHeader.HeaderSize)
            {
                RelayBaseHeader header = RelayBaseHeader.ReadHeader(ms);
                int bodyLen = (int)header.MsgLen;
                
                if (ms.Length - ms.Position >= bodyLen)
                {
                    byte[] body = new byte[bodyLen];
                    ms.Read(body, 0, bodyLen);

                    RelayEvent eEvent = RelayEvent.Parser.ParseFrom(body);
                    result.Add(eEvent);
                }
                else
                {
                    ms.Seek(-RelayBaseHeader.HeaderSize, SeekOrigin.Current);
                    break;
                }
            }

            if (ms.Position > 0)
            {
                int size = (int)(ms.Length - ms.Position);
                if (size == 0)
                {
                    ms.SetLength(0);
                }
                else
                {
                    byte[] buf2x = new byte[size];
                    ms.Read(buf2x, 0, size);
                    ms.SetLength(0);

                    ms.Write(buf2x, 0, size);
                    // Debug.Log(String.Format("write it {0}", size));
                }
            }

            ms.Seek(0, SeekOrigin.End);

            return result;
        }
        
    }
}