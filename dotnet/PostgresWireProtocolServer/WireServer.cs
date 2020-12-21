using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using PostgresWireProtocolServer.Exceptions;
using PostgresWireProtocolServer.Util;
using PostgresWireProtocolServer.PostgresTypeHandling;
using System.Data;

namespace PostgresWireProtocolServer
{
    public class WireServer
    {
        TcpListener server = null;
        public WireServer(string ip, int port)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
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
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();

            string data = null;
            var bytes = new byte[8192];
            int bytesReceived;
            try
            {
                bytesReceived = stream.Read(bytes, 0, bytes.Length);

                // startup: read ssl message if possible
                if (bytesReceived == 8)
                {
                    ReadSSLNegoPackage(bytes, bytesReceived);
                    // send 'N' as we are not using SSL
                    stream.Write(new byte[] { (byte) 'N'}, 0, 1);
                    // read startup message
                    bytesReceived = stream.Read(bytes, 0, bytes.Length);
                }
                else 
                {
                    // the startup message has been sent already
                }

                if (bytesReceived == 16 && BitConverter.ToInt32(bytes) == 16 && BitConverter.ToInt32(bytes, 4) == 80877102 /* magic number, see "CancelRequest" in https://www.postgresql.org/docs/12/protocol-message-formats.html */)
                {
                    // query cancellaction message received
                    var targetBackendProcessId = BitConverter.ToInt32(bytes, 8);
                    var targetBackendSecret = BitConverter.ToInt32(bytes, 12);
                }

                var startupMessageContent = ReadStartupMessage(bytes, bytesReceived);

                // send (clear-text) authentication request  
                SendClearTextAuthenticationRequest(stream);

                bytesReceived = stream.Read(bytes, 0, bytes.Length);
                if (bytesReceived == 0)
                {
                    // end the connection as with the client sending the password a new connection will be established
                    // this behaviour is required for the psql console tool
                    return;
                }
                var password = ReadPassword(bytes, bytesReceived);

                // cache multiple messages
                var combinedOutput = new MemoryStream();

                // send authentication success message
                SendStatusMessage(combinedOutput, 'R', 0);

                // send some status messages
                if (! startupMessageContent.TryGetValue("application_name", out var applicationName))
                {
                    applicationName = string.Empty;
                }
                if (! startupMessageContent.TryGetValue("client_encoding", out var clientEncoding))
                {
                    clientEncoding = "UTF8";
                }

                SendParameterStatusMessage(combinedOutput, "application_name", applicationName);
                SendParameterStatusMessage(combinedOutput, "client_encoding", clientEncoding);
                SendParameterStatusMessage(combinedOutput, "DateStyle", "ISO, DMY");
                SendParameterStatusMessage(combinedOutput, "intervalStyle", "postgres");
                SendParameterStatusMessage(combinedOutput, "integer_datetimes", "on");
                SendParameterStatusMessage(combinedOutput, "is_superuser", "off");
                SendParameterStatusMessage(combinedOutput, "server_encoding", "UTF8");
                SendParameterStatusMessage(combinedOutput, "server_version", "13.X");
                SendParameterStatusMessage(combinedOutput, "session_authorization", "postgres");
                SendParameterStatusMessage(combinedOutput, "standard_conforming_strings", "on");
                SendParameterStatusMessage(combinedOutput, "TimeZone", "Europe/London");
                SendParameterStatusMessage(combinedOutput, "standard_conforming_strings", "on");
                SendBackendKeyData(combinedOutput);
                // send multiple messages
                stream.Write(combinedOutput.ToArray());
                combinedOutput.SetLength(0);

                // send ready for query
                SendReadyForQuery(stream);

                var requestCount = 0;
                while ((bytesReceived = stream.Read(bytes, 0, bytes.Length)) != -1)
                {
                    try {
                        requestCount++;


                        if (bytesReceived == 16 && BitConverter.ToInt32(bytes) == 16 && BitConverter.ToInt32(bytes, 4) == 80877102 /* magic number, see "CancelRequest" in https://www.postgresql.org/docs/12/protocol-message-formats.html */)
                        {
                            // query cancellaction message received: we ignore this
                            // the process id and the secret would have to match the values sent in method <see cref="SendBackendKeyData(Stream)"/>
                            var targetBackendProcessId = BitConverter.ToInt32(bytes, 8);
                            var targetBackendSecret = BitConverter.ToInt32(bytes, 12);
                            continue;
                        }

                        data = ReadQuery(bytes, bytesReceived);

                        if (data == null)
                        {
                            // termination message received
                            break;
                        }
                    
                        // handle "special" queries, e. g. such to check the connectivity 
                        var parts = data.ToLowerInvariant().Split(';').Select(element => element.Trim()).Where(part => ! string.IsNullOrEmpty(part));
                        // show transaction_isolation: JDBC driver, Windows ODBC dialog
                        if (parts.Any(commandPart => System.Text.RegularExpressions.Regex.IsMatch(commandPart, "^show\\W*transaction_isolation$")))
                        {
                            SendTransactionIsolationState(stream);
                        }
                        // select oid, typbasetype from pg_type where typname = 'lo': Windows ODBC driver
                        else if (parts.Any(commandPart => System.Text.RegularExpressions.Regex.IsMatch(commandPart, "^select\\W*oid,\\W*typbasetype\\W*from\\W*pg_type\\W*where\\W*typname\\W*=\\W*'lo'")))
                        {
                            SendOidTypeQueryResult(stream);
                        }
                        else 
                        {
                            // do something or send the same result set over and over
                            SendStaticResult(stream);
                        }
                    } 
                    catch (Exception ex)
                    {
                        // ignore all exceptions except IOExceptions as they mean the connection has been lost
                        if (ex is System.IO.IOException)
                        {
                            throw ex;
                        }
                        Console.WriteLine("Exception: {0}", ex.ToString());
                    }
                    SendReadyForQuery(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally 
            {
                if (client != null)
                {
                    client.Close();
                }
            }
        }

        public void ReadSSLNegoPackage(byte[] message, int length)
        {
            if (length != 8)
            {
                throw new WireProtocolException("message does not have correct length");
            }
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(message, 0, 4);
                Array.Reverse(message, 4, 4);
            }

            var msglength = BitConverter.ToInt32(message, 0);
            var content = BitConverter.ToInt32(message, 4);
            if (msglength != 8)
            {
                throw new WireProtocolException("message length for SSL message is not correct");
            }
            if (content != 80877103) 
            {
                throw new WireProtocolException("SSL magic number is not correct");
            }
        }

        public Dictionary<string, string> ReadStartupMessage(byte[] bytes, int length)
        {
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, 4);
                Array.Reverse(bytes, 4, 4);
            }
            var startupMessageLength = BitConverter.ToInt32(bytes, 0);
            var protocolVersion = BitConverter.ToInt32(bytes, 4);
            if (length != startupMessageLength)
            {
                throw new WireProtocolException("startup message has incorrect length");
            }

