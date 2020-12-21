using System;

namespace PostgresWireProtocolServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("I am a Postgres Server");
            var wireServer = new WireServer("127.0.0.1", 9876);
            wireServer.StartListener();
        }
    }
}
