using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rap
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public class Server
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public Server()
        {
        }

        public Task Listen()
        {
            return Listen(CancellationToken.None);
        }

        public async Task Listen(CancellationToken cancelToken)
        {
            IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            await Listen(cancelToken, new IPEndPoint(ipAddress, 10111));
        }

        public async Task Listen(CancellationToken cancelToken, IPEndPoint localEndPoint)
        {
            var listener = new TcpListener(localEndPoint);
            listener.Start();
            Console.WriteLine($"listening on {localEndPoint}");
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();
                var socket = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                socket.Close();
                // var cw = new ClientWorking(client, true);
                // Task.Run((Func<Task>)cw.DoSomethingWithClientAsync);
            }
        }

        /*
                public static void AcceptCallback(IAsyncResult ar)
                {
                    // Signal the main thread to continue.  
                    allDone.Set();

                    // Get the socket that handles the client request.  
                    Socket listener = (Socket)ar.AsyncState;
                    Socket handler = listener.EndAccept(ar);

                    // Create the state object.  
                    StateObject state = new StateObject();
                    state.workSocket = handler;
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }

                public static void ReadCallback(IAsyncResult ar)
                {
                    String content = String.Empty;

                    // Retrieve the state object and the handler socket  
                    // from the asynchronous state object.  
                    StateObject state = (StateObject)ar.AsyncState;
                    Socket handler = state.workSocket;

                    // Read data from the client socket.   
                    int bytesRead = handler.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        // There  might be more data, so store the data received so far.  
                        state.sb.Append(Encoding.ASCII.GetString(
                            state.buffer, 0, bytesRead));

                        // Check for end-of-file tag. If it is not there, read   
                        // more data.  
                        content = state.sb.ToString();
                        if (content.IndexOf("<EOF>") > -1)
                        {
                            // All the data has been read from the   
                            // client. Display it on the console.  
                            Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                                content.Length, content);
                            // Echo the data back to the client.  
                            Send(handler, content);
                        }
                        else
                        {
                            // Not all data received. Get more.  
                            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback), state);
                        }
                    }
                }

                private static void Send(Socket handler, String data)
                {
                    // Convert the string data to byte data using ASCII encoding.  
                    byte[] byteData = Encoding.ASCII.GetBytes(data);

                    // Begin sending the data to the remote device.  
                    handler.BeginSend(byteData, 0, byteData.Length, 0,
                        new AsyncCallback(SendCallback), handler);
                }

                private static void SendCallback(IAsyncResult ar)
                {
                    try
                    {
                        // Retrieve the socket from the state object.  
                        Socket handler = (Socket)ar.AsyncState;

                        // Complete sending the data to the remote device.  
                        int bytesSent = handler.EndSend(ar);
                        Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            */
    }

}
