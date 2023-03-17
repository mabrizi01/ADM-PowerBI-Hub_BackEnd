using Azure.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using Microsoft.IdentityModel.Tokens;

namespace GetPowerBIReports.Data
{
    public class PowerBIService
    {
        private ILogger log { get; set; }             

        public PowerBIService(ILogger log)
        {
            this.log = log;
        }

        public async Task<EmbedContent> GetEmbedContent(PowerBiIdentity powerBIIdentity,
            Data.PowerBiReport powerBIReport,
            string param1,
            string param2,
            string param3,
            string param4,
            string param5
            )
        {
            HttpClient authclient = new HttpClient();
            var isGCC = false;
            var powerBI_API_URL = "https://api.powerbi.com";
            var powerBI_API_Scope = "https://analysis.windows.net/powerbi/api/.default";
            if (isGCC)
            {
                powerBI_API_Scope = "https://analysis.usgovcloudapi.net/powerbi/api/.default";
                powerBI_API_URL = "https://api.powerbigov.us";
            }

#pragma warning disable CS8604 // Possible null reference argument.
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



            EmbedContent data = new EmbedContent();
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");

            using (var client = new PowerBIClient(new Uri(powerBI_API_URL), tokenCredentials))
            {
                var report = await client.Reports.GetReportInGroupAsync(Guid.Parse(powerBIReport.GroupId), Guid.Parse(powerBIReport.ReportId));
                var generateTokenRequestParameters = new GenerateTokenRequest(TokenAccessLevel.View);


                if (powerBIReport.RLS)
                {
                    string[] paramArray = new string[] { param1, param2, param3, param4, param5};
                    string paramValue = null;
                    for (int paramIndex = 0; paramIndex < paramArray.Length; paramIndex++)
                    {
                        if (!string.IsNullOrEmpty(paramArray[paramIndex]))
                        {
                            paramValue = paramArray[paramIndex];

                            generateTokenRequestParameters = new GenerateTokenRequest(
                                 accessLevel: "View",
                                 identities: new List<EffectiveIdentity> { new EffectiveIdentity(username: paramValue, roles: new List<string> { $"Param{paramIndex+1}" },
                             datasets: new List<string> { powerBIReport.DatasetId }) }
                                );

                            //break;
                        }
                    }
                    
                    //No parameter has been set
                    if (String.IsNullOrEmpty(paramValue))
                    {
                        //Set a global parameter that will not filter anything
                        generateTokenRequestParameters = new GenerateTokenRequest(
                         accessLevel: "View",
                         identities: new List<EffectiveIdentity> { new EffectiveIdentity(username: "all", roles: new List<string> { "ParamAll" },
                             datasets: new List<string> { powerBIReport.DatasetId }) }
                        );
                    }

                    var tokenResponse = client.Reports.GenerateTokenInGroup(
                                        Guid.Parse(powerBIReport.GroupId),
                                        Guid.Parse(powerBIReport.ReportId),
                                        generateTokenRequestParameters);

                    data.EmbedToken = tokenResponse.Token;
                    data.EmbedUrl = report.EmbedUrl;
                    data.ReportId = report.Id.ToString();
                    data.AccessToken = accessToken;
                }
                else
                {
                    // Get PowerBi report url and embed token
                    HttpClient powerBiClient = new HttpClient();
                    powerBiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    string requestUri = $"{powerBI_API_URL}/v1.0/myorg/groups/{powerBIReport.GroupId}/reports/{powerBIReport.ReportId}";

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
                        PowerBiEmbeddedReport report = JsonConvert.DeserializeObject<PowerBiEmbeddedReport>(response.Result.Content.ReadAsStringAsync().Result);

                        //string embedUrl = report?.EmbedUrl;
                        string embedUrl = $"{report?.EmbedUrl}&{paramsFilters}";
                        return embedUrl;
                    });
                    var tokenContent = new FormUrlEncodedContent(new[]
                        {
                        new KeyValuePair<string, string>("accessLevel", "view")
                    });
                    var embedToken = await powerBiClient.PostAsync($"{powerBI_API_URL}/v1.0/myorg/groups/{powerBIReport.GroupId}/reports/{powerBIReport.ReportId}/GenerateToken", tokenContent)
                        .ContinueWith((response) =>
                        {
                            log.LogInformation(response.Result.StatusCode.ToString());
                            log.LogInformation(response.Result.ReasonPhrase.ToString());
                            PowerBiEmbedToken powerBiEmbedToken =
                                JsonConvert.DeserializeObject<PowerBiEmbedToken>(response.Result.Content.ReadAsStringAsync().Result);
                            return powerBiEmbedToken?.Token;
                        });
                    // JSON Response
                    data = new EmbedContent
                    {
                        EmbedToken = embedToken,
                        EmbedUrl = embedUrl,
                        ReportId = powerBIReport.ReportId,
                        AccessToken = accessToken
                    };
                }

                
            }

            return data;
        }
    }




}
