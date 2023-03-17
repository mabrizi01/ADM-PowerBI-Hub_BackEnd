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

namespace GetPowerBIReports.Implementing
{
    internal class v1
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

                //Declare the Identity for PowerBI Service
                string connectionString = Environment.GetEnvironmentVariable("POWERBI_APP_CONNECTION_STRING");
                Data.DBProcedures dbProcedures = new Data.DBProcedures(connectionString);
                Data.PowerBiIdentity powerBIIdentity = dbProcedures.GetReportIdentity(upnCaller, upnCaller, powerBIReportId);

                HttpClient authclient = new HttpClient();
                var isGCC = false;
                var powerBI_API_URL = "api.powerbi.com";
                var powerBI_API_Scope = "https://analysis.windows.net/powerbi/api/.default";
                if (isGCC)
                {
                    powerBI_API_Scope = "https://analysis.usgovcloudapi.net/powerbi/api/.default";
                    powerBI_API_URL = "api.powerbigov.us";
                }




                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", powerBIIdentity.ClientID),
                    new KeyValuePair<string, string>("scope", powerBI_API_Scope),
                    new KeyValuePair<string, string>("client_secret", powerBIIdentity.Secret)
                });
                // Generate Access Token to authenticate for Power BI
                var accessToken = await authclient.PostAsync($"https://login.microsoftonline.com/{powerBIIdentity.TenantID}/oauth2/v2.0/token", content).ContinueWith((response) =>
                {
                    log.LogInformation(response.Result.StatusCode.ToString());
                    log.LogInformation(response.Result.ReasonPhrase.ToString());
                    log.LogInformation(response.Result.Content.ReadAsStringAsync().Result);
                    AzureAdTokenResponse tokenRes =
                        JsonConvert.DeserializeObject<AzureAdTokenResponse>(response.Result.Content.ReadAsStringAsync().Result);
                    return tokenRes?.AccessToken; ;
                });
                // Get PowerBi report url and embed token
                HttpClient powerBiClient = new HttpClient();
                powerBiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                string requestUri = $"https://{powerBI_API_URL}/v1.0/myorg/groups/{groupId}/reports/{reportId}";

                string[] paramsReceived = { param1, param2, param3, param4, param5 };
                StringBuilder paramsFiltersBuilder = new StringBuilder();
                string concatLogic = "";
                string filterHint = "$filter=";
                for (int parIndex = 0; parIndex < paramsReceived.Length; parIndex++)
                {
                    if (!string.IsNullOrWhiteSpace(paramsReceived[parIndex]))
                    {
                        paramsFiltersBuilder.Append(filterHint);
                        filterHint = "";
                        paramsFiltersBuilder.Append(concatLogic);
                        paramsFiltersBuilder.Append($"Param{parIndex + 1}/Value eq '{paramsReceived[parIndex]}'");
                        concatLogic = " and ";
                    }
                }
                string paramsFilters = paramsFiltersBuilder.ToString();

                var embedUrl =
                await powerBiClient.GetAsync(requestUri)
                .ContinueWith((response) =>
                {
                    log.LogInformation(response.Result.StatusCode.ToString());
                    log.LogInformation(response.Result.ReasonPhrase.ToString());
                    PowerBiReport report =
                    JsonConvert.DeserializeObject<PowerBiReport>(response.Result.Content.ReadAsStringAsync().Result);

                    //string embedUrl = report?.EmbedUrl;
                    string embedUrl = $"{report?.EmbedUrl}&{paramsFilters}";
                    return embedUrl;
                });
                var tokenContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("accessLevel", "view")
                });
                var embedToken = await powerBiClient.PostAsync($"https://{powerBI_API_URL}/v1.0/myorg/groups/{groupId}/reports/{reportId}/GenerateToken", tokenContent)
                    .ContinueWith((response) =>
                    {
                        log.LogInformation(response.Result.StatusCode.ToString());
                        log.LogInformation(response.Result.ReasonPhrase.ToString());
                        PowerBiEmbedToken powerBiEmbedToken =
                            JsonConvert.DeserializeObject<PowerBiEmbedToken>(response.Result.Content.ReadAsStringAsync().Result);
                        return powerBiEmbedToken?.Token;
                    });
                // JSON Response
                EmbedContent data = new EmbedContent
                {
                    EmbedToken = embedToken,
                    EmbedUrl = embedUrl,
                    ReportId = reportId,
                    AccessToken = accessToken
                };
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

        public class AzureAdTokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
        }

        public class PowerBiReport
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
            [JsonProperty(PropertyName = "webUrl")]
            public string WebUrl { get; set; }
            [JsonProperty(PropertyName = "embedUrl")]
            public string EmbedUrl { get; set; }
            [JsonProperty(PropertyName = "datasetId")]
            public string DatasetId { get; set; }
        }

        public class PowerBiEmbedToken
        {
            [JsonProperty(PropertyName = "token")]
            public string Token { get; set; }
            [JsonProperty(PropertyName = "tokenId")]
            public string TokenId { get; set; }
            [JsonProperty(PropertyName = "expiration")]
            public DateTime? Expiration { get; set; }
        }

        public class EmbedContent
        {
            public string EmbedToken { get; set; }
            public string EmbedUrl { get; set; }
            public string ReportId { get; set; }
            public string AccessToken { get; set; }
        }
    }
}
