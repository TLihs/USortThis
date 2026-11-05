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
        public string GetDatabasePath();
        public Task InitializeAsync();
        public bool SaveDataToProfilesTable(ProfileItemModel profile);
        public void DeleteProfile(string profileId);
        public void LoadDataFromProfilesTable();
        public void LoadDataFromInterfaceDeviceTable();
        public void SaveFilterRule(string profileId, FilterRuleModel rule);
        public void SaveBridgeRoute(long profileId, FolderBridgeModel bridge, FilterRuleModel route);
        public void SaveProfileBridges(long profileId, IEnumerable<FolderBridgeModel> bridges);
        public List<FilterRuleModel> LoadRulesForProfile(long profileId);
        public List<FolderBridgeModel> LoadBridgesForProfile(long profileId);
        public void DeleteRule(string ruleId);
        public List<ConfiguredLocationModel> LoadConfiguredLocations();
        public void SaveConfiguredLocation(ConfiguredLocationModel location);
        public void DeleteConfiguredLocation(long locationId);
        public List<FileTransferDetailModel> LoadTransferHistory(long bridgeId);
        public void SaveTransferHistory(long bridgeId, FileTransferDetailModel detail);
        public void DeleteTransferHistory(long bridgeId);
        public void SaveFolder(FolderItemModel folder);
        public void LoadDataFromFolderItemsTable();
        public Task SaveProfileAsync(ProfileItemModel profile, int batchSize = 200);
        public void SaveOrUpdateCopyQueueRecord(DatabaseService.CopyQueueRecord record);
        public List<DatabaseService.CopyQueueRecord> LoadPendingFileItems();
        public void UpdateCopyQueueRecordStatus(long queueId, int status, int failureReason);
        public void DeleteCopyQueueRecord(long queueId);
    }
}
