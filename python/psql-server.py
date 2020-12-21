import socketserver 
import struct
import sys

def char_to_hex(char):
    if(char == 0):
        retval = "000"
    else:
        retval = hex(ord(char))

    if len(retval) == 4:
        return retval[-2:]
    else:
        assert len(retval) == 3
        return "0" + retval[-1]

def int_to_hex(value):
    retval = hex(value)
    if len(retval) == 4:
        return retval[-2:]
    if len(retval) == 1:
        return "0" + retval
    assert len(retval) == 3
    return "0" + retval[-1]

def to_hex(input):
    if isinstance(input, str):
        return str_to_hex(input)
    return bytes_to_hex(input)    

def str_to_hex(inputstr):
    return " ".join(char_to_hex(byte) for byte in inputstr)

def bytes_to_hex(bytearray):
    return " ".join(int_to_hex(byte) for byte in bytearray)

class Handler(socketserver.BaseRequestHandler):
    def handle(self):
        print("handle()")
        self.read_SSLRequest()
        self.send_to_socket("N")

        self.read_StartupMessage()
        self.send_AuthenticationClearText()
        self.read_PasswordMessage()
        self.send_AuthenticationOK()
        while (True):
            self.send_ReadyForQuery()
            self.read_Query()
            self.send_queryresult()

    def send_queryresult(self):
        field_format_code = 0 # 0=text 1=binary

        fieldnames = ['abc', 'def']
        HEADERFORMAT = "!cih"
        fields = b''
        for fieldname in fieldnames:
            fields += self.fieldname_msg(fieldname, field_format_code)

        rdheader = struct.pack(HEADERFORMAT, b'T', struct.calcsize(HEADERFORMAT) - 1 + len(fields), len(fieldnames))
        self.send_to_socket(rdheader + fields)

        rows = [[1, 2], [3, 4567890]]
        DRHEADER = "!cih"
        for row in rows:
            # null values: dr_data = struct.pack("!ii", -1, -1)
            dr_columns = b''
            for col in row:
                #dr_columns += struct.pack("!ii", 4, col)
                
                # the format_code in fieldname_msg might play a role here, but it seems like all integers have to be converted to strings anyways
                str_value = str(col).encode()
                dr_columns += struct.pack("!i", len(str_value))
                dr_columns += str_value
            
            dr_header = struct.pack(DRHEADER, b'D', struct.calcsize(DRHEADER) - 1 + len(dr_columns), len(row))
            dr = dr_header + dr_columns
            self.send_to_socket(dr)
        
        # add a "null" row
        dr_data = struct.pack("!ii", -1, -1)
        dr_header = struct.pack(DRHEADER, b'D', struct.calcsize(DRHEADER) - 1 + len(dr_data), 2)
        dr = dr_header + dr_data
        self.send_to_socket(dr)

        self.send_CommandComplete()

    def send_CommandComplete(self):
        HFMT = "!ci"
        msg = "SELECT 2\x00".encode()
        self.send_to_socket(struct.pack(HFMT, b'C', struct.calcsize(HFMT) - 1 + len(msg)) + msg)

    def fieldname_msg(self, name, format_code):
        tableid = 0
        columnid = 0
        datatypeid = 23
        datatypesize = 4
        typemodifier = -1
        
        assert format_code == 0 or format_code == 1 # 0=text 1=binary
        
        msg_end = struct.pack("!ihihih", tableid, columnid, datatypeid, datatypesize, typemodifier, format_code)
        return name.encode('ascii') + b'\0' + msg_end

    def read_socket(self):
        print("Trying recv...")
        data = self.request.recv(1024)
        print("Received {} bytes: {}".format(len(data), repr(data)))
        print("Hex: {}".format(bytes_to_hex(data)))
        return data

    def send_to_socket(self, data):
        print("Sending {} bytes: {}".format(len(data), repr(data)))
        print("Hex: {}".format(to_hex(data)))
        to_send = data
        if (type(to_send) == str):
            to_send = data.encode("ascii")
        return self.request.sendall(to_send)

    def read_Query(self):
        data = self.read_socket()
        msgident, msglen = struct.unpack("!ci", data[0:5])
        assert msgident == b'Q'
        print(data[5:])

    def send_ReadyForQuery(self):
        self.send_to_socket(struct.pack("!cic", b'Z', 5, b'I'))

    def read_PasswordMessage(self):
        i = 0
        data = b''
        for i in range(0, 9):
            data = self.read_socket()
            if len(data) > 0:
                break

        b, msglen = struct.unpack("!ci", data[0:5])
        assert b == b'p'
        print("Password: {}".format(data[5:]))


    def read_SSLRequest(self):
        data = self.read_socket()
        msglen, sslcode = struct.unpack("!ii", data)
        assert msglen == 8
        assert sslcode == 80877103

    def read_StartupMessage(self):
        data = self.read_socket()
        msglen, protoversion = struct.unpack("!ii", data[0:8])
        print("msglen: {}, protoversion: {}".format(msglen, protoversion))
        assert msglen == len(data)
        parameters = data[8:]
        parameters_string = parameters.decode("ascii")
        #print(parameters_string.split('\x00'))
        print(parameters_string.split('\x00'))

    def send_AuthenticationOK(self):
        retval = struct.pack("!cii", b'R', 8, 0)
        self.send_to_socket(retval)

    def send_AuthenticationClearText(self):
        retval = struct.pack("!cii", b'R', 8, 3)
        self.send_to_socket(retval)

if __name__ == "__main__":
    port = 9876

    if len(sys.argv) == 2:
        port = int(sys.argv[0])
    else:
        if len(sys.argv) > 2:
            print("Only a single parameter is supported: the port number")
            print("Default port: ", port)
            exit(1)

    server = socketserver.TCPServer(("localhost", port), Handler)
    try:
        print("Server started on port", port, ".")
        server.serve_forever()
    except:
        server.shutdown()
