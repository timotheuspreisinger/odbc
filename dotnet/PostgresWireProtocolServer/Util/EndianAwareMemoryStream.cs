using System;
using System.IO;

namespace PostgresWireProtocolServer.Util
{
    public class EndianAwareMemoryStream : MemoryStream
    {
        public bool IsLittleEndian { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="writeBigEndian">Flag indicating if all integer data types should be written in litte endian format (least significant byte first).</param>
        public EndianAwareMemoryStream(bool writeLittleEndian)
        {
            this.IsLittleEndian = writeLittleEndian;
        }
        public void Write(byte b)
        {
            base.Write(new byte[] { b });
        }

        protected void WriteEndianAware(byte[] value)
        {
            var val = new byte[value.Length];
            Array.Copy(value, val, value.Length);
            if (BitConverter.IsLittleEndian != this.IsLittleEndian)
            {
                Array.Reverse(val);
            }
            base.Write(val);
        }

        public void Write(short value)
        {
            var output = BitConverter.GetBytes(value);
            WriteEndianAware(output);
        }
        public void Write(int value)
        {
            var output = BitConverter.GetBytes(value);
            WriteEndianAware(output);
        }
        public void Write(uint value)
        {
            var output = BitConverter.GetBytes(value);
            WriteEndianAware(output);
        }
    }

}