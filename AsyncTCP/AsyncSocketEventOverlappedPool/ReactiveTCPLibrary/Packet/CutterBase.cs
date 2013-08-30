using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.IO;
using ReactiveTCPLibrary.Utilities;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveTCPLibrary.Packet
{
    public abstract class CutterBase<T> : ICutter<T>
    {
        public CutterBase(ISerializer<T> serializer)
        {
        
            _serializer = serializer;
            _packetsReceivedSubject = new Subject<T>();
            _dataSegmentsReceived = new Queue<ArraySegment<byte>>();

        }


        public virtual ConcurrentExclusiveSchedulerPair ExecutionSchedulerPair
        {
            get;
            set;
        }


        protected ISerializer<T> _serializer;
        protected int _offsetDelta;
        protected int _totalLength;

        Queue<ArraySegment<byte>> _dataSegmentsReceived;



        #region Abstracts
        protected abstract SegmentStream CutSegmentSequenceIntoStream(IEnumerable<ArraySegment<byte>> source);
        protected abstract void OnProcessingReceivedDataSegment(ArraySegment<byte> dataSegment);
        #endregion





        private void ProcessReceivedDataSegment(ArraySegment<byte> dataSegment)
        {
            try
            {
                OnProcessingReceivedDataSegment(dataSegment);
                _dataSegmentsReceived.Enqueue(dataSegment);
                _totalLength = _totalLength + dataSegment.Count;
                InternalGetPacket();
            }
            catch (Exception ex)
            {

                _packetsReceivedSubject.OnError(ex);
            }

        }




        Subject<T> _packetsReceivedSubject;


        #region Private Methods


        private void InternalDequeueUsedSegments(int segCount, int cutLength)
        {
            cutLength = cutLength + _offsetDelta;

            ArraySegment<byte> segTmp;
            for (int i = 0; i < segCount - 1; i++)
            {


                segTmp = _dataSegmentsReceived.Dequeue();
                cutLength = cutLength - segTmp.Count;
            }

            if (cutLength == 0)
            {
                segTmp = _dataSegmentsReceived.Dequeue();
            }
            else
            {
                segTmp = _dataSegmentsReceived.Peek();
                if (segTmp.Count == cutLength)
                {
                    segTmp = _dataSegmentsReceived.Dequeue();
                    cutLength = cutLength - segTmp.Count;
                }
            }
            _offsetDelta = cutLength;

        }


        private void InternalGetPacket()
        {
            var segmentCount = 0;
            var cutLength = 0;
            IEnumerable<ArraySegment<byte>> param;
            if (_offsetDelta > 0)
            {
                ArraySegment<byte> first;

                if (_dataSegmentsReceived.Count == 0) return;
                first = _dataSegmentsReceived.Peek();
                first = new ArraySegment<byte>(first.Array, _offsetDelta, first.Count - _offsetDelta);
                param = Enumerable.Range(0, 1).Select(x => first)
                    .Concat(_dataSegmentsReceived.Skip(1));
            }
            else
            {
                param = _dataSegmentsReceived;
            }

            var stream = CutSegmentSequenceIntoStream(param);
            cutLength = (int)stream.Length;
            segmentCount = stream.SegmentCount;

            _packetsReceivedSubject.OnNext(_serializer.Read(stream));
            _totalLength = _totalLength - cutLength;
            InternalDequeueUsedSegments(segmentCount, cutLength);

        }

        #endregion

        public void OnCompleted()
        {
            _packetsReceivedSubject.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _packetsReceivedSubject.OnError(error);
        }

        public void OnNext(ArraySegment<byte> value)
        {
            ProcessReceivedDataSegment(value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _packetsReceivedSubject.Subscribe(observer);
        }

        public void Dispose()
        {
            _packetsReceivedSubject.Dispose();
        }
    }
}
