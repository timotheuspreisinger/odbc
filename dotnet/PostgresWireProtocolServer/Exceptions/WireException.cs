namespace PostgresWireProtocolServer.Exceptions
{
    public class WireException : System.Exception
    {
        public WireException() { }
        public WireException(string message) : base(message) { }
        public WireException(string message, System.Exception inner) : base(message, inner) { }
        protected WireException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}