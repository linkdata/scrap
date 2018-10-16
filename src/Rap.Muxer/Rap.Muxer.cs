using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rap
{
    public class Muxer : IMuxer, IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly TcpClient _tcpClient;
        private readonly CancellationTokenRegistration _closeRegistration;
        private readonly NetworkStream _stream;
        private Conn[] _conns = new Conn[Constants.MaxConnID + 1];
        private BlockingCollection<IFrameData> _writes = new BlockingCollection<IFrameData>();
        private bool _disposed;

        public Muxer(CancellationToken cancelToken, TcpClient tcpClient)
        {
            if (tcpClient == null)
                throw new ArgumentNullException(nameof(tcpClient));
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            _tcpClient = tcpClient;
            _closeRegistration = _cts.Token.Register(tcpClient.Close);
            _stream = tcpClient.GetStream();
            for (ushort id = 0; id < _conns.Length; id++)
            {
                _conns[id] = new Conn(this, id);
            }
        }

        public async Task Run()
        {
            var cancelToken = _cts.Token;
            try
            {
                new Thread(WriterThread).Start();
                using (var sr = new StreamReader(_stream))
                {
                    var fd = FrameData.Take();
                    fd.Write("ok, lets see if this works.");
                    Write(fd);
                    var data = default(string);
                    while (!((data = await sr.ReadLineAsync().ConfigureAwait(false)).Equals("exit", StringComparison.OrdinalIgnoreCase)))
                    {
                        fd = FrameData.Take();
                        fd.Write(data);
                        Write(fd);
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                // ignore as they are expected
            }
            catch (System.IO.IOException)
            {
                // ignore I/O errors if we are cancelling
                if (!cancelToken.IsCancellationRequested)
                    throw;
            }
            finally
            {
                // ensure we cancel the write operations
                _cts.Cancel();
            }
        }

        public void Write(IFrameData fd)
        {
            _writes.Add(fd);
        }

        private void WriterThread()
        {
            var cancelToken = _cts.Token;
            try
            {
                while (true)
                {
                    IFrameData frameData;
                    if (_writes.TryTake(out frameData))
                    {
                        using (frameData)
                            frameData.CopyTo(_stream);
                    }
                    else
                    {
                        _stream.Flush();
                        cancelToken.ThrowIfCancellationRequested();
                        if (_writes.TryTake(out frameData, Timeout.Infinite, cancelToken))
                            using (frameData)
                                frameData.CopyTo(_stream);
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                // ignore as they are expected
            }
            catch (System.IO.IOException)
            {
                // ignore I/O errors if we are cancelling
                if (!cancelToken.IsCancellationRequested)
                    throw;
            }
            finally
            {
                // ensure we cancel the read operations
                _cts.Cancel();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_conns != null)
                {
                    for (int i = 0; i < _conns.Length; i++)
                    {
                        _conns[i]?.Dispose();
                        _conns[i] = null;
                    }
                    _conns = null;
                }
                if (_writes != null)
                {
                    IFrameData frameData;
                    while (_writes.TryTake(out frameData))
                        frameData?.Dispose();
                    _writes = null;
                }
                _stream?.Dispose();
                _closeRegistration.Dispose();
                _tcpClient?.Dispose();
                _cts.Dispose();
            }
        }
    }
}