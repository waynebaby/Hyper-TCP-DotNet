using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveTCPLibrary.Packet
{
    public abstract class PackerBase<T> : IPacker<T>
    {
        public abstract byte[] GetBytes(T obj);
        public abstract IList<ArraySegment<byte>> GetByteSegments(T obj, ByteSegmentLocators.IByteSegmentLocator nextSegmentLocator, int segmentMaxSize);

        public virtual ConcurrentExclusiveSchedulerPair ExecutionSchedulerPair
        {
            get;
            set;
        }
    }
}
