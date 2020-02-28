using System;
using System.Diagnostics;

namespace DaemonMaster.Core
{
    public class EventLogManager
    {
        public const string EventLogName = "Application";
        public const string EventSource = "DaemonMaster";

        public static bool CheckSourceExists()
        {
            if (EventLog.SourceExists(EventSource))
            {
                EventLog evLog = new EventLog { Source = EventSource };
                if (evLog.Log != EventLogName)
                {
                    EventLog.DeleteEventSource(EventSource);
                }
            }

            if (!EventLog.SourceExists(EventSource))
            {
                EventLog.CreateEventSource(EventSource, EventLogName);
                EventLog.WriteEntry(EventSource, $"Event Log Created '{EventLogName}'/'{EventSource}'", EventLogEntryType.Information);
            }

            return EventLog.SourceExists(EventSource);
        }

        public static bool RemoveSource()
        {
            if (EventLog.SourceExists(EventSource))
            {
              EventLog.DeleteEventSource(EventSource);
            }

            return !EventLog.SourceExists(EventSource);
        }

        public static void WriteEventToMyLog(string text, EventLogEntryType type)
        {
            if (CheckSourceExists())
            {
                EventLog.WriteEntry(EventSource, text, type);
            }
        }
    }
}
