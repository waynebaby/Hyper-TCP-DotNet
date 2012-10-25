using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactiveTCPLibrary.Utilities;

namespace ReactiveTCPLibrary.Packet
{
    public class NaturalCutter<T> :CutterBase<T>, ICutter<T>
    {
        public NaturalCutter(ISerializer<T> serializer,bool asyncSerializationAndOnNext=true)
            : base(serializer, asyncSerializationAndOnNext)
        { 
        
        }


        protected override SegmentStream CutSegmentSequenceIntoStream(IEnumerable<ArraySegment<byte>> source)
        {
            var segs = source.Take(1);
            return new SegmentStream(segs);
        }

        protected override void OperationBeforeEnqueue(ArraySegment<byte> dataSegment)
        {
            
        }
    }
}
