namespace SGNBranchReportingServer
{
    class Program
    {
        public static void Main(string[] args)
        {
            ReportServer server = new ReportServer("http://*:8000/");
            server.Start();
            server.Listen();
            server.Stop();
        }
    }
}