            var startupMessage = System.Text.Encoding.UTF8.GetString(bytes, 8, startupMessageLength - 10 /* 8 bytes are used for length and protocol version, the last used byte is 0 (as it is the delimiter), and we start with byte zero (eight is the offset) */ );
            var messageParts = startupMessage?.Split('\0');
            if (messageParts?.Length % 2 != 0)
            {
                throw new WireProtocolException($"startup message has invalid token count {messageParts?.Length}");
            }

            if (messageParts.Length < 2)
            {
                throw new WireProtocolException("startup message invalid");
            }

            var result = new Dictionary<string, string>();
            result["protocol_version"] = protocolVersion.ToString();

            for (int i = 0; i < messageParts.Length; i += 2)
            {
                result[messageParts[i]] = messageParts[i + 1];
            }
            return result;
        }

        public string ReadStringMessage(byte[] bytes, int length, IEnumerable<char> allowedMessageTypes)
        {
            if (! (bytes?.Length > 0) || length <= 0)
            {
                throw new WireProtocolException("message content required");
            }
            var messageType = char.ToLowerInvariant((char) bytes[0]);
            var validMessageTypes = allowedMessageTypes.Select(msgTyp => char.ToLowerInvariant(msgTyp));
            if (! validMessageTypes.Where(msgTyp => msgTyp == messageType).Any())
            {
                throw new WireProtocolException("unknown message type");
            }

            if (length < 5)
            {
                throw new WireProtocolException("invalid length length");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 1, 4);
            }
            var messageLength = BitConverter.ToInt32(bytes, 1);

            if (messageType == 'x' && length == 5 && messageLength == 4)
            {
                // termination message
                return null;
            }

