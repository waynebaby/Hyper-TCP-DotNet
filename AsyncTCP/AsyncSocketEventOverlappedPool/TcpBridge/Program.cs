using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsyncSocketEventOverlappedPool;
using ReactiveTCPLibrary;
using ReactiveTCPLibrary.Packet;

namespace TcpBridge
{
    class Program
    {


        static void Main(string[] args)
        {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            SocketAsyncEventArgsPool.ConfigBeforeCreateInstance(5000,4096,4096);
            SocketAsyncEventArgsPool.GetInstance();// <--warm up;
            using (var listener = new ReactiveTCPLibrary.ReactiveTCPListener(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 54321)))
            {
                TcpClientEventContainer.DefaultInstance.ClientException += new EventHandler<ClientExceptionEventArg>(DefaultInstance_ClientException);

                using (var sub = listener.Subscribe
                    (
                       socket =>
                       {

                           try
                           {
                               var cp = new ConnectionPair(socket, "127.0.0.1", 80);
                               cp.Pin();
                               ////var client = new ReactiveTCPLibrary.ReactiveTcpClient<object, string>(socket,
                               ////    new NaturalCutter<string>(stser),
                               ////    new NaturalPacker<string>(stser),
                               ////   () => SocketAsyncEventArgsPool.GetInstance().GetResourceTokenFromPool(),
                               ////   new NewArrayByteSegmentLocator());
                               //bag.AddOrUpdate(client, client, (a, b) => client);
                               //var x = client.Subscribe(p =>
                               //{

                               //    client.OnNext(new Tuple<string, PacketSentCallback<string>>(p, (PacketSentCallback<string>)null));
                               //});
                           }
                           catch (Exception)
                           {

                               return;
                           }
                           //      GCHandle.Alloc(client, GCHandleType.Pinned);



                           //     GCHandle.Alloc(x, GCHandleType.Pinned);


                       }

                    )
                    )
                {

                    Console.Read();
                }


            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            
            Console.WriteLine("Ignore");
        }

        static void DefaultInstance_ClientException(object sender, ClientExceptionEventArg e)
        {

            Console.WriteLine("ConnectionClosed");
            if (sender != null)
            {
                if (sender is ReactiveTcpClient<ConnectionPair, ArraySegment<byte>>)
                {
                    var cl = sender as ReactiveTcpClient<ConnectionPair, ArraySegment<byte>>;

                    if (cl != null)
                    {

                        var cp = cl.ConnectionContext;
                        if (cp != null)
                        {
                            try
                            {
                                cp.Dispose();
                            }
                            catch (Exception)
                            {


                            }
                        }
                    }
                }
            }
        }




    }
}
