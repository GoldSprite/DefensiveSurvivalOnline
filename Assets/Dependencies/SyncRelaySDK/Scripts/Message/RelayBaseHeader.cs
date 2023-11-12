using System;
using System.IO;

namespace Unity.Sync.Relay.Message
{
    public delegate void WriteRelaySegment(byte[] seg);

    public class RelayBaseHeader
    {
        // public const int HeaderSize = 12;
        public const int HeaderSize = 6;
        public const int MsgLenOffset = 2; 

        // private UInt32 magic;
        // private UInt16 version;
        // private UInt16 checkSum;
        private UInt16 magic;
        private UInt32 msgLen;

        public UInt16 Magic
        {
            get { return magic; }
            set { magic = value; }
        }

        // public UInt16 Version
        // {
        //     get { return version; }
        //     set { version = value; }
        // }
        //
        // public UInt16 CheckSum
        // {
        //     get { return checkSum; }
        //     set { checkSum = value; }
        // }

        public UInt32 MsgLen
        {
            get { return msgLen; }
            set { msgLen = value; }
        }

        void WriteToByteArray(byte[] buf)
        {
            // BitConverter.TryWriteBytes(new System.Span(buf, 8), msgLen);
            
            // 确保这个int是4字节
            byte[] arrMsgLen = BitConverter.GetBytes((int)msgLen);

            // 从小端，转换为大端
            ReverseByte(arrMsgLen, 0, arrMsgLen.Length);

            for (int i = 0; i < arrMsgLen.Length; i++)
            {
                // 这边的2是offset量，就是msgLen在Header中偏移量
                buf[i + MsgLenOffset] = arrMsgLen[i];
            }
        }

        public static void ReverseByte(byte[] arr, int s, int e)
        {
            for (int i = s, j = e - 1; i < j; i++, j--)
            {
                byte t = arr[i];
                arr[i] = arr[j];
                arr[j] = t;
            }
        }

        public static RelayBaseHeader ReadHeader(MemoryStream ms)
        {
            byte[] buf = new byte[HeaderSize];
            ms.Read(buf, 0, buf.Length);

            RelayBaseHeader res = new RelayBaseHeader();
            ReverseByte(buf, 0, 2);
            res.magic = BitConverter.ToUInt16(buf, 0);
            // ReverseByte(buf, 4, 6);
            // res.version = BitConverter.ToUInt16(buf, 4);
            // ReverseByte(buf, 6, 8);
            // res.checkSum = BitConverter.ToUInt16(buf, 6);
            ReverseByte(buf, 2, 6);
            res.msgLen = BitConverter.ToUInt32(buf, 2);
            return res;
        }
        
        public static void SendMessage(byte[] msgData, byte channel, WriteRelaySegment gate)
        {
            if (channel == 1)
            {
                gate.Invoke(msgData);
                return;
            }
            SendMessage(msgData, gate);
        }

        public static void SendMessage(byte[] msgData, WriteRelaySegment gate)
        {
            int segSize = 1024;

            for (int i = 0; i * segSize < msgData.Length; i++)
            {
                int tSize = Math.Min(msgData.Length - i * segSize, segSize);
                int offset2 = i * segSize;
                int offset1 = 0;
                byte[] buf = null;
                
                if (i == 0)
                {
                    buf = new byte[tSize + HeaderSize];
                    offset1 = HeaderSize;
                    
                    RelayBaseHeader header = new RelayBaseHeader();
                    header.msgLen = (UInt32)msgData.Length;
                    header.WriteToByteArray(buf);
                }
                else
                {
                    buf = new byte[tSize];
                }
                
                // 这边怎么优化呢？不知道怎么调用系统函数
                for (int j = 0; j < tSize; j++)
                {
                    buf[offset1 + j] = msgData[offset2 + j];
                }

                gate.Invoke(buf);
                
            }
        }
        
        
        public static byte[] SendMessageWithoutLimit(byte[] msgData)
        {
            int tSize = msgData.Length;
            byte[] buf = new byte[tSize + HeaderSize];

            RelayBaseHeader header = new RelayBaseHeader();
            header.msgLen = (UInt32)msgData.Length;
            header.WriteToByteArray(buf);
            
            int offset1 = HeaderSize;
            ;
            for (int j = 0; j < tSize; j++)
            {
                buf[offset1 + j] = msgData[j];
            }

            return buf;
        }
        
        
    }
}