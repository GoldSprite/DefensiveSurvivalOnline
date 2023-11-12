#if FISHNET
using System.Collections.Generic;
using System.IO;
using FishNet.Transporting;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Sync.Relay.Message;

namespace Unity.Sync.Relay.Transport.FishNet
{
    public class RelayUtpIoBuffer
    {
        private MemoryStream ms = new MemoryStream();

        public RelayUtpIoBuffer()
        {
        }
        
        public List<RelayEvent> readUnreliable(DataStreamReader reader)
        {

            List<RelayEvent> result = new List<RelayEvent>();

            int nBytes = reader.Length - reader.GetBytesRead();
            NativeArray<byte> nativeArray = new NativeArray<byte>(nBytes, Allocator.Temp);
            reader.ReadBytes(nativeArray);
            
            byte[] body = nativeArray.ToArray();
            nativeArray.Dispose();
            
            RelayEvent eEvent = RelayEvent.Parser.ParseFrom(body);
            result.Add(eEvent);
            
            return result;
        }

        public List<RelayEvent> read(DataStreamReader reader, Channel channel)
        {
            if (channel == Channel.Unreliable)
            {
                return readUnreliable(reader);
            }
            
            List<RelayEvent> result = new List<RelayEvent>();

            int nBytes = reader.Length - reader.GetBytesRead();
            NativeArray<byte> nativeArray = new NativeArray<byte>(nBytes, Allocator.Temp);
            reader.ReadBytes(nativeArray);

            // if (reader.HasFailedReads)
            // {
            //     Debug.Log("Relay reader fail!");
            // }

            byte[] content = nativeArray.ToArray();
            if (content != null && content.Length > 0)
            {
                this.ms.Write(content, 0, content.Length);
            }
            nativeArray.Dispose();
            
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

        // *) 需要dispose
        public void Dispose()
        {
            ms.Dispose();
        }
        
    }
}
#endif