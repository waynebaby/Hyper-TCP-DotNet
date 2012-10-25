using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AsyncSocketEventOverlappedPool
{

    public class Disposable<TState> : IDisposable
    {
        public Exception DisposedException { get; private set; }
        public Disposable(Action<TState> disposeAction, TState stateObject)
        {
            StateObject = stateObject;
            _disposeAction = disposeAction;
        }
        Action<TState> _disposeAction;
        public TState StateObject { get; private set; }
        public void Dispose()
        {
            try
            {
                var action = Interlocked.Exchange(ref _disposeAction, null);

                if (action != null)
                {

                    action(StateObject);
                    StateObject = default(TState);
                }

            }
            catch (Exception ex)
            {
                DisposedException = ex;

            }
        }
    }

}
