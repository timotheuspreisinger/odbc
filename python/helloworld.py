import SocketServer
import struct

def char_to_hex(char):
    retval = hex(ord(char))
    if len(retval) == 4:
        return retval[-2:]
    else:
        assert len(retval) == 3
        return "0" + retval[-1]

def str_to_hex(inputstr):
    return " ".join(char_to_hex(char) for char in inputstr)

class Handler(SocketServer.BaseRequestHandler):
    def handle(self):
        print "handle()"
        self.read_SSLRequest()
        self.send_to_socket("N")

        self.read_StartupMessage()
        self.send_AuthenticationClearText()
        self.read_PasswordMessage()
        self.send_AuthenticationOK()
        self.send_ReadyForQuery()
        self.read_Query()
        self.send_queryresult()

    def send_queryresult(self):
        fieldnames = ['abc', 'def']
        HEADERFORMAT = "!cih"
        fields = ''.join(self.fieldname_msg(name) for name in fieldnames)
        rdheader = struct.pack(HEADERFORMAT, 'T', struct.calcsize(HEADERFORMAT) - 1 + len(fields), len(fieldnames))
        self.send_to_socket(rdheader + fields)

        rows = [[1, 2], [3, 4]]
        DRHEADER = "!cih"
        for row in rows:
            dr_data = struct.pack("!ii", -1, -1)
            dr_header = struct.pack(DRHEADER, 'D', struct.calcsize(DRHEADER) - 1 + len(dr_data), 2)
            self.send_to_socket(dr_header + dr_data)

        self.send_CommandComplete()
        self.send_ReadyForQuery()

    def send_CommandComplete(self):
        HFMT = "!ci"
        msg = "SELECT 2\x00"
        self.send_to_socket(struct.pack(HFMT, "C", struct.calcsize(HFMT) - 1 + len(msg)) + msg)

    def fieldname_msg(self, name):
        tableid = 0
        columnid = 0
        datatypeid = 23
        datatypesize = 4
        typemodifier = -1
        format_code = 0 # 0=text 1=binary
        return name + "\x00" + struct.pack("!ihihih", tableid, columnid, datatypeid, datatypesize, typemodifier, format_code)

    def read_socket(self):
        print "Trying recv..."
        data = self.request.recv(1024)
        print "Received {} bytes: {}".format(len(data), repr(data))
        print "Hex: {}".format(str_to_hex(data))
        return data

    def send_to_socket(self, data):
        print "Sending {} bytes: {}".format(len(data), repr(data))
        print "Hex: {}".format(str_to_hex(data))
        return self.request.sendall(data)

    def read_Query(self):
        data = self.read_socket()
        msgident, msglen = struct.unpack("!ci", data[0:5])
        assert msgident == "Q"
        print data[5:]


    def send_ReadyForQuery(self):
        self.send_to_socket(struct.pack("!cic", 'Z', 5, 'I'))

    def read_PasswordMessage(self):
        data = self.read_socket()
        b, msglen = struct.unpack("!ci", data[0:5])
        assert b == "p"
        print "Password: {}".format(data[5:])


    def read_SSLRequest(self):
        data = self.read_socket()
        msglen, sslcode = struct.unpack("!ii", data)
        assert msglen == 8
        assert sslcode == 80877103

    def read_StartupMessage(self):
        data = self.read_socket()
        msglen, protoversion = struct.unpack("!ii", data[0:8])
        print "msglen: {}, protoversion: {}".format(msglen, protoversion)
        assert msglen == len(data)
        parameters_string = data[8:]
        print parameters_string.split('\x00')

    def send_AuthenticationOK(self):
        self.send_to_socket(struct.pack("!cii", 'R', 8, 0))

    def send_AuthenticationClearText(self):
        self.send_to_socket(struct.pack("!cii", 'R', 8, 3))

if __name__ == "__main__":
    server = SocketServer.TCPServer(("localhost", 9876), Handler)
    try:
        server.serve_forever()
    except:
        server.shutdown()