using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public class PendingMessage
    {
        public string message;
        public StackTrace stackTrace;
        public LogMessageType severity;

        public PendingMessage(string messsage, StackTrace trace, LogMessageType severity)
        {
            this.message = messsage;
            this.stackTrace = trace;
            this.severity = severity;
        }
    }

    public static class ThreadSafeLogger
    {
        private static ConcurrentQueue<PendingMessage> messages = new ConcurrentQueue<PendingMessage>();
        // ignore the value, no ConcurrentHashSet in the standard, this just avoids using a mutex-locked hashset
        private static ConcurrentDictionary<int, byte> keys = new ConcurrentDictionary<int, byte>();

        private const string MOD_TAG = "[Analyzer]";

        public static string PrependTag(string message)
        {
            if (message.StartsWith(MOD_TAG)) return message;

            var toInsert = MOD_TAG;
            if (message.First() != ' ') toInsert += ' ';
                
            message = message.Insert(0, toInsert);

            return message;
        }

        public static void Message(string message)
        {
            if (message == null) return;
            message = PrependTag(message);
            messages.Enqueue(new PendingMessage(message, new StackTrace(1, false), LogMessageType.Message));
        }
        public static void Warning(string message)
        {
            if (message == null) return;
            message = PrependTag(message);
            messages.Enqueue(new PendingMessage(message, new StackTrace(1, false), LogMessageType.Warning));
        }
        public static void Error(string message)
        {
            if (message == null) return;
            message = PrependTag(message);
            messages.Enqueue(new PendingMessage(message, new StackTrace(1, false), LogMessageType.Error));
        }

        public static void ErrorOnce(string message, int key)
        {
            if (keys.ContainsKey(key)) return;
            keys.TryAdd(key, 0);

            Error(message);
        }

        public static void ReportException(Exception e, string message)
        {
            var finalMessage = $"{message}, exception: {e.Message}, occured at \n{ExtractTrace(new StackTrace(e, false))}";
            Error(finalMessage);
        }

        public static void ReportExceptionOnce(Exception e, string message, int key)
        {
            if (keys.ContainsKey(key)) return;
            keys.TryAdd(key, 0);

            ReportException(e, message);
        }

        public static void DisplayLogs()
        {
            while (messages.TryDequeue(out var res))
            {
                switch (res.severity)
                {
                    case LogMessageType.Message: Log.messageQueue.Enqueue(new LogMessage(LogMessageType.Message, res.message, ExtractTrace(res.stackTrace))); break;
                    case LogMessageType.Warning:
                        UnityEngine.Debug.Log(res.message);
                        Log.messageQueue.Enqueue(new LogMessage(LogMessageType.Warning, res.message, ExtractTrace(res.stackTrace)));
                        break;
                    case LogMessageType.Error:
                        UnityEngine.Debug.LogError(res.message);
                        if (Prefs.PauseOnError && Current.ProgramState == ProgramState.Playing)
                        {
                            Find.TickManager.Pause();
                        }
                        Log.messageQueue.Enqueue(new LogMessage(LogMessageType.Error, res.message, ExtractTrace(res.stackTrace)));

                        if (!PlayDataLoader.Loaded || Prefs.DevMode)
                        {
                            Log.TryOpenLogWindow();
                        }
                        break;
                }
                Log.PostMessage();
            }
        }

        internal static string ExtractTrace(StackTrace stackTrace)
        {
            return StackTraceUtility.GetStackTraceString(stackTrace, out _);
        }
    }
}
