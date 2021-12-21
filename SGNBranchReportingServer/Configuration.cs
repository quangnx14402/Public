
using System.Configuration;
using System.Reflection;

namespace SGNBranchReportingServer
{
    public static class Configuration
    {
        private static string _SQLFolder;
        private static string _ResultFolder;
        public static string SQLFolder { get
            {
                if (_SQLFolder == null)
                    _SQLFolder = GetConfig("SQLFolder");
                return _SQLFolder;
            }
        }
        public static string ResultFolder
        {
            get
            {
                if (_ResultFolder == null)
                    _ResultFolder = GetConfig("ResultFolder");
                return _ResultFolder;
            }
        }
        private static string GetConfig(string key)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);
            return configFile.AppSettings.Settings[key]?.Value ?? "";
        }
    }
}
