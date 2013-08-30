using System;
using System.Runtime.CompilerServices;
using System.Net.Sockets;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
namespace ReactiveTCPLibrary
{

    public delegate void PacketSentCallback<TPacket>(params TPacket[] packets);
    public enum EventSource
    {
        BeforeSend,
        Serializing,
        SendingToRemote,
        
        ReceivingFromRemote,
        Deserializing

    }
    [Serializable]
    public class SocketException : Exception
    {
        public SocketException() { }
        public SocketException(string message) : base(message) { }
        public SocketException(string message, Exception inner) : base(message, inner) { }
        protected SocketException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
 

    public class ClientExceptionEventArg : EventArgs
    {
        static object[] empty = new object[0];
        public ClientExceptionEventArg(EventSource source, Exception exception,SocketAsyncEventArgs eventArgs, object[] packets = null)
        {
            EventSource = source;
            Exception = exception;
            EventArgs = eventArgs;
            Packets = packets??empty;
        }
        public EventSource EventSource { get; private set; }
        public Exception Exception { get; private set; }
        public object Packets { get; private set; }
        public SocketAsyncEventArgs EventArgs { get; private set; }
    }

    public interface ITcpClientEventContainer
    {
        event EventHandler<ClientExceptionEventArg> ClientException;
        void OnClientException(object sender, EventSource source, Exception ex,SocketAsyncEventArgs eventArgs,object[] packets=null);
    }


    public interface ITcpClient<TConnectionContext, TPacket> :        
        IDisposable
    {
        ConcurrentExclusiveSchedulerPair ExecutionSchedulerPair { get; }
        bool Connected { get; }
        TConnectionContext ConnectionContext { get; set; }
        ITcpClientEventContainer EventContainer { get; }
        bool Start();
        bool Started { get; }
    }

    public interface IReactiveTcpClient<TConnectionContext, TPacket> :
        ITcpClient<TConnectionContext, TPacket>,
        IObservable<TPacket>,
        IObserver<Tuple<TPacket, PacketSentCallback<TPacket>, AbortionChecker>>,
        IObserver<Tuple<TPacket[], PacketSentCallback<TPacket[]>, AbortionChecker>>
    {

    }
    public interface IAsyncTcpClient<TConnectionContext, TPacket> : ITcpClient<TConnectionContext, TPacket>
    {
        Task <TPacket> GetPacketAsync(CancellationToken cancellationToken);
        Task SendPackAsync(TPacket packet, bool waitUntilComplete, CancellationToken cancellationToken);
        Task SendPackAsync(TPacket[] packets, bool waitUntilComplete, CancellationToken cancellationToken);
    }
    /// <summary>
    /// 检测是否取消操作的代理方法
    /// </summary>
    /// <returns>如果需要取消这组操作 则返回true</returns>
    public delegate bool AbortionChecker();

}
