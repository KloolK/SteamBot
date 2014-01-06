using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.TF2;
using SteamKit2.Internal;

namespace SteamBot.TF2GC
{
    public class MsgSetItemPosition : IGCSerializableMessage
    {
        public ulong id;
        public uint position;

        public MsgSetItemPosition()
        {
            
        }

        public bool IsProto
        {
            get { return false; }
        }

        public uint MsgType
        {
            get { return 1001; }
        }

        public JobID TargetJobID
        {
            get { return new JobID(); }
            set { throw new NotImplementedException(); }
        }

        public JobID SourceJobID
        {
            get { return new JobID(); }
            set { throw new NotImplementedException(); }
        }

        public byte[] Serialize()
        {
            List<byte> ret = new List<byte>();
            ret.AddRange(BitConverter.GetBytes((ulong)id));
            ret.AddRange(BitConverter.GetBytes((uint)position));
            return ret.ToArray();
        }

        public void Serialize(Stream stream)
        {
            byte[] buf = Serialize();
            stream.Write(buf, 0, buf.Length);
        }

        public void Deserialize(Stream stream)
        {

        }

        public uint GetEMsg()
        {
            return 1001;
        }
    }
}