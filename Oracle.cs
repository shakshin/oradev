using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;
using System.Threading;

namespace oradev
{
    public delegate void ProcessQueryResult(DataTable result, long elapsed);
    public delegate void ProcessGetObjectsResult(ObservableCollection<DBObject> result);
    public delegate void ProcessExecuteResult(long elapsed);
    public class Oracle
    {
        

        private static String ConnectionString(DataBaseConfig config)
        {
            return string.Format("Data Source={0};Password={1};User ID={2};Unicode=true", config.DataBaseAlias, config.DataBasePassword, config.DataBaseUser);
        }

        public static OracleConnection GetConnection(DataBaseConfig config)
        {
            return new OracleConnection(ConnectionString(config));
        }

        public static void QueryAsync(String text, ProcessQueryResult callback, DataBaseConfig config, OracleConnection existingConnection = null)
        {
            if (config == null)
            {
                App.Current.Dispatcher.Invoke((Action)delegate {
                    callback(new DataTable(), 0);
                });
                return;
            }
            new Thread(delegate() {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                DataTable result = Query(text, config, existingConnection);
                sw.Stop();
                App.Current.Dispatcher.Invoke((Action)delegate {
                    callback(result, sw.ElapsedMilliseconds);
                });
            }).Start();
        }
        
        public static DataTable Query(String text, DataBaseConfig config, OracleConnection existingConnection = null, OracleTransaction tran = null)
        {
            int Counter = 0;

            DataTable result = new DataTable();
            if (config == null && existingConnection == null) return result;
            OracleConnection oracle = existingConnection == null
                    ? new OracleConnection(ConnectionString(config))
                    : existingConnection;
            try
            {
                if (oracle.State != ConnectionState.Open) oracle.Open();
                using (OracleCommand command = new OracleCommand())
                {
                    command.Connection = oracle;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = text;
                    if (tran != null) command.Transaction = tran;

                    OracleDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if (reader.FieldCount > result.Columns.Count)
                            {
                                for (int i = result.Columns.Count; i < reader.FieldCount; i++)
                                {
                                    result.Columns.Add(reader.GetName(i));

                                }
                            }

                            DataRow row = result.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    Type type = reader.GetFieldType(i);
                                    switch (type.Name)
                                    {
                                        case "Decimal":
                                            Decimal d = reader.GetDecimal(i);
                                            row[result.Columns[i]] = d.ToString();
                                            break;
                                        case "String":
                                            String s = reader.GetString(i);
                                            row[result.Columns[i]] = s;
                                            break;
                                        case "DateTime":
                                            DateTime dt = reader.GetDateTime(i);
                                            row[result.Columns[i]] = dt.ToString();
                                            break;
                                        default:
                                            row[result.Columns[i]] = "{" + type.Name + "}";
                                            break;
                                    }
                                }
                                else
                                {
                                    row[result.Columns[i]] = "{null}";
                                }
                            }

                            result.Rows.Add(row);
                            Counter++;
                            if (Counter == 10000)
                            {
                                //Console.Log("WARNING: query output limited to 10000 rows");
                                break;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
            }
            finally
            {
                if (existingConnection == null) oracle.Dispose();
            }
            return result;
        }

