using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.Util;
using Members = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// Lightweight reflection ORM over SQLite (Microsoft.Data.Sqlite) — the C#
    /// port of the Java JDBC <c>DbManager</c>. Entity class name = table name,
    /// property names = column names.
    /// <para>
    /// Entity type parameters carry <see cref="DynamicallyAccessedMembersAttribute"/>
    /// so the trimmer / Native AOT keeps every entity's public properties — the
    /// ORM reflects over them at runtime.
    /// </para>
    /// </summary>
    public sealed class DbManager
    {
        private readonly object _gate = new();
        private readonly Dictionary<Type, Mapping> _ormMappings = new();
        private readonly string _dbFile;

        private SqliteConnection _connection;
        private SqliteTransaction _currentTxn;

        public DbManager(string dbFile)
        {
            _dbFile = dbFile;
            bool shouldInitDb = !File.Exists(dbFile);
            Log.Info("open db at: " + dbFile);
            _connection = new SqliteConnection($"Data Source={dbFile}");
            _connection.Open();

            if (!shouldInitDb)
            {
                BackupDb();
            }
            else
            {
                Log.Info("init db...");
                string sqlText = LoadInitSql();
                string fullSql = Regex.Replace(sqlText, @"\s+", " ");
                Log.Info("loaded sql: " + fullSql);
                Tx(() =>
                {
                    foreach (string sql in fullSql.Split(';'))
                    {
                        if (sql.Trim().Length > 0)
                        {
                            Execute(sql);
                        }
                    }
                });
            }

            AppDomain.CurrentDomain.ProcessExit += (_, _) => Close();
        }

        private static string LoadInitSql()
        {
            var asm = Assembly.GetExecutingAssembly();
            using Stream stream = asm.GetManifestResourceStream("MyPasswordDesktop.Assets.init.sql")
                ?? throw new InvalidOperationException("init.sql resource not found");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var sb = new StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // strip line comments (`-- ...`)
                sb.Append(Regex.Replace(line, "--.*", "")).Append(' ');
            }
            return sb.ToString();
        }

        // ── backup ──────────────────────────────────────────────────────────

        public void BackupDb()
        {
            lock (_gate)
            {
                string backup = _dbFile + ".bak";
                if (File.Exists(backup))
                {
                    try
                    {
                        if (File.GetLastWriteTime(backup).Date == DateTime.Now.Date)
                        {
                            Log.Info("db already backed up today, skip: " + backup);
                            return;
                        }
                    }
                    catch (IOException e)
                    {
                        Log.Warn("failed to read backup mtime: " + backup, e);
                    }
                }
                try
                {
                    // rotate: .bak8 -> .bak9, ..., .bak -> .bak1
                    for (int i = 9; i >= 1; i--)
                    {
                        string src = (i == 1) ? backup : _dbFile + ".bak" + (i - 1);
                        string dst = _dbFile + ".bak" + i;
                        if (File.Exists(src))
                        {
                            File.Move(src, dst, overwrite: true);
                        }
                    }
                    File.Copy(_dbFile, backup, overwrite: true);
                    File.SetLastWriteTime(backup, DateTime.Now);
                    Log.Info("db backed up to: " + backup);
                }
                catch (IOException e)
                {
                    Log.Warn("db backup failed: " + backup, e);
                }
            }
        }

        // ── version helpers ─────────────────────────────────────────────────

        public int QueryAppVersion()
            => QueryUnique<VaultVersion>("WHERE id = ?", VaultVersion.ID_APP_VERSION).version;

        public void IncDataVersion()
            => Execute("UPDATE VaultVersion SET version = version + 1 WHERE id = ?", VaultVersion.ID_DATA_VERSION);

        public int QueryDataVersion()
            => QueryUnique<VaultVersion>("WHERE id = ?", VaultVersion.ID_DATA_VERSION).version;

        public VaultConfig QueryVaultConfig() => QueryFirst<VaultConfig>("WHERE id = ?", 1);

        // ── queries ─────────────────────────────────────────────────────────

        public T QueryFirst<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(
            string where, params object[] args) where T : new()
            => QueryForObject<T>(true, where, args);

        public T QueryUnique<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(
            string where, params object[] args) where T : new()
        {
            T obj = QueryForObject<T>(false, where, args);
            if (obj == null)
            {
                throw new InvalidOperationException("No result set.");
            }
            return obj;
        }

        private T QueryForObject<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(
            bool allowMultipleResults, string where, object[] args) where T : new()
        {
            lock (_gate)
            {
                string sql = "SELECT * FROM " + typeof(T).Name + " " + where;
                Log.Info("sql: " + sql);
                using SqliteCommand cmd = NewCommand(sql, args);
                using SqliteDataReader rs = cmd.ExecuteReader();
                if (!rs.Read())
                {
                    return default;
                }
                T obj = CreateObject<T>(rs);
                if (rs.Read() && !allowMultipleResults)
                {
                    throw new InvalidOperationException("Non unique result set.");
                }
                return obj;
            }
        }

        public List<T> QueryForList<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(
            string where, params object[] args) where T : new()
        {
            lock (_gate)
            {
                string sql = "SELECT * FROM " + typeof(T).Name + " " + where;
                Log.Info("sql: " + sql);
                using SqliteCommand cmd = NewCommand(sql, args);
                using SqliteDataReader rs = cmd.ExecuteReader();
                var list = new List<T>();
                while (rs.Read())
                {
                    list.Add(CreateObject<T>(rs));
                }
                return list;
            }
        }

        // ── mutations ───────────────────────────────────────────────────────

        public void Insert<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(T obj)
        {
            lock (_gate)
            {
                Mapping mapping = GetMapping(typeof(T));
                Log.Info("insert: " + mapping.InsertSql);
                var args = new object[mapping.InsertFields.Count];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = mapping.InsertFields[i].GetValue(obj);
                }
                using SqliteCommand cmd = NewCommand(mapping.InsertSql, args);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(T obj)
        {
            lock (_gate)
            {
                Mapping mapping = GetMapping(typeof(T));
                string sql = $"DELETE FROM {typeof(T).Name} WHERE {mapping.IdField.Name} = ?";
                Execute(sql, mapping.IdField.GetValue(obj));
            }
        }

        public void Update<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(
            T obj, params string[] fields)
        {
            lock (_gate)
            {
                Mapping mapping = GetMapping(typeof(T));
                var sb = new StringBuilder(128);
                sb.Append("UPDATE ").Append(typeof(T).Name).Append(" SET ");
                var values = new object[fields.Length + 1];
                for (int i = 0; i < fields.Length; i++)
                {
                    sb.Append(fields[i]).Append(" = ?,");
                    values[i] = mapping.Fields[fields[i]].GetValue(obj);
                }
                sb.Length--; // drop trailing comma
                sb.Append(" WHERE ").Append(mapping.IdField.Name).Append(" = ?");
                values[fields.Length] = mapping.IdField.GetValue(obj);
                using SqliteCommand cmd = NewCommand(sb.ToString(), values);
                cmd.ExecuteNonQuery();
            }
        }

        public void Tx(Action action)
        {
            lock (_gate)
            {
                _currentTxn = _connection.BeginTransaction();
                try
                {
                    action();
                    _currentTxn.Commit();
                }
                catch
                {
                    try { _currentTxn.Rollback(); }
                    catch (Exception er) { Log.Error("rollback failed.", er); }
                    throw;
                }
                finally
                {
                    _currentTxn.Dispose();
                    _currentTxn = null;
                }
            }
        }

        public void Execute(string sql, params object[] args)
        {
            lock (_gate)
            {
                Log.Info("executing SQL: " + sql);
                using SqliteCommand cmd = NewCommand(sql, args);
                cmd.ExecuteNonQuery();
            }
        }

        public void Close()
        {
            lock (_gate)
            {
                if (_connection != null)
                {
                    try
                    {
                        _connection.Close();
                        _connection.Dispose();
                    }
                    catch (Exception e)
                    {
                        Log.Error("failed close db.", e);
                    }
                    _connection = null;
                }
            }
        }

        // ── helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Build a command, rewriting positional <c>?</c> placeholders to named
        /// <c>$pN</c> parameters. Microsoft.Data.Sqlite — unlike JDBC — requires
        /// every parameter to carry a name.
        /// </summary>
        private SqliteCommand NewCommand(string sql, object[] args)
        {
            var cmd = _connection.CreateCommand();
            cmd.Transaction = _currentTxn;
            if (args == null || args.Length == 0)
            {
                cmd.CommandText = sql;
                return cmd;
            }
            var sb = new StringBuilder(sql.Length + 16);
            int idx = 0;
            foreach (char c in sql)
            {
                if (c == '?')
                {
                    string name = "$p" + idx;
                    sb.Append(name);
                    cmd.Parameters.Add(new SqliteParameter(name, args[idx] ?? DBNull.Value));
                    idx++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            cmd.CommandText = sb.ToString();
            return cmd;
        }

        private T CreateObject<[DynamicallyAccessedMembers(Members.PublicProperties)] T>(SqliteDataReader rs)
            where T : new()
        {
            T obj = new();
            Mapping mapping = GetMapping(typeof(T));
            foreach (var kv in mapping.Fields)
            {
                PropertyInfo prop = kv.Value;
                int ordinal;
                try { ordinal = rs.GetOrdinal(kv.Key); }
                catch (IndexOutOfRangeException) { continue; }
                Type t = prop.PropertyType;
                if (rs.IsDBNull(ordinal))
                {
                    continue;
                }
                if (t == typeof(long))
                {
                    prop.SetValue(obj, rs.GetInt64(ordinal));
                }
                else if (t == typeof(int))
                {
                    prop.SetValue(obj, rs.GetInt32(ordinal));
                }
                else if (t == typeof(bool))
                {
                    prop.SetValue(obj, rs.GetInt64(ordinal) != 0);
                }
                else if (t == typeof(string))
                {
                    prop.SetValue(obj, rs.GetString(ordinal));
                }
                else
                {
                    throw new ArgumentException("Unsupported type: " + t);
                }
            }
            return obj;
        }

        private Mapping GetMapping([DynamicallyAccessedMembers(Members.PublicProperties)] Type clazz)
        {
            if (!_ormMappings.TryGetValue(clazz, out Mapping mapping))
            {
                mapping = new Mapping(clazz);
                _ormMappings[clazz] = mapping;
            }
            return mapping;
        }
    }

    /// <summary>Cached column metadata for one entity type.</summary>
    internal sealed class Mapping
    {
        public readonly PropertyInfo IdField;
        public readonly List<PropertyInfo> InsertFields = new();
        public readonly Dictionary<string, PropertyInfo> Fields = new();
        public readonly string InsertSql;

        public Mapping([DynamicallyAccessedMembers(Members.PublicProperties)] Type clazz)
        {
            foreach (PropertyInfo p in clazz.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || !p.CanWrite)
                {
                    continue;
                }
                Fields[p.Name] = p;
                if (p.GetCustomAttribute<IdAttribute>() != null)
                {
                    if (IdField != null)
                    {
                        throw new InvalidOperationException("Duplicate [Id] defined in " + clazz);
                    }
                    IdField = p;
                }
                InsertFields.Add(p);
            }
            if (IdField == null)
            {
                throw new InvalidOperationException("No [Id] defined in " + clazz);
            }
            var cols = InsertFields.ConvertAll(f => f.Name);
            InsertSql = $"INSERT INTO {clazz.Name} ({string.Join(", ", cols)}) VALUES ("
                        + string.Join(", ", cols.ConvertAll(_ => "?")) + ")";
        }
    }
}
