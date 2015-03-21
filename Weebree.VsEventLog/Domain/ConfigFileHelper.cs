namespace Weebree.VsEventLog.Domain
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class ConfigFileHelper
    {
        /// <summary>
        /// Retrive ApiKey from config file.
        /// </summary>
        /// <returns></returns>
        public static string GetApiKey()
        {
            StringBuilder keyValue = new StringBuilder(255);
            string configFilepath = GetConfigFilePath();
            if (string.IsNullOrWhiteSpace(configFilepath) == false)
            {
                if (GetPrivateProfileString("settings", "api_key", "", keyValue, 255, configFilepath) > 0)
                {
                    return keyValue.ToString();
                }
            }

            return null;
        }

        public static string GetConfigFilePath()
        {
            string cfgFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(cfgFilePath) == false)
            {
                string cfgfileName = string.Format("{0}\\.wakatime.cfg", cfgFilePath);

                return cfgfileName;
            }

            return null;
        }

        /// <summary>
        /// Update ApiKey file in config file
        /// </summary>
        /// <returns></returns>
        public static void UpdateApiKey(string apiKey)
        {
            string configFilepath = GetConfigFilePath();
            if (string.IsNullOrWhiteSpace(apiKey) == false)
            {
                WritePrivateProfileString("settings", "api_key", apiKey, configFilepath);
            }
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
    }
}