using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Azure;
using System.Data;
using Newtonsoft.Json;


namespace GetPowerBIReports.Data
{
    public class DBProcedures
    {
        private string _connectionString;

        public DBProcedures() 
        {
            const string DB_STRING = "POWERBI_APP_DB_CONNECTION";
            string? str = Environment.GetEnvironmentVariable(DB_STRING);
            _connectionString = ConfigurationManager.AppSettings[DB_STRING];
        }

        public DBProcedures(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<PowerBiReport> GetReportsByUser(string upnCaller, string upnRequested)
        {
            List<PowerBiReport> reportList = new List<PowerBiReport>();

            try
            {
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlCommand sql_cmnd = new SqlCommand("usp_GetReportsByUser", conn);
                    sql_cmnd.CommandType = CommandType.StoredProcedure;
                    sql_cmnd.Parameters.AddWithValue("@upnCaller", SqlDbType.NVarChar).Value = upnCaller;
                    sql_cmnd.Parameters.AddWithValue("@upnRequested", SqlDbType.NVarChar).Value = upnRequested;


                    using (var reader = sql_cmnd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PowerBiReport report = new PowerBiReport()
                            {
                                PowerBIReportID = ((Guid)reader["PowerBIReportID"]).ToString(),
                                TenantID = ((Guid)reader["TenantID"]).ToString(),
                                GroupId = ((Guid)reader["WorkspaceID"]).ToString(),
                                ReportId = ((Guid)reader["ReportID"]).ToString(),
                                CreationDate = ((DateTime)reader["CreationDate"]).ToString(),
                                Author = (string)reader["Author"],
                                Description = (string)reader["Description"],
                                Email= (string)reader["Upn"],
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
            }
            catch (System.Exception ex)
            {
                //Log somewhere
                throw;
            }

            return reportList;
        }

        public PowerBiIdentity GetReportIdentity(string upnCaller, 
            string upnRequested,
            string workspaceId,
            string reportId)
        {
            PowerBiIdentity identity = null;

            try
            {

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlCommand sql_cmnd = new SqlCommand("usp_GetReportIdentity", conn);
                    sql_cmnd.CommandType = CommandType.StoredProcedure;
                    sql_cmnd.Parameters.AddWithValue("@upnCaller", SqlDbType.NVarChar).Value = upnCaller;
                    sql_cmnd.Parameters.AddWithValue("@upnRequested", SqlDbType.NVarChar).Value = upnRequested;
                    sql_cmnd.Parameters.AddWithValue("@workspaceId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(workspaceId);
                    sql_cmnd.Parameters.AddWithValue("@reportId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(reportId);

                    using (var reader = sql_cmnd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            identity = new PowerBiIdentity()
                            {
                                TenantID = ((Guid)reader["TenantID"]).ToString(),
                                ClientID = ((Guid)reader["ClientID"]).ToString(),
                                Secret = (string)reader["Secret"],
                            };
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //Log somewhere
                throw;
            }

            return identity;
        }

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
}
