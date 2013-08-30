using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactiveTCPLibrary.Utilities;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace ReactiveTCPLibrary
{
    //public class AsyncTcpClient<TConnectionContext, TPacket> : IAsyncTcpClient<TConnectionContext, TPacket>
    //{
    //    /// <summary>
    //    /// 对于立即返回的Task制作的缓存项
    //    /// </summary>
    //    static readonly ConfiguredTaskAwaitable CACHED_FINISHED_TASK_AWAITABLE = Task.Factory.StartNew(() => { }).ConfigureAwait(false);
    //    public AsyncTcpClient(IReactiveTcpClient<TConnectionContext, TPacket> core)
    //    {
    //        _core = core;
    //        _reader = new BufferBlock<TPacket>();
    //        subcription = _core.Subscribe(_reader.AsObserver ());
    //    }

    //    IReactiveTcpClient<TConnectionContext, TPacket> _core;
    //    BufferBlock<TPacket> _reader;
    //    IDisposable subcription;

    //    public ConfiguredTaskAwaitable<TPacket> GetPacketAsync(CancellationToken cancellationToken)
    //    {
    //        return _reader.ReceiveAsync(cancellationToken).ConfigureAwait(false);
    //    }



    //    public ConfiguredTaskAwaitable SendPackAsync(TPacket[] packets, bool waitUntilComplete, CancellationToken cancellationToken)
    //    {

    //        if (waitUntilComplete)
    //        {
    //            var rval = new Task(() => { });
    //            _core.OnNext(new Tuple<TPacket[], PacketSentCallback<TPacket[]>,AbortionChecker>(packets, _ => rval.Start(),()=>cancellationToken.IsCancellationRequested ));
    //            return rval.ConfigureAwait(false);
    //        }
    //        else
    //            return CACHED_FINISHED_TASK_AWAITABLE;
    //    }

    //    public ConfiguredTaskAwaitable SendPackAsync(TPacket packet, bool waitUntilComplete, CancellationToken cancellationToken)
    //    {

    //        if (waitUntilComplete)
    //        {
    //            var rval = new Task(() => { });
    //            _core.OnNext(new Tuple<TPacket, PacketSentCallback<TPacket>, AbortionChecker>(packet, _ => rval.Start(), () => cancellationToken.IsCancellationRequested));
    //            return rval.ConfigureAwait(false);
    //        }
    //        else
    //            return CACHED_FINISHED_TASK_AWAITABLE;
    //    }

    //    public bool Connected
    //    {
    //        get { return _core.Connected; }
    //    }

    //    public TConnectionContext ConnectionContext
    //    {
    //        get
    //        {
    //            return _core.ConnectionContext;
    //        }
    //        set
    //        {
    //            _core.ConnectionContext = value;
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        if (subcription != null)
    //        {

    //            IDisposable sub;
    //            sub = Interlocked.Exchange(ref subcription, null);
    //            if (sub != null)
    //            {
    //                try
    //                {
    //                    sub.Dispose();
    //                }
    //                catch (Exception)
    //                {
    //                }
    //            }
    //            sub = Interlocked.Exchange(ref _core, null);
    //            if (sub != null)
    //            {
    //                try
    //                {
    //                    sub.Dispose();
    //                }
    //                catch (Exception)
    //                {
    //                }
    //            }



    //        }
    //    }


    //    public ITcpClientEventContainer EventContainer
    //    {
    //        get { return _core.EventContainer; }
    //    }


    //    public bool Start()
    //    {
    //        return _core.Start();
    //    }


    //    public bool Started
    //    {
    //        get { return _core.Started; }
    //    }
    //}
}
