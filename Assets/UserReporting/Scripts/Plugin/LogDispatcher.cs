using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.UserReporting.Scripts.Plugin
{
    public static class LogDispatcher
    {
        #region Static Fields

        private static readonly List<WeakReference> listeners;

        #endregion

        #region Static Constructors

        static LogDispatcher()
        {
            listeners = new List<WeakReference>();
            Application.logMessageReceivedThreaded += (logString, stackTrace, logType) =>
            {
                lock (listeners)
                {
                    var i = 0;
                    while (i < listeners.Count)
                    {
                        var listener = listeners[i];
                        var logListener = listener.Target as ILogListener;
                        if (logListener != null)
                        {
                            logListener.ReceiveLogMessage(logString, stackTrace, logType);
                            i++;
                        }
                        else
                        {
                            listeners.RemoveAt(i);
                        }
                    }
                }
            };
        }

        #endregion

        #region Static Methods

        public static void Register(ILogListener logListener)
        {
            lock (listeners)
            {
                listeners.Add(new WeakReference(logListener));
            }
        }

        #endregion
    }
}