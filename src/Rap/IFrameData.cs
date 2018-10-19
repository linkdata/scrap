using System;
using System.IO;

namespace Rap
{
    public interface IFrameData : IDisposable
    {
        void WriteTo(Stream s); // Implemented by MemoryStream
        void ReadFrom(Stream s);
    }
}