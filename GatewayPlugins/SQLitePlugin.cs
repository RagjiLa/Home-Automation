using Hub;
using Kernel;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace HubPlugins
{
    public class SqLitePlugin : IMultiSessionPlugin
    {
        private readonly Dictionary<string, FiniteBufferQue<SqlData>> _buffers;
        private readonly int _maxdataToBuffer;
        public SqLitePlugin(Dictionary<string, FiniteBufferQue<SqlData>> buffers,int maxdataToBuffer)
        {
            _buffers = buffers;
            _maxdataToBuffer = maxdataToBuffer;
        }

        public IMultiSessionPlugin Clone()
        {
            return new SqLitePlugin(_buffers, _maxdataToBuffer);
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
            return new byte[0];
        }

        public void PostResponseProcess(ISample requestSample, IEnumerable<byte> responseData, MessageBus communicationBus)
        {
            var sqlData = requestSample as SqlData;
            // ReSharper disable once PossibleNullReferenceException
            if (!_buffers.ContainsKey(sqlData.TableName)) _buffers.Add(sqlData.TableName, new FiniteBufferQue<SqlData>(_maxdataToBuffer));

            if (_buffers[sqlData.TableName].Enqueue(sqlData))
            {
                if (!File.Exists(sqlData.DatabaseFullPath))
                {
                    SQLiteConnection.CreateFile(sqlData.DatabaseFullPath);
                }

                using (SQLiteConnection sqlConnection = new SQLiteConnection(sqlData.DatabaseFullPath))
                {
                    sqlConnection.Open();
                    //Verify Table exists
                    using (SQLiteCommand sqlCreateTableCmd = new SQLiteCommand(sqlConnection))
                    {
                        string columnNames = string.Empty;
                        foreach (var val in sqlData.KeyValuePairData)
                        {
                            columnNames += val.Key + "NVARCHAR(255)  NULL,";
                        }
                        columnNames = columnNames.Remove(columnNames.Length - 1, 1);

                        sqlCreateTableCmd.CommandText = @"CREATE TABLE IF NOT EXISTS [MyTable] (
                          [ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," + columnNames + ")";
                        sqlCreateTableCmd.ExecuteNonQuery();
                    }
                    //Insert Data
                    using (SQLiteCommand sqlInsertCmd = new SQLiteCommand(sqlConnection))
                    {
                        SqlData overflowSample;
                        while (_buffers[sqlData.TableName].TryDequeue(out overflowSample))
                        {
                            string columnNames = "(", columnValues = "(";
                            foreach (var val in overflowSample.KeyValuePairData)
                            {
                                columnNames += "'" + val.Key + "',";
                                columnValues += "'" + val.Value + "',";
                            }
                            columnNames = columnNames.Remove(columnNames.Length - 1, 1) + ")";
                            columnValues = columnValues.Remove(columnValues.Length - 1, 1) + ")";

                            sqlInsertCmd.CommandText = "INSERT INTO " + overflowSample.TableName + " " + columnNames + " " + columnValues;
                            sqlInsertCmd.ExecuteNonQuery();
                        }
                    }
                    sqlConnection.Close();
                }

            }
        }
    }

    public class SqlData : ISample
    {
        private Dictionary<string, string> _kvpData;
        public SqlData(string databaseName, string table, Dictionary<string, string> keyValuePairData)
        {
            _kvpData = keyValuePairData;
            DatabaseFullPath = databaseName;
            TableName = table;
        }

        public SqlData()
        {

        }

        public string DatabaseFullPath
        {
            get { return _kvpData["D"]; }
            private set
            {
                if (_kvpData.ContainsKey("D"))
                    _kvpData.Add("D", "");
                _kvpData["D"] = value;
            }
        }
        public string TableName
        {
            get { return _kvpData["T"]; }
            private set
            {
                if (_kvpData.ContainsKey("T"))
                    _kvpData.Add("T", "");
                _kvpData["T"] = value;
            }
        }
        public Dictionary<string, string> KeyValuePairData
        {
            get
            {
                var data = _kvpData;
                data.Remove("T");
                data.Remove("D");
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
            if (!_kvpData.ContainsKey("D")) throw new InvalidDataException("Need database name in data");
            if (_kvpData.Count <= 2 ) throw new InvalidDataException("No data");
        }
    }
}
