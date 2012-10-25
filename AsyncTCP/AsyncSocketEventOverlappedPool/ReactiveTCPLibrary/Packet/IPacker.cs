using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactiveTCPLibrary.ByteSegmentLocators;

namespace ReactiveTCPLibrary.Packet
{
    public interface IPacker<in T>
    {
     
        Byte[] GetBytes(T obj);
        IList<ArraySegment<byte>> GetByteSegments(T obj, IByteSegmentLocator nextSegmentLocator,int segmentMaxSize);

    }
}
