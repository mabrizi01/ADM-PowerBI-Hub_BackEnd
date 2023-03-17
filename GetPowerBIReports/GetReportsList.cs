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
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;

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
           return await Implementing.v1.GetReportsList(req, log, claimsPrincipal);
        }
    }

   
}
