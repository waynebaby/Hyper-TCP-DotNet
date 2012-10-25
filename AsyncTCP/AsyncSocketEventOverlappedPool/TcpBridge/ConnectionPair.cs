using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using ReactiveTCPLibrary.Packet;
using AsyncSocketEventOverlappedPool;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using ReactiveTCPLibrary;
namespace TcpBridge
{
    public class ConnectionPair : IDisposable
    {

        public event EventHandler ConnectionAborted = (o, e) => { };
        static EventArgs _empty = new EventArgs();
        GCHandle? thisHandle;

        public void Pin()
        {
            lock (this)
            {
                if (thisHandle == null)
                {
                    thisHandle = GCHandle.Alloc(this, GCHandleType.Pinned);
                }


            }
        }

        public ConnectionPair(Socket incomingSocket, string outGoingHost, int outGoingPort)
        {
            var outGoingSocket = new TcpClient(outGoingHost, outGoingPort).Client;


            IncomingClient = new ReactiveTCPLibrary.ReactiveTcpClient<ConnectionPair, ArraySegment<byte>>
            (
                incomingSocket,
                new NodoCutter(),
                new NaturalPacker<ArraySegment<byte>>(new NodoSerializer()),
                     () => SocketAsyncEventArgsPool.GetInstance().GetResourceTokenFromPool(),
                     null
            )
            {
                ConnectionContext = this
            }
            ;

            OutgoingClient = new ReactiveTCPLibrary.ReactiveTcpClient<ConnectionPair, ArraySegment<byte>>
            (
                outGoingSocket,
                new NodoCutter(),
                new NaturalPacker<ArraySegment<byte>>(new NodoSerializer()),
                     () => SocketAsyncEventArgsPool.GetInstance().GetResourceTokenFromPool(),
                     null
            )
            {
                ConnectionContext = this
            }
            ;


            var dis1 = IncomingClient
      
                .Subscribe(
                   x =>
                   {
                       OutgoingClient.OnNext(new Tuple<ArraySegment<byte>, ReactiveTCPLibrary.PacketSentCallback<ArraySegment<byte>>, AbortionChecker>(x, null,null));
                   },
                   e =>
                   {
                       OutgoingClient.OnError(e);
                       ConnectionAborted(this, _empty);

                   },
                   () =>
                   {
                       OutgoingClient.OnCompleted();
                       ConnectionAborted(this, _empty);

                   }
                   );
            var dis2 = OutgoingClient
      
                .Subscribe(
                    x =>
                    {
                        IncomingClient.OnNext(new Tuple<ArraySegment<byte>, ReactiveTCPLibrary.PacketSentCallback<ArraySegment<byte>>,AbortionChecker>(x, null,null));
                    },
                    e =>
                    {
                        IncomingClient.OnError(e);
                        ConnectionAborted(this, _empty);

                    },
                    () =>
                    {
                        IncomingClient.OnCompleted();
                        ConnectionAborted(this, _empty);

                    }
                    );
            _disposeList = new List<IDisposable> 
            {
                OutgoingClient,
                IncomingClient,
                dis1,
                dis2
            };

            IncomingClient.Start();
            OutgoingClient.Start();

        }

        List<IDisposable> _disposeList;

        public ReactiveTCPLibrary.ReactiveTcpClient<ConnectionPair, ArraySegment<byte>> IncomingClient
        { get; private set; }
        public ReactiveTCPLibrary.ReactiveTcpClient<ConnectionPair, ArraySegment<byte>> OutgoingClient
        { get; private set; }


        public void Dispose()
        {
            lock (this)
            {
                if (thisHandle.HasValue)
                {
                    thisHandle.Value.Free();
                    thisHandle = null;

                }

                _disposeList.ForEach(
                    d =>
                    {
                        try
                        {
                            d.Dispose();
                        }
                        catch (Exception)
                        {


                        }
                    }
                    );
                _disposeList.Clear();



            }

        }
    }
}
