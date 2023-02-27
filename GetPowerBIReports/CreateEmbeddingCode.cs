

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
            try
            {
                var debuggingModeAnonymous = bool.Parse(System.Environment.GetEnvironmentVariable("POWERBI_APP_ANONYMOUS_DEBUG"));

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
                string groupId = req.Query["groupId"];
                string reportId = req.Query["reportId"];
                string param1 = req.Query["param1"];
                string param2 = req.Query["param2"];

                //inspect also the POST body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic requestBodyObject = JsonConvert.DeserializeObject(requestBody);

                //try to extract parameters from POST request:
                if (debuggingModeAnonymous)
                {
                    upnCaller = upnCaller ?? requestBodyObject?.upnCaller;
                }
                groupId = groupId ?? requestBodyObject?.groupId;
                reportId = reportId ?? requestBodyObject?.reportId;
                param1 = param1 ?? requestBodyObject?.param1;
                param2 = param2 ?? requestBodyObject?.param2;

                //Declare the Identity for PowerBI Service
                string connectionString = System.Environment.GetEnvironmentVariable("POWERBI_APP_CONNECTION_STRING");
                GetPowerBIReports.Data.DBProcedures dbProcedures = new Data.DBProcedures(connectionString);
                GetPowerBIReports.Data.PowerBiIdentity powerBIIdentity = dbProcedures.GetReportIdentity(upnCaller, upnCaller, groupId, reportId);

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
                var accessToken = await authclient.PostAsync($"https://login.microsoftonline.com/{powerBIIdentity.TenantID}/oauth2/v2.0/token", content).ContinueWith<string>((response) =>
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
                var embedUrl =
                    await powerBiClient.GetAsync($"https://{powerBI_API_URL}/v1.0/myorg/groups/{groupId}/reports/{reportId}")
                    .ContinueWith<string>((response) =>
                    {
                        log.LogInformation(response.Result.StatusCode.ToString());
                        log.LogInformation(response.Result.ReasonPhrase.ToString());
                        PowerBiReport report =
                            JsonConvert.DeserializeObject<PowerBiReport>(response.Result.Content.ReadAsStringAsync().Result);
                        return report?.EmbedUrl;
                    });
                var tokenContent = new FormUrlEncodedContent(new[]
                {
        new KeyValuePair<string, string>("accessLevel", "view")
    });
                var embedToken = await powerBiClient.PostAsync($"https://{powerBI_API_URL}/v1.0/myorg/groups/{groupId}/reports/{reportId}/GenerateToken", tokenContent)
                    .ContinueWith<string>((response) =>
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


        //    [FunctionName("CreateEmbeddingCode")]
        //    public static async Task<HttpResponseMessage> Run(
        //        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, 
        //        ILogger log, 
        //        ClaimsPrincipal claimsPrincipal)
        //    {
        //        var debuggingModeAnonymous = true;

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

        //        //try to extract parameters from GET request:
        //        string groupId = req.Query["groupId"];
        //        string reportId = req.Query["reportId"];
        //        string param1 = req.Query["param1"];
        //        string param2 = req.Query["param2"];

        //        //inspect also the POST body
        //        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //        dynamic requestBodyObject = JsonConvert.DeserializeObject(requestBody);

        //        //try to extract parameters from POST request:
        //        if (debuggingModeAnonymous)
        //        {
        //            upnCaller = upnCaller ?? requestBodyObject?.upnCaller;
        //        }
        //        groupId = groupId ?? requestBodyObject?.groupId;
        //        reportId = reportId ?? requestBodyObject?.reportId;
        //        param1 = param1 ?? requestBodyObject?.param1;
        //        param2 = param2 ?? requestBodyObject?.param2;

        //        //Declare the Identity for PowerBI Service
        //        PowerBIIdentity powerBIIdentity = null;

        //        if (param1 == "External Tenant" && param2 != "" && upnCaller == "admin@MngEnvMCAP203777.onmicrosoft.com")
        //        {
        //            log.LogInformation("Get Report from External Tenant");

        //            //Recover the identity Info for external Tenant
        //            // Azure App Registration:
        //            //  appName = "PowerBI | CustomerDataReportViewer";
        //            var tenantId = param2;
        //            //Look somewhere to find the identity for teh external Tenant required
        //            var clientId = "6efca74c-cca4-4f74-a50a-2c4cffa5e7af";
        //            var secret = "RZu8Q~XlCZ-W.3E8LeHKVUHI9eMXnMZPE9Vvqdot";

        //            powerBIIdentity = new PowerBIIdentity
        //            {
        //                TenantId = tenantId,
        //                ClientId = clientId,
        //                Secret = secret
        //            };
        //        }
        //        else
        //        {
        //            log.LogInformation("Get Report from Internal Tenant");

        //            //Recover the identity Info for internal Tenant
        //            // Azure App Registration:
        //            //  appName = "ADM-PowerBI-HUB-aad";
        //            var tenantId = System.Environment.GetEnvironmentVariable("POWERBI_APP_IDENTITY_TENANT_ID");
        //            var clientId = System.Environment.GetEnvironmentVariable("POWERBI_APP_IDENTITY_CLIENT_ID");
        //            var secret = System.Environment.GetEnvironmentVariable("POWERBI_APP_IDENTITY_CLIENT_SECRET");

        //            powerBIIdentity = new PowerBIIdentity
        //            {
        //                TenantId = tenantId,
        //                ClientId = clientId,
        //                Secret = secret
        //            };
        //        }




        //        HttpClient authclient = new HttpClient();
        //        var isGCC = false;
        //        var powerBI_API_URL = "api.powerbi.com";
        //        var powerBI_API_Scope = "https://analysis.windows.net/powerbi/api/.default";
        //        if (isGCC)
        //        {
        //            powerBI_API_Scope = "https://analysis.usgovcloudapi.net/powerbi/api/.default";
        //            powerBI_API_URL = "api.powerbigov.us";
        //        }




        //        var content = new FormUrlEncodedContent(new[]
        //        {
        //    new KeyValuePair<string, string>("grant_type", "client_credentials"),
        //    new KeyValuePair<string, string>("client_id", powerBIIdentity.ClientId),
        //    new KeyValuePair<string, string>("scope", powerBI_API_Scope),
        //    new KeyValuePair<string, string>("client_secret", powerBIIdentity.Secret)
        //});
        //        // Generate Access Token to authenticate for Power BI
        //        var accessToken = await authclient.PostAsync($"https://login.microsoftonline.com/{powerBIIdentity.TenantId}/oauth2/v2.0/token", content).ContinueWith<string>((response) =>
        //        {
        //            log.LogInformation(response.Result.StatusCode.ToString());
        //            log.LogInformation(response.Result.ReasonPhrase.ToString());
        //            log.LogInformation(response.Result.Content.ReadAsStringAsync().Result);
        //            AzureAdTokenResponse tokenRes =
        //                JsonConvert.DeserializeObject<AzureAdTokenResponse>(response.Result.Content.ReadAsStringAsync().Result);
        //            return tokenRes?.AccessToken; ;
        //        });
        //        // Get PowerBi report url and embed token
        //        HttpClient powerBiClient = new HttpClient();
        //        powerBiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        //        var embedUrl =
        //            await powerBiClient.GetAsync($"https://{powerBI_API_URL}/v1.0/myorg/groups/{groupId}/reports/{reportId}")
        //            .ContinueWith<string>((response) =>
        //            {
        //                log.LogInformation(response.Result.StatusCode.ToString());
        //                log.LogInformation(response.Result.ReasonPhrase.ToString());
        //                PowerBiReport report =
        //                    JsonConvert.DeserializeObject<PowerBiReport>(response.Result.Content.ReadAsStringAsync().Result);
        //                return report?.EmbedUrl;
        //            });
        //        var tokenContent = new FormUrlEncodedContent(new[]
        //        {
        //    new KeyValuePair<string, string>("accessLevel", "view")
        //});
        //        var embedToken = await powerBiClient.PostAsync($"https://{powerBI_API_URL}/v1.0/myorg/groups/{groupId}/reports/{reportId}/GenerateToken", tokenContent)
        //            .ContinueWith<string>((response) =>
        //            {
        //                log.LogInformation(response.Result.StatusCode.ToString());
        //                log.LogInformation(response.Result.ReasonPhrase.ToString());
        //                PowerBiEmbedToken powerBiEmbedToken =
        //                    JsonConvert.DeserializeObject<PowerBiEmbedToken>(response.Result.Content.ReadAsStringAsync().Result);
        //                return powerBiEmbedToken?.Token;
        //            });
        //        // JSON Response
        //        EmbedContent data = new EmbedContent
        //        {
        //            EmbedToken = embedToken,
        //            EmbedUrl = embedUrl,
        //            ReportId = reportId,
        //            AccessToken = accessToken
        //        };
        //        string jsonp = JsonConvert.SerializeObject(data);

        //        // Return Response
        //        return new HttpResponseMessage(HttpStatusCode.OK)
        //        {
        //            Content = new StringContent(jsonp, Encoding.UTF8, "application/json")
        //        };
        //    }

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