using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Weebree.VsEventLog.Domain
{
    /// <summary>
    /// Singleton class to check plugin .
    /// </summary>
    internal class UtilityManager
    {
        private UtilityManager()
        {
        }

        private static readonly bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();
        private static readonly bool is64BitProcess = (IntPtr.Size == 8);
        private static UtilityManager instance;
        private const string PluginName = "visualstudio-wakatime";
        private const string Version = "2.0.2";
        private readonly Process process = new Process();
        private string apiKey = null;

        public static UtilityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UtilityManager();
                }
                return instance;
            }
        }
        public string ApiKey
        {
            get
            {
                return this.apiKey;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    this.apiKey = value;
                    ConfigFileHelper.UpdateApiKey(value);
                }
            }
        }

        /// <summary>
        /// Returns current working dir
        /// </summary>
        /// <returns></returns>
        static public string GetCurrentDirectory()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assembly);

            return path;
        }

        /// <summary>
        /// Is it 64 bit Windows?
        /// http://stackoverflow.com/questions/336633/how-to-detect-windows-64-bit-platform-with-net
        /// </summary>
        /// <returns></returns>
        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }

        public string GetCli()
        {
            return string.Format("{0}\\wakatime-master\\wakatime-cli.py", this.GetCliDir());
        }

        public string GetCliDir()
        {
            return string.Format("{0}\\wakatime", GetCurrentDirectory());
        }

        public string GetPython()
        {
            string[] locations =
            {
                "pythonw",
                "python",
                "\\Python37\\pythonw",
                "\\Python36\\pythonw",
                "\\Python35\\pythonw",
                "\\Python34\\pythonw",
                "\\Python33\\pythonw",
                "\\Python32\\pythonw",
                "\\Python31\\pythonw",
                "\\Python30\\pythonw",
                "\\Python27\\pythonw",
                "\\Python26\\pythonw",
                "\\python37\\pythonw",
                "\\python36\\pythonw",
                "\\python35\\pythonw",
                "\\python34\\pythonw",
                "\\python33\\pythonw",
                "\\python32\\pythonw",
                "\\python31\\pythonw",
                "\\python30\\pythonw",
                "\\python27\\pythonw",
                "\\python26\\pythonw",
                "\\Python37\\python",
                "\\Python36\\python",
                "\\Python35\\python",
                "\\Python34\\python",
                "\\Python33\\python",
                "\\Python32\\python",
                "\\Python31\\python",
                "\\Python30\\python",
                "\\Python27\\python",
                "\\Python26\\python",
                "\\python37\\python",
                "\\python36\\python",
                "\\python35\\python",
                "\\python34\\python",
                "\\python33\\python",
                "\\python32\\python",
                "\\python31\\python",
                "\\python30\\python",
                "\\python27\\python",
                "\\python26\\python",
            };
            foreach (string location in locations)
            {
                try
                {
                    ProcessStartInfo procInfo = new ProcessStartInfo();
                    procInfo.UseShellExecute = false;
                    procInfo.RedirectStandardError = true;
                    procInfo.FileName = location;
                    procInfo.CreateNoWindow = true;
                    procInfo.Arguments = "--version";
                    var proc = Process.Start(procInfo);
                    string errors = proc.StandardError.ReadToEnd();
                    if (errors == null || errors == "")
                    {
                        return location;
                    }
                }
                catch (Exception ex)
                {
                }
            }
            return null;
        }

        public string GetPythonDir()
        {
            return string.Format("{0}\\Python", GetCurrentDirectory());
        }

        /// <summary>
        /// Check Python installed or not, APIKEY exists or not, Command line utility installed or not
        /// </summary>
        /// <returns></returns>
        public void Initialize()
        {
            try
            {
                // Make sure python is installed
                if (!this.IsPythonInstalled())
                {
                    Logger.Instance.Info("UtilityManager: Python not found.");
                    string pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.msi";
                    if (is64BitOperatingSystem)
                    {
                        pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.amd64.msi";
                    }
                    Downloader.DownloadPython(pythonDownloadUrl, null);
                }
                else
                {
                    Logger.Instance.Info(string.Format("UtilityManager: Python found at {0}", this.GetPython()));
                }

                if (!this.DoesCliExist())
                {
                    Logger.Instance.Info("UtilityManager: wakatime-cli not found.");
                    Downloader.DownloadCli("https://github.com/wakatime/wakatime/archive/master.zip", this.GetCliDir());
                }
                else
                {
                    Logger.Instance.Info(string.Format("UtilityManager: wakatime-cli found at {0}", this.GetCli()));
                }

                this.apiKey = ConfigFileHelper.GetApiKey();

                if (string.IsNullOrWhiteSpace(this.apiKey))
                {
                    Logger.Instance.Error("API Key could not be found.");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(string.Format("UtilityManager initialize : {0}", ex.Message));
            }
        }

        public void SendFile(string fileName, string projectName, bool isWrite, string visualStudioVersion)
        {
            string arguments = string.Format("\"{0}\" --key=\"{1}\" --file=\"{2}\" --plugin=\"visualstudio/{3} {4}/{5}\"", this.GetCli(), this.apiKey, fileName, visualStudioVersion, PluginName, Version);

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                arguments = string.Format("{0} --project=\"{1}\"", arguments, projectName);
            }

            if (isWrite)
            {
                arguments = string.Format("{0} --write", arguments);
            }

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.FileName = this.GetPython();
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            try
            {
                var proc = Process.Start(procInfo);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Instance.Error(string.Format("UtilityManager sendFile : {0} {1}", this.GetPython(), arguments));
                Logger.Instance.Error(string.Format("UtilityManager sendFile : {0}", ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(string.Format("UtilityManager sendFile : {0} {1}", this.GetPython(), arguments));
                Logger.Instance.Error(string.Format("UtilityManager sendFile : {0}", ex.Message));
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In]
            IntPtr hProcess,
            [Out]
            out bool wow64Process);

        /// <summary>
        /// Check if wakatime command line exists or not
        /// </summary>
        /// <returns></returns>
        private bool DoesCliExist()
        {
            if (File.Exists(this.GetCli()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if bundled python installation exists
        /// </summary>
        /// <returns></returns>
        private bool DoesPythonExist()
        {
            if (File.Exists(string.Format("{0}\\pythonw.exe", this.GetPythonDir())))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if python is installed
        /// </summary>
        /// <returns></returns>
        private bool IsPythonInstalled()
        {
            if (this.GetPython() != null)
            {
                return true;
            }
            return false;
        }
    }
}