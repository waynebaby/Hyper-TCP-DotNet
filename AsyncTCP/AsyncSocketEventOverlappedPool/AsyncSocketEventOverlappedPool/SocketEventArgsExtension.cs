using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;

namespace AsyncSocketEventOverlappedPool
{
    public static class SocketEventArgsExtension
    {
        static Socket _dummySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static void Reset(this SocketAsyncEventArgs target)
        {
            target.UserToken = null;
            _setter_of_m_ConnectSocket(target, null);
            _setter_of_m_CurrentSocket(target, _dummySocket);
            _setter_of_m_PinnedSocketAddress(target, null);
            _setter_of_m_SocketAddress(target, null);
            var gh = _getter_of_m_SocketAddressGCHandle(target);
            if (gh != null)
            {
                if (gh.IsAllocated)
                    try
                    {
                        gh.Free();
                    }
                    catch (Exception)
                    {

                    }
            }
        }

        static FieldFastSetInvokeHandler<SocketAsyncEventArgs, Socket> _setter_of_m_CurrentSocket = BaseFieldAccessor.SetNonpublicFieldInvoker<SocketAsyncEventArgs, Socket>("m_CurrentSocket");
        static FieldFastSetInvokeHandler<SocketAsyncEventArgs, Socket> _setter_of_m_ConnectSocket = BaseFieldAccessor.SetNonpublicFieldInvoker<SocketAsyncEventArgs, Socket>("m_ConnectSocket");
        static FieldFastSetInvokeHandler<SocketAsyncEventArgs, SocketAddress> _setter_of_m_PinnedSocketAddress = BaseFieldAccessor.SetNonpublicFieldInvoker<SocketAsyncEventArgs, SocketAddress>("m_PinnedSocketAddress");
        static FieldFastSetInvokeHandler<SocketAsyncEventArgs, SocketAddress> _setter_of_m_SocketAddress = BaseFieldAccessor.SetNonpublicFieldInvoker<SocketAsyncEventArgs, SocketAddress>("m_SocketAddress");
        static FieldFastGetInvokeHandler<SocketAsyncEventArgs, GCHandle> _getter_of_m_SocketAddressGCHandle = BaseFieldAccessor.GetNonpublicFieldInvoker<SocketAsyncEventArgs, GCHandle>("m_SocketAddressGCHandle");

    }
}
