using GetPowerBIReports.Data;
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
    internal class portalCode
    {
        public static async Task<HttpResponseMessage> CreateEmbeddingCode(
            HttpRequest req,
            ILogger log,
            ClaimsPrincipal claimsPrincipal)
        {
            var debuggingModeAnonymous = true;

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
            groupId = groupId ?? requestBodyObject?.groupId;
            reportId = reportId ?? requestBodyObject?.reportId;
            param1 = param1 ?? requestBodyObject?.param1;
            param2 = param2 ?? requestBodyObject?.param2;
            param3 = param3 ?? requestBodyObject?.param3;
            param4 = param4 ?? requestBodyObject?.param4;
            param5 = param5 ?? requestBodyObject?.param5;

            //Declare the Identity for PowerBI Service
            PowerBIIdentity powerBIIdentity = null;

            if (param1 == "External Tenant" && param2 != "" && upnCaller == "admin@MngEnvMCAP203777.onmicrosoft.com")
            {
                log.LogInformation("Get Report from External Tenant");

                //Recover the identity Info for external Tenant
                // Azure App Registration:
                //  appName = "PowerBI | CustomerDataReportViewer";
                var tenantId = param2;
                //Look somewhere to find the identity for teh external Tenant required
                var clientId = "6efca74c-cca4-4f74-a50a-2c4cffa5e7af";
                var secret = "RZu8Q~XlCZ-W.3E8LeHKVUHI9eMXnMZPE9Vvqdot";

                powerBIIdentity = new PowerBIIdentity
                {
                    TenantId = tenantId,
                    ClientId = clientId,
                    Secret = secret
                };
            }
            else
            {
                log.LogInformation("Get Report from Internal Tenant");

                //Recover the identity Info for internal Tenant
                // Azure App Registration:
                //  appName = "ADM-PowerBI-HUB-aad";
                var tenantId = Environment.GetEnvironmentVariable("POWERBI_APP_IDENTITY_TENANT_ID");
                var clientId = Environment.GetEnvironmentVariable("POWERBI_APP_IDENTITY_CLIENT_ID");
                var secret = Environment.GetEnvironmentVariable("POWERBI_APP_IDENTITY_CLIENT_SECRET");

                powerBIIdentity = new PowerBIIdentity
                {
                    TenantId = tenantId,
                    ClientId = clientId,
                    Secret = secret
                };

                log.LogInformation("powerBIIdentity: " + JsonConvert.SerializeObject(powerBIIdentity));
            }




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
        new KeyValuePair<string, string>("client_id", powerBIIdentity.ClientId),
        new KeyValuePair<string, string>("scope", powerBI_API_Scope),
        new KeyValuePair<string, string>("client_secret", powerBIIdentity.Secret)
    });

            // Generate Access Token to authenticate for Power BI
            var accessToken = await authclient.PostAsync($"https://login.microsoftonline.com/{powerBIIdentity.TenantId}/oauth2/v2.0/token", content).ContinueWith((response) =>
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

            //string requestUri = $"https://{powerBI_API_URL}/v1.0/myorg/groups/{groupId}/reports/{reportId}";
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

            // embedUrl
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

            // tokenContent
            var tokenContent = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("accessLevel", "view")
    });

            // embedToken
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

        public static async Task<IActionResult> GetReportsList(
                HttpRequest req,
                ILogger log,
                ClaimsPrincipal claimsPrincipal)
        {
            var debuggingModeAnonymous = true;

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

            string upn = upnCaller;

            //If the caller is an Admin, then provide the list for the upn received as a parameter
            if (upnCaller == "admin@MngEnvMCAP203777.onmicrosoft.com")
            {
                upn = upnRequested;
            }

            List<GetPowerBIReports.Data.PowerBiReport> switchRetList = new List<GetPowerBIReports.Data.PowerBiReport>();
            switch (upn)
            {
                case "lverdi@MngEnvMCAP203777.onmicrosoft.com":
                case "admin@MngEnvMCAP203777.onmicrosoft.com":
                    switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
                    {
                        Email = upn,
                        TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
                        GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
                        ReportId = "4dd26748-294e-4544-a024-54579bdb3049",
                        CreationDate = "2022/02/04",
                        Name = "Test Report 1",
                        Author = "ADM Data Department",
                        Description = "Evaluate the embedding feature",
                        Param1 = "Azure Hosted Function",
                        Param2 = "",
                        Param3 = "",
                        Param4 = "",
                        Param5 = ""
                    });
                    switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
                    {
                        Email = upn,
                        TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
                        GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
                        ReportId = "9f3a6a10-0b45-4afc-90ac-1687c0d22bbd",
                        CreationDate = "2022/02/04",
                        Name = "Test Report 2",
                        Author = "ADM Finance Department",
                        Description = "Evaluate the embedding feature",
                        Param1 = "Azure Hosted Function",
                        Param2 = "",
                        Param3 = "",
                        Param4 = "",
                        Param5 = ""
                    });
                    switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
                    {
                        Email = upn,
                        TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
                        GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
                        ReportId = "7049dba5-8fa5-41ac-bf6b-6553354fb00a",
                        CreationDate = "2022/02/04",
                        Name = "Test Report 3",
                        Author = "ADM HR Department",
                        Description = "Evaluate the embedding feature",
                        Param1 = "Azure Hosted Function",
                        Param2 = "",
                        Param3 = "",
                        Param4 = "",
                        Param5 = ""
                    });
                    switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
                    {
                        Email = upn,
                        TenantID = "c9d7632f-9235-4b36-ad8f-ff8808ddfb96",
                        GroupId = "c7670d8c-7331-4e20-9524-db43c26ff198",
                        ReportId = "4e294ba9-8efc-4a67-8541-a62b08c129d9",
                        CreationDate = "2022/02/04",
                        Name = "External Report 3",
                        Author = "Contoso HR Department",
                        Description = "Evaluate the embedding feature from external tenants",
                        Param1 = "External Tenant",
                        Param2 = "c9d7632f-9235-4b36-ad8f-ff8808ddfb96",
                        Param3 = "",
                        Param4 = "",
                        Param5 = ""
                    });
                    break;
                case "mrossi@MngEnvMCAP203777.onmicrosoft.com":
                    switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
                    {
                        Email = upn,
                        TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
                        GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
                        ReportId = "4dd26748-294e-4544-a024-54579bdb3049",
                        CreationDate = "2022/02/04",
                        Name = "Test Report 1",
                        Author = "ADM Data Department",
                        Description = "Evaluate the embedding feature",
                        Param1 = "Azure Hosted Function",
                        Param2 = "",
                        Param3 = "",
                        Param4 = "",
                        Param5 = ""
                    });
                    switchRetList.Add(new GetPowerBIReports.Data.PowerBiReport()
                    {
                        Email = upn,
                        TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
                        GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
                        ReportId = "9f3a6a10-0b45-4afc-90ac-1687c0d22bbd",
                        CreationDate = "2022/02/04",
                        Name = "Test Report 2",
                        Author = "ADM Data Department",
                        Description = "Evaluate the embedding feature",
                        Param1 = "Azure Hosted Function",
                        Param2 = "",
                        Param3 = "",
                        Param4 = "",
                        Param5 = ""
                    });
                    break;
                // case "lverdi@MngEnvMCAP203777.onmicrosoft.com":
                //     switchRetList.Add(new PowerBiReport()
                //     {
                //         Email = upn,
                //         TenantID = "7e98e324-b2c6-486a-b6c7-4ba3a4d5befb",
                //         GroupId = "58983dbb-1358-44ce-aa9c-897edd6d034d",
                //         ReportId = "4dd26748-294e-4544-a024-54579bdb3049",
                //         CreationDate="2022/02/04",
                //         Name = "Test Report 1",
                //         Author = "ADM Data Department",
                //         Description = "Evaluate the embedding feature",
                //         Param1 = "Azure Hosted Function",
                //         Param2 = "",
                //         Param3 = "",
                //         Param4 = "",
                //         Param5 = ""
                //     });
                //     break;
                default:
                    break;
            }

            string responseMessage = JsonConvert.SerializeObject(switchRetList);

            return new OkObjectResult(responseMessage);
        }

        public class PowerBIIdentity
        {
            [JsonProperty(PropertyName = "tenantId")]
            public string TenantId { get; set; }

            [JsonProperty(PropertyName = "clientId")]
            public string ClientId { get; set; }

            [JsonProperty(PropertyName = "secret")]
            public string Secret { get; set; }
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
