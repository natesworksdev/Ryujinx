using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Sfdnsres
{
    // C definition
    // struct addrinfo {
    //     int              ai_flags;
    //     int              ai_family;
    //     int              ai_socktype;
    //     int              ai_protocol;
    //     socklen_t        ai_addrlen;
    //     struct sockaddr *ai_addr;
    //     char            *ai_canonname;
    //     struct addrinfo *ai_next;
    // };
    class AddrInfo
    {
        public int Magic;
        public int Flags;
        public AddressFamily Family;
        public int SocketType;
        public int Protocol;
        // public int AddrLen;
        public byte[] Addrs;
        public string CanonName;
    }
}