        public static void GetPackagesAsync(String query, ProcessGetObjectsResult callback, DataBaseConfig config)
        {
            if (config == null)
            {
                callback(new ObservableCollection<DBObject>());
                return;
            }
            String sql = "";

            if (string.IsNullOrEmpty(query))
            {
                sql = @"
SELECT 
    OBJECT_NAME, 
    OBJECT_TYPE,
    (SELECT COUNT(*) FROM USER_OBJECTS O2 WHERE O2.OBJECT_TYPE='PACKAGE' AND OBJECT_NAME=O1.OBJECT_NAME AND STATUS='INVALID') AS INVALIDHEAD,
    (SELECT COUNT(*) FROM USER_OBJECTS O2 WHERE O2.OBJECT_TYPE='PACKAGE BODY' AND OBJECT_NAME=O1.OBJECT_NAME AND STATUS='INVALID') AS INVALIDBODY
FROM USER_OBJECTS O1 WHERE OBJECT_TYPE = 'PACKAGE' ORDER BY OBJECT_NAME";
            }
            else
            {
                sql = string.Format(@"
SELECT 
    OBJECT_NAME, 
    OBJECT_TYPE,
    (SELECT COUNT(*) FROM USER_OBJECTS O2 WHERE O2.OBJECT_TYPE='PACKAGE' AND OBJECT_NAME=O1.OBJECT_NAME AND STATUS='INVALID') AS INVALIDHEAD,
    (SELECT COUNT(*) FROM USER_OBJECTS O2 WHERE O2.OBJECT_TYPE='PACKAGE BODY' AND OBJECT_NAME=O1.OBJECT_NAME AND STATUS='INVALID') AS INVALIDBODY
FROM USER_OBJECTS O1 WHERE OBJECT_TYPE = 'PACKAGE' AND OBJECT_NAME LIKE '{0}%' ORDER BY OBJECT_NAME", query);
            }
            QueryAsync(sql, delegate (DataTable result, long elapsed) {
                ObservableCollection<DBObject> res = new ObservableCollection<DBObject>();
                for (int row = 0; row < result.Rows.Count; row++)
                {
                    res.Add(new DBObject() {
                        Name = result.Rows[row][0].ToString(),
                        Type = result.Rows[row][1].ToString(),
                        IsInvalidHead = result.Rows[row][2].ToString() == "1",
                        IsInvalidBody = result.Rows[row][3].ToString() == "1",
                        IsInvalid = result.Rows[row][2].ToString() == "1" || result.Rows[row][3].ToString() == "1",
                        Description = 
                            "Package " + result.Rows[row][0].ToString()
                            + (result.Rows[row][2].ToString() == "1" ? "\nHead is invalid" : "") 
                            + (result.Rows[row][3].ToString() == "1" ? "\nBody is invalid" : "")
                            + (result.Rows[row][2].ToString() != "1" && result.Rows[row][3].ToString() != "1" ? "\nValid" : "")
                    });
                }
                callback(res);
            }, config);
        }

        public static void GetTablesAsync(String query, ProcessGetObjectsResult callback, DataBaseConfig config)
        {
            if (config == null)
            {
                callback(new ObservableCollection<DBObject>());
                return;
            }
            String sql = "";

            if (string.IsNullOrEmpty(query))
            {
                sql = @"SELECT OBJECT_NAME, OBJECT_TYPE FROM USER_OBJECTS WHERE OBJECT_TYPE = 'TABLE' ORDER BY OBJECT_NAME";
            }
            else
            {
                sql = string.Format(@"SELECT OBJECT_NAME, OBJECT_TYPE FROM USER_OBJECTS WHERE OBJECT_TYPE = 'TABLE' AND OBJECT_NAME LIKE '{0}%' ORDER BY OBJECT_NAME", query);
            }
            QueryAsync(sql, delegate (DataTable result, long elapsed) {
                ObservableCollection<DBObject> res = new ObservableCollection<DBObject>();
                for (int row = 0; row < result.Rows.Count; row++)
                {
                    res.Add(new DBObject() {
                        Name = result.Rows[row][0].ToString(),
                        Type = result.Rows[row][1].ToString(),
                        IsInvalid = false,
                        IsInvalidBody = false,
                        IsInvalidHead = false
                    });
                }
                callback(res);
            }, config);
        }

        public static void GetObjectsAsync(String query, ProcessGetObjectsResult callback, DataBaseConfig config)
        {
            if (config == null)
            {
                callback(new ObservableCollection<DBObject>());
                return;
            }
            String sql = "";
            
            if (string.IsNullOrEmpty(query))
            {
                sql = @"SELECT OBJECT_NAME, OBJECT_TYPE FROM USER_OBJECTS WHERE OBJECT_TYPE IN ('TABLE', 'PACKAGE') ORDER BY OBJECT_NAME";
            }
            else
            {
                sql = string.Format(@"SELECT OBJECT_NAME, OBJECT_TYPE FROM USER_OBJECTS WHERE OBJECT_TYPE IN ('TABLE', 'PACKAGE') AND OBJECT_NAME LIKE '{0}%' ORDER BY OBJECT_NAME", query);
            }
            QueryAsync(sql, delegate(DataTable result, long elapsed) {
                ObservableCollection<DBObject> res = new ObservableCollection<DBObject>();
                for (int row = 0; row < result.Rows.Count; row++) {
                    res.Add(new DBObject() { Name = result.Rows[row][0].ToString(), Type = result.Rows[row][1].ToString() });
                }
                callback(res);
            }, config);
        }

        private static ObservableCollection<DBObject> GetObjects(String query, DataBaseConfig config)
        {
            ObservableCollection<DBObject> objects = new ObservableCollection<DBObject>();
            if (config == null) return objects;
            String sql = "";
            if (string.IsNullOrEmpty(query))
            {
                sql = @"SELECT OBJECT_NAME, OBJECT_TYPE FROM USER_OBJECTS WHERE OBJECT_TYPE IN ('TABLE', 'PACKAGE') ORDER BY OBJECT_NAME";
            }
            else
            {
                sql = string.Format(@"SELECT OBJECT_NAME, OBJECT_TYPE FROM USER_OBJECTS WHERE OBJECT_TYPE IN ('TABLE', 'PACKAGE') AND OBJECT_NAME LIKE '{0}%' ORDER BY OBJECT_NAME", query);
            }
            try
            {
                using (OracleConnection oracle = new OracleConnection(ConnectionString(config)))
                {
                    oracle.Open();

                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = oracle;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = sql;

                        OracleDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                DBObject obj = new DBObject();
                                obj.Name = reader.GetString(0);
                                obj.Type = reader.GetString(1);
                                objects.Add(obj);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
            }

            return objects;
        }

        public static String GetTable(String tableName, DataBaseConfig config)
        {
            if (config == null) return "";
            try
            {
                using (OracleConnection oracle = new OracleConnection(ConnectionString(config)))
                {
                    oracle.Open();
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = oracle;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = string.Format(@"select dbms_metadata.get_ddl('TABLE', '{0}') from dual", tableName);

                        OracleDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            String text = "";
                            while (reader.Read())
                            {
                                text = text + reader.GetString(0) + ";\r\n";
                            }

                            using (OracleCommand command2 = new OracleCommand())
                            {
                                command.Connection = oracle;
                                command.CommandType = System.Data.CommandType.Text;
                                command.CommandText = string.Format("select dbms_metadata.get_ddl('INDEX', index_name, owner) from all_indexes where table_name = '{0}' and table_owner = '{1}'", tableName, config.DataBaseUser.ToUpper());
                                OracleDataReader reader2 = command.ExecuteReader();
                                if (reader2.HasRows)
                                {
                                    while (reader2.Read())
                                    {
                                        text += string.Format("\r\n{0};", reader2.GetString(0));
                                    }
                                }
                            }


                            return text;
                        }
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
                return "";
            }
        }

        public static String GetPackageHead(String packageName, DataBaseConfig config)
        {
            if (config == null) return "";
            try
            {
                using (OracleConnection oracle = new OracleConnection(ConnectionString(config)))
                {
                    oracle.Open();
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = oracle;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = string.Format(@"select text from user_source where name = '{0}' and type='PACKAGE'", packageName);

                        OracleDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            String text = "create or replace ";
                            while (reader.Read())
                            {
                                text = text + reader.GetString(0);
                            }
                            return text;
                        }
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
                return "";
            }
        }

        public static String GetPackageBody(String packageName, DataBaseConfig config)
        {
            if (config == null) return "";
            try
            {
                using (OracleConnection oracle = new OracleConnection(ConnectionString(config)))
                {
                    oracle.Open();
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = oracle;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = string.Format(@"select text from user_source where name = '{0}' and type='PACKAGE BODY'", packageName);

                        OracleDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            String text = "create or replace ";
                            while (reader.Read())
                            {
                                text += reader.GetString(0);
                            }
                            return text;
                        }
                        return "";
                    }
                }
            } 
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
                return "";
            }
        }

