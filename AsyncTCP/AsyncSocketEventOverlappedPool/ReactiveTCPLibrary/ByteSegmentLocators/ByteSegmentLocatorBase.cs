using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ReactiveTCPLibrary.ByteSegmentLocators
{
    public abstract class ByteSegmentLocatorBase:IByteSegmentLocator
    {


        protected abstract void Locate(int count, out byte[] array, out int offset);

        public ArraySegment<byte> CopyToNextSegment(SocketAsyncEventArgs sourceEventArgs)
        {
            Byte[] array;
            int offset;
            int count = sourceEventArgs.BytesTransferred;
            Locate(count, out array, out offset);
            var data = new ArraySegment<byte>(array, offset, count);


            if (sourceEventArgs.Buffer != null)
            {
                Buffer.BlockCopy(sourceEventArgs.Buffer, sourceEventArgs.Offset, array, offset, count);
            }
            else if (sourceEventArgs.BufferList != null)
            {
                int currentOffset = offset;
                int copiedCount = 0;
                foreach (var item in sourceEventArgs.BufferList)
                {
                    int willcopyCount = count - copiedCount;
                    int copyingCount = willcopyCount > item.Count ? item.Count : willcopyCount;
                    Buffer.BlockCopy(item.Array, item.Offset, array, currentOffset, copyingCount);
                    copiedCount = copiedCount + copiedCount;
                    currentOffset = copiedCount + offset;
                }
            }
            return data;
        }


        public abstract void Dispose();



        public ArraySegment<byte> GetNextSegment(int size)
        {
            Byte[] array;
            int offset;
            int count = size;
            Locate(count, out array, out offset);
            return new ArraySegment<byte>(array, offset, count);
        }
    }
}
