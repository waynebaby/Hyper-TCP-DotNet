using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics.Contracts;

namespace ReactiveTCPLibrary.ByteSegmentLocators
{
    public class ThreadLocalByteSegmentLocator : ByteSegmentLocatorBase
    {
        public ThreadLocalByteSegmentLocator(int memoryBlockSize)
        {
            _memoryBlockSize = memoryBlockSize;

            _currentBlock = new ThreadLocal<Tuple<byte[], int>>();

            RenewValue();
        }

        private void RenewValue()
        {
            //Contract.Requires(0 <= this._memoryBlockSize);
            //Contract.Requires(this._currentBlock != null);
            _currentBlock.Value = new Tuple<byte[], int>(new byte[_memoryBlockSize], 0);

        }
        int _memoryBlockSize;
        ThreadLocal<Tuple<byte[], int>> _currentBlock;


        protected override void Locate(int count, out byte[] array, out int offset)
        {

            array = _currentBlock.Value.Item1;
            offset = _currentBlock.Value.Item2;
            if (array.Length - offset < count)
            {
                array = _currentBlock.Value.Item1;
                offset = _currentBlock.Value.Item2;
            }
            _currentBlock.Value = Tuple.Create(array, offset + count);
        }




        public override void Dispose()
        {
            _currentBlock.Dispose();
        }
    }
}