            if (length != messageLength + 1)
            {
                throw new WireProtocolException("message length incorrect");
            }
            if (bytes[length - 1] != 0)
            {
                throw new WireProtocolException("message format incorrect");
            }

            var result = System.Text.Encoding.UTF8.GetString(bytes, 5, messageLength - 5);
            return result;
        }

        public string ReadPassword(byte[] bytes, int length)
        {
            return ReadStringMessage(bytes, length, new char[] { 'p' /* password message */ } );
        }

        public string ReadQuery(byte[] bytes, int length)
        {
            return ReadStringMessage(bytes, length, new char[] { 'q' /* query message */ , 'x' /* termination message */ });
        }

        public void SendClearTextAuthenticationRequest(Stream stream)
        {
            SendStatusMessage(stream, 'R', 3);
        }
        public void SendReadyForQuery(Stream stream)
        {
            SendStatusMessage(stream, 'Z', 'I');
        }

        public void SendStatusMessage(Stream outputStream, char messageType, int status)
        {
            var wireOutput = new WireOutputMemoryStream();
            wireOutput.Write((byte) messageType);
            // message length is always 8 (as the length is sent as int32 and the status as int32)
            wireOutput.Write(8);
            wireOutput.Write(status);
            outputStream.Write(wireOutput.ToArray());
        }
        public void SendStatusMessage(Stream outputStream, char messageType, char message)
        {
            var wireOutput = new WireOutputMemoryStream();
            wireOutput.Write((byte) messageType);
            // message length is always 5 (as the length is sent as int32 and the message as byte)
            wireOutput.Write(5);
            wireOutput.Write((byte) message);
            outputStream.Write(wireOutput.ToArray());
        }
        public void SendStatusMessage(Stream outputStream, char messageType, string message)
        {
            var wireOutput = new WireOutputMemoryStream();
            wireOutput.Write((byte) messageType);
            
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

            wireOutput.Write(4 + messageBytes.Length + 1 /* int32:length + message-byte-count + zero-byte */);
            wireOutput.Write(messageBytes);
            wireOutput.WriteZeroByte();
            outputStream.Write(wireOutput.ToArray());
        }
        public void SendParameterStatusMessage(Stream outputStream, string parameterName, string parameterValue)
        {
            var wireOutput = new WireOutputMemoryStream();
            wireOutput.Write((byte) 'S');
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(parameterName);
            var valueBytes = System.Text.Encoding.UTF8.GetBytes(parameterValue);
            wireOutput.Write(4 + nameBytes.Length + 1 + valueBytes.Length + 1 /* int32:length + name length + zero byte + value length + zero byte */);
            wireOutput.Write(nameBytes);
            wireOutput.WriteZeroByte();
            wireOutput.Write(valueBytes);
            wireOutput.WriteZeroByte();
            outputStream.Write(wireOutput.ToArray());
        }

        public void SendBackendKeyData(Stream outputStream)
        {
            var wireOutput = new WireOutputMemoryStream();
            wireOutput.Write((byte) 'K');
            wireOutput.Write(12);

            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            wireOutput.Write(processId);
            wireOutput.Write(3913636326 /* a magic random number */);
            outputStream.Write(wireOutput.ToArray());
        }

        public void SendTransactionIsolationState(Stream outputStream)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("transaction_isolation", typeof(string)));
            var row = table.NewRow();
            row[0] = "uncommitted read";
            table.Rows.Add(row);
            SendResult(outputStream, table, "SHOW");
        }

        public void SendOidTypeQueryResult(Stream outputStream)
        {
            var column1 = new DataColumn("oid", typeof(int));
            var oidTableId = 1247;
            column1.ExtendedProperties[PostgresTypeInformation.ExtendedPropertyName] = PostgresTypeInformation.PredefinedTypes[PostgresTypeOID.Oid];
            column1.ExtendedProperties[Constants.ExtendedPropertyTableId] = oidTableId;
            var column2 = new DataColumn("typbasetype", typeof(int));
            column2.ExtendedProperties[PostgresTypeInformation.ExtendedPropertyName] = PostgresTypeInformation.PredefinedTypes[PostgresTypeOID.Oid];
            column2.ExtendedProperties[Constants.ExtendedPropertyTableId] = oidTableId;
            var table = new DataTable();
            table.Columns.Add(column1);
            table.Columns.Add(column2);
            SendResult(outputStream, table, "SELECT 0");
        }

        public void SendStaticResult(Stream outputStream)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("abc", typeof(int)));
            table.Columns.Add(new DataColumn("def", typeof(int)));

            var row = table.NewRow();
            row[0] = 1;
            row[1] = 2;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 3;
            row[1] = 4567890;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = DBNull.Value;
            row[1] = 5;
            table.Rows.Add(row);

            SendResult(outputStream, table, "SELECT 2");
        }

        public void SendResult(Stream outputStream, DataTable data, string commandCompleteMessage)
        {
            var columnHeaders = new WireOutputMemoryStream();
            foreach (DataColumn column in data.Columns)
            {
                columnHeaders.Write(CreateColumnHeader(column));
            }

            var columnHeadersLength = columnHeaders.Length;
            columnHeadersLength = 4 + 2 + columnHeadersLength  /* 4 bytes for the (int) column definition byte count + 2 bytes for the (short) column count + column definition bytes */;

            var columnCount = data.Columns.Count;


            var header = new WireOutputMemoryStream();
            header.Write(new byte[] { (byte) 'T' });
            header.Write((int) columnHeadersLength);
            header.Write((short) columnCount);
            header.Write(columnHeaders.ToArray());
            outputStream.Write(header.ToArray());

            foreach (DataRow row in data.Rows)
            {
                var cells = new WireOutputMemoryStream();
                foreach (var cell in row.ItemArray)
                {
                    if (cell == null)
                    {
                        cells.Write(-1);
                    }
                    else if (cell is string stringCell)
                    {
                        var cellContent = System.Text.Encoding.UTF8.GetBytes(stringCell);
                        cells.Write((int) cellContent.Length);
                        cells.Write(cellContent);
                    }
                    else 
                    {
                        var cellContent = cell.ToString();
                        var cellContentBytes = System.Text.Encoding.UTF8.GetBytes(cellContent);
                        cells.Write((int) cellContentBytes.Length);
                        cells.Write(cellContentBytes);
                    }
                }
                var rowOutput = new WireOutputMemoryStream();
                rowOutput.Write((byte) 'D');
                var rowLength = cells.Length;
                rowLength = 4 + 2 + rowLength /* 4 bytes for the (int) cell data + 2 bytes for the (short) column count + cell content */;
                rowOutput.Write((int) rowLength);
                rowOutput.Write((short) row.ItemArray.Count());
                rowOutput.Write(cells.ToArray());
                outputStream.Write(rowOutput.ToArray());
            }
            
            // send "command complete" message
            SendStatusMessage(outputStream, 'C', "SELECT 0");
        }

        public byte[] CreateColumnHeader(DataColumn column)
        {
            PostgresTypeInformation postgresTypeInformation = null;
            if (column.ExtendedProperties.ContainsKey(PostgresTypeInformation.ExtendedPropertyName))
            {
                postgresTypeInformation = column.ExtendedProperties[PostgresTypeInformation.ExtendedPropertyName] as PostgresTypeInformation;
            }
            if (postgresTypeInformation == null)
            {
                postgresTypeInformation = PostgresTypeInformation.Mapping[column.DataType];
            }
            var tableId = column.ExtendedProperties.ContainsKey(Constants.ExtendedPropertyTableId) ? (column.ExtendedProperties[Constants.ExtendedPropertyTableId] as int?) : 0;
            var columnId = (short) (column.Ordinal + 1);
            var dataTypeId = postgresTypeInformation.Oid;
            var dataTypeSize = (short) (postgresTypeInformation.Size ?? 4);
            var typeModifier = -1;
            var formatCode = (short) 0; // 0=text 1=binary; use text, as the binary representation might change with new releases of Postgres servers and clients

            var wireOutput = new WireOutputMemoryStream();
            wireOutput.Write(System.Text.Encoding.UTF8.GetBytes(column.ColumnName));
            wireOutput.WriteZeroByte();
            wireOutput.Write((int) tableId);
            wireOutput.Write(columnId);
            wireOutput.Write(dataTypeId);
            wireOutput.Write(dataTypeSize);
            wireOutput.Write(typeModifier);
            wireOutput.Write((short) formatCode);
            
            return wireOutput.ToArray();
        }
    }
}