using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Models;
using USortThis.Core.Services;

namespace USortThis.Core.Interfaces
{
    public interface IDatabaseService
    {
        /// <summary>Gets the path of the application's database file.</summary>
        public string GetDatabasePath();
        /// <summary>Initializes the database and its required schema.</summary>
        public Task InitializeAsync();
        /// <summary>Saves a profile and reports whether the operation succeeded.</summary>
        /// <param name="profile">The profile to persist.</param>
        public bool SaveDataToProfilesTable(ProfileItemModel profile);
        /// <summary>Deletes a profile and its associated persisted data.</summary>
        /// <param name="profileId">The identifier of the profile to delete.</param>
        public void DeleteProfile(string profileId);
        /// <summary>Loads profile records from the database into application state.</summary>
        public void LoadDataFromProfilesTable();
        /// <summary>Loads interface-device records from the database.</summary>
        public void LoadDataFromInterfaceDeviceTable();
        /// <summary>Saves a filter rule for a profile.</summary>
        /// <param name="profileId">The profile identifier that owns the rule.</param>
        /// <param name="rule">The rule to persist.</param>
        public void SaveFilterRule(string profileId, FilterRuleModel rule);
        /// <summary>Saves a route and its bridge association for a profile.</summary>
        /// <param name="profileId">The profile identifier that owns the bridge.</param>
        /// <param name="bridge">The bridge associated with the route.</param>
        /// <param name="route">The route to persist.</param>
        public void SaveBridgeRoute(long profileId, FolderBridgeModel bridge, FilterRuleModel route);
        /// <summary>Saves the bridges belonging to a profile.</summary>
        /// <param name="profileId">The profile identifier that owns the bridges.</param>
        /// <param name="bridges">The bridges to persist.</param>
        public void SaveProfileBridges(long profileId, IEnumerable<FolderBridgeModel> bridges);
        /// <summary>Loads all filter rules associated with a profile.</summary>
        /// <param name="profileId">The identifier of the profile to query.</param>
        public List<FilterRuleModel> LoadRulesForProfile(long profileId);
        /// <summary>Loads all folder bridges associated with a profile.</summary>
        /// <param name="profileId">The identifier of the profile to query.</param>
        public List<FolderBridgeModel> LoadBridgesForProfile(long profileId);
        /// <summary>Deletes a filter rule by its identifier.</summary>
        /// <param name="ruleId">The identifier of the rule to delete.</param>
        public void DeleteRule(string ruleId);
        /// <summary>Loads all configured locations.</summary>
        public List<ConfiguredLocationModel> LoadConfiguredLocations();
        /// <summary>Saves a configured location.</summary>
        /// <param name="location">The location to persist.</param>
        public void SaveConfiguredLocation(ConfiguredLocationModel location);
        /// <summary>Deletes a configured location by its identifier.</summary>
        /// <param name="locationId">The identifier of the location to delete.</param>
        public void DeleteConfiguredLocation(long locationId);
        /// <summary>Loads transfer-history entries for a bridge.</summary>
        /// <param name="bridgeId">The identifier of the bridge to query.</param>
        public List<FileTransferDetailModel> LoadTransferHistory(long bridgeId);
        /// <summary>Saves a transfer-history entry for a bridge.</summary>
        /// <param name="bridgeId">The identifier of the bridge associated with the transfer.</param>
        /// <param name="detail">The transfer details to persist.</param>
        public void SaveTransferHistory(long bridgeId, FileTransferDetailModel detail);
        /// <summary>Deletes all transfer-history entries associated with a bridge.</summary>
        /// <param name="bridgeId">The identifier of the bridge whose history is deleted.</param>
        public void DeleteTransferHistory(long bridgeId);
        /// <summary>Saves a folder record.</summary>
        /// <param name="folder">The folder to persist.</param>
        public void SaveFolder(FolderItemModel folder);
        /// <summary>Loads folder records from the database into application state.</summary>
        public void LoadDataFromFolderItemsTable();
        /// <summary>Asynchronously saves a profile and its related data in batches.</summary>
        /// <param name="profile">The profile to persist.</param>
        /// <param name="batchSize">The maximum number of records written per batch.</param>
        public Task SaveProfileAsync(ProfileItemModel profile, int batchSize = 200);
        /// <summary>Creates or updates a persisted copy-queue record.</summary>
        /// <param name="record">The queue record to save.</param>
        public void SaveOrUpdateCopyQueueRecord(DatabaseService.CopyQueueRecord record);
        /// <summary>Loads copy-queue records that are pending processing.</summary>
        public List<DatabaseService.CopyQueueRecord> LoadPendingFileItems();
        /// <summary>Updates the status and failure reason of a copy-queue record.</summary>
        /// <param name="queueId">The identifier of the queue record.</param>
        /// <param name="status">The new processing status.</param>
        /// <param name="failureReason">The failure reason associated with the status.</param>
        public void UpdateCopyQueueRecordStatus(long queueId, int status, int failureReason);
        /// <summary>Deletes a copy-queue record.</summary>
        /// <param name="queueId">The identifier of the record to delete.</param>
        public void DeleteCopyQueueRecord(long queueId);
    }
}
