using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using USortThis.Core.Models;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Services
{
    /// <summary>
    /// Provides database services for USortThis, including methods for initializing the database,
    /// saving and loading profiles, rules, and other entities, and executing queries and commands
    /// against the database.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private const string DatabaseFileName = "USortThis.db";

        private static readonly string[] _SQLProfiles = ["ProfileId", "Name", "Enabled", "LastRun", "CreatedAt", "ConflictHandling"];
        private static readonly string[] _SQLRules = ["RuleId", "Name", "ProfileId", "BridgeId", "DestinationPath", "Enabled"];
        private static readonly string[] _SQLRuleCriteria = ["CriteriaId", "RuleId", "Type", "Value"];
        private static readonly string[] _SQLFolderItems = ["FolderId", "FolderPath", "CustomName", "FolderType", "AssociatedDeviceId"];
        private static readonly string[] _SQLFolderItemBridges = ["BridgeId", "CustomName", "ProfileId", "SourceFolderId", "DestinationFolderId", "MoveSourceAfterCopy"];
        private static readonly string[] _SQLDevices = ["DeviceId", "DeviceType", "DevicePath", "DeviceName", "LastSeenAt"];
        private static readonly string[] _SQLCopyQueue = ["QueueId", "ProfileId", "SourcePath", "DestinationPath", "FileName", "FileSize", "Status", "FailureReason", "SourceDeviceId", "DestinationDeviceId", "CreatedAt", "UpdatedAt", "RetryCount"];
        private static readonly string[] _SQLCopyHistory = ["HistoryId", "QueueId", "Status", "Message", "Timestamp"];

        // Define expected tables and required columns
        private readonly Dictionary<string, string[]> _SQLTablesAndColumns = new()
        {
            { "Profiles", _SQLProfiles },
            { "Rules", _SQLRules },
            { "RuleCriteria", _SQLRuleCriteria },
            { "FolderItems", _SQLFolderItems },
            { "FolderItemBridges", _SQLFolderItemBridges },
            { "Devices", _SQLDevices },
            { "CopyQueue", _SQLCopyQueue },
            { "CopyHistory", _SQLCopyHistory }
        };

        private readonly IFilterRuleCriteriaFactory _filterRuleCriteriaFactory;
        private readonly IFolderItemModelFactory _folderItemModelFactory;
        private readonly IFolderBridgeModelFactory _folderBridgeModelFactory;
        private readonly IFileItemModelFactory _fileItemModelFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// </summary>
        /// <param name="filterRuleCriteriaFactory">The filter rule criteria factory.</param>
        /// <param name="folderItemModelFactory">The folder item model factory.</param>
        /// <param name="folderBridgeModelFactory">The folder bridge model factory.</param>
        /// <param name="fileItemModelFactory">The file item model factory.</param>
        public DatabaseService(IFilterRuleCriteriaFactory filterRuleCriteriaFactory, IFolderItemModelFactory folderItemModelFactory, IFolderBridgeModelFactory folderBridgeModelFactory, IFileItemModelFactory fileItemModelFactory)
        {
            _filterRuleCriteriaFactory = filterRuleCriteriaFactory;
            _folderItemModelFactory = folderItemModelFactory;
            _folderBridgeModelFactory = folderBridgeModelFactory;
            _fileItemModelFactory = fileItemModelFactory;
        }

        /// <summary>
        /// Saves the bridge route for the specified profile and filter rule.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <param name="bridge">The folder bridge model.</param>
        /// <param name="route">The filter rule model.</param>
        public void SaveBridgeRoute(long profileId, FolderBridgeModel bridge, FilterRuleModel route)
        {
            SaveFilterRule(profileId.ToString(), route);
            var routeSettings = JsonSerializer.Serialize(new
            {
                route.DestinationPath,
                PathMode = route.PathMode.ToString(),
                route.DateFolderPattern,
                route.UseCreationTime
            });
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Rules SET BridgeId=@BridgeId, DestinationPath=@DestinationPath WHERE RuleId=@RuleId";
            command.Parameters.AddWithValue("@BridgeId", bridge.Id.ToString());
            command.Parameters.AddWithValue("@DestinationPath", routeSettings);
            command.Parameters.AddWithValue("@RuleId", route.Id.ToString());
            command.ExecuteNonQuery();
            SaveRuleCriteriaForRoute(route);

        }

        /// <summary>
        /// Saves the rule criteria for the specified filter rule route.
        /// </summary>
        /// <param name="route">The filter rule model.</param>
        private void SaveRuleCriteriaForRoute(FilterRuleModel route)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM RuleCriteria WHERE RuleId=@RuleId";
                delete.Parameters.AddWithValue("@RuleId", route.Id.ToString());
                delete.ExecuteNonQuery();
            }
            foreach (var criteria in route.Criterias)
            {
                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO RuleCriteria (CriteriaId, RuleId, Type, Value) VALUES (@CriteriaId,@RuleId,@Type,@Value)";
                insert.Parameters.AddWithValue("@CriteriaId", criteria.Id.ToString());
                insert.Parameters.AddWithValue("@RuleId", route.Id.ToString());
                insert.Parameters.AddWithValue("@Type", criteria.GetType().Name);
                insert.Parameters.AddWithValue("@Value", SerializeCriteriaValue(criteria));
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        /// <summary>
        /// Saves the specified bridges for the given profile.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <param name="bridges">The collection of folder bridge models to save.</param>
        public void SaveProfileBridges(long profileId, IEnumerable<FolderBridgeModel> bridges)
        {
            var bridgeList = bridges.ToList();
            foreach (var bridge in bridgeList)
                SaveBridge(bridge, profileId);

            var currentIds = bridgeList.Select(bridge => bridge.Id.ToString()).ToHashSet(StringComparer.Ordinal);
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT BridgeId FROM FolderItemBridges WHERE ProfileId=@ProfileId";
            command.Parameters.AddWithValue("@ProfileId", profileId.ToString());
            var removedIds = new List<string>();
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                {
                    var id = reader.GetString(0);
                    if (!currentIds.Contains(id)) removedIds.Add(id);
                }

            foreach (var id in removedIds)
            {
                using var deleteHistory = connection.CreateCommand();
                deleteHistory.CommandText = "DELETE FROM TransferHistory WHERE BridgeId=@BridgeId";
                deleteHistory.Parameters.AddWithValue("@BridgeId", id);
                deleteHistory.ExecuteNonQuery();
                using var deleteCriteria = connection.CreateCommand();
                deleteCriteria.CommandText = "DELETE FROM RuleCriteria WHERE RuleId IN (SELECT RuleId FROM Rules WHERE BridgeId=@BridgeId)";
                deleteCriteria.Parameters.AddWithValue("@BridgeId", id);
                deleteCriteria.ExecuteNonQuery();
                using var deleteRules = connection.CreateCommand();
                deleteRules.CommandText = "DELETE FROM Rules WHERE BridgeId=@BridgeId";
                deleteRules.Parameters.AddWithValue("@BridgeId", id);
                deleteRules.ExecuteNonQuery();
                using var deleteBridge = connection.CreateCommand();
                deleteBridge.CommandText = "DELETE FROM FolderItemBridges WHERE BridgeId=@BridgeId";
                deleteBridge.Parameters.AddWithValue("@BridgeId", id);
                deleteBridge.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Saves the specified folder bridge for the given profile.
        /// </summary>
        /// <param name="bridge">The folder bridge model to save.</param>
        /// <param name="profileId">The profile identifier.</param>
        private void SaveBridge(FolderBridgeModel bridge, long profileId)
        {
            if (bridge.SourceFolder != null)
                SaveFolder(bridge.SourceFolder);

            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO FolderItemBridges (BridgeId, CustomName, ProfileId, SourceFolderId, DestinationFolderId, MoveSourceAfterCopy) VALUES (@BridgeId,@CustomName,@ProfileId,@SourceFolderId,NULL,@Move) ON CONFLICT(BridgeId) DO UPDATE SET CustomName=excluded.CustomName, ProfileId=excluded.ProfileId, SourceFolderId=excluded.SourceFolderId, MoveSourceAfterCopy=excluded.MoveSourceAfterCopy";
            command.Parameters.AddWithValue("@BridgeId", bridge.Id.ToString());
            command.Parameters.AddWithValue("@CustomName", bridge.CustomName ?? string.Empty);
            command.Parameters.AddWithValue("@ProfileId", profileId.ToString());
            command.Parameters.AddWithValue("@SourceFolderId", (object?)bridge.SourceFolder?.Id.ToString() ?? DBNull.Value);
            command.Parameters.AddWithValue("@Move", bridge.MoveSourceAfterCopy ? 1 : 0);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets the path to the database file.
        /// </summary>
        /// <returns>The database file path.</returns>
        public string GetDatabasePath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localAppData, "USortThis");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            return Path.Combine(appFolder, DatabaseFileName);
        }

        /// <summary>
        /// Initializes the database, creating the schema and tables if they do not exist.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            string dbPath = GetDatabasePath();

            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();


            // Ensure database file is created and tables exist
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            if (await TableExistsAsync(connection, "FolderItemBridges").ConfigureAwait(false))
            {
                var bridgeForeignKeys = new List<(string Table, string From, string To)>();
                using (var foreignKeyCommand = connection.CreateCommand())
                {
                    foreignKeyCommand.CommandText = "PRAGMA foreign_key_list('FolderItemBridges')";
                    using var reader = await foreignKeyCommand.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        bridgeForeignKeys.Add((reader.GetString(2), reader.GetString(3), reader.GetString(4)));
                    }
                }

                var expectedBridgeForeignKeys = new HashSet<(string Table, string From, string To)>
                {
                    ("Profiles", "ProfileId", "ProfileId"),
                    ("FolderItems", "SourceFolderId", "FolderId"),
                    ("FolderItems", "DestinationFolderId", "FolderId")
                };

                if (bridgeForeignKeys.Count != expectedBridgeForeignKeys.Count ||
                    !expectedBridgeForeignKeys.SetEquals(bridgeForeignKeys))
                {
                    Debug.Print("Database has an invalid FolderItemBridges schema. Resetting the development database.");
                    await connection.CloseAsync().ConfigureAwait(false);
                    await RecreateDatabaseAsync(dbPath).ConfigureAwait(false);
                    await connection.OpenAsync().ConfigureAwait(false);
                }
            }

            // Migration: ensure Rules table schema matches expected foreign keys/columns. If mismatch recreate Rules safely.
            try
            {
                if (await TableExistsAsync(connection, "Rules").ConfigureAwait(false))
                {
                    var existingCols = await GetTableColumnsAsync(connection, "Rules").ConfigureAwait(false);
                    // Get PRAGMA foreign_key_list for Rules to inspect parent table names
                    try
                    {
                        using var fkCmd = connection.CreateCommand();
                        fkCmd.CommandText = "PRAGMA foreign_key_list('Rules')";
                        using var fkReader = fkCmd.ExecuteReader();
                        var fkParents = new System.Collections.Generic.List<string>();
                        while (fkReader.Read())
                        {
                            // column 2 is the parent table name in the pragma result
                            fkParents.Add(fkReader.GetString(2));
                        }

                        bool fkOk = fkParents.Contains("Profiles") && fkParents.Contains("FolderItemBridges");

                        if (!fkOk)
                        {
                            // Need to migrate table: create new table, copy data, drop old, rename
                            Console.WriteLine("Database migration: repairing Rules table schema.");
                            using var tx = connection.BeginTransaction();
                            try
                            {
                                // Create temp table with expected schema
                                var createRulesNew =
                                    "CREATE TABLE IF NOT EXISTS Rules_new (" +
                                    "RuleId TEXT PRIMARY KEY, " +
                                    "Name TEXT NOT NULL, " +
                                    "Enabled INTEGER NOT NULL DEFAULT 1, " +
                                    "ProfileId TEXT NOT NULL, " +
                                    "BridgeId TEXT, " +
                                    "DestinationPath TEXT, " +
                                    "FOREIGN KEY(ProfileId) REFERENCES Profiles(ProfileId) ON DELETE CASCADE, " +
                                    "FOREIGN KEY(BridgeId) REFERENCES FolderItemBridges(BridgeId) ON DELETE CASCADE" +
                                    ");";
                                using (var ccmd = connection.CreateCommand()) { ccmd.Transaction = tx; ccmd.CommandText = createRulesNew; ccmd.ExecuteNonQuery(); }

                                // Build column list for insert-select from old table to new. Map missing columns to NULL or default.
                                var selectCols = new System.Collections.Generic.List<string>();
                                if (existingCols.Contains("RuleId", System.StringComparer.OrdinalIgnoreCase)) selectCols.Add("RuleId"); else selectCols.Add("NULL AS RuleId");
                                if (existingCols.Contains("ProfileId", System.StringComparer.OrdinalIgnoreCase)) selectCols.Add("ProfileId"); else selectCols.Add("NULL AS ProfileId");
                                if (existingCols.Contains("Name", System.StringComparer.OrdinalIgnoreCase)) selectCols.Add("Name"); else selectCols.Add("'' AS Name");
                                if (existingCols.Contains("DestinationPath", System.StringComparer.OrdinalIgnoreCase)) selectCols.Add("DestinationPath"); else selectCols.Add("NULL AS DestinationPath");
                                if (existingCols.Contains("Enabled", System.StringComparer.OrdinalIgnoreCase)) selectCols.Add("Enabled"); else selectCols.Add("1 AS Enabled");
                                if (existingCols.Contains("BridgeId", System.StringComparer.OrdinalIgnoreCase)) selectCols.Add("BridgeId"); else selectCols.Add("NULL AS BridgeId");

                                var insertSql = $"INSERT INTO Rules_new (RuleId, ProfileId, Name, DestinationPath, Enabled, BridgeId) SELECT {string.Join(",", selectCols)} FROM Rules;";
                                using (var icmd = connection.CreateCommand()) { icmd.Transaction = tx; icmd.CommandText = insertSql; icmd.ExecuteNonQuery(); }

                                // Drop old table and rename new
                                using (var dcmd = connection.CreateCommand()) { dcmd.Transaction = tx; dcmd.CommandText = "DROP TABLE IF EXISTS Rules;"; dcmd.ExecuteNonQuery(); }
                                using (var rcmd = connection.CreateCommand()) { rcmd.Transaction = tx; rcmd.CommandText = "ALTER TABLE Rules_new RENAME TO Rules;"; rcmd.ExecuteNonQuery(); }

                                tx.Commit();
                            }
                            catch (Exception mex)
                            {
                                try { tx.Rollback(); } catch { }
                                Console.WriteLine($"Rules table migration failed: {mex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to inspect Rules foreign keys: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Schema migration check failed: {ex.Message}");
            }

            // Create schema idempotently. If this fails we will attempt to recreate the database as a last resort.
            try
            {
                await CreateSchemaAsync(connection).ConfigureAwait(false);
                var profileColumns = await GetTableColumnsAsync(connection, "Profiles").ConfigureAwait(false);
                if (!profileColumns.Contains("ConflictHandling", StringComparer.OrdinalIgnoreCase))
                    await ExecuteNonQueryAsync(connection, "ALTER TABLE Profiles ADD COLUMN ConflictHandling INTEGER NOT NULL DEFAULT 1").ConfigureAwait(false);
                if (!await CheckSchemaCompatibilityAsync(connection).ConfigureAwait(false))
                {
                    Debug.Print("Database schema is incompatible with the current development model. Resetting the database.");
                    await connection.CloseAsync().ConfigureAwait(false);
                    await RecreateDatabaseAsync(dbPath).ConfigureAwait(false);
                    await connection.OpenAsync().ConfigureAwait(false);
                    await CreateSchemaAsync(connection).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateSchemaAsync failed: {ex.Message}. Attempting to recreate database.");
                try
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                    await connection.DisposeAsync().ConfigureAwait(false);
                    await RecreateDatabaseAsync(dbPath).ConfigureAwait(false);

                    // Re-open and create schema on fresh DB
                    using var conn2 = new SqliteConnection(connectionString);
                    await conn2.OpenAsync().ConfigureAwait(false);
                    await CreateSchemaAsync(conn2).ConfigureAwait(false);
                    await conn2.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"Failed to recreate database: {ex2.Message}");
                    throw;
                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
            LoadDataFromProfilesTable();
            LoadDataFromInterfaceDeviceTable();
        }

        private async Task CreateSchemaAsync(SqliteConnection connection)
        {
            // Use consolidated Devices table and normalized FolderRules relation
            var createProfiles =
                $"CREATE TABLE IF NOT EXISTS Profiles (\n" +
                $"ProfileId TEXT PRIMARY KEY,\n" +
                $"Name TEXT NOT NULL,\n" +
                $"Enabled INTEGER NOT NULL DEFAULT 1,\n" +
                $"LastRun TEXT,\n" +
                $"CreatedAt TEXT NOT NULL,\n" +
                $"ConflictHandling INTEGER NOT NULL DEFAULT 1\n" +
                $");\n";

            var createRules =
                $"CREATE TABLE IF NOT EXISTS Rules (\n" +
                $"RuleId TEXT PRIMARY KEY,\n" +
                $"Name TEXT NOT NULL,\n" +
                $"Enabled INTEGER NOT NULL DEFAULT 1,\n" +
                $"ProfileId TEXT NOT NULL,\n" +
                $"BridgeId TEXT,\n" +
                $"DestinationPath TEXT,\n" +
                $"FOREIGN KEY(ProfileId) REFERENCES Profiles(ProfileId) ON DELETE CASCADE,\n" +
                $"FOREIGN KEY(BridgeId) REFERENCES FolderItemBridges(BridgeId) ON DELETE CASCADE\n" +
                $");\n";

            var createRuleCriteria =
                $"CREATE TABLE IF NOT EXISTS RuleCriteria (\n" +
                $"CriteriaId TEXT PRIMARY KEY,\n" +
                $"Type TEXT NOT NULL,\n" +
                $"Value TEXT,\n" +
                $"RuleId TEXT NOT NULL,\n" +
                $"FOREIGN KEY(RuleId) REFERENCES Rules(RuleId) ON DELETE CASCADE\n" +
                $");\n";

            var createFolderItems =
                $"CREATE TABLE IF NOT EXISTS FolderItems (\n" +
                $"FolderId TEXT PRIMARY KEY,\n" +
                $"FolderPath TEXT NOT NULL,\n" +
                $"CustomName TEXT,\n" +
                $"FolderType TEXT,\n" +
                $"AssociatedDeviceId TEXT\n" +
                $");\n";

            var createFolderItemBridges =
                $"CREATE TABLE IF NOT EXISTS FolderItemBridges (\n" +
                $"BridgeId TEXT PRIMARY KEY,\n" +
                $"CustomName TEXT,\n" +
                $"ProfileId TEXT NOT NULL,\n" +
                $"SourceFolderId TEXT,\n" +
                $"DestinationFolderId TEXT,\n" +
                $"MoveSourceAfterCopy INTEGER NOT NULL DEFAULT 0,\n" +
                $"FOREIGN KEY(ProfileId) REFERENCES Profiles(ProfileId) ON DELETE CASCADE,\n" +
                $"FOREIGN KEY(SourceFolderId) REFERENCES FolderItems(FolderId),\n" +
                $"FOREIGN KEY(DestinationFolderId) REFERENCES FolderItems(FolderId)\n" +
                $");\n";

            var createDevices =
                $"CREATE TABLE IF NOT EXISTS Devices (\n" +
                $"DeviceId TEXT PRIMARY KEY,\n" +
                $"DeviceType INTEGER NOT NULL,\n" +
                $"DevicePath TEXT,\n" +
                $"DeviceName TEXT,\n" +
                $"LastSeenAt TEXT\n" +
                $");\n";

            var createCopyQueue =
                $"CREATE TABLE IF NOT EXISTS CopyQueue (\n" +
                $"QueueId TEXT PRIMARY KEY,\n" +
                $"ProfileId TEXT,\n" +
                $"SourcePath TEXT NOT NULL,\n" +
                $"DestinationPath TEXT NOT NULL,\n" +
                $"FileName TEXT NOT NULL,\n" +
                $"FileSize INTEGER NOT NULL DEFAULT 0,\n" +
                $"Status INTEGER NOT NULL DEFAULT 0,\n" +
                $"FailureReason INTEGER NOT NULL DEFAULT 0,\n" +
                $"SourceDeviceId TEXT,\n" +
                $"DestinationDeviceId TEXT,\n" +
                $"CreatedAt TEXT NOT NULL,\n" +
                $"UpdatedAt TEXT,\n" +
                $"RetryCount INTEGER NOT NULL DEFAULT 0\n" +
                $");\n";

            var createCopyHistory =
                $"CREATE TABLE IF NOT EXISTS CopyHistory (\n" +
                $"HistoryId TEXT PRIMARY KEY,\n" +
                $"QueueId TEXT,\n" +
                $"Status INTEGER NOT NULL,\n" +
                $"Message TEXT,\n" +
                $"Timestamp TEXT NOT NULL\n" +
                $");\n";

            var createIndexes =
                $"CREATE INDEX IF NOT EXISTS IDX_CopyQueue_Status ON CopyQueue(Status);\n" +
                $"CREATE INDEX IF NOT EXISTS IDX_CopyQueue_SourceDeviceId ON CopyQueue(SourceDeviceId);\n" +
                $"CREATE INDEX IF NOT EXISTS IDX_CopyQueue_DestinationDeviceId ON CopyQueue(DestinationDeviceId);\n";

            await ExecuteNonQueryAsync(connection, createProfiles).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createRules).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createRuleCriteria).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createFolderItems).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createFolderItemBridges).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createDevices).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS ConfiguredLocations (LocationId TEXT PRIMARY KEY, Label TEXT NOT NULL, Path TEXT NOT NULL, Username TEXT, Domain TEXT, UseCurrentWindowsCredentials INTEGER NOT NULL DEFAULT 1);").ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createCopyQueue).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createCopyHistory).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS TransferHistory (BridgeId TEXT NOT NULL, SourcePath TEXT NOT NULL, DestinationPath TEXT NOT NULL, RelativePath TEXT NOT NULL, Status INTEGER NOT NULL, Progress REAL NOT NULL DEFAULT 0, FailureReason TEXT NOT NULL DEFAULT '', LastUpdatedUtc TEXT NOT NULL, PRIMARY KEY (BridgeId, SourcePath, DestinationPath));").ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, createIndexes).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the transfer history for the specified bridge identifier.
        /// </summary>
        /// <param name="bridgeId">The bridge identifier.</param>
        /// <returns>A list of file transfer detail models representing the transfer history.</returns>
        public List<FileTransferDetailModel> LoadTransferHistory(long bridgeId)
        {
            var details = new List<FileTransferDetailModel>();
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT SourcePath, DestinationPath, RelativePath, Status, Progress, FailureReason, LastUpdatedUtc FROM TransferHistory WHERE BridgeId=@BridgeId ORDER BY LastUpdatedUtc DESC";
            command.Parameters.AddWithValue("@BridgeId", bridgeId.ToString());
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var status = Enum.IsDefined(typeof(FileItemModel.CopyStatus), reader.GetInt32(3))
                    ? (FileItemModel.CopyStatus)reader.GetInt32(3)
                    : FileItemModel.CopyStatus.Pending;
                if (status == FileItemModel.CopyStatus.InProgress)
                    status = FileItemModel.CopyStatus.Pending;
                DateTime.TryParse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind, out var updated);
                details.Add(new FileTransferDetailModel(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), status,
                    status == FileItemModel.CopyStatus.Pending ? 0 : reader.GetDouble(4),
                    reader.GetString(5), updated));
            }
            return details;
        }

        /// <summary>
        /// Saves the transfer history for the specified bridge identifier and file transfer detail model.
        /// </summary>
        /// <param name="bridgeId">The bridge identifier.</param>
        /// <param name="detail">The file transfer detail model.</param>
        public void SaveTransferHistory(long bridgeId, FileTransferDetailModel detail)
        {
            ArgumentNullException.ThrowIfNull(detail);
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO TransferHistory (BridgeId, SourcePath, DestinationPath, RelativePath, Status, Progress, FailureReason, LastUpdatedUtc) VALUES (@BridgeId, @SourcePath, @DestinationPath, @RelativePath, @Status, @Progress, @FailureReason, @Updated) ON CONFLICT(BridgeId, SourcePath, DestinationPath) DO UPDATE SET RelativePath=excluded.RelativePath, Status=excluded.Status, Progress=excluded.Progress, FailureReason=excluded.FailureReason, LastUpdatedUtc=excluded.LastUpdatedUtc";
            command.Parameters.AddWithValue("@BridgeId", bridgeId.ToString());
            command.Parameters.AddWithValue("@SourcePath", detail.SourcePath);
            command.Parameters.AddWithValue("@DestinationPath", detail.DestinationPath);
            command.Parameters.AddWithValue("@RelativePath", detail.RelativePath);
            command.Parameters.AddWithValue("@Status", (int)detail.Status);
            command.Parameters.AddWithValue("@Progress", detail.Progress);
            command.Parameters.AddWithValue("@FailureReason", detail.FailureReason);
            command.Parameters.AddWithValue("@Updated", detail.LastUpdatedUtc.ToUniversalTime().ToString("o"));
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes the transfer history for the specified bridge identifier.
        /// </summary>
        /// <param name="bridgeId">The bridge identifier.</param>
        public void DeleteTransferHistory(long bridgeId)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM TransferHistory WHERE BridgeId=@BridgeId";
            command.Parameters.AddWithValue("@BridgeId", bridgeId.ToString());
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Loads the configured locations from the database.
        /// </summary>
        /// <returns>A list of configured location models.</returns>
        public List<ConfiguredLocationModel> LoadConfiguredLocations()
        {
            var locations = new List<ConfiguredLocationModel>();
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT LocationId, Label, Path, Username, Domain, UseCurrentWindowsCredentials FROM ConfiguredLocations ORDER BY Label";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var location = new ConfiguredLocationModel(
                    long.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2))
                {
                    Username = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Domain = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    UseCurrentWindowsCredentials = reader.GetInt32(5) != 0
                };
                locations.Add(location);
            }
            return locations;
        }

        /// <summary>
        /// Saves the specified configured location to the database.
        /// </summary>
        /// <param name="location">The configured location model to save.</param>
        public void SaveConfiguredLocation(ConfiguredLocationModel location)
        {
            ArgumentNullException.ThrowIfNull(location);
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO ConfiguredLocations (LocationId, Label, Path, Username, Domain, UseCurrentWindowsCredentials) VALUES (@Id, @Label, @Path, @Username, @Domain, @UseCurrent) ON CONFLICT(LocationId) DO UPDATE SET Label=excluded.Label, Path=excluded.Path, Username=excluded.Username, Domain=excluded.Domain, UseCurrentWindowsCredentials=excluded.UseCurrentWindowsCredentials";
            command.Parameters.AddWithValue("@Id", location.Id.ToString());
            command.Parameters.AddWithValue("@Label", location.Label);
            command.Parameters.AddWithValue("@Path", location.Path);
            command.Parameters.AddWithValue("@Username", location.UseCurrentWindowsCredentials ? DBNull.Value : location.Username);
            command.Parameters.AddWithValue("@Domain", location.UseCurrentWindowsCredentials ? DBNull.Value : location.Domain);
            command.Parameters.AddWithValue("@UseCurrent", location.UseCurrentWindowsCredentials ? 1 : 0);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes the configured location with the specified identifier.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        public void DeleteConfiguredLocation(long locationId)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ConfiguredLocations WHERE LocationId=@Id";
            command.Parameters.AddWithValue("@Id", locationId.ToString());
            command.ExecuteNonQuery();
        }

        private async Task<bool> CheckSchemaCompatibilityAsync(SqliteConnection connection)
        {
            foreach (var kv in _SQLTablesAndColumns)
            {
                string table = kv.Key;
                if (!await TableExistsAsync(connection, table).ConfigureAwait(false))
                    return false;

                var existingCols = await GetTableColumnsAsync(connection, table).ConfigureAwait(false);
                var requiredCols = kv.Value;

                // require that all requiredCols exist in existingCols
                foreach (var rc in requiredCols)
                {
                    if (!existingCols.Contains(rc, StringComparer.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }

        private async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name";
            cmd.Parameters.AddWithValue("@name", tableName);
            var res = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return res != null;
        }

        private async Task<List<string>> GetTableColumnsAsync(SqliteConnection connection, string tableName)
        {
            var cols = new List<string>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info('{tableName}')";
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                cols.Add(reader.GetString(1)); // name
            }
            return cols;
        }

        private async Task RecreateDatabaseAsync(string dbPath)
        {
            try
            {
                if (File.Exists(dbPath))
                    File.Delete(dbPath);

                // Create empty file
                using var fs = File.Create(dbPath);
                await fs.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.Print($"Failed to recreate database: {ex.Message}");
                throw;
            }
        }

        private async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Saves the specified profile data to the Profiles table in the database.
        /// </summary>
        /// <param name="profile">The profile data to save.</param>
        /// <returns><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="profile"/> is null.</exception>
        public bool SaveDataToProfilesTable(ProfileItemModel profile)
        {
            if (profile is null)
                throw new ArgumentNullException(nameof(profile));

            string dbPath = GetDatabasePath();
            Debug.Print($"DatabaseService.SaveDataToProfilesTable - dbPath={dbPath}");
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                string profileId = profile.Id.ToString();
                Debug.Print($"Saving profile id={profileId} name={profile.Name}");

                // Check if profile already exists
                using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT COUNT(1) FROM Profiles WHERE ProfileId = @ProfileId";
                    checkCmd.Parameters.AddWithValue("@ProfileId", profileId);
                    checkCmd.Transaction = transaction;

                    var exists = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0) > 0;

                    if (exists)
                    {
                        using var updateCmd = connection.CreateCommand();
                        updateCmd.Transaction = transaction;
                        updateCmd.CommandText = $"UPDATE Profiles\n" +
                            $"SET Name = @Name,\n" +
                            $"Enabled = @Enabled,\n" +
                            $"LastRun = @LastRun,\n" +
                            $"ConflictHandling = @ConflictHandling\n" +
                            $"WHERE ProfileId = @ProfileId";

                        updateCmd.Parameters.AddWithValue("@Name", profile.Name ?? string.Empty);
                        // No Enabled property on ProfileItem in current model; store as 1 (enabled)
                        updateCmd.Parameters.AddWithValue("@Enabled", 1);
                        updateCmd.Parameters.AddWithValue("@LastRun", DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@ConflictHandling", (int)profile.ConflictHandling);
                        updateCmd.Parameters.AddWithValue("@ProfileId", profileId);

                        var updated = updateCmd.ExecuteNonQuery();
                        Debug.Print($"Updated rows: {updated}");
                    }
                    else
                    {
                        using var insertCmd = connection.CreateCommand();
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = $"INSERT INTO Profiles (ProfileId, Name, Enabled, LastRun, CreatedAt, ConflictHandling)\n" +
                            $"VALUES (@ProfileId, @Name, @Enabled, @LastRun, @CreatedAt, @ConflictHandling)";

                        insertCmd.Parameters.AddWithValue("@ProfileId", profileId);
                        insertCmd.Parameters.AddWithValue("@Name", profile.Name ?? string.Empty);
                        insertCmd.Parameters.AddWithValue("@Enabled", 1);
                        insertCmd.Parameters.AddWithValue("@LastRun", DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
                        insertCmd.Parameters.AddWithValue("@ConflictHandling", (int)profile.ConflictHandling);

                        var inserted = insertCmd.ExecuteNonQuery();
                        Debug.Print($"Inserted rows: {inserted}");
                    }
                }

                transaction.Commit();
                Debug.Print("DatabaseService.SaveDataToProfilesTable - commit successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print($"Error adding/updating data in Profiles table: {ex.Message}");
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    Debug.Print("Error rolling back transaction.");
                }
                return false;
            }
        }

        /// <summary>
        /// Loads data from the Profiles table in the database into the application model.
        /// </summary>
        public void LoadDataFromProfilesTable()
        {
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();
            using var connection = new SqliteConnection(connectionString);
            try
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT ProfileId, Name, ConflictHandling FROM Profiles";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!long.TryParse(reader.GetString(0), out var id))
                        continue;
                    string name = reader.GetString(1);
                    var profile = new ProfileItemModel(id, name);
                    var conflictHandlingValue = reader.GetInt32(2);
                    profile.ConflictHandling = Enum.IsDefined(typeof(ProfileItemModel.ConflictHandlingMode), conflictHandlingValue)
                        ? (ProfileItemModel.ConflictHandlingMode)conflictHandlingValue
                        : ProfileItemModel.ConflictHandlingMode.Overwrite;
                    profile.ReplaceFolderBridges(LoadBridgesForProfile(id));

                    // If a shared ProfileListModel is registered, add loaded profiles into it so the UI sees them
                    try
                    {
                        var profileList = USortThis.App.ServiceProvider?.GetService(typeof(ProfileListModel)) as ProfileListModel;
                        if (profileList != null)
                            profileList.AddProfile(profile);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                // Ignore database read errors during startup; app can continue with an empty profile list.
            }
            finally
            {
                try { connection.Close(); } catch { }
            }
        }

        /// <summary>
        /// Loads the bridges for the specified profile identifier.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <returns>A list of folder bridge models associated with the profile.</returns>
        public List<FolderBridgeModel> LoadBridgesForProfile(long profileId)
        {
            var bridges = new List<FolderBridgeModel>();
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT BridgeId, CustomName, SourceFolderId, DestinationFolderId, MoveSourceAfterCopy FROM FolderItemBridges WHERE ProfileId=@ProfileId";
            command.Parameters.AddWithValue("@ProfileId", profileId.ToString());
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var bridgeId = long.Parse(reader.GetString(0));
                var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var sourceId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var destinationId = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                var source = string.IsNullOrWhiteSpace(sourceId) ? null : LoadFolderById(sourceId);
                var destination = string.IsNullOrWhiteSpace(destinationId) ? null : LoadFolderById(destinationId);
                var bridge = _folderBridgeModelFactory.LoadFromDatabase(bridgeId, name, source, destination);
                bridge.MoveSourceAfterCopy = reader.GetInt32(4) != 0;
                foreach (var route in LoadRulesForBridge(bridgeId))
                    bridge.AddRule(route);
                bridge.RestoreTransferHistory(LoadTransferHistory(bridgeId));
                bridges.Add(bridge);
            }
            return bridges;
        }

        private FolderItemModel? LoadFolderById(string folderId)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT FolderId, FolderPath, CustomName FROM FolderItems WHERE FolderId=@FolderId";
            command.Parameters.AddWithValue("@FolderId", folderId);
            using var reader = command.ExecuteReader();
            return reader.Read()
                ? _folderItemModelFactory.LoadFromDatabase(long.Parse(reader.GetString(0)), reader.GetString(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2), string.Empty)
                : null;
        }

        private List<FilterRuleModel> LoadRulesForBridge(long bridgeId)
        {
            var all = LoadRulesForProfileRulesTable(bridgeId);
            return all;
        }

        private List<FilterRuleModel> LoadRulesForProfileRulesTable(long bridgeId)
        {
            var rules = new List<FilterRuleModel>();
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(), Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared, Pooling = false
            }.ToString();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT RuleId, Name, DestinationPath FROM Rules WHERE BridgeId=@BridgeId";
            command.Parameters.AddWithValue("@BridgeId", bridgeId.ToString());
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var ruleId = long.Parse(reader.GetString(0));
                var rule = new FilterRuleModel(ruleId, reader.GetString(1))
                {
                    DestinationPath = string.Empty
                };
                if (!reader.IsDBNull(2))
                    DeserializeRouteSettings(rule, reader.GetString(2));
                foreach (var criteria in LoadRuleCriteriaForRule(ruleId))
                    rule.AddRuleCriteria(criteria);
                rules.Add(rule);
            }
            return rules;
        }

        private static void DeserializeRouteSettings(FilterRuleModel rule, string value)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                if (settings == null) return;
                if (settings.TryGetValue("DestinationPath", out var destination))
                    rule.DestinationPath = destination.GetString() ?? string.Empty;
                if (settings.TryGetValue("PathMode", out var pathMode) && Enum.TryParse(pathMode.GetString(), out FilterRuleModel.DestinationPathMode mode))
                    rule.PathMode = mode;
                if (settings.TryGetValue("DateFolderPattern", out var pattern))
                    rule.DateFolderPattern = pattern.GetString() ?? "yyyy-MM-dd";
                if (settings.TryGetValue("UseCreationTime", out var useCreationTime))
                    rule.UseCreationTime = useCreationTime.GetBoolean();
            }
            catch
            {
                rule.DestinationPath = value;
            }
        }

        /// <summary>
        /// Deletes the profile with the specified identifier, including all associated rules, bridges, and transfer history.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="profileId"/> is null or whitespace.</exception>
        public void DeleteProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                throw new ArgumentException("A profile ID is required.", nameof(profileId));

            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(),
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var ruleIds = new List<string>();
                using (var selectRules = connection.CreateCommand())
                {
                    selectRules.Transaction = transaction;
                    selectRules.CommandText = "SELECT RuleId FROM Rules WHERE ProfileId = @ProfileId";
                    selectRules.Parameters.AddWithValue("@ProfileId", profileId);
                    using var reader = selectRules.ExecuteReader();
                    while (reader.Read())
                        ruleIds.Add(reader.GetString(0));
                }

                foreach (var ruleId in ruleIds)
                {
                    using var deleteCriteria = connection.CreateCommand();
                    deleteCriteria.Transaction = transaction;
                    deleteCriteria.CommandText = "DELETE FROM RuleCriteria WHERE RuleId = @RuleId";
                    deleteCriteria.Parameters.AddWithValue("@RuleId", ruleId);
                    deleteCriteria.ExecuteNonQuery();
                }

                using (var deleteRules = connection.CreateCommand())
                {
                    deleteRules.Transaction = transaction;
                    deleteRules.CommandText = "DELETE FROM Rules WHERE ProfileId = @ProfileId";
                    deleteRules.Parameters.AddWithValue("@ProfileId", profileId);
                    deleteRules.ExecuteNonQuery();
                }

                using (var deleteBridges = connection.CreateCommand())
                {
                    deleteBridges.Transaction = transaction;
                    deleteBridges.CommandText = "DELETE FROM FolderItemBridges WHERE ProfileId = @ProfileId";
                    deleteBridges.Parameters.AddWithValue("@ProfileId", profileId);
                    deleteBridges.ExecuteNonQuery();
                }

                using (var deleteTransferHistory = connection.CreateCommand())
                {
                    deleteTransferHistory.Transaction = transaction;
                    deleteTransferHistory.CommandText = "DELETE FROM TransferHistory WHERE BridgeId NOT IN (SELECT BridgeId FROM FolderItemBridges)";
                    deleteTransferHistory.ExecuteNonQuery();
                }

                using (var deleteProfile = connection.CreateCommand())
                {
                    deleteProfile.Transaction = transaction;
                    deleteProfile.CommandText = "DELETE FROM Profiles WHERE ProfileId = @ProfileId";
                    deleteProfile.Parameters.AddWithValue("@ProfileId", profileId);
                    if (deleteProfile.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException($"Profile '{profileId}' was not found.");
                }

                transaction.Commit();
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Loads data from the Devices table in the database into the application model.
        /// </summary>
        public void LoadDataFromInterfaceDeviceTable()
        {
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DeviceType, DevicePath, DeviceName FROM Devices";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var deviceType = (DeviceModel.DeviceTypes)reader.GetInt32(0);
                string? devicePath = reader.IsDBNull(1) ? null : reader.GetString(1);
                string? deviceName = reader.IsDBNull(2) ? null : reader.GetString(2);
                if (!Enum.IsDefined(deviceType) || string.IsNullOrWhiteSpace(devicePath) || string.IsNullOrWhiteSpace(deviceName))
                    continue;

                DeviceService.AddDevice(deviceType, deviceName, devicePath);
            }
            connection.Close();
        }

        /// <summary>
        /// Saves the specified filter rule for the given profile identifier.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <param name="rule">The filter rule model to save.</param>
        public void SaveFilterRule(string profileId, FilterRuleModel rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                string ruleId = rule.Id.ToString();
                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "SELECT COUNT(1) FROM Rules WHERE RuleId = @RuleId";
                checkCmd.Parameters.AddWithValue("@RuleId", ruleId);
                var exists = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0) > 0;

                // Some existing DBs may have mismatched foreign key definitions. To avoid failing the save in that case,
                // temporarily disable foreign key enforcement for this connection while performing the upsert.
                try
                {
                    using var pragmaOff = connection.CreateCommand();
                    pragmaOff.CommandText = "PRAGMA foreign_keys = OFF;";
                    pragmaOff.ExecuteNonQuery();
                }
                catch { }

                try
                {
                    if (exists)
                    {
                        using var upd = connection.CreateCommand();
                        upd.Transaction = transaction;
                        upd.CommandText = "UPDATE Rules SET Name=@Name, ProfileId=@ProfileId, DestinationPath=@DestinationPath, Enabled=@Enabled WHERE RuleId=@RuleId";
                        upd.Parameters.AddWithValue("@Name", rule.Name ?? string.Empty);
                        upd.Parameters.AddWithValue("@ProfileId", profileId ?? string.Empty);
                        upd.Parameters.AddWithValue("@DestinationPath", string.Empty);
                        upd.Parameters.AddWithValue("@Enabled", 1);
                        upd.Parameters.AddWithValue("@RuleId", ruleId);
                        upd.ExecuteNonQuery();
                    }
                    else
                    {
                        using var ins = connection.CreateCommand();
                        ins.Transaction = transaction;
                        ins.CommandText = "INSERT INTO Rules (RuleId, ProfileId, Name, DestinationPath, Enabled) VALUES (@RuleId,@ProfileId,@Name,@DestinationPath,@Enabled)";
                        ins.Parameters.AddWithValue("@RuleId", ruleId);
                        ins.Parameters.AddWithValue("@ProfileId", profileId ?? string.Empty);
                        ins.Parameters.AddWithValue("@Name", rule.Name ?? string.Empty);
                        ins.Parameters.AddWithValue("@DestinationPath", string.Empty);
                        ins.Parameters.AddWithValue("@Enabled", 1);
                        ins.ExecuteNonQuery();
                    }
                }
                finally
                {
                    try
                    {
                        using var pragmaOn = connection.CreateCommand();
                        pragmaOn.CommandText = "PRAGMA foreign_keys = ON;";
                        pragmaOn.ExecuteNonQuery();
                    }
                    catch { }
                }

                // delete existing criteria and re-insert
                using (var del = connection.CreateCommand())
                {
                    del.Transaction = transaction;
                    del.CommandText = "DELETE FROM RuleCriteria WHERE RuleId = @RuleId";
                    del.Parameters.AddWithValue("@RuleId", ruleId);
                    del.ExecuteNonQuery();
                }

                foreach (var criteria in rule.Criterias)
                {
                    using var insertCriteria = connection.CreateCommand();
                    insertCriteria.Transaction = transaction;
                    insertCriteria.CommandText = "INSERT INTO RuleCriteria (CriteriaId, RuleId, Type, Value) VALUES (@CriteriaId,@RuleId,@Type,@Value)";
                    insertCriteria.Parameters.AddWithValue("@CriteriaId", criteria.Id.ToString());
                    insertCriteria.Parameters.AddWithValue("@RuleId", ruleId);
                    insertCriteria.Parameters.AddWithValue("@Type", criteria.GetType().Name);
                    insertCriteria.Parameters.AddWithValue("@Value", SerializeCriteriaValue(criteria));
                    insertCriteria.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }

        private string SerializeCriteriaValue(FilterRuleCriteriaBaseModel c)
        {
            if (c is FilterRuleCriteriaModelNone) return string.Empty;
            if (c is FilterRuleCriteriaModelExtension ext) return JsonSerializer.Serialize(new { ext.Extensions });
            if (c is FilterRuleCriteriaModelFileName fn) return JsonSerializer.Serialize(new { fn.Pattern, fn.UseRegex });
            if (c is FilterRuleCriteriaModelMinDate min) return JsonSerializer.Serialize(new { min.MinDate, min.UseCreationTime });
            if (c is FilterRuleCriteriaModelMaxDate max) return JsonSerializer.Serialize(new { MaxDate = max.MaxDate, max.UseCreationTime });
            return string.Empty;
        }

        /// <summary>
        /// Loads the rules for the specified profile identifier.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <returns>A list of filter rule models associated with the profile.</returns>
        public List<FilterRuleModel> LoadRulesForProfile(long profileId)
        {
            var result = new List<FilterRuleModel>();
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT RuleId, Name FROM Rules WHERE ProfileId = @ProfileId";
            cmd.Parameters.AddWithValue("@ProfileId", profileId.ToString());
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long id = long.Parse(reader.GetString(0));
                string name = reader.GetString(1);
                var rule = new FilterRuleModel(id, name);
                // load criteria
                var criteria = LoadRuleCriteriaForRule(id);
                foreach (var c in criteria)
                    rule.AddRuleCriteria(c);
                result.Add(rule);
            }

            connection.Close();
            return result;
        }

        /// <summary>
        /// Deletes the rule with the specified identifier.
        /// </summary>
        /// <param name="ruleId">The rule identifier.</param>
        public void DeleteRule(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId)) return;

            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                using var delCriteria = connection.CreateCommand();
                delCriteria.Transaction = transaction;
                delCriteria.CommandText = "DELETE FROM RuleCriteria WHERE RuleId = @RuleId";
                delCriteria.Parameters.AddWithValue("@RuleId", ruleId);
                delCriteria.ExecuteNonQuery();

                using var delRule = connection.CreateCommand();
                delRule.Transaction = transaction;
                delRule.CommandText = "DELETE FROM Rules WHERE RuleId = @RuleId";
                delRule.Parameters.AddWithValue("@RuleId", ruleId);
                delRule.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
            finally
            {
                try { connection.Close(); } catch { }
            }
        }

        private List<FilterRuleCriteriaBaseModel> LoadRuleCriteriaForRule(long ruleId)
        {
            var list = new List<FilterRuleCriteriaBaseModel>();
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT CriteriaId, Type, Value FROM RuleCriteria WHERE RuleId = @RuleId";
            cmd.Parameters.AddWithValue("@RuleId", ruleId.ToString());
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long cid = long.Parse(reader.GetString(0));
                string type = reader.GetString(1);
                string value = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

                FilterRuleCriteriaBaseModel? c = DeserializeCriteria(cid, type, value);
                if (c != null) list.Add(c);
            }

            connection.Close();
            return list;
        }

        private FilterRuleCriteriaBaseModel? DeserializeCriteria(long id, string type, string value)
        {
            try
            {
                switch (type)
                {
                    case nameof(FilterRuleCriteriaModelNone):
                    case "FilterRuleCriteriaNone":
                        return _filterRuleCriteriaFactory.LoadFromDatabaseNoFilter(id);
                    case nameof(FilterRuleCriteriaModelExtension):
                    case "FilterRuleCriteriaExtension":
                        {
                            var doc = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(value);
                            if (doc != null)
                            {
                                if (!doc.TryGetValue("Extensions", out var s))
                                    throw new Exception("Parameter not found: Extensions");
                                var ext = _filterRuleCriteriaFactory.LoadFromDatabaseExtensionFilter(id, s);
                                return ext;
                            }
                            break;
                        }
                    case nameof(FilterRuleCriteriaModelFileName):
                    case "FilterRuleCriteriaFileName":
                        {
                            var doc = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                            if (doc != null)
                            {
                                if (!doc.TryGetValue("Pattern", out var p) || p.GetString() == null)
                                    throw new Exception($"Parameter not found: Pattern");
                                if (!doc.TryGetValue("UseRegex", out var ur))
                                    throw new Exception($"Parameter not found: Pattern");
                                var fn = _filterRuleCriteriaFactory.LoadFromDatabaseFileNameFilter(id, p.GetString()!, ur.GetBoolean());
                                return fn;
                            }
                            break;
                        }
                    case nameof(FilterRuleCriteriaModelMinDate):
                    case "FilterRuleCriteriaMinDate":
                        {
                            var doc = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                            if (doc != null)
                            {
                                if (!doc.TryGetValue("MinDate", out var minDate) && !doc.TryGetValue("MinDateTicks", out minDate))
                                    throw new Exception("Parameter not found: MinDate");
                                if (!doc.TryGetValue("UseCreationTime", out var useCreateTime))
                                    throw new Exception("Parameter not found: UseCreationTime");
                                var parsedMinDate = minDate.ValueKind == JsonValueKind.Number
                                    ? new DateTime(minDate.GetInt64())
                                    : minDate.GetDateTime();
                                var min = _filterRuleCriteriaFactory.LoadFromDatabaseMinDateFilter(id, parsedMinDate, useCreateTime.GetBoolean());
                                return min;
                            }
                            break;
                        }
                    case nameof(FilterRuleCriteriaModelMaxDate):
                    case "FilterRuleCriteriaMaxDate":
                        {
                            var doc = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                            if (doc != null)
                            {
                                if (!doc.TryGetValue("MaxDate", out var maxDate) && !doc.TryGetValue("MaxDateTicks", out maxDate))
                                    throw new Exception("Parameter not found: MaxDate");
                                if (!doc.TryGetValue("UseCreationTime", out var useCreateTime))
                                    throw new Exception("Parameter not found: UseCreationTime");
                                var parsedMaxDate = maxDate.ValueKind == JsonValueKind.Number
                                    ? new DateTime(maxDate.GetInt64())
                                    : maxDate.GetDateTime();
                                var max = _filterRuleCriteriaFactory.LoadFromDatabaseMaxDateFilter(id, parsedMaxDate, useCreateTime.GetBoolean());
                                return max;
                            }
                            break;
                        }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Saves the specified folder item to the database.
        /// </summary>
        /// <param name="folder">The folder item model to save.</param>
        public void SaveFolder(FolderItemModel folder)
        {
            ArgumentNullException.ThrowIfNull(folder);
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                string folderId = folder.Id.ToString();
                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "SELECT COUNT(1) FROM FolderItems WHERE FolderId = @FolderId";
                checkCmd.Parameters.AddWithValue("@FolderId", folderId);
                var exists = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0) > 0;

                if (exists)
                {
                    using var upd = connection.CreateCommand();
                    upd.Transaction = transaction;
                    upd.CommandText = "UPDATE FolderItems SET FolderPath=@FolderPath, CustomName=@CustomName WHERE FolderId=@FolderId";
                    upd.Parameters.AddWithValue("@FolderId", folderId);
                    upd.Parameters.AddWithValue("@FolderPath", folder.FolderPath ?? string.Empty);
                    upd.Parameters.AddWithValue("@CustomName", folder.CustomName ?? string.Empty);
                    upd.ExecuteNonQuery();
                }
                else
                {
                    using var ins = connection.CreateCommand();
                    ins.Transaction = transaction;
                    ins.CommandText = "INSERT INTO FolderItems (FolderId, FolderPath, CustomName) VALUES (@FolderId,@FolderPath,@CustomName)";
                    ins.Parameters.AddWithValue("@FolderId", folderId);
                    ins.Parameters.AddWithValue("@FolderPath", folder.FolderPath ?? string.Empty);
                    ins.Parameters.AddWithValue("@CustomName", folder.CustomName ?? string.Empty);
                    ins.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Loads data from the FolderItems table in the database into the application model.
        /// </summary>
        public void LoadDataFromFolderItemsTable()
        {
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT FolderId, FolderPath, CustomName FROM FolderItems";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // Read folder data and add to the corresponding profile's SourceFolders collection
                long folderId = reader.GetInt64(0);
                string folderPath = reader.GetString(1);
                string customName = reader.GetString(2);
                string customDescription = string.Empty;

                _folderItemModelFactory.LoadFromDatabase(folderId, folderPath, customName, customDescription);
            }
            connection.Close();
        }

        /// <summary>
        /// Saves the specified profile asynchronously, including its rules and folder bridges.
        /// </summary>
        /// <param name="profile">The profile data to save.</param>
        /// <param name="batchSize">The batch size for processing rules and bridges. Default is 200.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="profile"/> is null.</exception>
        public async Task SaveProfileAsync(ProfileItemModel profile, int batchSize = 200)
        {
            if (profile is null)
                throw new ArgumentNullException(nameof(profile));

            const int BatchSizeDefault = 200; // kept for reference
            int BatchSize = batchSize > 0 ? batchSize : BatchSizeDefault;

            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            try
            {
                string profileId = profile.Id.ToString();

                // Upsert profile in its own small transaction
                using (var tx = connection.BeginTransaction())
                {
                    using var checkCmd = connection.CreateCommand();
                    checkCmd.Transaction = tx;
                    checkCmd.CommandText = "SELECT COUNT(1) FROM Profiles WHERE ProfileId = @ProfileId";
                    checkCmd.Parameters.AddWithValue("@ProfileId", profileId);
                    var exists = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0) > 0;

                    if (exists)
                    {
                        using var upd = connection.CreateCommand();
                        upd.Transaction = tx;
                        upd.CommandText = "UPDATE Profiles SET Name=@Name, Enabled=@Enabled, LastRun=@LastRun, ConflictHandling=@ConflictHandling WHERE ProfileId=@ProfileId";
                        upd.Parameters.AddWithValue("@Name", profile.Name ?? string.Empty);
                        upd.Parameters.AddWithValue("@Enabled", 1);
                        upd.Parameters.AddWithValue("@LastRun", DBNull.Value);
                        upd.Parameters.AddWithValue("@ConflictHandling", (int)profile.ConflictHandling);
                        upd.Parameters.AddWithValue("@ProfileId", profileId);
                        upd.ExecuteNonQuery();
                    }
                    else
                    {
                        using var ins = connection.CreateCommand();
                        ins.Transaction = tx;
                        ins.CommandText = "INSERT INTO Profiles (ProfileId, Name, Enabled, LastRun, CreatedAt, ConflictHandling) VALUES (@ProfileId,@Name,@Enabled,@LastRun,@CreatedAt,@ConflictHandling)";
                        ins.Parameters.AddWithValue("@ProfileId", profileId);
                        ins.Parameters.AddWithValue("@Name", profile.Name ?? string.Empty);
                        ins.Parameters.AddWithValue("@Enabled", 1);
                        ins.Parameters.AddWithValue("@LastRun", DBNull.Value);
                        ins.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
                        ins.Parameters.AddWithValue("@ConflictHandling", (int)profile.ConflictHandling);
                        ins.ExecuteNonQuery();
                    }

                    await tx.CommitAsync().ConfigureAwait(false);
                }

                //// Save rules in batches
                //var rules = profile.Rules.ToList();
                //for (int i = 0; i < rules.Count; i += BatchSize)
                //{
                //    var batch = rules.Skip(i).Take(BatchSize).ToList();
                //    using var tx = connection.BeginTransaction();
                //    try
                //    {
                //        foreach (var rule in batch)
                //        {
                //            string ruleId = rule.Id.ToString();

                //            // Upsert rule
                //            using var checkRule = connection.CreateCommand();
                //            checkRule.Transaction = tx;
                //            checkRule.CommandText = "SELECT COUNT(1) FROM Rules WHERE RuleId = @RuleId";
                //            checkRule.Parameters.AddWithValue("@RuleId", ruleId);
                //            var existsRule = Convert.ToInt64(checkRule.ExecuteScalar() ?? 0) > 0;

                //            if (existsRule)
                //            {
                //                using var upd = connection.CreateCommand();
                //                upd.Transaction = tx;
                //                upd.CommandText = "UPDATE Rules SET Name=@Name, ProfileId=@ProfileId, DestinationPath=@DestinationPath, Enabled=@Enabled WHERE RuleId=@RuleId";
                //                upd.Parameters.AddWithValue("@Name", rule.RuleName ?? string.Empty);
                //                upd.Parameters.AddWithValue("@ProfileId", profileId);
                //                upd.Parameters.AddWithValue("@DestinationPath", string.Empty);
                //                upd.Parameters.AddWithValue("@Enabled", 1);
                //                upd.Parameters.AddWithValue("@RuleId", ruleId);
                //                upd.ExecuteNonQuery();
                //            }
                //            else
                //            {
                //                using var ins = connection.CreateCommand();
                //                ins.Transaction = tx;
                //                ins.CommandText = "INSERT INTO Rules (RuleId, ProfileId, Name, DestinationPath, Enabled) VALUES (@RuleId,@ProfileId,@Name,@DestinationPath,@Enabled)";
                //                ins.Parameters.AddWithValue("@RuleId", ruleId);
                //                ins.Parameters.AddWithValue("@ProfileId", profileId);
                //                ins.Parameters.AddWithValue("@Name", rule.RuleName ?? string.Empty);
                //                ins.Parameters.AddWithValue("@DestinationPath", string.Empty);
                //                ins.Parameters.AddWithValue("@Enabled", 1);
                //                ins.ExecuteNonQuery();
                //            }

                //            // Replace criteria for the rule
                //            using (var del = connection.CreateCommand())
                //            {
                //                del.Transaction = tx;
                //                del.CommandText = "DELETE FROM RuleCriteria WHERE RuleId = @RuleId";
                //                del.Parameters.AddWithValue("@RuleId", ruleId);
                //                del.ExecuteNonQuery();
                //            }

                //            foreach (var c in rule.Criterias)
                //            {
                //                string criteriaId = c.Id.ToString();
                //                string type = c.GetType().Name;
                //                string value = SerializeCriteriaValue(c);

                //                using var icmd = connection.CreateCommand();
                //                icmd.Transaction = tx;
                //                icmd.CommandText = "INSERT INTO RuleCriteria (CriteriaId, RuleId, Type, Value) VALUES (@CriteriaId,@RuleId,@Type,@Value)";
                //                icmd.Parameters.AddWithValue("@CriteriaId", criteriaId);
                //                icmd.Parameters.AddWithValue("@RuleId", ruleId);
                //                icmd.Parameters.AddWithValue("@Type", type);
                //                icmd.Parameters.AddWithValue("@Value", value);
                //                icmd.ExecuteNonQuery();
                //            }
                //        }

                //        await tx.CommitAsync().ConfigureAwait(false);
                //    }
                //    catch
                //    {
                //        try { tx.Rollback(); } catch { }
                //        throw;
                //    }
                //}

                // Save folder bridges in batches
                var folderBridges = profile.GetFolderBridges();
                for (int i = 0; i < folderBridges.Count; i += BatchSize)
                {
                    var batch = folderBridges.Skip(i).Take(BatchSize).ToList();
                    using var tx = connection.BeginTransaction();
                    try
                    {
                        foreach (var bridge in batch)
                        {
                            string bridgeId = bridge.Id.ToString();
                            string? sourceFolderId = bridge.SourceFolder?.Id.ToString();
                            string? destinationFolderId = bridge.DestinationFolder?.Id.ToString();

                            using (var checkF = connection.CreateCommand())
                            {
                                checkF.Transaction = tx;
                                checkF.CommandText = "SELECT COUNT(1) FROM FolderItemBridges WHERE BridgeId = @BridgeId";
                                checkF.Parameters.AddWithValue("@BridgeId", bridgeId);
                                var existsF = Convert.ToInt64(checkF.ExecuteScalar() ?? 0) > 0;
                                
                                if (existsF)
                                {
                                    using var upd = connection.CreateCommand();
                                    upd.Transaction = tx;
                                    upd.CommandText = "UPDATE FolderItemBridges SET CustomName=@CustomName, ProfileId=@ProfileId, SourceFolderId=@SourceFolderId, DestinationFolderId=@DestinationFolderId, MoveSourceAfterCopy=@MoveSourceAfterCopy WHERE BridgeId=@BridgeId";
                                    upd.Parameters.AddWithValue("@BridgeId", bridgeId);
                                    upd.Parameters.AddWithValue("@CustomName", bridge.CustomName);
                                    upd.Parameters.AddWithValue("@ProfileId", profileId);
                                    upd.Parameters.AddWithValue("@SourceFolderId", (object?)sourceFolderId ?? DBNull.Value);
                                    upd.Parameters.AddWithValue("@DestinationFolderId", (object?)destinationFolderId ?? DBNull.Value);
                                    upd.Parameters.AddWithValue("@MoveSourceAfterCopy", bridge.MoveSourceAfterCopy ? 1 : 0);
                                    upd.ExecuteNonQuery();
                                }
                                else
                                {
                                    using var ins = connection.CreateCommand();
                                    ins.Transaction = tx;
                                    ins.CommandText = "INSERT INTO FolderItemBridges (BridgeId, CustomName, ProfileId, SourceFolderId, DestinationFolderId, MoveSourceAfterCopy) VALUES (@BridgeId,@CustomName,@ProfileId,@SourceFolderId,@DestinationFolderId,@MoveSourceAfterCopy)";
                                    ins.Parameters.AddWithValue("@BridgeId", bridgeId);
                                    ins.Parameters.AddWithValue("@CustomName", bridge.CustomName);
                                    ins.Parameters.AddWithValue("@ProfileId", profileId);
                                    ins.Parameters.AddWithValue("@SourceFolderId", (object?)sourceFolderId ?? DBNull.Value);
                                    ins.Parameters.AddWithValue("@DestinationFolderId", (object?)destinationFolderId ?? DBNull.Value);
                                    ins.Parameters.AddWithValue("@MoveSourceAfterCopy", bridge.MoveSourceAfterCopy ? 1 : 0);
                                    ins.ExecuteNonQuery();
                                }
                            }
                        }

                        await tx.CommitAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        // Simple DTO for copy queue records
        public class CopyQueueRecord
        {
            public long QueueId { get; set; }
            public string FilePath { get; set; } = string.Empty;
            public int Status { get; set; }
            public int FailureReason { get; set; }
            public long FolderBridgeId { get; set; }
            public long FileSize { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTime LastWriteTime { get; set; }
            public int RetryCount { get; set; }
        }

        // Save or update a copy queue record (synchronous)
        /// <summary>
        /// Saves or updates a copy queue record in the database.
        /// </summary>
        /// <param name="record">The copy queue record to save or update.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="record"/> is null.</exception>
        public void SaveOrUpdateCopyQueueRecord(CopyQueueRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                string qid = record.QueueId.ToString();
                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "SELECT COUNT(1) FROM CopyQueue WHERE QueueId = @QueueId";
                checkCmd.Parameters.AddWithValue("@QueueId", qid);
                var exists = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0) > 0;

                if (exists)
                {
                    using var upd = connection.CreateCommand();
                    upd.Transaction = transaction;
                    upd.CommandText = @"UPDATE CopyQueue SET FolderBridgeId=@FolderBridgeId, FilePath=@FilePath, Status=@Status, FailureReason=@FailureReason, FileSize=@FileSize, CreationTime=@CreationTime, LastWriteTime=@LastWriteTime, RetryCount=@RetryCount WHERE QueueId=@QueueId";
                    upd.Parameters.AddWithValue("@QueueId", qid);
                    upd.Parameters.AddWithValue("@FolderBridgeId", record.FolderBridgeId);
                    upd.Parameters.AddWithValue("@FilePath", record.FilePath ?? string.Empty);
                    upd.Parameters.AddWithValue("@Status", record.Status);
                    upd.Parameters.AddWithValue("@FailureReason", record.FailureReason);
                    upd.Parameters.AddWithValue("@FileSize", record.FileSize);
                    upd.Parameters.AddWithValue("@CreationTime", DateTime.UtcNow.ToString("o"));
                    upd.Parameters.AddWithValue("@LastWriteTime", DateTime.UtcNow.ToString("o"));
                    upd.Parameters.AddWithValue("@RetryCount", record.RetryCount);
                    upd.ExecuteNonQuery();
                }
                else
                {
                    using var ins = connection.CreateCommand();
                    ins.Transaction = transaction;
                    ins.CommandText = $"INSERT INTO CopyQueue (QueueId, FolderBridgeId, FilePath, Status, FailureReason, FileSize, CreationTime, LastWriteTime, RetryCount)\n" +
                        $"VALUES (@QueueId,@FolderBridgeId,@FilePath,@Status,@FailureReason,@FileSize,@CreationTime,@LastWriteTime,@RetryCount)";
                    ins.Parameters.AddWithValue("@FolderBridgeId", record.FolderBridgeId);
                    ins.Parameters.AddWithValue("@FilePath", record.FilePath ?? string.Empty);
                    ins.Parameters.AddWithValue("@Status", record.Status);
                    ins.Parameters.AddWithValue("@FailureReason", record.FailureReason);
                    ins.Parameters.AddWithValue("@FileSize", record.FileSize);
                    ins.Parameters.AddWithValue("@CreationTime", DateTime.UtcNow.ToString("o"));
                    ins.Parameters.AddWithValue("@LastWriteTime", DateTime.UtcNow.ToString("o"));
                    ins.Parameters.AddWithValue("@RetryCount", record.RetryCount);
                    ins.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        // Load pending/in-progress copy queue items
        /// <summary>
        /// Loads the pending and in-progress file items from the copy queue.
        /// </summary>
        /// <returns>A list of copy queue records representing the pending file items.</returns>
        public List<CopyQueueRecord> LoadPendingFileItems()
        {
            var list = new List<CopyQueueRecord>();
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            // Load items with status Pending(0) or InProgress(1)
            cmd.CommandText = "SELECT QueueId, FolderBridgeId, FilePath, Status, FailureReason, FileSize, CreationTime, LastWriteTime, RetryCount FROM CopyQueue WHERE Status IN (0,1)";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var rec = new CopyQueueRecord
                {
                    QueueId = reader.GetInt64(0),
                    FolderBridgeId = reader.GetInt64(1),
                    FilePath = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Status = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    FailureReason = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    FileSize = reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                    CreationTime = reader.IsDBNull(6) ? DateTime.MinValue : DateTime.Parse(reader.GetString(6)),
                    LastWriteTime = reader.IsDBNull(7) ? DateTime.MinValue : DateTime.Parse(reader.GetString(7)),
                    RetryCount = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                };
                list.Add(rec);
            }
            connection.Close();
            return list;
        }

        /// <summary>
        /// Updates the status and failure reason of the copy queue record with the specified queue identifier.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="status">The new status value.</param>
        /// <param name="failureReason">The new failure reason value.</param>
        public void UpdateCopyQueueRecordStatus(long queueId, int status, int failureReason)
        {
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"UPDATE CopyQueue SET Status=@Status, FailureReason=@FailureReason, UpdatedAt=@UpdatedAt WHERE QueueId=@QueueId";
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@FailureReason", failureReason);
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@QueueId", queueId.ToString());
            cmd.ExecuteNonQuery();
            connection.Close();
        }

        /// <summary>
        /// Deletes the copy queue record with the specified queue identifier.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        public void DeleteCopyQueueRecord(long queueId)
        {
            string dbPath = GetDatabasePath();
            string connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM CopyQueue WHERE QueueId = @QueueId";
            cmd.Parameters.AddWithValue("@QueueId", queueId.ToString());
            cmd.ExecuteNonQuery();
            connection.Close();
        }
    }
}
