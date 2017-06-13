using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SqlServerBatchBackup
{
    internal class Logger
    {
        /// <summary>
        /// The name of the log file
        /// </summary>
        private const string LOG_FILE_NAME = "LogFile.txt";

        /// <summary>
        /// The event view event id used to indicate the log entries created by this app
        /// </summary>
        private const int LOG_EVENT_ID = 51442;

        /// <summary>
        /// The full file path of the log file
        /// </summary>
        private static string LogFilePath
        {
            get
            {
                string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
                string executingAssemblyFolder = Path.GetDirectoryName(executingAssemblyPath);
                string filePath = Path.Combine(executingAssemblyFolder, LOG_FILE_NAME);
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, string.Empty);
                }

                return filePath;
            }
        }

        /// <summary>
        /// Writes a message in the log file
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="location">The source location where the log message was created</param>
        internal static void Write(string message, string location)
        {
            string logMessage = string.Format("{0}\t{1}\t{2}{3}",
                DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                location,
                message.Replace("\r\n", "\r\n\t\t"),
                Environment.NewLine);

            try
            {
                File.AppendAllText(LogFilePath, logMessage);
            }
            catch (Exception ex)
            {
                WriteToEventLog(string.Format("{0} - [Ex={1}]", message, ex));
            }
        }

        /// <summary>
        /// Writes the message in the event log
        /// </summary>        
        private static void WriteToEventLog(string message)
        {
            try
            {
                string source = Assembly.GetExecutingAssembly().GetName().Name;
                string logName = "Application";

                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, logName);
                }

                EventLog.WriteEntry(source, message, EventLogEntryType.Warning, LOG_EVENT_ID);
            }
            catch
            {
                // both log file and event log have failed
            }
        }
    }
}
