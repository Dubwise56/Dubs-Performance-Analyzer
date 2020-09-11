using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Analyzer
{
    public enum Severity { Message, Warning, Error }

    public class PendingMessage
    {
        public string message;
        public Severity severity;

        public PendingMessage(string messsage, Severity severity)
        {
            this.message = messsage;
            this.severity = severity;
        }
    }

    public static class ThreadSafeLogger
    {
        private static ConcurrentQueue<PendingMessage> messages = new ConcurrentQueue<PendingMessage>();

        public static void Message(string message)
        {
            messages.Enqueue(new PendingMessage(message, Severity.Message));
        }
        public static void Warning(string message)
        {
            messages.Enqueue(new PendingMessage(message, Severity.Warning));
        }
        public static void Error(string message)
        {
            messages.Enqueue(new PendingMessage(message, Severity.Error));
        }

        public static void DisplayLogs()
        {
            while (messages.TryDequeue(out PendingMessage res))
            {
                switch (res.severity)
                {
                    case Severity.Message: Log.Message(res.message); break;
                    case Severity.Warning: Log.Warning(res.message); break;
                    case Severity.Error: Log.Error(res.message); break;
                }
            }
        }
    }
}
