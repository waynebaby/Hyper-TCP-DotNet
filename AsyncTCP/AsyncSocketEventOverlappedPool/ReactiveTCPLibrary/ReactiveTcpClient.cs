using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using AsyncSocketEventOverlappedPool;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;
using ReactiveTCPLibrary.ByteSegmentLocators;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Collections.Concurrent;
using ReactiveTCPLibrary.Utilities;
using ReactiveTCPLibrary.Packet;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace ReactiveTCPLibrary
{

    /// <summary>
    /// 响应式TCP Client
    /// </summary>
    /// <typeparam name="TConnectionContext">一个表示Connection状态的内容类型，一般与业务逻辑上的Connection标识相关</typeparam>
    /// <typeparam name="TPacket">表示发送和接收的数据包基类类型。可以为Object或者一个公用接口</typeparam>
    public class ReactiveTcpClient<TConnectionContext, TPacket> : IDisposable, ReactiveTCPLibrary.IReactiveTcpClient<TConnectionContext, TPacket>
    {



        /// <summary>
        /// 启动标记，0表示尚未启动，1表示已经启动
        /// </summary>
        int _startState = 0;

        /// <summary>
        /// 是否启动
        /// </summary>
        public bool Started { get { return _startState == 1; } }


        /// <summary>
        /// 当前链接的状态内容，一般与业务逻辑上的Connection标识相关，比如用户信息实体
        /// </summary>
        public TConnectionContext ConnectionContext { get; set; }

        /// <summary>
        /// 将要在该client被释放的时候运行的操作列表
        /// </summary>
        Stack<Action> _disposeActionList = new Stack<Action>();
        /// <summary>
        /// 发送用的异步事件参数
        /// </summary>
        SocketAsyncEventArgs _sendEventArgs;
        /// <summary>
        /// 接收用的异步事件参数
        /// </summary>
        SocketAsyncEventArgs _receiveEventArgs;


        /// <summary>
        /// 数据切割器 将数据转化为数据包实体
        /// </summary>
        ICutter<TPacket> _dataCutter;

        /// <summary>
        /// 打包器 将数据包实体转化为实际发送的数据
        /// </summary>
        IPacker<TPacket> _dataPacker;

        /// <summary>
        /// 核心socket对象
        /// </summary>
        Socket _socket;

        /// <summary>
        /// 该对象是否已经运行过销毁程序
        /// </summary>
        bool _disposed;


        /// <summary>
        /// 收到且解析完毕的包序列
        /// </summary>
        Subject<TPacket> _packetReceivedSubject;

        /// <summary>
        /// 准备发送的包的队列
        /// </summary>
        ConcurrentQueue<
                Tuple<
                    Queue<ArraySegment<byte>>,
                    AsyncCallback,
                    AbortionChecker
                >
            > _sendingQueue;

        /// <summary>
        /// 创建数据片段的策略工具
        /// </summary>
        IByteSegmentLocator _byteSegmentLocator;

        /// <summary>
        /// 已经发送的包序列
        /// </summary>
        Subject<TPacket> _packetSentSubject;

        /// <summary>
        /// 开始发送的信号序列
        /// </summary>
        Subject<SocketAsyncEventArgs> _sendLaucherSubject;


        /// <summary>
        /// 是否正在发送中 是为1，否为0
        /// </summary>
        int _isSending;

        /// <summary>
        /// 构造函数根，所有其他构造函数都从此进入
        /// </summary>
        /// <param name="socket">核心socket,必须是已经打开的TCP socket</param>
        /// <param name="eventArgsfactory">
        /// <para>获得异步事件对象和回收该对象的方法。</para>
        /// <para>输入空值会创建默认新建和销毁逻辑.</para></param>
        /// <param name="byteSegmentLocator">
        /// <para>收到数据后创建和定位目标数据片段的定位器。</para>
        /// <para>输入空值会创建默认创建新数组的逻辑</para>
        /// </param>
        /// <param name="byteSegmentLocator">
        /// <para>事件容器，负责处理统一的事件相应。</para>
        /// <para>输入空值会使用静态的默认容器，可以为一组client指定同一个容器进行分流</para>
        /// </param>
        public ReactiveTcpClient(
            Socket socket,
            ICutter<TPacket> dataCutter,
            IPacker<TPacket> dataPacker,
            Func<Disposable<SocketAsyncEventArgs>> eventArgsfactory = null,
            IByteSegmentLocator byteSegmentLocator = null,
            ITcpClientEventContainer eventContainer = null
            )
        {

            //Contract.Requires<ArgumentNullException>(dataCutter != null, "Parameter dataCutter cannot be null.");
            //Contract.Requires<ArgumentNullException>(dataPacker != null, "Parameter dataPacker cannot be null.");
            //Contract.Requires<ArgumentNullException>(eventArgsfactory != null, "Parameter eventArgsfactory cannot be null.");
            //Contract.Requires<ArgumentNullException>(socket != null, "Parameter socket cannot be null.");
            //Contract.Requires<ArgumentException>(socket.ProtocolType.HasFlag(ProtocolType.Tcp), "Parameter socket should be TCP socket.");
            //Contract.Requires<ArgumentException>(socket.Connected, "Parameter socket must be open.");

            //_disposeActionList.Push(() => socket.Dispose());
            _dataCutter = dataCutter;
            _dataPacker = dataPacker;

            //默认创建销毁逻辑。
            eventArgsfactory =
                eventArgsfactory ??
                new Func<Disposable<SocketAsyncEventArgs>>(
                    () =>
                        new Disposable<SocketAsyncEventArgs>(
                            ea => ea.Dispose(),
                            new SocketAsyncEventArgs()
                            )
                    );

            //默认创建新数组逻辑
            _byteSegmentLocator = byteSegmentLocator ?? new NewArrayByteSegmentLocator();

            //默认事件订阅容器
            EventContainer = eventContainer ?? TcpClientEventContainer.DefaultInstance;

            //创建字段对象


            _socket = socket;
            _disposeActionList.Push(() => _socket.Dispose());

            _disposed = false;
            //_asyncSupport = new ObservableToAsync<TPacket>();
            _sendingQueue = new ConcurrentQueue<Tuple<Queue<ArraySegment<byte>>, AsyncCallback, AbortionChecker>>();


            _packetReceivedSubject = new Subject<TPacket>();
            _packetSentSubject = new Subject<TPacket>();
            _sendLaucherSubject = new Subject<SocketAsyncEventArgs>();


            _disposeActionList.Push(() => _packetReceivedSubject.Dispose());
            _disposeActionList.Push(() => _packetSentSubject.Dispose());
            _disposeActionList.Push(() => _sendLaucherSubject.Dispose());




            //获得发送的事件参数并且登记销毁
            var tokenSend = eventArgsfactory();
            _sendEventArgs = tokenSend.StateObject;
            _disposeActionList.Push(
                () =>
                {
                    tokenSend.Dispose();
                    _sendEventArgs = null;
                });

            //获得接收的事件参数并且登记销毁
            var tokenReceive = eventArgsfactory();
            _receiveEventArgs = tokenReceive.StateObject;
            _disposeActionList.Push(
                () =>
                {
                    tokenReceive.Dispose();
                    _receiveEventArgs = null;

                });


            //设置接收订阅
            ConfigReceive();
            //设置发送订阅
            ConfigSend();

        }

        private void ConfigSend()
        {
            var sendObservable = Observable.FromEventPattern<SocketAsyncEventArgs>(
                eh => _sendEventArgs.Completed += eh,
                eh => _sendEventArgs.Completed -= eh);


            var sendOkObservable =
                sendObservable
                .Where(e => e.EventArgs.SocketError == SocketError.Success && e.EventArgs.BytesTransferred > 0)
                .Select(e => e.EventArgs)
                .Merge(_sendLaucherSubject);//除了接收成功外 也可以手动开始这段序列;

            Tuple<Queue<ArraySegment<byte>>, AsyncCallback, AbortionChecker> currentSendTask = null;
            var nextcall = sendOkObservable
                .Subscribe(
                    e =>
                    {
                        var isAsync = false;
                        while ((!isAsync) && _socket.Connected)//如果万一发送出了一些反常,不是异步发出的 则继续同步发送
                        {
                            if (_disposed)
                            {
                                return;

                            }
                            if (e != null)
                            {

                                //检查eventarg是否发送完毕，未完毕则继续发送
                                if (e.BytesTransferred < e.Count)
                                {
                                    try
                                    {
                                        //Console.WriteLine("Broken segment");
                                        var off = e.Offset;
                                        var count = e.Count;
                                        off = off + e.BytesTransferred;
                                        count = count - e.BytesTransferred;
                                        e.SetBuffer(off, count);
                                        isAsync = _socket.SendAsync(e); //确定是否是异步发送
                                    }
                                    catch (Exception)
                                    {


                                    }
                                    continue;
                                }
                                //通过上面的检查 则此eventarg已经发送完毕


                                //检查eventarg是否具有回调，有则运行回调
                                var ac = e.UserToken as Action;
                                if (ac != null)
                                {
                                    try
                                    {
                                        ac();
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    e.UserToken = null;
                                }
                            }



                            //如果当前项为空  或者包内队列已经取光 则取下一个包
                            while (currentSendTask == null || currentSendTask.Item1.Count == 0)
                            {
                                //如果全部都取光 则停止发送
                                if (!_sendingQueue.TryDequeue(out currentSendTask))
                                {
                                    Interlocked.Exchange(ref _isSending, 0);//设置为非发送状态
                                    return;//不再运行 SendAsync
                                }
                            }

                            //取得下一个包内数据片段并且发送
                            var seg = currentSendTask.Item1.Dequeue();
                            Buffer.BlockCopy(seg.Array, seg.Offset, _sendEventArgs.Buffer, 0, seg.Count);
                            _sendEventArgs.SetBuffer(0, seg.Count);
                            //如果是包内最后一个数据片段 则将回调保存在eventarg中备用
                            if (currentSendTask.Item1.Count == 0)
                            {
                                _sendEventArgs.UserToken = currentSendTask.Item2;
                            }
                            else
                            {
                                _sendEventArgs.UserToken = null;
                            }


                            try
                            {
                                isAsync = _socket.SendAsync(_sendEventArgs); //确定是否是异步发送
                            }
                            catch (Exception)
                            {


                            }

                        }

                    }

                );


            //TODO:建立接收失败的过滤和逻辑
            var errorHandle =
                sendObservable
                .Where(e => !(e.EventArgs.SocketError == SocketError.Success && e.EventArgs.BytesTransferred > 0))
                .Subscribe(
                    e =>
                    {

                        EventContainer.OnClientException(
                            this,
                            EventSource.SendingToRemote,
                            new SocketException("Socket error or closed by remote. "),
                            e.EventArgs);

                    }
            );




        }



        private void ConfigReceive()
        {
            //将事件触发变成一个序列
            var receiveObservable = Observable.FromEventPattern<SocketAsyncEventArgs>(
                eh => _receiveEventArgs.Completed += eh,
                eh => _receiveEventArgs.Completed -= eh);



            var receiveOKObservable = receiveObservable
                .Where(e => e.EventArgs.SocketError == SocketError.Success && e.EventArgs.BytesTransferred > 0)
                .Select(e => e.EventArgs)
                .Select(
                    e =>
                    {
                        var rval = _byteSegmentLocator.CopyToNextSegment(e);
                        return rval;
                    });


            //订阅成功数据片段序列处理程序，试图处理序列中的数据,开始下一次接收。
            var nextCall = receiveOKObservable
                .Synchronize()
                .Subscribe(
                    seg =>
                    {

                        try
                        {
                            _dataCutter.OnNext(seg);
                            if (_disposed)
                            {

                                return;
                            }
                            _socket.ReceiveAsync(_receiveEventArgs);
                        }
                        catch (Exception ex)
                        {
                            EventContainer.OnClientException(
                                this,
                                EventSource.ReceivingFromRemote,
                                ex,
                                _receiveEventArgs);
                        }

                    },

                    ex =>
                    {
                        EventContainer.OnClientException(
                                   this,
                                   EventSource.ReceivingFromRemote,
                                   ex,
                                   _receiveEventArgs);
                    },
                    () =>
                    {


                    }



                    );




            //注册Client销毁时取消成功数据序列订阅的操作
            _disposeActionList.Push(() => nextCall.Dispose());



            //将 ICutter 与 _packetSubject 关联            
            var cutterSub = _dataCutter.Subscribe(
                o => _packetReceivedSubject.OnNext(o),
                e =>
                {
                    EventContainer.OnClientException(
                    this,
                    EventSource.Deserializing,
                    new SocketException("Socket error or closed by remote."),
                    null);
                    _packetReceivedSubject.OnError(e);
                },
                () => _packetReceivedSubject.OnCompleted()

                );
            _disposeActionList.Push(() => cutterSub.Dispose());


            //将_dataCutter与异步读取转化器进行关联
            // var asyncSupport = _dataCutter.PacketsReceived.Subscribe(_asyncSupport);
            //   _disposeActionList.Push(() => asyncSupport.Dispose());

            //TODO:建立接收失败的过滤和逻辑
            var receiveNonOKObservable = receiveObservable
                .Where(e => !(e.EventArgs.SocketError == SocketError.Success && e.EventArgs.BytesTransferred > 0))
                .Subscribe(
                    e =>
                    {
                        EventContainer.OnClientException(
                        this,
                        EventSource.ReceivingFromRemote,
                        new SocketException("Socket error or closed by remote."),
                        e.EventArgs);
                    });


        }




        private void InternalSendingQueueEnqueue(TPacket[] packets, PacketSentCallback<TPacket[]> callback, AbortionChecker abortionChecker)
        {

            var segList = packets
                .SelectMany(packet => _dataPacker.GetByteSegments(packet, _byteSegmentLocator, _sendEventArgs.Buffer.Length))
                .ToList();
            AsyncCallback cb;
            if (callback == null)
            {
                cb = null;
            }
            else
            {
                cb = _ => callback(packets);
            }

            _sendingQueue.Enqueue(
                new Tuple<Queue<ArraySegment<byte>>, AsyncCallback, AbortionChecker>(new Queue<ArraySegment<byte>>(segList),
                    cb, abortionChecker));

            StartSendingIfNoOneIsSending();
        }

        private void StartSendingIfNoOneIsSending()
        {
            int val = Interlocked.Exchange(ref _isSending, 1);//用正在发送状态与当前状态进行交换 得到的如果是未发送状态 则开始发送
            if (val == 0)
            {
                _sendLaucherSubject.OnNext(null);
            }

        }

        private void InternalSendingQueueEnqueue(TPacket packet, PacketSentCallback<TPacket> callback, AbortionChecker abortionChecker)
        {
            var segList = _dataPacker.GetByteSegments(packet, _byteSegmentLocator, _sendEventArgs.Buffer.Length);
            AsyncCallback cb;
            if (callback == null)
            {
                cb = null;
            }
            else
            {
                cb = _ => callback(packet);
            }

            _sendingQueue.Enqueue(
                new Tuple<Queue<ArraySegment<byte>>, AsyncCallback, AbortionChecker>(new Queue<ArraySegment<byte>>(segList),
                 cb, abortionChecker));
            StartSendingIfNoOneIsSending();
        }





        public bool Connected
        {
            get
            {
                return _socket.Connected;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (this)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                this._socket.Shutdown(SocketShutdown.Both);
                this._socket.Close();


                while (_disposeActionList.Count > 0)
                {
                    try
                    {
                        _disposeActionList.Pop()();
                    }
                    catch (Exception)
                    {
                    };
                }

            }

        }



        public void OnCompleted()
        {
            _socket.Close();
        }

        public void OnError(Exception error)
        {
            EventContainer.OnClientException(this, EventSource.BeforeSend, error, null);
        }

        public void OnNext(Tuple<TPacket, PacketSentCallback<TPacket>, AbortionChecker> value)
        {
            //Contract.Requires<InvalidOperationException>(Started, "Should call Start() before send any packet.");
            try
            {
                InternalSendingQueueEnqueue(value.Item1, value.Item2,value.Item3);
            }
            catch (Exception ex)
            {
                EventContainer.OnClientException(this, EventSource.SendingToRemote, ex, null);
            }
        }


        public void OnNext(Tuple<TPacket[], PacketSentCallback<TPacket[]>, AbortionChecker> value)
        {
            //Contract.Requires<InvalidOperationException>(Started, "Should call Start() before send any packet.");
            try
            {
                InternalSendingQueueEnqueue(value.Item1, value.Item2,value.Item3);
            }
            catch (Exception ex)
            {
                EventContainer.OnClientException(this, EventSource.Serializing, ex, null);
            }

        }

        public IDisposable Subscribe(IObserver<TPacket> observer)
        {
            return _packetReceivedSubject.Subscribe(observer);

        }


        public ITcpClientEventContainer EventContainer
        {
            get;
            private set;
        }

        public bool Start()
        {
            if (Interlocked.Exchange(ref _startState, 1) == 0)
            {
                _socket.ReceiveAsync(_receiveEventArgs);
                return true;
            }
            else
            {
                return false;

            }
        }
    }
}
