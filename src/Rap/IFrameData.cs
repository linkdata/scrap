using System;
using System.IO;

namespace Rap
{
    public interface IFrameData : IDisposable
    {
        void CopyTo(Stream s);
    }
}