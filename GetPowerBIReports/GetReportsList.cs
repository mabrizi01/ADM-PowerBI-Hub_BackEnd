using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.Http;
//
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GetPowerBIReports
{

    public static class GetReportsList
    {
        [FunctionName("GetReportsList")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
             ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                var debuggingModeAnonymous = bool.Parse(System.Environment.GetEnvironmentVariable("POWERBI_APP_ANONYMOUS_DEBUG"));

                log.LogInformation("C# HTTP trigger function processed a request.");

                //Extract upn from security context, and verify if the report can be visualized
                string upnCaller = "";
                if (debuggingModeAnonymous)
                {
                    upnCaller = req.Query["upnCaller"];
                }
                else
                {
                    upnCaller = claimsPrincipal.Claims.First(c => c.Type == "preferred_username")?.Value;
                }

                //

                //try to extract parameters from GET request:
                string upnRequested = req.Query["upnRequested"];

                //inspect also the POST body    
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                if (debuggingModeAnonymous)
                {
                    upnCaller = upnCaller ?? data?.upnCaller;
                }
                upnRequested = upnRequested ?? data?.upnRequested;

                string connectionString = System.Environment.GetEnvironmentVariable("POWERBI_APP_CONNECTION_STRING");
                GetPowerBIReports.Data.DBProcedures dbProcedures = new Data.DBProcedures(connectionString);
                List<GetPowerBIReports.Data.PowerBiReport> switchRetList = dbProcedures.GetReportsByUser(upnCaller, upnRequested);

                string responseMessage = JsonConvert.SerializeObject(switchRetList);

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                return new Microsoft.AspNetCore.Mvc.OkObjectResult(ex.ToString());
            }

            
        }
    }

    //public static class GetReportsList
    //{
    //    [FunctionName("GetReportsList")]
    //    public static async Task<IActionResult> Run(
    //        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
    //        ILogger log,
    //         ClaimsPrincipal claimsPrincipal)
    //    {
    //        var debuggingModeAnonymous = true;

    //        log.LogInformation("C# HTTP trigger function processed a request.");

    //        //Extract upn from security context, and verify if the report can be visualized
    //        string upnCaller = "";
    //        if (debuggingModeAnonymous)
    //        {
    //            upnCaller = req.Query["upnCaller"];
    //        }
    //        else
    //        {
    //            upnCaller = claimsPrincipal.Claims.First(c => c.Type == "preferred_username")?.Value;
    //        }

    //        //

    //        //try to extract parameters from GET request:
    //        string upnRequested = req.Query["upnRequested"];

    //        //inspect also the POST body    
    //        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    //        dynamic data = JsonConvert.DeserializeObject(requestBody);
    //        if (debuggingModeAnonymous)
    //        {
    //            upnCaller = upnCaller ?? data?.upnCaller;
    //        }
    //        upnRequested = upnRequested ?? data?.upnRequested;

    //        string upn = upnCaller;

    //        //If the caller is an Admin, then provide the list for the upn received as a parameter
    //        if (upnCaller == "admin@MngEnvMCAP203777.onmicrosoft.com")
    //        {
    //            upn = upnRequested;
    //        }

    //        List<GetPowerBIReports.Data.PowerBiReport> switchRetList = new List<GetPowerBIReports.Data.PowerBiReport>();
    //        switch (upn)
    //        {
    //            case "lverdi@MngEnvMCAP203777.onmicrosoft.com":
    //            case "admin@MngEnvMCAP203777.onmicrosoft.com":
    //                switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
    //                {
    //                    Email = upn,
    //                    TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
    //                    GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
    //                    ReportId = "4dd26748-294e-4544-a024-54579bdb3049",
    //                    CreationDate = "2022/02/04",
    //                    Name = "Test Report 1",
    //                    Author = "ADM Data Department",
    //                    Description = "Evaluate the embedding feature",
    //                    Param1 = "Azure Hosted Function",
    //                    Param2 = "",
    //                    Param3 = "",
    //                    Param4 = "",
    //                    Param5 = ""
    //                });
    //                switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
    //                {
    //                    Email = upn,
    //                    TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
    //                    GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
    //                    ReportId = "9f3a6a10-0b45-4afc-90ac-1687c0d22bbd",
    //                    CreationDate = "2022/02/04",
    //                    Name = "Test Report 2",
    //                    Author = "ADM Finance Department",
    //                    Description = "Evaluate the embedding feature",
    //                    Param1 = "Azure Hosted Function",
    //                    Param2 = "",
    //                    Param3 = "",
    //                    Param4 = "",
    //                    Param5 = ""
    //                });
    //                switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
    //                {
    //                    Email = upn,
    //                    TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
    //                    GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
    //                    ReportId = "7049dba5-8fa5-41ac-bf6b-6553354fb00a",
    //                    CreationDate = "2022/02/04",
    //                    Name = "Test Report 3",
    //                    Author = "ADM HR Department",
    //                    Description = "Evaluate the embedding feature",
    //                    Param1 = "Azure Hosted Function",
    //                    Param2 = "",
    //                    Param3 = "",
    //                    Param4 = "",
    //                    Param5 = ""
    //                });
    //                switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
    //                {
    //                    Email = upn,
    //                    TenantID = "c9d7632f-9235-4b36-ad8f-ff8808ddfb96",
    //                    GroupId = "c7670d8c-7331-4e20-9524-db43c26ff198",
    //                    ReportId = "4e294ba9-8efc-4a67-8541-a62b08c129d9",
    //                    CreationDate = "2022/02/04",
    //                    Name = "External Report 3",
    //                    Author = "Contoso HR Department",
    //                    Description = "Evaluate the embedding feature from external tenants",
    //                    Param1 = "External Tenant",
    //                    Param2 = "c9d7632f-9235-4b36-ad8f-ff8808ddfb96",
    //                    Param3 = "",
    //                    Param4 = "",
    //                    Param5 = ""
    //                });
    //                break;
    //            case "mrossi@MngEnvMCAP203777.onmicrosoft.com":
    //                switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
    //                {
    //                    Email = upn,
    //                    TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
    //                    GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
    //                    ReportId = "4dd26748-294e-4544-a024-54579bdb3049",
    //                    CreationDate = "2022/02/04",
    //                    Name = "Test Report 1",
    //                    Author = "ADM Data Department",
    //                    Description = "Evaluate the embedding feature",
    //                    Param1 = "Azure Hosted Function",
    //                    Param2 = "",
    //                    Param3 = "",
    //                    Param4 = "",
    //                    Param5 = ""
    //                });
    //                switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
    //                {
    //                    Email = upn,
    //                    TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
    //                    GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
    //                    ReportId = "9f3a6a10-0b45-4afc-90ac-1687c0d22bbd",
    //                    CreationDate = "2022/02/04",
    //                    Name = "Test Report 2",
    //                    Author = "ADM Data Department",
    //                    Description = "Evaluate the embedding feature",
    //                    Param1 = "Azure Hosted Function",
    //                    Param2 = "",
    //                    Param3 = "",
    //                    Param4 = "",
    //                    Param5 = ""
    //                });
    //                break;
    //            // case "lverdi@MngEnvMCAP203777.onmicrosoft.com":
    //            //     switchRetList.Add(new PowerBiReport()
    //            //     {
    //            //         Email = upn,
    //            //         TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
    //            //         GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
    //            //         ReportId = "4dd26748-294e-4544-a024-54579bdb3049",
    //            //         CreationDate="2022/02/04",
    //            //         Name = "Test Report 1",
    //            //         Author = "ADM Data Department",
    //            //         Description = "Evaluate the embedding feature",
    //            //         Param1 = "Azure Hosted Function",
    //            //         Param2 = "",
    //            //         Param3 = "",
    //            //         Param4 = "",
    //            //         Param5 = ""
    //            //     });
    //            //     break;
    //            default:
    //                break;
    //        }

    //        string responseMessage = JsonConvert.SerializeObject(switchRetList);

    //        return new OkObjectResult(responseMessage);
    //    }
    //}
}
