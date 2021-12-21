using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SGNBranchReportingServer
{
    public class Utility
    {
        public static void CreateProcess(string fileName, string arguments = "", bool waitForExit=true, int timeOut=0)
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = fileName,
                Arguments = $@"{arguments}"
            };
            Process process = new Process()
            {
                StartInfo = info
            };
            process.Start();
            if (waitForExit)
            {
                if (timeOut>0)
                {
                    process.WaitForExit(timeOut);
                }
                else
                {
                    process.WaitForExit();
                }
            }
        }
        public static Dictionary<string,string> GetRequestPostData(HttpListenerRequest request)
        {
            var postData = new Dictionary<string, string>();
            if (!request.HasEntityBody)
            {
                return postData;
            }
            using (System.IO.Stream body = request.InputStream) // here we have data
            {
                using (var reader = new System.IO.StreamReader(body, request.ContentEncoding))
                {
                    string bodyText = reader.ReadToEnd();
                    try
                    {
                        bodyText.Split('&').ToList().ForEach(
                        param => {
                        string[] paramKeyValue = param.Split('=');
                        string paramKey = HttpUtility.UrlDecode(paramKeyValue[0]);
                        string paramValye = HttpUtility.UrlDecode(paramKeyValue[1]);
                        postData.Add(paramKey, paramValye);
                            });
                    }
                    catch (Exception)
                    {
                    }
                    
                }
            }
            return postData;
        }
        public static bool IsFileinUse(string excelFile)
        {
            FileInfo fileInfo = new FileInfo(excelFile);
            FileStream stream = null;
            try
            {
                stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }
    }
}
