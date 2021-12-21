using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SGNBranchReportingServer
{
    public class Report
    {
        public string FullReportName { get; set; }
        public string ReportName { get; set; }
        public string DatabaseName { get; set; }
        public Parameter[] Paramaters { get; set; }
        public int TimeOut { get; set; }
        public string ResultFormat { get; set; }
        public string PrototypeSQLFile { get =>
            Path.Combine(Configuration.SQLFolder, $"{FullReportName}.sql");
        }
    }
    public struct Parameter
    {
        public string Name;
        public string Type;
        public string Text { get => Name + "-" + Type; }
    }
}
