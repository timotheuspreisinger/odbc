namespace PostgresWireProtocolServer.Exceptions
{
        public class WireProtocolException : WireException
    {
        public WireProtocolException() { }
        public WireProtocolException(string message) : base(message) { }
        public WireProtocolException(string message, System.Exception inner) : base(message, inner) { }
        protected WireProtocolException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}