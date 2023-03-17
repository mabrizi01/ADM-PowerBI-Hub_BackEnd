using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;
using Microsoft.PowerBI.Api.Models;
using System.Data;

namespace GetPowerBIReports.Implementing
{
    internal class v2
    {
        public static async Task<HttpResponseMessage> CreateEmbeddingCode(
            HttpRequest req,
            ILogger log,
            ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                var debuggingModeAnonymous = bool.Parse(Environment.GetEnvironmentVariable("POWERBI_APP_ANONYMOUS_DEBUG"));

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

                //try to extract parameters from GET request:
                string powerBIReportId = req.Query["powerBIReportId"];
                string tenantId = req.Query["tenantId"];
                string groupId = req.Query["groupId"];
                string reportId = req.Query["reportId"];
                string param1 = req.Query["param1"];
                string param2 = req.Query["param2"];
                string param3 = req.Query["param3"];
                string param4 = req.Query["param4"];
                string param5 = req.Query["param5"];
                

                //inspect also the POST body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic requestBodyObject = JsonConvert.DeserializeObject(requestBody);

                //try to extract parameters from POST request:
                if (debuggingModeAnonymous)
                {
                    upnCaller = upnCaller ?? requestBodyObject?.upnCaller;
                }
                powerBIReportId = powerBIReportId ?? requestBodyObject?.powerBIReportId;
                tenantId = tenantId ?? requestBodyObject?.tenantId;
                groupId = groupId ?? requestBodyObject?.groupId;
                reportId = reportId ?? requestBodyObject?.reportId;
                param1 = param1 ?? requestBodyObject?.param1;
                param2 = param2 ?? requestBodyObject?.param2;
                param3 = param3 ?? requestBodyObject?.param3;
                param4 = param4 ?? requestBodyObject?.param4;
                param5 = param5 ?? requestBodyObject?.param5;
                
                
                //Declare data access component
                string connectionString = Environment.GetEnvironmentVariable("POWERBI_APP_CONNECTION_STRING");
                Data.DBProcedures dbProcedures = new Data.DBProcedures(connectionString);
                
                //Get the Identity for PowerBI Service
                Data.PowerBiIdentity powerBIIdentity = dbProcedures.GetReportIdentity(upnCaller, upnCaller, powerBIReportId);

                //Get Report Details From DB
                Data.PowerBiReport powerBIReport = dbProcedures.GetReport(powerBIReportId);

                Data.PowerBIService powerBIService = new Data.PowerBIService(log);
                Data.EmbedContent data = await powerBIService.GetEmbedContent(powerBIIdentity, 
                    powerBIReport, 
                    param1, 
                    param2,
                    param3, 
                    param4,
                    param5); 

                string jsonp = JsonConvert.SerializeObject(data);

                // Return Response
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonp, Encoding.UTF8, "application/json")
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ex.ToString(), Encoding.UTF8, "application/json")
                };
            }
        }


        public static async Task<IActionResult> GetReportsList(
            HttpRequest req,
            ILogger log,
             ClaimsPrincipal claimsPrincipal)
        {
            return await v1.GetReportsList(req, log, claimsPrincipal);
        }

        

        
    }
}
