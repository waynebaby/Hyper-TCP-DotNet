using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;

namespace AsyncSocketEventOverlappedPool
{
    public class SocketAsyncEventArgsPool : IDisposable
    {


        const int DEFAULT_POOL_SIZE = 120000;
        const int DEFAULT_BUFFER_SIZE = 4096;
        //const int DEFAULT_BUFFERBLOCK_STEP_SIZE = 1024 * 1024;
        const int DEFAULT_RESERVED_OVERLAPS_COUNT = 100000;
        //const int DEFAULT_MAX_BUFFERS_IN_BUFFERBLOCK_COUNT = 1024 * 128;

        ConcurrentBag<SocketAsyncEventArgs> _core;
        bool _disposed = false;
        static volatile object _criticalLock = new object();

        static SocketAsyncEventArgsPool _instance;
        static int _poolSize = DEFAULT_POOL_SIZE;
        static int _bufferSize = DEFAULT_BUFFER_SIZE;
        //static int _bufferBlockStepSize = DEFAULT_BUFFERBLOCK_STEP_SIZE;
        static int _reservedOverlapCount = DEFAULT_RESERVED_OVERLAPS_COUNT;
        //static int _maxBuffersInBufferBlockCount = DEFAULT_MAX_BUFFERS_IN_BUFFERBLOCK_COUNT;
        public static void ConfigBeforeCreateInstance(
            int poolSize = DEFAULT_POOL_SIZE,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            //int bufferBlockStepSize = DEFAULT_BUFFERBLOCK_STEP_SIZE,
            int reservedOverlapCount = DEFAULT_RESERVED_OVERLAPS_COUNT//,
            //int maxBuffersInBufferBlockCount = DEFAULT_MAX_BUFFERS_IN_BUFFERBLOCK_COUNT
            )
        {
            //Contract.Requires(_lazyInstance.IsValueCreated == false);
            _poolSize = poolSize;
            _bufferSize = bufferSize;
            //_bufferBlockStepSize = bufferBlockStepSize;
            _reservedOverlapCount = reservedOverlapCount;
            //_maxBuffersInBufferBlockCount = maxBuffersInBufferBlockCount;
        }



        static SocketAsyncEventArgsPool()
        {

            //ReconfigLazyInstance();

        }

        //private static void ReconfigLazyInstance()
        //{

        //    _instance = new Lazy<SocketAsyncEventArgsPool>(
        //        PoolFactory,
        //        false);
        //}


        public static SocketAsyncEventArgsPool GetInstance()
        {
            //lock (_criticalLock)
            //{
            //    return _instance.Value;
            //}
            if (_instance == null)
            {
                lock (_criticalLock)
                {
                    if (_instance == null)
                    {
                        _instance = PoolFactory();
                    }
                }
            }
            return _instance;

        }

        private static SocketAsyncEventArgsPool PoolFactory()
        {
            //Create buffer blocks.
            //long acturalBufferSize = (long)_poolSize * (long)_bufferSize;
            //var steps = acturalBufferSize / _bufferBlockStepSize;
            //var mod = acturalBufferSize % _bufferBlockStepSize;
            //if (mod > 0)
            //{
            //    steps++;
            //}
            //int maxBufferBlockSize = _maxBuffersInBufferBlockCount * _bufferSize;
            //long totalSize = (long)(steps * _bufferBlockStepSize);
            //var fullBlockCount = (int)(totalSize / maxBufferBlockSize);
            //var lastBlockSize = totalSize % maxBufferBlockSize;
            //var buffers =
            //    Enumerable.Range(0, fullBlockCount)
            //        .Select(
            //            i => new byte[maxBufferBlockSize]
            //            )
            //        .ToList();
            //if (lastBlockSize != 0) buffers.Add(new byte[(int)lastBlockSize]);

            //Create reserved items in overlap pool. 
            //These overlap objects in SAES will be return to overlap pool for other io after SAE pool is built.


            var reservedList = Enumerable.Range(0, _reservedOverlapCount)
                .Select(
                    i =>
                    {
                        unsafe
                        {
                            NativeOverlapped* ptr = new Overlapped().Pack((x, y, z) => { }, null);
                            return new Action(() =>
                            {
                                Overlapped.Free(ptr);
                            });
                        }

                    }
                )
                .ToList();

            //Create working SAEs
            var query = Enumerable.Range(0, _poolSize)
                .Select
                (
                    i =>
                    {
                        var e = new SocketAsyncEventArgs();
                        //long totalOffset = i * (long)_bufferSize;
                        //var blockIndex = (int)(totalOffset / maxBufferBlockSize);
                        //var offset = (int)(totalOffset % maxBufferBlockSize);
                        e.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
                        return e;
                    }
                );
            var itmsList = query.ToList();

            //Create SAE pool instance
            var rval = new SocketAsyncEventArgsPool(new ConcurrentBag<SocketAsyncEventArgs>(itmsList));


            //Release the reserved overlap object.
            //Best practice of overlap pool: the last released object will be visited first.

            reservedList.Reverse();
            reservedList.ForEach(
                act =>
                {
                    try
                    {
                        act();
                    }
                    catch (Exception)
                    {
                    }

                }
                );
            reservedList.Clear();

            //make sure all items in pool are in MaxGen of GC
            for (int i = 0; i < GC.MaxGeneration; i++)
            {

                GC.Collect();
                GC.WaitForFullGCComplete();

            }
            //Pin all buffer & SocketEventArgs to GC.  
            //GC will ignore them.


            return rval;
        }

        private SocketAsyncEventArgsPool(ConcurrentBag<SocketAsyncEventArgs> core)
        {
            _core = core;
        }


        public Disposable<SocketAsyncEventArgs> GetResourceTokenFromPool()
        {



            SocketAsyncEventArgs rval;
            if (_core.TryTake(out rval))
            {
                rval.Reset();
                return new Disposable<SocketAsyncEventArgs>
                (
                     r =>
                     {
                         r.Reset();
                         _core.Add(r);
                     },
                     rval
                );
            }
            else
            {
                throw new Exception("Not enough pool size. Pool is empty.");
            }

        }

        public void Dispose()
        {
            lock (_criticalLock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                _core = new ConcurrentBag<SocketAsyncEventArgs>();


                //ReconfigLazyInstance();
            }
        }
    }
}
