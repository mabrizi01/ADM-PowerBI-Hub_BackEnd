using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetPowerBIReports.Data
{
    public class PowerBiReport
    {
        [JsonProperty(PropertyName = "powerBIReportID")]
        public string PowerBIReportID { get; set; }

        [JsonProperty(PropertyName = "tenantID")]
        public string TenantID { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "creationDate")]
        public string CreationDate { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "author")]
        public string Author { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "reportId")]
        public string ReportId { get; set; }

        [JsonProperty(PropertyName = "datasetId")]
        public string DatasetId { get; set; }

        [JsonProperty(PropertyName = "RLS")]
        public bool RLS { get; set; }

        [JsonProperty(PropertyName = "groupId")]
        public string GroupId { get; set; }

        [JsonProperty(PropertyName = "param1")]
        public string? Param1 { get; set; }

        [JsonProperty(PropertyName = "param2")]
        public string? Param2 { get; set; }

        [JsonProperty(PropertyName = "param3")]
        public string? Param3 { get; set; }

        [JsonProperty(PropertyName = "param4")]
        public string? Param4 { get; set; }

        [JsonProperty(PropertyName = "param5")]
        public string? Param5 { get; set; }

    }

    public class PowerBiEmbeddedReport
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

    public class PowerBiIdentity
    {
        [JsonProperty(PropertyName = "tenantID")]
        public string TenantID { get; set; }


        [JsonProperty(PropertyName = "clientID")]
        public string? ClientID { get; set; }

        [JsonProperty(PropertyName = "secret")]
        public string? Secret { get; set; }

    }

    public class EmbedContent
    {
        public string EmbedToken { get; set; }
        public string EmbedUrl { get; set; }
        public string ReportId { get; set; }
        public string AccessToken { get; set; }
    }

    public class Identities
    {
        [JsonProperty("identities")]
        public List<Identity> identities { get; set; }

        [JsonProperty("accessLevel")]
        public string accessLevel { get; set; }
    }

    public class Identity
    {
        [JsonProperty("username")]
        public string username { get; set; }

        [JsonProperty("roles")]
        public List<string> roles { get; set; }

        [JsonProperty("datasets")]
        public List<string> datasets { get; set; }
    }

    public class AzureAdTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
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
}
