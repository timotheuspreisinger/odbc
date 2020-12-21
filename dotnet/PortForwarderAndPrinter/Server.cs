using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PortForwarderAndPrinter
{
    public class Server
    {
        TcpListener server = null;
        string remoteIp;
        int remotePort;

        static int messageCounter = 0;

        public Server(string remoteIp, int remotePort, int listeningPort)
        {
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            this.server = new TcpListener(localAddr, listeningPort);
            this.remoteIp = remoteIp;
            this.remotePort = remotePort;
        }

        public void StartListener()
        {
            server.Start();
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    Thread t = new Thread(new ParameterizedThreadStart(HandleRequest));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                if (server != null)
                {
                    server.Stop();
                }
            }
        }

        public void HandleRequest(object obj)
        {
            TcpClient fromClient = (TcpClient)obj;
            var inputStream = fromClient.GetStream();

            TcpClient toServer = new TcpClient(remoteIp, remotePort);
            var outputStream = toServer.GetStream();
            
            var bytes = new byte[8192];
            int bytesReceived;

            try 
            {
                while ((bytesReceived = inputStream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    Print("From client", bytes, bytesReceived);
                    outputStream.Write(bytes, 0, bytesReceived);

                    try
                    {
                        //while (outputStream.DataAvailable && (bytesReceived = outputStream.Read(bytes, 0, bytes.Length)) > 0)
                        bytesReceived = outputStream.Read(bytes, 0, bytes.Length);
                        {
                            if (bytesReceived != -1) {
                                Print("From server", bytes, bytesReceived);
                                inputStream.Write(bytes, 0, bytesReceived);
                            }
                        }
                    } 
                    catch (IOException e)
                    {
                        var closeStream = IsConnectionTerminationByOtherParty(e);
                        if (closeStream)
                        {
                            // server has terminated the connection
                            inputStream.Close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                IsConnectionTerminationByOtherParty(e);
            }
        }


        public bool IsConnectionTerminationByOtherParty(Exception e)
        {
            if (e.InnerException != null && e.InnerException is SocketException se && se.NativeErrorCode == 10054)
            {
                lock (this) 
                {
                    messageCounter++;
                    Console.WriteLine($"{messageCounter} Connection terminated by other party");
                    return true;
                }
            }
            else 
            {
                Console.WriteLine("Exception: {0}", e);
            }
            return false;
        }

        public void Print(string prefix, byte[] bytes, int length)
        {
            lock (this)
            {
                if (length == 0)
                {
                    return;
                }
                messageCounter++;
                var hex = BitConverter.ToString(bytes, 0, length);
                var txt = System.Text.Encoding.UTF8.GetString(bytes, 0, length);
                Console.WriteLine($"========================================================================");
                Console.WriteLine($"{messageCounter} {prefix} LENGTH: {length}, hex {length.ToString("X")}");
                Console.WriteLine($"{messageCounter} {prefix} HEX: {hex}");
                Console.WriteLine($"{messageCounter} {prefix} TXT: {txt}");
            }
        }
    }
}