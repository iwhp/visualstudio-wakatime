namespace Weebree.VsEventLog.Domain
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Net;

    internal class Downloader
    {
        /// <summary>
        /// Directory copy
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    string.Format("Source directory does not exist or could not be found: {0}", sourceDirName));
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// Download wakatime-cli
        /// </summary>
        /// <param name="url"></param>
        /// <param name="installDir"></param>
        static public void DownloadCli(string url, string installDir)
        {
            WebClient client = new WebClient();
            string currentDir = GetCurrentDirectory();
            string fileToDownload = string.Format("{0}\\wakatime-cli.zip", currentDir);

            // Download utility
            Logger.Instance.Info("Downloader: downloading wakatime-cli...");
            client.DownloadFile(url, fileToDownload);

            //Extract to some temp folder
            Logger.Instance.Info("Downloader: extracting wakatime-cli...");
            ZipFile.ExtractToDirectory(fileToDownload, installDir);
        }

        /// <summary>
        /// Download and install Python
        /// </summary>
        /// <param name="url"></param>
        /// <param name="installDir"></param>
        static public void DownloadPython(string url, string installDir)
        {
            string fileToDownload = string.Format("{0}\\python.msi", GetCurrentDirectory());

            WebClient client = new WebClient();
            Logger.Instance.Info("Downloader: downloading python.msi...");
            client.DownloadFile(url, fileToDownload);

            string arguments = string.Format("/i \"{0}\"", fileToDownload);
            if (installDir != null)
            {
                arguments = string.Format("{0} TARGETDIR=\"{1}\"", arguments, installDir);
            }
            arguments = string.Format("{0} /norestart /qb!", arguments);

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardError = true;
            procInfo.FileName = "msiexec";
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            Logger.Instance.Info("Downloader: installing python...");
            var proc = Process.Start(procInfo);
            Logger.Instance.Info("Downloader: finished installing python.");
        }

        static public string GetCurrentDirectory()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assembly);

            return path;
        }

        /// <summary>
        /// Search command line utility from downloaded folder
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="fileToSearch"></param>
        /// <returns></returns>
        static public string SearchFile(string dir, string fileToSearch)
        {
            foreach (string subDir in Directory.GetDirectories(dir))
            {
                foreach (string file in Directory.GetFiles(subDir, fileToSearch))
                {
                    if (file.Contains(fileToSearch))
                    {
                        return file;
                    }
                }
                SearchFile(subDir, fileToSearch);
            }

            return null;
        }

        /// <summary>
        /// Search 'wakatime' folder from downloaded folder
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="serachDir"></param>
        /// <returns></returns>
        static public string SearchFolder(string dir, string serachDir)
        {
            string[] directory = Directory.GetDirectories(dir, serachDir, SearchOption.AllDirectories);
            if (directory.Length > 0)
            {
                return directory[0];
            }

            return null;
        }
    }
}