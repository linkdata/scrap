using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rap
{
    public class Muxer : IMuxer
    {
        private readonly CancellationTokenSource _cts;
        private readonly TaskCompletionSource<object> _writerTcs = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _readerTcs = new TaskCompletionSource<object>();
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
                    var data = default(string);
                    while (!((data = await sr.ReadLineAsync().ConfigureAwait(false)).Equals("exit", StringComparison.OrdinalIgnoreCase)))
                    {
                        var fd = FrameData.Take();
                        using (var sw = new System.IO.StreamWriter(fd, System.Text.Encoding.UTF8, 512, true))
                        {
                            sw.WriteLine(data);
                        }
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

        private void ReaderThread()
        {
            var cancelToken = _cts.Token;
            try
            {
                while (true)
                {
                    IFrameData fd;
                    if (_writes.TryTake(out fd))
                    {
                        using (fd)
                            fd.WriteTo(_stream);
                    }
                    else
                    {
                        _stream.Flush();
                        cancelToken.ThrowIfCancellationRequested();
                        if (_writes.TryTake(out fd, Timeout.Infinite, cancelToken))
                            using (fd)
                                fd.WriteTo(_stream);
                    }
                }
            }
            catch (System.Exception e)
            {
                if (cancelToken.IsCancellationRequested)
                    _writerTcs.SetCanceled();
                else
                    _writerTcs.SetException(e);
            }
            finally
            {
                // ensure we cancel the read operations
                _cts.Cancel();
                _writerTcs.TrySetResult(null);
            }
        }

        private void WriterThread()
        {
            var cancelToken = _cts.Token;
            try
            {
                while (true)
                {
                    IFrameData fd;
                    if (_writes.TryTake(out fd))
                    {
                        using (fd)
                            fd.WriteTo(_stream);
                    }
                    else
                    {
                        _stream.Flush();
                        cancelToken.ThrowIfCancellationRequested();
                        if (_writes.TryTake(out fd, Timeout.Infinite, cancelToken))
                            using (fd)
                                fd.WriteTo(_stream);
                    }
                }
            }
            catch (System.Exception e)
            {
                if (cancelToken.IsCancellationRequested)
                    _writerTcs.SetCanceled();
                else
                    _writerTcs.SetException(e);
            }
            finally
            {
                // ensure we cancel the read operations
                _cts.Cancel();
                _writerTcs.TrySetResult(null);
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