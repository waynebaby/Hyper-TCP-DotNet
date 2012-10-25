using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ReactiveTCPLibrary
{
    public class ReactiveTCPListener : IObservable<Socket>, IDisposable
    {
        public ReactiveTCPListener(IPEndPoint localEndPoint)
        {
            _subject = new Subject<Socket>();
            _socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(localEndPoint);
            _socket.Listen(120000);
            var eventArg = new SocketAsyncEventArgs();
            subcribtion = Observable
                .FromEventPattern<SocketAsyncEventArgs>(
                    eh => eventArg.Completed += eh,
                    eh => eventArg.Completed -= eh
                )
                .Subscribe(
                    e =>
                    {
                        AcceptLoop(e.EventArgs);
                    }
                );

            var socket = _socket;
            if (socket != null)
            {
                AcceptLoop(eventArg);
            }

        }

        private void AcceptLoop(SocketAsyncEventArgs eventArgs)
        {
            var isAsync = false;
            while (!isAsync)
            {
                _subject.OnNext(eventArgs.AcceptSocket);
                eventArgs.AcceptSocket = null;
                isAsync = _socket.AcceptAsync(eventArgs);

            }
        }

        IDisposable subcribtion;
        Socket _socket;
        Subject<Socket> _subject;



        public IDisposable Subscribe(IObserver<Socket> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void Dispose()
        {
            Socket socket = Interlocked.Exchange(ref _socket, null);
            if (socket != null)
            {
                socket.Dispose();
            }

            Subject<Socket> subject = Interlocked.Exchange(ref _subject, null);
            if (subject != null)
            {
                subject.OnCompleted();
                subject.Dispose();
            }


        }
    }
}
