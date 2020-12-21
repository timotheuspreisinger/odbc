* Postgres Wire Protocol
This document contains the description of the implemented parts of the Postgres Wire Protocol.
For more details on the protocol, see the [Postgres protocol documentation](https://www.postgresql.org/docs/13/protocol-message-formats.html) 
and [https://www.pgcon.org/2014/schedule/attachments/330_postgres-for-the-wire.pdf](https://www.pgcon.org/2014/schedule/attachments/330_postgres-for-the-wire.pdf).

The protocol uses network byte order (i. e. big endian).

The "overall message length", sent as int32 after the message type char usually includes the four bytes for the message length, but not the character before 
that determines the message type.

*** Connection 1 
Using psql command line tool as client, this first connection ends with the password prompt. 
The first steps will be repeated in the second connection.
1. SSL negotiation
   a. client->server: 8 bytes: (int32) 8, (int32) 80877103: the first int32 is the overall message length 
      (including that first int32), the second is a magic number
   b. server->client: character 'N'
2. Startup message
   a. client->server: (byte) 3, (int32) protocol version, list of string key-value pairs with each string terminated by byte 0

3. Authentication request (sending a very unsafe clear text password)
   a. server->client: character 'R', (int32) 8, (int32) 3

*** Connection 2
Continuing with the answer to the password request:
3. Authentication request
   a. client->server: 'p', (int32) overall message length, (string) password, (byte) 0
   b. server->client: 'R', (int32) 8, (int32) 0: authentication success
4. Various status messages:
   a. server->client: 'S', (int32) overall message length, setting name, zero-byte, setting value, zero-byte
      status messages might include: 
      - application_name (as received in startup message)
      - client_encoding (as received in startup message)
      - server_encoding
      - server_version
      - session_authorization: postgres
      - DateStyle: ISO, DMY
      - TimeZone: Europe/London
   b. server->client: 'K', (int32) 12, (int32) target backend process id, (int32) target backend secret key: backend key data (for cancel requests)
5. Ready for query
   a. server->client: 'Z', (int32) 5, (char) 'I'

After this there is a loop of queries and results:

6. Query
   a. client->server: 'Q', (int32) message length, query string with trailing zero-byte
   b. server->client: result header (starting with 'T' and any number of data rows, starting with 'D'
                      'T', (int32) overall message length, (int16) column count, 1 to n column definitions with each being: 
                      (string) column name, (byte) 0, (int32) table-id, (int16) column-id, (int32) data type id, (int16) data type size, (int32) type modifier, (int16) format code 0=text, 1=binary
                      'D', (int32) overall message length, (int16) column count, 1 to n column values with each being:
                      (int32) cell content length, (string) cell content as string
                      For NULL values, (int32) -1 is used (so a negative size and no content bytes)
   c. server->client: 'Z', (int32) 5, (char) 'I': ready for query

*** Other messages

Connection end:
- client->server: 'X', (int32) 4: connection termination message

Cancel request (to cancel a running query):
- client->server: (int32) 16, (int32) 80877102, (int32) target backend process id, (int32) target backend secret key)


