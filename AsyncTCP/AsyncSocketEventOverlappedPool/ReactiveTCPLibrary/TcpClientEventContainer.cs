using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ReactiveTCPLibrary
{
    public class TcpClientEventContainer : ITcpClientEventContainer
    {
    

        public event EventHandler<ClientExceptionEventArg> ClientException = (o, e) => { };

        public void OnClientException(object sender, EventSource source, Exception ex, SocketAsyncEventArgs eventArgs, object[] packets)
        {
            ClientException(sender, new ClientExceptionEventArg(source, ex, eventArgs,packets));

        }



        static TcpClientEventContainer _defaultInstance = new TcpClientEventContainer();
        static public TcpClientEventContainer DefaultInstance
        {
            get
            {
                return _defaultInstance;
            }
        }
        
    }
}
