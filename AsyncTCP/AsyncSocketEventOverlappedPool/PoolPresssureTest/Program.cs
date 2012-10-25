using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using AsyncSocketEventOverlappedPool;
using System.Threading;

namespace PoolPresssureTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var rnd=new Random();
            Stopwatch s = new Stopwatch();
            s.Start();
            AsyncSocketEventOverlappedPool.SocketAsyncEventArgsPool.ConfigBeforeCreateInstance
                (
                    poolSize: 100000
                    //bufferSize: 1024
                    //reservedOverlapCount:120000
                    

                );
        
            var p = AsyncSocketEventOverlappedPool.SocketAsyncEventArgsPool.GetInstance();
            s.Stop();

            Console.WriteLine("CreateTime:{0}", s.ElapsedMilliseconds);

            s.Reset();
            s.Start();

            Enumerable.Range(0, 10000000)
                  .AsParallel()
                  .ForAll(x => p.GetResourceTokenFromPool().Dispose());

            s.Stop();
            Console.WriteLine("getReturn:{0}", s.ElapsedMilliseconds);
            Console.Read();
            

        }
    }
}
