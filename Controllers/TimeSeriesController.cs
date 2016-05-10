using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Web.Http;

namespace Controllers
{
    public class TimeSeriesController : ApiController
    {
        public string DataBaseFullPath { get; set; }

        public TimeSeriesController()
        {
            string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

            DataBaseFullPath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");
        }

        [HttpPost]
        [Route("TimeSeries/{DeviceName}")]
        public IHttpActionResult Post(string DeviceName, [FromBody]Dictionary<string, string> timeseriesData)
        {
            try
            {
                if (timeseriesData.Values.Count > 0)
                {
                    var failures = InsertIntoDatabse(DataBaseFullPath, DeviceName, timeseriesData);
                    if (failures.Count > 0)
                    {
                        var str = string.Empty;
                        foreach (var fail in failures) str += fail + ", ";
                        return Content(System.Net.HttpStatusCode.Conflict, "Failed some values already exists in database (" + str + ")");
                    }
                    else
                        return Ok();
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.BadRequest, "Need values to insert.");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("TimeSeries/{DeviceName}/{time}/{value}")]
        public IHttpActionResult Post(string DeviceName, string time, string value)
        {
            return Post(DeviceName, new Dictionary<string, string>() { { time, value } });
        }

        [HttpGet]
        [Route("TimeSeries/{DeviceName}/{timeStamp}")]
        public IHttpActionResult GetSingle(string DeviceName, string timeStamp)
        {
            try
            {
                var value = string.Empty;
                if (QueryTable(DeviceName, timeStamp, out value))
                    return Ok<string>(value);
                else
                    return Content(System.Net.HttpStatusCode.NotFound, "Key doesnot exits");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //[NonAction]
        //public IHttpActionResult GetMany(string uniqueName, [FromBody]List<string> timestamps)
        //{
        //    try
        //    {
        //        if (timestamps.Count > 0)
        //        {
        //            string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

        //            dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");
        //            var results = QueryTable(dataBasePath, uniqueName, timestamps);
        //            return Ok<Dictionary<string, string>>(results);
        //        }
        //        else
        //        {
        //            return Content(System.Net.HttpStatusCode.BadRequest, "Should contain one or more time");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}

        //[NonAction]
        ////[Route("TimeSeries/Recent/{uniqueName}/{recentCount}")]
        //public IHttpActionResult GetRecent(string uniqueName, int recentCount)
        //{
        //    try
        //    {
        //        if (recentCount > 0)
        //        {
        //            string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

        //            dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");
        //            var results = QueryTable(dataBasePath, uniqueName, recentCount);
        //            return Ok<Dictionary<string, string>>(results);
        //        }
        //        else
        //        {
        //            return Content(System.Net.HttpStatusCode.BadRequest, "Recent Count has to be a positive number, and cannot be " + recentCount);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}

        [HttpDelete]
        [Route("TimeSeries/{DeviceName}/{timeStamp}")]
        public IHttpActionResult DeleteByTimestamp(string DeviceName, string timestamp)
        {
            try
            {
                DeleteRow(DeviceName, timestamp);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("TimeSeries/Devices")]
        public IHttpActionResult GetDevices(string uniqueName, string timeStamp)
        {
            try
            { 
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    var returnValue = new List<String>();
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                    using (var dt = QueryDatabaseWithWithReader(string.Empty, command))
                    {
                        if (dt.Rows.Count <= 0) return Content(System.Net.HttpStatusCode.NoContent, "");
                        foreach (DataRow row in dt.Rows)
                        {
                            var tableName = row[0].ToString();
                            returnValue.Add(tableName.Split("_".ToCharArray())[0]);
                        }

                    }
                    return Ok(returnValue);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("TimeSeries/Tags/{DeviceName}")]
        public IHttpActionResult GetDeviceTags(string DeviceName)
        {
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        private string CreateDatabaseIfNotExists(string databaseDirectoryFullPath, string filename)
        {
            if (!Directory.Exists(databaseDirectoryFullPath)) Directory.CreateDirectory(databaseDirectoryFullPath);
            var databaseFullPath = Path.Combine(databaseDirectoryFullPath, filename);
            if (!File.Exists(databaseFullPath))
            {
                SQLiteConnection.CreateFile(databaseFullPath);
            }
            return databaseFullPath;
        }

        private void CreateTableIfNotEsists(string tableName, SQLiteConnection sqlConnection)
        {
            //Verify Table exists
            using (SQLiteCommand sqlCreateTableCmd = new SQLiteCommand(sqlConnection))
            {
                sqlCreateTableCmd.CommandText = @"CREATE TABLE IF NOT EXISTS [" + tableName + "] ( [ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, [Time] TEXT NOT NULL, [Value] NVARCHAR(255), [Flags] INT)";
                sqlCreateTableCmd.ExecuteNonQuery();
            }
        }

        private List<string> InsertIntoDatabse(string databaseFullPath, string tableName, Dictionary<string, string> timeseriesData)
        {
            var returnResult = new List<string>();
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(tableName, sqlConnection);
                //Insert Data
                foreach (var row in timeseriesData)
                {
                    if (GetSingle(tableName, row.Key).ExecuteAsync(CancellationToken.None).Result.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        if (DeleteByTimestamp(tableName, row.Key).ExecuteAsync(CancellationToken.None).Result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            returnResult.Add(row.Key);
                            continue;
                        }
                    }

                    using (SQLiteCommand sqlInsertCmd = new SQLiteCommand(sqlConnection))
                    {
                        sqlInsertCmd.CommandText = "INSERT INTO [" + tableName + "] ([Time],[Value],[FLAGS])  VALUES (@timeLong, @data,0)";
                        var timeLong = new SQLiteParameter("@timeLong");
                        var data = new SQLiteParameter("@data");
                        timeLong.Value = row.Key;
                        data.Value = row.Value;
                        sqlInsertCmd.Parameters.Add(timeLong);
                        sqlInsertCmd.Parameters.Add(data);
                        sqlInsertCmd.ExecuteNonQuery();
                    }
                }
                sqlConnection.Close();
            }
            return returnResult;
        }

        private bool QueryTable(string tableName, string queryTime, out string value)
        {
            value = string.Empty;
            using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand())
            {
                sqlQueryTableCmd.CommandText = "select [value] from [" + tableName + "] where [FLAGS]==0 and [Time] == @timeLong";
                var timeLong = new SQLiteParameter("@timeLong");
                timeLong.Value = queryTime;
                sqlQueryTableCmd.Parameters.Add(timeLong);
                using (var dt = QueryDatabaseWithWithReader(tableName, sqlQueryTableCmd))
                {
                    if (dt.Rows.Count <= 0) return false;
                    foreach (DataRow row in dt.Rows)
                    {
                        value = (string)row[0];
                    }
                }
                return true;
            }
        }

        //private Dictionary<string, string> QueryTable( string tableName, List<string> alltimes)
        //{
        //    var returnValue = new Dictionary<string, string>();
        //    var stringValue = string.Empty;
        //    foreach (var time in alltimes) stringValue += " " + time + " ,";
        //    stringValue = stringValue.Remove(stringValue.Length - 1, 1);
        //    using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand())
        //    {
        //        sqlQueryTableCmd.CommandText = "select [Time] , [value] from [" + tableName + "] where [FLAGS]==0 and [Time] in ( " + stringValue + " )";
        //        using (var dt = QueryDatabaseWithWithReader(tableName, sqlQueryTableCmd))
        //        {
        //            foreach (DataRow row in dt.Rows)
        //            {
        //                returnValue.Add((string)row[0], (string)row[1]);
        //            }
        //        }
        //        return returnValue;
        //    }
        //}

        //private Dictionary<string, string> QueryTable(string tableName, int recentCount)
        //{
        //    var returnValue = new Dictionary<string, string>();

        //    using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand())
        //    {
        //        sqlQueryTableCmd.CommandText = "select [Time] , [value] from [" + tableName + "] where [FLAGS]==0 LIMIT @limit  ";
        //        var limitCount = new SQLiteParameter("limit");
        //        limitCount.Value = recentCount;
        //        sqlQueryTableCmd.Parameters.Add(limitCount);
        //        using (var dt = QueryDatabaseWithWithReader(tableName, sqlQueryTableCmd))
        //        {
        //            foreach (DataRow row in dt.Rows)
        //            {
        //                returnValue.Add((string)row[0], (string)row[1]);
        //            }
        //        }
        //        return returnValue;

        //    }
        //}

        private void DeleteRow(string tableName, string timestamp)
        {
            using (var sqlQueryTableCmd = new SQLiteCommand())
            {
                sqlQueryTableCmd.CommandText = "Update [" + tableName + "] set flags=1 where [Time]==@timeStamp";
                var timeLong = new SQLiteParameter("timeStamp");
                timeLong.Value = timestamp;
                sqlQueryTableCmd.Parameters.Add(timeLong);
                using (var dt = QueryDatabaseWithWithReader(tableName, sqlQueryTableCmd)) { }
            }
        }

        private DataTable QueryDatabaseWithWithReader(string tableName, SQLiteCommand command)
        {
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + DataBaseFullPath))
            {
                using (command)
                {
                    command.Connection = sqlConnection;
                    sqlConnection.Open();
                    if (String.IsNullOrEmpty(tableName)) CreateTableIfNotEsists(tableName, sqlConnection);
                    using (var reader = command.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        reader.Close();
                        sqlConnection.Close();
                        return dataTable;
                    }
                }
            }
        }


    }
}

