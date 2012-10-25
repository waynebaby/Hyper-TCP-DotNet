using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveTCPLibrary.Packet;
using AsyncSocketEventOverlappedPool;
using ReactiveTCPLibrary.ByteSegmentLocators;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using ReactiveTCPLibrary;
namespace ReactiveTcpServer
{
    class Program
    {


        static ConcurrentDictionary<object, object> bag = new ConcurrentDictionary<object,object>(); 
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("params: [Port]");
                Console.WriteLine("Press any key...");
                Console.ReadLine();
                return;
            }
          
            var port = int.Parse(args[0]);
            SocketAsyncEventArgsPool.ConfigBeforeCreateInstance(300000);
            var pool = SocketAsyncEventArgsPool.GetInstance();
            using (var listener = new ReactiveTCPLibrary.ReactiveTCPListener(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port)))
            {
                TcpClientEventContainer.DefaultInstance.ClientException += new EventHandler<ClientExceptionEventArg>(DefaultInstance_ClientException);
                var stser = new SimpleStringSerializer();
                using (var sub = listener.Subscribe
                    (
                       socket =>
                       {

                           try
                           {


                               var client = new ReactiveTCPLibrary.ReactiveTcpClient<object, string>(socket,
                                   new NaturalCutter<string>(stser),
                                   new NaturalPacker<string>(stser),
                                  () => pool.GetResourceTokenFromPool(),
                                  new NewArrayByteSegmentLocator());
                               bag.AddOrUpdate(client, client, (a,b)=>client);
                               var x = client.Subscribe(p =>
                               {
                                   
                                   client.OnNext (new Tuple<string,PacketSentCallback<string>,AbortionChecker>(p,(PacketSentCallback<string>) null,null));
                               });


                               client.Start();
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
                    Console.WriteLine("Ready");
                    Console.Read();
                }


            }



        }

        static void DefaultInstance_ClientException(object sender, ClientExceptionEventArg e)
        {
            
            
            bag.TryRemove(sender ,out sender);

            try
            {
                Console.WriteLine(e.Exception.ToString());
                ((IDisposable)sender).Dispose();
            }
            catch (Exception)
            {
                
                
            }

        }
    }
}
