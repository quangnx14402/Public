using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

namespace SGNBranchReportingServer
{
    class ReportServer
    {
        public static string pageData = "";
            
        public ReportServer(string url)
        {
            this.url = url;
            ReportList = GetReportList();
            currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        private HttpListener listener;
        private readonly string url;
        private Dictionary<string, Report> ReportList;
        private string currentDirectory;

        private async Task SendResponseAsync(HttpListenerResponse resp, string pageData, string docType)
        {
            resp.ContentType = docType;
            byte[] data = Encoding.UTF8.GetBytes(pageData);
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            // Write out to the response stream (asynchronously), then close it
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }
        private void SendResponse(HttpListenerResponse resp, string pageData, string docType)
        {
            resp.ContentType = docType;
            byte[] data = Encoding.UTF8.GetBytes(pageData);
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            // Write out to the response stream (asynchronously), then close it
            resp.OutputStream.Write(data, 0, data.Length);
            resp.Close();
        }
        private void SendResultPath(HttpListenerResponse resp, Report report, Dictionary<string,string> _params)
        {

            // Write out to the response stream (asynchronously), then close it
            Dictionary<string, string> cleanParams = new Dictionary<string, string>();
            foreach (var item in _params.Keys)
            {
                cleanParams.Add(item, Regex.Replace(_params[item].Trim('\r', '\n'), @"([^ \S])+", "','"));
                if (DateTime.TryParse(cleanParams[item], out DateTime val))
                {
                    cleanParams[item] = val.ToString("MM-dd-yyyy");
                }
                cleanParams[item] = cleanParams[item].ToUpper();
            }
            string resultPath = DbVis.RunReport(report, cleanParams);
            SendResponse(resp, resultPath, "text/plain");
        }
        private async Task SendResultPathAsync(HttpListenerResponse resp, Report report, Dictionary<string, string> _params)
        {

            // Write out to the response stream (asynchronously), then close it
            Dictionary<string, string> cleanParams = new Dictionary<string, string>();
            foreach (var item in _params.Keys)
            {
                cleanParams.Add(item, Regex.Replace(_params[item].Trim('\r', '\n'), @"(\n|\r|\r\n)+", "','"));
                if (DateTime.TryParse(cleanParams[item],out DateTime val))
                {
                    cleanParams[item] = val.ToString("MM-dd-yyyy");
                }
            }
            string resultPath = await DbVis.RunReportAsync(report, cleanParams);
            await SendResponseAsync(resp, resultPath, "text/plain");
        }
        private async Task HandleIncomingConnections()
        {
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (true)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                string method = req.HttpMethod;
                string endPoint = req.Url.AbsolutePath;
                if (method == "GET")
                {
                    //If Home Page
                    if (endPoint.Replace(@"/", "") == "")
                    {
                        pageData = GetHomePage();
                        //SendResponse(resp, pageData, "text/html");
                        _ = SendResponseAsync(resp, pageData, "text/html");
                    }
                    else
                    //Report Page
                    {
                        if (endPoint=="/getReport")
                        {
                            string reportName = req.QueryString.Get("reportName");
                            if (!ReportList.ContainsKey(reportName))
                            {
                                resp.StatusCode = (int)HttpStatusCode.NotFound;
                                resp.Close();
                                continue;
                            }
                            
                            if (ReportList[reportName].Paramaters.Length>0)
                            {
                                pageData = GetReportPage(reportName);
                                
                                _ = SendResponseAsync(resp, pageData, "text/html");
                                //SendResponse(resp, pageData, "text/html");
                            }
                            else
                            {
                                Console.WriteLine(reportName);
                                _ = SendResultPathAsync(resp, ReportList[reportName],null);
                                //SendResultPath(resp, ReportList[reportName], null);
                            }
                        }
                        else
                        {
                            resp.StatusCode = (int)HttpStatusCode.NotFound;
                            resp.Close();
                        }
                    }
                }
                else
                //POST
                {
                    string reportName = req.QueryString.Get("reportName");
                    if (!ReportList.ContainsKey(reportName))
                    {
                        resp.StatusCode = (int)HttpStatusCode.NotFound;
                        resp.Close();
                    }
                    else
                    {
                        var _params = Utility.GetRequestPostData(req);
                        Console.WriteLine(reportName);
                        _ = SendResultPathAsync(resp, ReportList[reportName], _params);
                        //SendResultPath(resp, ReportList[reportName], _params);
                    }
                }
            }
        }
        public void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);
        }
        public void Listen()
        {
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
        }

        public void Stop()
        {
            listener.Close();
        }
        private string GetHomePage()
        {
            string homePage = File.ReadAllText(Path.Combine(currentDirectory,@"HTML\Reports.html"));
            var allReportNames = "";
            foreach (var reportName in ReportList.Keys)
            {
                allReportNames += $"<option>{reportName}</option>";
            }
            return string.Format(homePage,GetStyle(), allReportNames);
        }
        private string GetStyle()
        {
            return File.ReadAllText(Path.Combine(currentDirectory,@"CSS\Style.css"));
        }
        private string GetReportPage(string reportName)
        {
            Report report = ReportList[reportName];
            string reportPage = File.ReadAllText(Path.Combine(currentDirectory, @"HTML\Report.html"));
            string htmlInputElements = "";
            foreach (Parameter para in report.Paramaters)
            {
                htmlInputElements += $"<label for=\"{para.Name}\">{para.Name}:</label><br>";
                switch (para.Type.ToLower())
                {
                    case "textarea":
                        htmlInputElements += $"<textarea name=\"{para.Name}\" rows=\"15\" required></textarea><br>";
                        break;
                    default:
                        htmlInputElements += $"<input type=\"{para.Type}\" name=\"{para.Name}\" required><br>";
                        break;
                }
            }
            return string.Format(reportPage, GetStyle(), reportName, htmlInputElements);
        }
        private Dictionary<string,Report> GetReportList()
        {
            Dictionary<string, Report> reportList = new Dictionary<string, Report>();
            foreach (var file in Directory.GetFiles(Configuration.SQLFolder,"*.sql"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file).ToUpper();
                Match m = Regex.Match(fileName, @"^(?<Database>[\w ]+)(-(?<ResultFormat>[A-Z]+)-(?<TimeOutInMin>\d+))?-(?<Dept>[A-Z ]+)-(?<User>[A-Z ]+)-(?<Customer>[A-Z ]+)-(?<Description>[^-]+)$");
                if (m.Success)
                {
                    string db = m.Groups["Database"].Value;
                    string format = m.Groups["ResultFormat"].Value;
                    format = format == "" ? "xlsx" : format;
                    string timeOut = m.Groups["TimeOutInMin"].Value;
                    timeOut = timeOut == "" ? "1" : timeOut;
                    string dept = m.Groups["Dept"].Value;
                    string user = m.Groups["User"].Value;
                    string customer = m.Groups["Customer"].Value;
                    string desc = m.Groups["Description"].Value;
                    Report report = new Report()
                    {
                        FullReportName = fileName,
                        DatabaseName = db,
                        TimeOut = int.Parse(timeOut) * 1000 * 60,
                        ResultFormat = format,
                        ReportName = $"{dept}-{user}-{customer}-{desc}"
                    };
                    string sqlContent = File.ReadAllText(file);
                    List<Parameter> parameters = new List<Parameter>(); 
                    foreach (Match param in Regex.Matches(sqlContent,@"\[(?<ParamName>.+?)-(?<ParamType>.+?)\]"))
                    {
                        string paramName = param.Groups["ParamName"].Value.ToUpper();
                        string paramType = param.Groups["ParamType"].Value.ToUpper();
                        if (parameters.Where(p=>p.Name == paramName && p.Type == paramType).Count()==0)
                        {
                            parameters.Add(new Parameter() { Name = paramName, Type = paramType });
                        }
                    }
                    report.Paramaters = parameters.ToArray();
                    reportList.Add(report.ReportName, report);
                }
            }
            return reportList;
        }
    }
}