        public static void ExecuteAsync(String text, ProcessExecuteResult callback, DataBaseConfig config, OracleConnection existingConnection = null)
        {
            if (config == null)
            {
                callback(0);
                return;
            }
            new Thread(delegate() {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Execute(text, config, existingConnection);
                sw.Stop();
                App.Current.Dispatcher.Invoke((Action) delegate {
                    callback(sw.ElapsedMilliseconds);
                });
            }).Start();
        }
        public static void Execute(String text, DataBaseConfig config, OracleConnection existingConnection = null, OracleTransaction tran = null)
        {
            if (config == null) return;
            OracleConnection oracle = existingConnection == null
                ? new OracleConnection(ConnectionString(config))
                : existingConnection;
            try
            {
                if (oracle.State != ConnectionState.Open) oracle.Open();
                using (OracleCommand command = new OracleCommand())
                {
                    command.Connection = oracle;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = text;
                    if (tran != null) command.Transaction = tran;

                    int rows = command.ExecuteNonQuery();

                    Console.Log("Execution complete.");

                    if (rows > 0)
                    {
                        Console.Log(string.Format("{0} rows affected.", rows));
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
            }
            finally
            {
                if (existingConnection == null) oracle.Dispose();
            }

        }

        public static ObservableCollection<SourceError> CheckErrors(String type, String name, DataBaseConfig config) 
        {
            ObservableCollection<SourceError> errors = new ObservableCollection<SourceError>();
            if (config == null) return errors;
            try
            {
                using (OracleConnection oracle = new OracleConnection(ConnectionString(config)))
                {
                    oracle.Open();
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = oracle;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = @"select line, text from user_errors where type='" + type + "' and name='" + name + "'";

                        OracleDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int line = reader.GetInt32(0);
                                string text = reader.GetString(1).Replace("\n", " ");
                                SourceError err = new SourceError()
                                {
                                    LineNumber = line,
                                    Message = text
                                };
                                errors.Add(err);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
            }
            return errors;
        }

        public static ObservableCollection<DBSession> GetLockers(String name, DataBaseConfig config)
        {
            ObservableCollection<DBSession> result = new ObservableCollection<DBSession>();
            if (config == null) return result;

            DataTable data = Query(string.Format(
                @"select distinct l.session_id, s.username, s.serial#
                    from dba_ddl_locks l, v$session s 
                    where 
                        s.sid = l.session_id 
                        and l.name = '{0}'",
                name.ToUpper()
            ), config);

            foreach (DataRow row in data.Rows)
            {
                result.Add(new DBSession() { Id = row[0].ToString(), User = row[1].ToString(), Serial = row[2].ToString()});
            }

            return result;
        }

        public static void KillSession(String Id, String Serial, DataBaseConfig config)
        {
            if (config == null) return;
            new Thread(delegate()
            {
                try
                {
                    using (OracleConnection oracle = new OracleConnection(ConnectionString(config)))
                    {
                        oracle.Open();
                        using (OracleCommand command = new OracleCommand())
                        {
                            command.Connection = oracle;
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = string.Format("alter system kill session '{0},{1}'", Id, Serial);

                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Log(ex.Message.Replace("\n", " "));
                }
            }).Start();
        }

        public static void KillSessions(List<DBSession> sessions, DataBaseConfig config)
        {
            if (config == null) return;
            new Thread(delegate()
            {
                try
                {
                    using (OracleConnection oracle = new OracleConnection(ConnectionString(config)))
                    {
                        oracle.Open();
                        foreach (DBSession session in sessions)
                        {
                            using (OracleCommand command = new OracleCommand())
                            {
                                command.Connection = oracle;
                                command.CommandType = System.Data.CommandType.Text;
                                command.CommandText = string.Format("alter system disconnect session '{0},{1}' immediate", session.Id, session.Serial);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Log(ex.Message.Replace("\n", " "));
                }
            }).Start();
        }

        public static DataTable ExplainPlan(String text, DataBaseConfig config, OracleConnection existingConnection = null)
        {
            DataTable result = new DataTable();
            
            result.Columns.Add("Operation");
            result.Columns.Add("Options");
            result.Columns.Add("Object type");
            result.Columns.Add("Object name");
            result.Columns.Add("Cost");
            result.Columns.Add("Rows");
            result.Columns.Add("Bytes");
            result.Columns.Add("Optimizer");

            if (config == null) return result;

            String stid = DateTime.Now.ToString("o").Replace(":", "").Replace(".", "").Replace("-","");
            OracleConnection oracle = existingConnection == null
                ? new OracleConnection(ConnectionString(config))
                : existingConnection;
            try
            {
                if (oracle.State != ConnectionState.Open) oracle.Open();
                using (OracleCommand command = new OracleCommand())
                {
                    command.Connection = oracle;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = string.Format("explain plan set statement_id = '{1}' for {0}", text, stid);

                    command.ExecuteNonQuery();
                }

                using (OracleCommand command = new OracleCommand())
                {
                    command.Connection = oracle;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = string.Format(@"
                            
                        SELECT 
                            level,
                            operation,
                            options,
                            object_type,
                            object_name,
                            cost,
                            cardinality,
                            bytes,
                            optimizer
                        FROM PLAN_TABLE
                        CONNECT BY prior id = parent_id
                            AND prior statement_id = statement_id
                        START WITH id = 0
                            AND statement_id = '{0}'
                        ORDER BY id
                    ", stid);

                    OracleDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int level = 1;
                            DataRow row = result.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (i == 0)
                                {
                                    level = Decimal.ToInt32(reader.GetDecimal(0));
                                    continue;
                                }
                                if (!reader.IsDBNull(i))
                                {
                                    Type type = reader.GetFieldType(i);
                                    switch (type.Name)
                                    {
                                        case "Decimal":
                                            Decimal d = reader.GetDecimal(i);
                                            row[result.Columns[i - 1]] = d.ToString();
                                            break;
                                        case "String":
                                            String s = reader.GetString(i);
                                            if (i == 1)
                                            {
                                                int pad = s.Length + Decimal.ToInt16(level - 1)*2;
                                                s = s.PadLeft(pad, ' ');
                                            }

                                            row[result.Columns[i - 1]] = s;
                                            break;
                                        case "DateTime":
                                            DateTime dt = reader.GetDateTime(i);
                                            row[result.Columns[i - 1]] = dt.ToString();
                                            break;
                                        default:
                                            row[result.Columns[i - 1]] = "{" + type.Name + "}";
                                            break;
                                    }
                                }
                                else
                                {
                                    row[result.Columns[i - 1]] = "{null}";
                                }
                            }
                            result.Rows.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Log(ex.Message.Replace("\n", " "));
            }
            finally
            {
                if (existingConnection == null) oracle.Dispose();
            }
            return result;
        }
    }
}
