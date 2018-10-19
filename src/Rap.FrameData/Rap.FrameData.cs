using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Rap
{
    public class FrameData : MemoryStream, IFrameData
    {
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

        public void ReadFrom(Stream s)
        {
            Flush();
            int count = 0;
            // read header
            Position = 0;
            var ba = GetBuffer();
            while (count < 4)
                count += s.Read(ba, count, 4 - count);
            int payloadSize = ((int)ba[0] << 8) | ba[1];
            int needed = count + payloadSize;
            while (count < needed)
            {
                var gotten = s.Read(GetBuffer(), count, needed);
                count += gotten;
                needed -= gotten;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pool.Count < Constants.MaxConnID)
                {
                    SetLength(0);
                    _pool.Add(this);
                    return;
                }
                base.Dispose();
            }
        }
    }
}
