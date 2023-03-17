// See https://aka.ms/new-console-template for more information

using GetPowerBIReports.Data;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello, World!");

        GetReports();
        TestADM();
        //TestLocal();

        Console.ReadLine();
    }

    static async void GetReports()
    {
        GetPowerBIReports.Data.DBProcedures dbClient = new DBProcedures();

        var reports = dbClient.GetReportsByUser("admin@MngEnvMCAP203777.onmicrosoft.com", "admin@MngEnvMCAP203777.onmicrosoft.com");
        var report = dbClient.GetReport("1a9d51de-8219-4675-89c5-cb71860fcf0a");
    }

    static async void TestADM()
    {
        
        //CallReport("1a9d51de-8219-4675-89c5-cb71860fcf0a");
        CallReport("a392df9f-6299-48ef-b856-b1c409b8c5f9");
    }

    
    static async void CallReport(string powerBIReportId)
    {
        string upnCaller = "admin@MngEnvMCAP203777.onmicrosoft.com";
  
        //Declare data access component
        GetPowerBIReports.Data.DBProcedures dbProcedures = new GetPowerBIReports.Data.DBProcedures();

        //Get the Identity for PowerBI Service
        GetPowerBIReports.Data.PowerBiIdentity powerBIIdentity = dbProcedures.GetReportIdentity(upnCaller, upnCaller, powerBIReportId);

        //Get Report Details From DB
        GetPowerBIReports.Data.PowerBiReport powerBIReport = dbProcedures.GetReport(powerBIReportId);

        var log = new MyLog();
        GetPowerBIReports.Data.PowerBIService powerBIService = new GetPowerBIReports.Data.PowerBIService(log);
        GetPowerBIReports.Data.EmbedContent data = await powerBIService.GetEmbedContent(powerBIIdentity,
            powerBIReport,
            powerBIReport.Param1,
            powerBIReport.Param2,
            powerBIReport.Param3,
            powerBIReport.Param4,
            powerBIReport.Param5);
    }

    static void Test2()
    {
        //var test = new GetPowerBIReports.Data.DBProcedures();

        //var result1 = test.GetReportsByUser("admin@MngEnvMCAP203777.onmicrosoft.com", "admin@MngEnvMCAP203777.onmicrosoft.com"); 
        //var result2 = test.GetReportsByUser("admin@MngEnvMCAP203777.onmicrosoft.com", "mrossi@MngEnvMCAP203777.onmicrosoft.com");
        //var result3 = test.GetReportsByUser("mrossi@MngEnvMCAP203777.onmicrosoft.com", "admin@MngEnvMCAP203777.onmicrosoft.com");
        //var result4 = test.GetReportsByUser("lverdi@MngEnvMCAP203777.onmicrosoft.com", "admin@MngEnvMCAP203777.onmicrosoft.com");

        //var report1 = result1[0];
        //var result5 = test.GetReportIdentity("admin@MngEnvMCAP203777.onmicrosoft.com", "admin@MngEnvMCAP203777.onmicrosoft.com", report1.GroupId, report1.ReportId);

        //var report2 = test.GetReport("c7670d8c-7331-4e20-9524-db43c26ff198", "6727dfe8-1a6a-42d2-b01c-7c80f0728057");

    }

    class MyLog : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            
        }
    }
}



