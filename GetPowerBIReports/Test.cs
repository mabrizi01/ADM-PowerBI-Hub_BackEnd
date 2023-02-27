using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Protocols;
using System.Collections.Generic;
using System.Data;

namespace GetPowerBIReports
{
    public static class Test
    {
        [FunctionName("Test")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            const string UPN = "admin@mbonline19.onmicrosoft.com";
            List<PowerBiReport> reportList = new List<PowerBiReport>();
            var str = Environment.GetEnvironmentVariable("POWERBI_APP_DB_CONNECTION");
                
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                SqlCommand sql_cmnd = new SqlCommand("usp_GetReportsByUser", conn);
                sql_cmnd.CommandType = CommandType.StoredProcedure;
                sql_cmnd.Parameters.AddWithValue("@upn", SqlDbType.NVarChar).Value = UPN;

                using (var reader = sql_cmnd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        PowerBiReport report = new PowerBiReport()
                        {
                            PowerBIReportID = ((Guid)reader["PowerBIReportID"]).ToString(),
                            TenantID = ((Guid)reader["TenantID"]).ToString(),
                            GroupId = ((Guid)reader["GroupID"]).ToString(),
                            ReportId = ((Guid)reader["ReportID"]).ToString(),
                            ClientID = ((Guid)reader["ClientID"]).ToString(),
                            CreationDate = ((DateTime)reader["CreationDate"]).ToString(),
                            Secret = (string)reader["Secret"],
                            Author = (string)reader["Author"],
                            Description = (string)reader["Description"],
                            Email = (string)reader["upn"],
                            Name = (string)reader["Name"],
                            Param1 = reader["Param1"] as string,
                            Param2 = reader["Param2"] as string,
                            Param3 = reader["Param3"] as string,
                            Param4 = reader["Param4"] as string,
                            Param5 = reader["Param5"] as string,


                        };
                        reportList.Add(report);
                    }
                }
            }
            

            string responseMessage = JsonConvert.SerializeObject(reportList);

            return new OkObjectResult(responseMessage);
        }

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

            [JsonProperty(PropertyName = "clientID")]
            public string? ClientID { get; set; }

            [JsonProperty(PropertyName = "secret")]
            public string? Secret { get; set; }

        }
    }


}
