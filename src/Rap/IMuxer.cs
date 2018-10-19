using System;

namespace Rap
{
    public interface IMuxer : IDisposable
    {
        void Write(IFrameData fd);
    }
}