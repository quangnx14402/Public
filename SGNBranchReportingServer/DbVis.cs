using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SGNBranchReportingServer
{
    public class DbVis
    {
        public static Task<string> RunReportAsync(Report report,Dictionary<string, string> _params)
        {
            string dq = ((char)34).ToString();
            string rsDir = Configuration.ResultFolder;
            string resultPath = Path.Combine(rsDir, $"{report.ReportName} {DateTime.Now.ToString("yy-MM-dd hh-mm-ss")}.{report.ResultFormat}");
            if (!Directory.Exists(rsDir))
            {
                Directory.CreateDirectory(rsDir);
            }
            if (FindDbVis(out string DbvisPath))
            {
                string format = report.ResultFormat;
                if (report.ResultFormat.ToLower()=="xlsx")
                {
                    format = "xls";
                }
                
                string sqlContent = File.ReadAllText(report.PrototypeSQLFile).ToUpper();
                string exportCommand = $@"@export set filename={dq}{resultPath}{dq} format={format} DateFormat=MM/dd/yyyy ExcelSheetName={dq}DATA{dq} shownullas=;"+"\r\n";

                sqlContent = exportCommand + sqlContent;
                sqlContent = "@export on;\r\n" + sqlContent;
                sqlContent = sqlContent + ";\r\n@export off;";

                if (_params != null)
                    foreach (var variable in _params.Keys)
                    {
                        var param = report.Paramaters.Where(p=> p.Name.ToUpper() == variable.ToUpper());
                        if (param.Count() > 0)
                        {
                            string paramText = param.First().Text;
                            sqlContent = sqlContent.Replace($"[{paramText}]", _params[variable]);
                        }
                    }
                string tempFolder = Path.GetTempPath();

                if (File.Exists(resultPath)) File.Delete(resultPath);
                string tempSQLFile = Path.Combine(tempFolder, Path.ChangeExtension(Path.GetRandomFileName(),".SQL"));
                File.WriteAllText(tempSQLFile, sqlContent);

                Utility.CreateProcess(DbvisPath, $"-connection {dq}{report.DatabaseName}{dq} -sqlfile {dq}{tempSQLFile}{dq}",true,report.TimeOut);
 
                if (!File.Exists(resultPath)) return Task.FromResult("Fail To Run Report!");
                Task.Run(() =>
                {
                    while (Utility.IsFileinUse(resultPath))
                    {
                        Thread.Sleep(100);
                    }
                }
                );
                return Task.FromResult(resultPath);
            }
            else throw new Exception("Cannot Find DbVisualizer in your computer!");
        }
        public static string RunReport(Report report, Dictionary<string, string> _params)
        {
            string dq = ((char)34).ToString();
            string rsDir = Configuration.ResultFolder;
            string resultPath = Path.Combine(rsDir, $"{report.ReportName} {DateTime.Now.ToString("yy-MM-dd hh-mm-ss")}.{report.ResultFormat}");
            if (!Directory.Exists(rsDir))
            {
                Directory.CreateDirectory(rsDir);
            }
            if (FindDbVis(out string DbvisPath))
            {
                string format = report.ResultFormat;
                if (report.ResultFormat.ToLower() == "xlsx")
                {
                    format = "xls";
                }

                string sqlContent = File.ReadAllText(report.PrototypeSQLFile);
                Console.WriteLine(sqlContent);
                string exportCommand = $@"@export set filename={dq}{resultPath}{dq} format={format} DateFormat=MM/dd/yyyy ExcelSheetName={dq}DATA{dq} shownullas=;";

                sqlContent = exportCommand + sqlContent;
                sqlContent = "@export on;\r\n" + sqlContent;
                sqlContent = sqlContent + ";\r\n@export off;";

                if (_params != null)
                    foreach (var variable in _params.Keys)
                        sqlContent = sqlContent.Replace($"[{variable}]", _params[variable]);
                string tempFolder = Path.GetTempPath();

                if (File.Exists(resultPath)) File.Delete(resultPath);
                string tempSQLFile = Path.Combine(tempFolder, Path.GetFileName(report.PrototypeSQLFile));
                File.WriteAllText(tempSQLFile, sqlContent);

                Utility.CreateProcess(DbvisPath, $"-connection {dq}{report.DatabaseName}{dq} -sqlfile {dq}{tempSQLFile}{dq}", true, report.TimeOut);

                if (!File.Exists(resultPath)) return "Fail To Run Report!";
                Task.Run(() =>
                {
                    while (Utility.IsFileinUse(resultPath))
                    {
                        Thread.Sleep(100);
                    }
                }
                );
                return resultPath;
            }
            else throw new Exception("Cannot Find DbVisualizer in your computer!");
        }

        private static bool FindDbVis(out string DbvisPath)
        {
            string path_1 = @"C:\Program Files\DbVisualizer\dbviscmd.bat";
            string path_2 = @"C:\Program Files (x86)\DbVisualizer\dbviscmd.bat";
            if (File.Exists(path_1))
            {
                DbvisPath = path_1;
                return true;
            }
            else
            {
                if (File.Exists(path_2))
                {
                    DbvisPath = path_2;
                    return true;
                }
            }
            DbvisPath = "";
            return false;
        }
    }
}