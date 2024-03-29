﻿using Firmware.DAL.Models;
using Firmware.Model.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Firmware.DAL.DataOperations
{
    public class DataOperations
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["FirmwareSqlProvider"].ConnectionString;
        private SqlConnection _sqlConnection = null;

        private void OpenConnection()
        {
            _sqlConnection = new SqlConnection { ConnectionString = _connectionString };
            _sqlConnection.Open();
        }
        private void CloseConnection()
        {
            if (_sqlConnection?.State != ConnectionState.Closed)
            {
                _sqlConnection?.Close();
            }
        }
        public List<SoftwarePackage> GetAllSoftwarePackage(int pageNo, int pageSize, string searchText, string sortColumn, string sortDirection)
        {
            try
            {
                OpenConnection();

                List<SoftwarePackage> inventory = new List<SoftwarePackage>();
                int totalRecs = 0;

                using (SqlCommand command = new SqlCommand("Inventory.usp_GetAllSoftwarePackages", _sqlConnection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter { ParameterName = "@PageNo", SqlDbType = SqlDbType.Int, Value = pageNo });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@PageSize", SqlDbType = SqlDbType.Int, Value = pageSize });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SearchText", SqlDbType = SqlDbType.VarChar, Value = searchText });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SortColumn", SqlDbType = SqlDbType.VarChar, Value = sortColumn.ToUpper() });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SortDirection", SqlDbType = SqlDbType.VarChar, Value = sortDirection.ToUpper() });

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ColorStandard colorStandard = ((ColorStandard)Convert.ToInt32(reader["SwColorStandardID"]));

                            inventory.Add(
                            new SoftwarePackage
                            {
                                SwPkgUID = new Guid(reader["SwPkgUID"].ToString()),
                                SwPkgVersion = reader["SwPkgVersion"].ToString(),
                                SwColorStandardID = colorStandard.GetType()
                                                        .GetMember(colorStandard.ToString())
                                                        .First()
                                                        .GetCustomAttribute<DisplayAttribute>()
                                                        .GetName(),
                                SwAddedDate = Convert.ToDateTime(reader["AddedDate"]),
                                SwFileName = reader["FileName"].ToString(),
                                SwFileSize = String.IsNullOrEmpty(reader["FileSize"].ToString()) ? 0 : (Convert.ToInt64(reader["FileSize"]) / 1024f) / 1024f,
                                Manufacturer = reader["Manufacturer"].ToString(),
                                DeviceType = reader["DeviceType"].ToString()
                            }
                            );
                        }

                        reader.NextResult();
                        while (reader.Read())
                        {
                            totalRecs = Convert.ToInt32(reader["TotalRecords"]);
                        }

                        reader.NextResult();
                        Dictionary<Guid, string> keyValuePairs = new Dictionary<Guid, string>();
                        while (reader.Read())
                        {
                            keyValuePairs.Add(new Guid(reader["SwPkgUID"].ToString()), reader["FileName"].ToString());
                        }
                        inventory.ForEach(i =>
                        {
                            if (keyValuePairs.ContainsKey(i.SwPkgUID))
                            {
                                i.HelpDocFileName = keyValuePairs[i.SwPkgUID];
                            }
                        });

                        reader.NextResult();
                        Dictionary<Guid, List<CameraModelName>> swModelMap = new Dictionary<Guid, List<CameraModelName>>();
                        while (reader.Read())
                        {
                            var key = new Guid(reader["SwPkgUID"].ToString());
                            var value = reader["DeviceModelName"].ToString();

                            if (swModelMap.ContainsKey(key))
                            {
                                swModelMap[key].Add(new CameraModelName { ModelName = value });
                            }
                            else
                            {
                                swModelMap.Add(key, new List<CameraModelName> { new CameraModelName { ModelName = value } });
                            }
                        }
                        inventory.ForEach(i =>
                        {
                            if (swModelMap.ContainsKey(i.SwPkgUID))
                            {
                                i.CameraModels = swModelMap[i.SwPkgUID];
                                i.TotalRecords = totalRecs;
                            }
                        });
                        if ("TYPE" == sortColumn.ToUpper() && "ASC" == sortDirection.ToUpper())
                        {
                            inventory = inventory.OrderBy(i => i.SwColorStandardID).ToList();
                        }
                        if ("TYPE" == sortColumn.ToUpper() && "DESC" == sortDirection.ToUpper())
                        {
                            inventory = inventory.OrderByDescending(i => i.SwColorStandardID).ToList();
                        }
                    }
                }

                return inventory;
            }
            catch (SqlException ex)
            {
                throw;
            }
            finally
            {
                CloseConnection();
            }

        }
        public SoftwarePackage GetSoftwarePackage(string id)
        {
            OpenConnection();

            SoftwarePackage swPackg = null;
            string sql = $"";
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                command.CommandType = CommandType.Text;
                SqlDataReader dataReader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (dataReader.Read())
                {
                    swPackg = new SoftwarePackage
                    {
                    };
                }
                dataReader.Close();
            }
            return swPackg;
        }
        public bool AddSoftwarePackage(byte[] Swpackage, byte[] Swhelpdoc, string SwPkgVersion, string SwPkgDescription, int SwColorStandardID, string SwFileName, string SwFileFormat, long SwFileSize, string SwFileURL, string SwFileChecksum, string SwFileChecksumType, string SwCreatedBy, string SwManufacturer, string SwDeviceType, List<string> SupportedModels, string BlobDescription, string helDocFileName, string helpDocFileFormat, long? helpDocFileSize)
        {
            bool result = true;
            try
            {
                OpenConnection();

                // Execute using our connection.
                using (SqlCommand command = new SqlCommand("Inventory.usp_AddSoftwarePackage", _sqlConnection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter { ParameterName = "@Swpackage", SqlDbType = SqlDbType.VarBinary, Value = Swpackage });

                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwPkgUID", SqlDbType = SqlDbType.UniqueIdentifier, Value = Guid.NewGuid() });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwAddedDate", SqlDbType = SqlDbType.DateTime2, Value = DateTime.Now });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwPkgVersion", SqlDbType = SqlDbType.VarChar, Value = SwPkgVersion });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwPkgDescription", SqlDbType = SqlDbType.VarChar, Value = DBNull.Value });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwColorStandardID", SqlDbType = SqlDbType.Int, Value = SwColorStandardID });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileDetailsUID", SqlDbType = SqlDbType.UniqueIdentifier, Value = Guid.NewGuid() });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileName", SqlDbType = SqlDbType.VarChar, Value = SwFileName });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwModels", SqlDbType = SqlDbType.VarChar, Value = ConvertListToCommaSepartedString(SupportedModels) });

                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwManufacturer", SqlDbType = SqlDbType.VarChar, Value = SwManufacturer });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwDeviceType", SqlDbType = SqlDbType.VarChar, Value = SwDeviceType });

                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileFormat", SqlDbType = SqlDbType.VarChar, Value = SwFileFormat });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileSize", SqlDbType = SqlDbType.BigInt, Value = SwFileSize });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileURL", SqlDbType = SqlDbType.VarChar, Value = DBNull.Value });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileUploadDate", SqlDbType = SqlDbType.DateTime2, Value = DateTime.Now });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileChecksum", SqlDbType = SqlDbType.VarChar, Value = SwFileChecksum });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwFileChecksumType", SqlDbType = SqlDbType.VarChar, Value = SwFileChecksumType });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@SwCreatedBy", SqlDbType = SqlDbType.VarChar, Value = SwCreatedBy });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@BlobUID", SqlDbType = SqlDbType.UniqueIdentifier, Value = Guid.NewGuid() });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@BlobDescription", SqlDbType = SqlDbType.VarChar, Value = SwPkgDescription });

                    command.Parameters.Add(new SqlParameter { ParameterName = "@Swhelpdoc", SqlDbType = SqlDbType.VarBinary, Value = Swhelpdoc ?? new byte[0] });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@HdFileDetailsUID", SqlDbType = SqlDbType.UniqueIdentifier, Value = Guid.NewGuid() });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@HdFileName", SqlDbType = SqlDbType.VarChar, Value = helDocFileName });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@HdFileFormat", SqlDbType = SqlDbType.VarChar, Value = helpDocFileFormat });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@HdFileSize", SqlDbType = SqlDbType.BigInt, Value = helpDocFileSize ?? 0 });


                    command.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                // Log it to the log file.
                result = false; ;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public bool DeleteSoftwarePackage(List<Guid> packageIds, bool deleteAll)
        {

            try
            {
                OpenConnection();

                SqlCommand cmd = new SqlCommand("Inventory.usp_DeleteFirmware", _sqlConnection);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter { ParameterName = "@PackageIds", SqlDbType = SqlDbType.Structured, TypeName = "Inventory.PkgUidList", Value = GetDataTableFromList(packageIds) });
                cmd.Parameters.AddWithValue("@DeleteAll", deleteAll ? 1 : 0);

                cmd.ExecuteNonQuery();

                return true;
            }
            catch (SqlException ex)
            {
                return false;  // return error message
            }
            finally
            {
                CloseConnection();
            }
        }
        public byte[] GetHelpDoc(Guid key)
        {
            byte[] helpDoc = null;

            try
            {
                OpenConnection();

                using (SqlCommand command = new SqlCommand("Inventory.GetHelpDoc", _sqlConnection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter { ParameterName = "@Key", SqlDbType = SqlDbType.UniqueIdentifier, Value = key });

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            helpDoc = reader["HelpDoc"] as byte[];
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return helpDoc;
            }
            finally { CloseConnection(); }
            return helpDoc;
        }
        public List<CameraMakeModel> GetCameraModels()
        {
            List<CameraMakeModel> cameraMakeModels = new List<CameraMakeModel>();

            try
            {
                OpenConnection();

                using (SqlCommand command = new SqlCommand("Inventory.usp_GetCameraModels", _sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cameraMakeModels.Add(new CameraMakeModel
                            {
                                CamMake = reader["CAM_MAKE"] != null ? reader["CAM_MAKE"].ToString() : String.Empty,
                                CamModel = reader["CAM_MODEL"] != null ? reader["CAM_MODEL"].ToString() : String.Empty
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw;
            }
            finally { CloseConnection(); }
            return cameraMakeModels;
        }
        private DataTable GetDataTableFromList(List<Guid> guids)
        {
            DataTable table = new DataTable();
            table.Columns.Add("ID", typeof(Guid));
            foreach (Guid id in guids)
            {
                table.Rows.Add(id);
            }
            return table;
        }
        private string ConvertListToCommaSepartedString(List<string> lstStrings)
        {
            return String.Join(",", lstStrings);
        }
    }
}
