using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ReactiveTCPLibrary.ByteSegmentLocators
{
    public interface IByteSegmentLocator:IDisposable
    {
       
        ArraySegment<Byte> CopyToNextSegment(SocketAsyncEventArgs sourceEventArgs);
        ArraySegment<Byte> GetNextSegment(int size);
    }
}
