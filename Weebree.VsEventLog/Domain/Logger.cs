namespace Weebree.VsEventLog.Domain
{
    using System;
    using System.Globalization;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Singleton class for logging in Visual Studio default logger file ActivityLog.xml
    /// </summary>
    internal class Logger
    {
        private static Logger instance;
        private IVsActivityLog log;

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Logger();
                }
                return instance;
            }
        }

        public void Error(string message)
        {
            this.log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                    "{0}", message));
        }

        public void Info(string message)
        {
            this.log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                    "{0}", message));
        }

        public void Initialize(IVsActivityLog log)
        {
            this.log = log;
        }
    }
}