using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.IO;
using ReactiveTCPLibrary.Utilities;

namespace ReactiveTCPLibrary.Packet
{
    public class NaturalPacker<T> : IPacker<T>
    {

        public NaturalPacker(ISerializer<T> serializer)
        {
            //Contract.Requires<ArgumentNullException>(serializer != null, "Param serializer should not be null");
            _serializer = serializer;
        }

        ISerializer<T> _serializer;

        public byte[] GetBytes(T obj)
        {
            var ms = new MemoryStream();
            _serializer.WriteObject(obj, ms);
            return ms.ToArray();
        }

        public IList<ArraySegment<byte>> GetByteSegments(T obj, ByteSegmentLocators.IByteSegmentLocator nextSegmentLocator, int segmentMaxSize)
        {
            var strm = new SegmentStream(size => nextSegmentLocator.GetNextSegment(size > segmentMaxSize ? segmentMaxSize : size));
            _serializer.WriteObject(obj, strm);
            return strm.GetSegments();
            
        }
    }
}
