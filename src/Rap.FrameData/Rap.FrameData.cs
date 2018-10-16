using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Rap
{
    public class FrameData : StreamWriter, IFrameData
    {
        private const int InitialBufferSize = 4096 - 16;
        private static ConcurrentBag<FrameData> _pool = new ConcurrentBag<FrameData>();

        public static FrameData Take()
        {
            FrameData frameData;
            if (_pool.TryTake(out frameData))
            {
                return frameData;
            }
            return new FrameData();
        }

        private FrameData() : base(new MemoryStream(InitialBufferSize))
        {
        }

        public new void Dispose()
        {
            if (_pool.Count < Constants.MaxConnID)
            {
                Flush();
                BaseStream.SetLength(0);
                _pool.Add(this);
            }
            else
            {
                base.Dispose();
            }
        }

        public void CopyTo(Stream s)
        {
            Flush();
            var memoryStream = (MemoryStream)BaseStream;
            memoryStream.Position = 0;
            memoryStream.CopyTo(s);
        }
    }
}
