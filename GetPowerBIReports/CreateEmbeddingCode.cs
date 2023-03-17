

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System;
//
using System.Text;
using System.Net.Http;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using GetPowerBIReports.Data;

namespace GetPowerBIReports
{
    public static class CreateEmbeddingCode
    {

        [FunctionName("CreateEmbeddingCode")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ClaimsPrincipal claimsPrincipal)
        {
            return await Implementing.v2.CreateEmbeddingCode(req, log, claimsPrincipal);
        }
    }
}