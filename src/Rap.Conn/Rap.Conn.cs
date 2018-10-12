using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rap
{
    public class Conn
    {
        public const ushort MuxerConnID = 8192;
        public const ushort MaxConnID = MuxerConnID - 1;

        private readonly IMuxer _muxer;

        public Conn(IMuxer muxer)
        {
            _muxer = muxer;
        }
    }
}