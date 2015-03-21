namespace Weebree.VsEventLog.Domain
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.ComponentModel.Design;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell;
    using EnvDTE;
    using Weebree.VsEventLog.Domain;

    public sealed class VsEventLog
    {
        #region Constructors

        public VsEventLog(IVsActivityLog iVsActivityLog, DTE dte)
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            this.IVsActivityLog = iVsActivityLog;
            this.Dte = dte;

            this.Initialize();
            this.InitializeEvents();
        }

        #endregion

        #region Properties

        //private const int HeartbeatInterval = 2 * 60 * 1000; // 2 minute in milli seconds

        private IVsActivityLog IVsActivityLog { get; set; }

        private DTE Dte { get; set; }

        private readonly UtilityManager utilityManager = UtilityManager.Instance;
        private EnvDTE.DTE objDte = null;
        private DocumentEvents docEvents;
        private WindowEvents windowEvents;
        private string lastFileSent = string.Empty;
        private DateTime lastTimeSent = DateTime.Parse("01/01/1970 00:00:00");

        #endregion

        #region Initialize

        protected void Initialize()
        {
            IVsActivityLog log = this.IVsActivityLog;

            Logger.Instance.Initialize(log);
            try
            {
                // IVsExtensionManager manager = GetService(typeof(SVsExtensionManager)) as IVsExtensionManager;
                // var extension = manager.GetInstalledExtensions().Where(n => n.Header.Name == "WakaTime").SingleOrDefault();
                // Version currentVersion = extension.Header.Version;

                // Check for Python, Wakatime utility and Api Key
                utilityManager.Initialize();

                bool isApiKeyFound = CheckForApiKey();
                if (isApiKeyFound)
                {
                    InitializeEvents();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
            }
        }

        /// <summary>
        /// Initialize events and create timer
        /// </summary>
        protected void InitializeEvents()
        {
            if (objDte == null)
            {
                //Initialize events for file open/switch/save.
                objDte = this.Dte;
                docEvents = objDte.Events.DocumentEvents;
                windowEvents = objDte.Events.WindowEvents;

                docEvents.DocumentOpened += new _dispDocumentEvents_DocumentOpenedEventHandler(DocumentEvents_DocumentOpened);
                docEvents.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(DocumentEvents_DocumentSaved);
                windowEvents.WindowActivated += new _dispWindowEvents_WindowActivatedEventHandler(Window_Activated);
            }
        }

        #endregion

        #region Event Handler

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary> Hack to fix syntax highligting: , ( { { "

        /// <summary>
        /// Send file on switching document.
        /// </summary>
        /// <param name="gotFocus">Activated window</param>
        /// <param name="lostFocus">Deactivated window</param>
        public void Window_Activated(Window gotFocus, Window lostFocus)
        {
            try
            {
                Document document = objDte.ActiveWindow.Document;
                if (document != null)
                {
                    SendFileToWakatime(document.FullName, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Window_Activated : " + ex.Message);
            }
        }

        /// <summary>
        /// Called when any document is opened.
        /// </summary>
        /// <param name="document"></param>
        public void DocumentEvents_DocumentOpened(EnvDTE.Document document)
        {
            try
            {
                SendFileToWakatime(document.FullName, false);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("DocumentEvents_DocumentOpened : " + ex.Message);
            }
        }

        /// <summary>
        /// Called when any document is saved.
        /// </summary>
        /// <param name="document"></param>
        public void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            try
            {
                SendFileToWakatime(document.FullName, true);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("DocumentEvents_DocumentSaved : " + ex.Message);
            }
        }

        #endregion

        /// <summary>
        /// Display Api Key dialog box.
        /// </summary>
        public DialogResult DisplayApiKeyDialog()
        {
            APIKeyForm form = new APIKeyForm();
            DialogResult result = form.ShowDialog();
            if (result == DialogResult.OK)
            {
                InitializeEvents(); //If during plgin initialization user does not eneter key then this time 'events' should be initialized
            }

            return result;
        }

        private bool CheckForApiKey()
        {
            if (string.IsNullOrWhiteSpace(utilityManager.ApiKey))
            { // If key does not exist then prompt user to enter key
                DialogResult result = DisplayApiKeyDialog();
                if (result == DialogResult.Cancel)
                { //Otherwise it is assumed that user has entered some key.
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Send file with absolute path to wakatime and store same in _lastFileSent
        /// </summary>
        /// <param name="fileName"></param>
        private void SendFileToWakatime(string fileName, bool isWrite)
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan minutesSinceLastSent = now - lastTimeSent;
            if (fileName != lastFileSent || isWrite || minutesSinceLastSent.Minutes >= 2)
            {
                string projectName = objDte.Solution != null && !string.IsNullOrWhiteSpace(objDte.Solution.FullName) ? objDte.Solution.FullName : null;
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    projectName = Path.GetFileNameWithoutExtension(projectName);
                }
                utilityManager.SendFile(fileName, projectName, isWrite, objDte.Version);
                lastFileSent = fileName;
                lastTimeSent = now;
            }
        }
    }
}
