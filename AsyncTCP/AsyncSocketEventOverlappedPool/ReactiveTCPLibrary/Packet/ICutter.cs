using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactiveTCPLibrary.ByteSegmentLocators;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace ReactiveTCPLibrary.Packet
{
    public interface ICutter<out T> : IObserver<ArraySegment<byte>>, IObservable<T> ,IDisposable
    {
      ////  void ReceiveDataSegment(ArraySegment<byte> dataSegment);
      //  IObservable<T> PacketsReceived{get;}
        ConcurrentExclusiveSchedulerPair ExecutionSchedulerPair { get; set; }
    }



   
}
