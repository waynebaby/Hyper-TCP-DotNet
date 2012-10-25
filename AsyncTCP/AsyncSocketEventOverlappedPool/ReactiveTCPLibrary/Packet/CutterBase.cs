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

namespace ReactiveTCPLibrary.Packet
{
    public abstract class CutterBase<T> : ICutter<T>
    {
        public CutterBase(ISerializer<T> serializer, bool asyncSerializationAndOnNext)
        {
            //Contract.Requires<ArgumentNullException>(serializer != null);
            _serializer = serializer;
            _packetsReceivedSubject = new Subject<T>();            
            _dataSegmentsReceived = new Queue<ArraySegment<byte>>();
            _asyncSerializationAndOnNext = asyncSerializationAndOnNext;
        }

        protected ISerializer<T> _serializer;
        protected int _offsetDelta;
        protected int _totalLength;
        protected bool _asyncSerializationAndOnNext;
        Queue<ArraySegment<byte>> _dataSegmentsReceived;



        #region Abstracts
        protected abstract SegmentStream CutSegmentSequenceIntoStream(IEnumerable<ArraySegment<byte>> source);
        protected abstract void OperationBeforeEnqueue(ArraySegment<byte> dataSegment);
        #endregion





        private void ReceiveDataSegment(ArraySegment<byte> dataSegment)
        {
            try
            {
                OperationBeforeEnqueue(dataSegment);
                _dataSegmentsReceived.Enqueue(dataSegment);
                _totalLength = _totalLength + dataSegment.Count;
                InternalGetPacket(_asyncSerializationAndOnNext);
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


        private void InternalGetPacket(bool asyncSerializationAndOnNext)
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
                    .Union(_dataSegmentsReceived.Skip(1));
            }
            else
            {
                param = _dataSegmentsReceived;
            }

            var stream = CutSegmentSequenceIntoStream(param);
            cutLength = (int)stream.Length;
            segmentCount = stream.SegmentCount;

            WaitCallback action = _ => _packetsReceivedSubject.OnNext(_serializer.Read(stream));

            if (asyncSerializationAndOnNext)
            {
                ThreadPool.QueueUserWorkItem(action, null);
            }
            else
            {
                action(null);
            }

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
            ReceiveDataSegment(value);
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
