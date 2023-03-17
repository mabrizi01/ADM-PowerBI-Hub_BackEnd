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
using System.Xml.Linq;
using System.Reflection.PortableExecutable;


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
                            PowerBiReport report = report = GetReportByReader(reader);
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

        public PowerBiReport GetReport(string powerBIReportId)
        {
            PowerBiReport report = null;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlCommand sql_cmnd = new SqlCommand("usp_GetReport", conn);
                    sql_cmnd.CommandType = CommandType.StoredProcedure;
                    sql_cmnd.Parameters.AddWithValue("@powerBIReportId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(powerBIReportId);
                    


                    using (var reader = sql_cmnd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            report = GetReportByReader(reader);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //Log somewhere
                throw;
            }

            return report;
        }

        private PowerBiReport GetReportByReader(SqlDataReader reader)
        {
            PowerBiReport report = new PowerBiReport()
            {
                PowerBIReportID = ((Guid)reader["PowerBIReportID"]).ToString(),
                TenantID = ((Guid)reader["TenantID"]).ToString(),
                GroupId = ((Guid)reader["WorkspaceID"]).ToString(),
                ReportId = ((Guid)reader["ReportID"]).ToString(),
                DatasetId = reader.GetValue("DatasetId") is DBNull ? "" : reader.GetGuid("DatasetId").ToString(),
                RLS = reader.GetValue("RLS") is DBNull ? false : reader.GetBoolean("RLS"),
                CreationDate = ((DateTime)reader["CreationDate"]).ToString(),
                Author = (string)reader["Author"],
                Description = (string)reader["Description"],
                Email = (string)reader["Upn"],
                Name = (string)reader["Name"],
                Param1 = reader["Param1"] as string,
                Param2 = reader["Param2"] as string,
                Param3 = reader["Param3"] as string,
                Param4 = reader["Param4"] as string,
                Param5 = reader["Param5"] as string,
            };

            return report;
        }

        public PowerBiIdentity GetReportIdentity(string upnCaller, 
            string upnRequested,
            string powerBIReportId)
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
                    sql_cmnd.Parameters.AddWithValue("@powerBIReportID", SqlDbType.UniqueIdentifier).Value = Guid.Parse(powerBIReportId);
                    
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



}
