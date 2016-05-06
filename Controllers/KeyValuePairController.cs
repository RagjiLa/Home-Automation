using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Controllers
{
    public class KeyValuePairController : ApiController
    {
        public IHttpActionResult Post([FromBody]Dictionary<string, string> timeseriesData)
        {
            try
            {
                if (timeseriesData.Values.Count > 0)
                {
                    string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                    dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "KeyvalueStore.db3");

                    var failures = InsertIntoDatabse(dataBasePath, timeseriesData);
                    if (failures.Count > 0)
                    {
                        var str = string.Empty;
                        foreach (var fail in failures) str += fail.ToString() + ", ";
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

        public IHttpActionResult GetSingle(string key)
        {
            try
            {
                string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "KeyvalueStore.db3");
                var value = string.Empty;
                if (QueryTable(dataBasePath, key, out value))
                    return Ok<string>(value);
                else
                    return Content(System.Net.HttpStatusCode.NotFound, "Key doesnot exits");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public IHttpActionResult GetMany(List<string> keys)
        {
            try
            {
                if (keys.Count > 0)
                {
                    string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                    dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "KeyvalueStore.db3");
                    var results = QueryTable(dataBasePath, keys);
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

        public IHttpActionResult Delete(string key)
        {
            try
            {
                string dataBasePath = Path.Combine(Environment.CurrentDirectory, "Stores");

                dataBasePath = CreateDatabaseIfNotExists(dataBasePath, "KeyvalueStore.db3");
                DeleteRow(dataBasePath, key);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public IHttpActionResult Put(KeyValuePair<string, string> keyvaluePair)
        {
            try
            {
                var result = GetSingle(keyvaluePair.Key).ExecuteAsync(CancellationToken.None).Result;
                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Content(System.Net.HttpStatusCode.NotFound, "Key doesnot exists");
                }
                else
                {
                    result = Delete(keyvaluePair.Key).ExecuteAsync(CancellationToken.None).Result;
                    var insertVal = new Dictionary<string, string>();
                    insertVal.Add(keyvaluePair.Key, keyvaluePair.Value);
                    result = Post(insertVal).ExecuteAsync(CancellationToken.None).Result;
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        return Ok();
                    else
                        return Content(result.StatusCode, result.ReasonPhrase);
                }
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

        private void CreateTableIfNotEsists(SQLiteConnection sqlConnection)
        {
            //Verify Table exists
            using (SQLiteCommand sqlCreateTableCmd = new SQLiteCommand(sqlConnection))
            {
                sqlCreateTableCmd.CommandText = @"CREATE TABLE IF NOT EXISTS KeyValues ( [ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, [Time] TEXT NOT NULL, [Value] NVARCHAR(255), [Flags] INT)";
                sqlCreateTableCmd.ExecuteNonQuery();
            }
        }

        private List<string> InsertIntoDatabse(string databaseFullPath, Dictionary<string, string> keyvaluePairs)
        {
            var returnResult = new List<string>();
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(sqlConnection);
                //Insert Data
                foreach (var row in keyvaluePairs)
                {
                    if (GetSingle(row.Key).ExecuteAsync(CancellationToken.None).Result.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        returnResult.Add(row.Key);
                        continue;
                    }

                    using (SQLiteCommand sqlInsertCmd = new SQLiteCommand(sqlConnection))
                    {
                        sqlInsertCmd.CommandText = "INSERT INTO KeyValues ([Time],[Value],[FLAGS])  VALUES (@timeLong, @data,0)";
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

        private bool QueryTable(string databaseFullPath, string queryKey, out string value)
        {
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(sqlConnection);
                using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand(sqlConnection))
                {
                    sqlQueryTableCmd.CommandText = "select [value] from KeyValues where [FLAGS] == 0 and [Time] == @timeLong";
                    var timeLong = new SQLiteParameter("@timeLong");
                    timeLong.Value = queryKey;
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

        private Dictionary<string, string> QueryTable(string databaseFullPath, List<string> keys)
        {
            var returnValue = new Dictionary<string, string>();
            var stringValue = string.Empty;
            foreach (var time in keys) stringValue += " " + time + " ,";
            stringValue = stringValue.Remove(stringValue.Length - 1, 1);
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(sqlConnection);
                using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand(sqlConnection))
                {
                    sqlQueryTableCmd.CommandText = "select [Time] , [value] from KeyValues where [FLAGS]==0 and [Time] in ( " + stringValue + " )";

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

        private void DeleteRow(string databaseFullPath, string key)
        {
            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();
                CreateTableIfNotEsists(sqlConnection);
                using (SQLiteCommand sqlQueryTableCmd = new SQLiteCommand(sqlConnection))
                {
                    sqlQueryTableCmd.CommandText = "Update KeyValues set flags=1 where [Time]==@timeStamp";
                    var timeLong = new SQLiteParameter("timeStamp");
                    timeLong.Value = key;
                    sqlQueryTableCmd.Parameters.Add(timeLong);

                    sqlQueryTableCmd.ExecuteScalar();
                    sqlConnection.Close();
                }
            }
        }
    }
}
