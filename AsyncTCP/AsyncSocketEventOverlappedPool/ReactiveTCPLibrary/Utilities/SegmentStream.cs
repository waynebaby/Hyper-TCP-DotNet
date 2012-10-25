using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ReactiveTCPLibrary.ByteSegmentLocators;
using System.Diagnostics.Contracts;

namespace ReactiveTCPLibrary.Utilities
{
    internal enum StreamType
    {
        Read,
        Write
    }

    public delegate ArraySegment<byte> SegmentFactory(int sizeRequest);
    public class SegmentStream : Stream
    {


        StreamType StreamType
        {
            get;
            set;

        }

        class SegmentData
        {
            public ArraySegment<byte> Segment;
            public int StartIndexInStream;
        }

        List<SegmentData> _core;
        public ArraySegment<byte>[] GetSegments()
        {
            return _core.Select(x => x.Segment).ToArray();
        }

        public int SegmentCount { get { return _core.Count; } }

        /// <summary>
        /// 产生读取用的Stream
        /// </summary>
        /// <param name="source">读取来源</param>
        public SegmentStream(IEnumerable<ArraySegment<byte>> source)
        {
            //Contract.Requires(source != null);
            StreamType = StreamType.Read;
            _currentSegIndex = 0;
            _position = 0;
            int length = 0;
            _core =
                source.Select(
                    seg =>
                    {
                        var rval = new SegmentData { Segment = seg, StartIndexInStream = length };
                        length = length + seg.Count;
                        return rval;
                    }
                )
                .ToList();
            _length = length;
        }


        SegmentFactory _segmentCreator;
        public SegmentStream(SegmentFactory segmentCreator)
        {
            _currentSegIndex = 0;
            _position = 0;
            _core = new List<SegmentData>();
            StreamType = StreamType.Write;
            _segmentCreator = segmentCreator;
        }


        public override bool CanRead
        {
            get { return StreamType == Utilities.StreamType.Read; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return StreamType == Utilities.StreamType.Write; }
        }

        public override void Flush()
        {

        }

        long _length;
        public override long Length
        {
            get { return _length; }

        }
        int _currentSegIndex;
        long _position;
        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
  

            if (_currentSegIndex >= _core.Count) return 0;

            var segData = _core[_currentSegIndex];
            var currentOffset = (int)(_position - segData.StartIndexInStream);
            var copied = 0;
            for (int left = count; left > 0; )
            {
                var needMoveNext = (segData.Segment.Count < left);
                var currentCount = !needMoveNext ? left : segData.Segment.Count;
                var targetOffset = offset + copied;
                Buffer.BlockCopy(segData.Segment.Array, currentOffset, buffer, targetOffset, currentCount);
                copied = copied + currentCount;
                left = count - copied;
                if (needMoveNext)
                {

                    _currentSegIndex++;
                    if (_currentSegIndex < _core.Count)
                    {
                        segData = _core[_currentSegIndex];

                    }
                    else
                    {

                        break;
                    }

                }



            }
            return copied;

        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            
            if (_core.Count == 0)
            {
                _core.Add(new SegmentData { Segment = _segmentCreator(count), StartIndexInStream = 0 });
            }
            var segData = _core[_currentSegIndex];
            var currentOffset = (int)(_position - segData.StartIndexInStream);
            var copied = 0;
            for (int left = count; left > 0; )
            {
                var needMoveNext = (segData.Segment.Count < left);
                var currentCount = !needMoveNext ? left : segData.Segment.Count;
                var fromOffset = offset + copied;
                Buffer.BlockCopy(buffer, fromOffset, segData.Segment.Array, currentOffset, currentCount);
                copied = copied + currentCount;
                _length = _length + currentCount;
                left = count - copied;
                if (needMoveNext)
                {
                    _currentSegIndex++;
                    if (_currentSegIndex < _core.Count)
                    {


                        segData = _core[_currentSegIndex];


                    }
                    else
                    {

                        var newseg = new SegmentData
                        {
                            Segment = _segmentCreator(left),
                            StartIndexInStream = segData.StartIndexInStream + segData.Segment.Count
                        };

                        segData = newseg;
                        _core.Add(newseg);
                    }

                }



            }
            //return copied;
        }
    }
}
