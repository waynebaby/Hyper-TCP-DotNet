using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ReactiveTCPLibrary.Packet
{
    public class NodoCutter : ICutter<ArraySegment<byte>>
    {

        public Subject<ArraySegment<byte>> sub = new Subject<ArraySegment<byte>>();
        public void OnCompleted()
        {
            sub.OnCompleted();
        }

        public void OnError(Exception error)
        {
            sub.OnError(error);
        }

        public void OnNext(ArraySegment<byte> value)
        {
            sub.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<ArraySegment<byte>> observer)
        {
            return sub.Synchronize().Subscribe(observer);
        }

        public void Dispose()
        {
            sub.Dispose();
        }
    }
}
