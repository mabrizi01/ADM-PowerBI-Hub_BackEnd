// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var test = new GetPowerBIReports.Data.DBProcedures();

var result1 = test.GetReportsByUser("admin@mbonline19.onmicrosoft.com", "admin@mbonline19.onmicrosoft.com"); 
var result2 = test.GetReportsByUser("admin@mbonline19.onmicrosoft.com", "mrossi@mbonline19.onmicrosoft.com");
var result3 = test.GetReportsByUser("mrossi@mbonline19.onmicrosoft.com", "admin@mbonline19.onmicrosoft.com");
var result4 = test.GetReportsByUser("lverdi@mbonline19.onmicrosoft.com", "admin@mbonline19.onmicrosoft.com");

var report1 = result1[0];
var result5 = test.GetReportIdentity("admin@mbonline19.onmicrosoft.com", "admin@mbonline19.onmicrosoft.com", report1.GroupId, report1.ReportId);

Console.ReadLine();
