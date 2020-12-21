using System;
using System.IO;

namespace PostgresWireProtocolServer.Util
{
    public class WireOutputMemoryStream : EndianAwareMemoryStream
    {
        public WireOutputMemoryStream() : base(false)
        {
        }

        public void WriteZeroByte() 
        {
            base.Write((byte) 0);
        }
    }

}