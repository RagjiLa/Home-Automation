using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Web.Http;

namespace Controllers
{
    public class TimeSeriesController : ApiController
    {
        public IHttpActionResult Post(string uniqueName, [FromBody]Dictionary<string, string> timeseriesData)
        {
            try
            {
                if (timeseriesData.Values.Count > 0)
                {
                    string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                    dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");

                    var failures = InsertIntoDatabse(dataBasePath, uniqueName, timeseriesData);
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

        public IHttpActionResult GetSingle(string uniqueName, string timeStamp)
        {
            try
            {
                string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");
                var value = string.Empty;
                if (QueryTable(dataBasePath, uniqueName, timeStamp, out value))
                    return Ok<string>(value);
                else
                    return Content(System.Net.HttpStatusCode.NotFound, "Key doesnot exits");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public IHttpActionResult GetMany(string uniqueName, List<string> selectiveTimes)
        {
            try
            {
                if (selectiveTimes.Count > 0)
                {
                    string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                    dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");
                    var results = QueryTable(dataBasePath, uniqueName, selectiveTimes);
                    return Ok<Dictionary<string, string>>(results);
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.BadRequest, "Should contain one or more time");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public IHttpActionResult GetRecent(string uniqueName, int recentCount)
        {
            try
            {
                if (recentCount > 0)
                {
                    string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                    dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");
                    var results = QueryTable(dataBasePath, uniqueName, recentCount);
                    return Ok<Dictionary<string, string>>(results);
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.BadRequest, "Recent Count has to be a positive number, and cannot be " + recentCount);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public IHttpActionResult DeleteByTimestamp(string uniqueName, string timestamp)
        {
            try
            {
                string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "TimeseriesStore.db3");
                DeleteRow(dataBasePath, uniqueName, timestamp);
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
                sqlCreateTableCmd.CommandText = @"CREATE TABLE IF NOT EXISTS " + tableName + " ( [ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, [Time] TEXT NOT NULL, [Value] NVARCHAR(255), [Flags] INT)";
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
                        sqlInsertCmd.CommandText = "INSERT INTO Timeseries ([Time],[Value],[FLAGS])  VALUES (@timeLong, @data,0)";
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

        private bool QueryTable(string databaseFullPath, string tableName, string queryTime, out string value)
        {
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(tableName, sqlConnection);
                using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand(sqlConnection))
                {
                    sqlQueryTableCmd.CommandText = "select [value] from Timeseries where [FLAGS]==0 and [Time] == @timeLong";
                    var timeLong = new SQLiteParameter("@timeLong");
                    timeLong.Value = queryTime;
                    sqlQueryTableCmd.Parameters.Add(timeLong);

                    value = string.Empty;
                    bool rowsPresent = false;
                    using (var dataReader = sqlQueryTableCmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            rowsPresent = true;
                            value = (string)dataReader[0];
                        }
                        dataReader.Close();
                    }
                    sqlConnection.Close();
                    return rowsPresent;
                }
            }
        }

        private Dictionary<string, string> QueryTable(string databaseFullPath, string tableName, List<string> alltimes)
        {
            var returnValue = new Dictionary<string, string>();
            var stringValue = string.Empty;
            foreach (var time in alltimes) stringValue += " " + time + " ,";
            stringValue = stringValue.Remove(stringValue.Length - 1, 1);
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(tableName, sqlConnection);
                using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand(sqlConnection))
                {
                    sqlQueryTableCmd.CommandText = "select [Time] , [value] from Timeseries where [FLAGS]==0 and [Time] in ( " + stringValue + " )";

                    using (var dataReader = sqlQueryTableCmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            returnValue.Add((string)dataReader[0], (string)dataReader[1]);
                        }
                        dataReader.Close();
                    }
                    sqlConnection.Close();
                    return returnValue;
                }
            }
        }

        private Dictionary<string, string> QueryTable(string databaseFullPath, string tableName, int recentCount)
        {
            var returnValue = new Dictionary<string, string>();
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(tableName, sqlConnection);
                using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand(sqlConnection))
                {
                    sqlQueryTableCmd.CommandText = "select [Time] , [value] from Timeseries where [FLAGS]==0 LIMIT @limit  ";
                    var limitCount = new SQLiteParameter("limit");
                    limitCount.Value = recentCount;
                    sqlQueryTableCmd.Parameters.Add(limitCount);

                    using (var dataReader = sqlQueryTableCmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            returnValue.Add((string)dataReader[0], (string)dataReader[1]);
                        }
                        dataReader.Close();
                    }
                    sqlConnection.Close();
                    return returnValue;
                }
            }
        }

        private void DeleteRow(string databaseFullPath, string tableName, string timestamp)
        {
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(tableName, sqlConnection);
                using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand(sqlConnection))
                {
                    sqlQueryTableCmd.CommandText = "Update " + tableName + " set flags=1 where [Time]==@timeStamp";
                    var timeLong = new SQLiteParameter("timeStamp");
                    timeLong.Value = timestamp;
                    sqlQueryTableCmd.Parameters.Add(timeLong);

                    sqlQueryTableCmd.ExecuteScalar();
                    sqlConnection.Close();
                }
            }
        }
    }
}

