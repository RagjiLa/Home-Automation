using Hub;
using Kernel;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace HubPlugins
{
    public class SqLitePlugin : ISingleSessionPlugin
    {
        private readonly Dictionary<string, FiniteBufferQue<SqlData>> _buffers;
        private readonly object _syncLockForBuffers = new object();
        private readonly int _maxdataToBuffer;
        private readonly string _databasePath;
        public SqLitePlugin(string databaseFullPath, Dictionary<string, FiniteBufferQue<SqlData>> buffers, int maxdataToBuffer)
        {
            _databasePath = databaseFullPath;
            _buffers = buffers;
            _maxdataToBuffer = maxdataToBuffer;
        }

        public PluginName Name
        {
            get { return PluginName.SqLitePlugin; }
        }

        public ISample AssociatedSample
        {
            get { return new SqlData(); }
        }

        public IEnumerable<byte> Respond(ISample sample)
        {
            try
            {
                var requestPacket = sample as SqlData;
                // ReSharper disable once PossibleNullReferenceException
                if (!_buffers.ContainsKey(requestPacket.TableName)) _buffers.Add(requestPacket.TableName, new FiniteBufferQue<SqlData>(_maxdataToBuffer));

                if (_buffers[requestPacket.TableName].Enqueue(requestPacket))
                {
                    PushToDatabase(_databasePath, _buffers[requestPacket.TableName]);
                }
                return new byte[0];
            }
            catch (Exception ex)
            {
                return Encoding.UTF8.GetBytes(ex.ToString());
            }
        }

        public void PostResponseProcess(ISample requestSample, IEnumerable<byte> responseData, MessageBus communicationBus)
        {

        }

        public void ShutDown()
        {
            foreach (var key in _buffers.Keys)
                PushToDatabase(_databasePath, _buffers[key]);
        }

        private void PushToDatabase(string databaseFullPath, FiniteBufferQue<SqlData> dataToPush)
        {
            if (!File.Exists(databaseFullPath))
            {
                SQLiteConnection.CreateFile(databaseFullPath);
            }

            using (SQLiteConnection sqlConnection = new SQLiteConnection("Data Source=" + databaseFullPath))
            {
                sqlConnection.Open();

                //Insert Data
                using (SQLiteCommand sqlInsertCmd = new SQLiteCommand(sqlConnection))
                {
                    SqlData overflowSample;
                    string tableName = string.Empty;
                    string allvalues = string.Empty;
                    string columnNames = string.Empty;
                    while (dataToPush.TryDequeue(out overflowSample))
                    {
                        tableName = overflowSample.TableName;
                        if (allvalues == string.Empty)
                        {
                            //Verify Table exists
                            using (SQLiteCommand sqlCreateTableCmd = new SQLiteCommand(sqlConnection))
                            {
                                string createTableColumnNames = string.Empty;
                                foreach (var val in overflowSample.KeyValuePairData)
                                {
                                    createTableColumnNames += val.Key + " NVARCHAR(255)  NULL,";
                                }
                                createTableColumnNames = createTableColumnNames.Remove(createTableColumnNames.Length - 1, 1);

                                sqlCreateTableCmd.CommandText = @"CREATE TABLE IF NOT EXISTS " + overflowSample.TableName + @" ( [ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," + createTableColumnNames + ")";
                                sqlCreateTableCmd.ExecuteNonQuery();
                            }
                        }

                        columnNames = "(";
                        allvalues += "(";
                        foreach (var val in overflowSample.KeyValuePairData)
                        {
                            columnNames += " '" + val.Key + "',";
                            allvalues += " '" + val.Value + "',";
                        }
                        columnNames = columnNames.Remove(columnNames.Length - 1, 1) + ")";
                        allvalues = allvalues.Remove(allvalues.Length - 1, 1) + "),";
                    }
                    allvalues = allvalues.Remove(allvalues.Length - 1, 1);
                    if (tableName != string.Empty)
                        sqlInsertCmd.CommandText = "INSERT INTO " + tableName + " " + columnNames + " VALUES " + allvalues;
                    sqlInsertCmd.ExecuteNonQuery();
                }
                sqlConnection.Close();
            }

        }
    }

    public class SqlData : ISample
    {
        private Dictionary<string, string> _kvpData;

        public SqlData(string table, Dictionary<string, string> keyValuePairData)
        {
            _kvpData = keyValuePairData;
            TableName = table;
        }

        public SqlData()
        {

        }

        public string TableName
        {
            get { return _kvpData["T"]; }
            private set
            {
                if (!_kvpData.ContainsKey("T"))
                    _kvpData.Add("T", "");
                _kvpData["T"] = value;
            }
        }

        public Dictionary<string, string> KeyValuePairData
        {
            get
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                foreach (var kvp in _kvpData)
                {
                    if (kvp.Key == "T") continue;
                    data.Add(kvp.Key, kvp.Value);
                }
                return data;
            }
        }

        public IDictionary<string, string> ToKeyValuePair()
        {
            return _kvpData;
        }

        public void FromKeyValuePair(IDictionary<string, string> kvpData)
        {
            _kvpData = new Dictionary<string, string>(kvpData);
            if (!_kvpData.ContainsKey("T")) throw new InvalidDataException("Need table in data");
            if (_kvpData.Count <= 1) throw new InvalidDataException("No data");
        }
    }
}
