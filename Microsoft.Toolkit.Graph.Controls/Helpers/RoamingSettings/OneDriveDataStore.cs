using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Storage;

namespace Microsoft.Toolkit.Graph.Helpers.RoamingSettings
{
    /// <summary>
    /// A DataStore for managing roaming settings in OneDrive.
    /// </summary>
    public class OneDriveDataStore : BaseRoamingSettingsDataStore
    {
        private class SyncData : Dictionary<string, object>
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileWithExt"></param>
        /// <returns></returns>
        public static async Task Create(string fileWithExt)
        {
            await OneDriveDataSource.Create(fileWithExt);
        }

        private const string ROAMING_SETTINGS_FILENAME = "roamingSettings.json";

        private SyncData _syncData;

        /// <inheritdoc />
        public override IDictionary<string, object> Settings => _syncData;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneDriveDataStore"/> class.
        /// </summary>
        public OneDriveDataStore(IObjectSerializer objectSerializer = null)
            : base(objectSerializer)
        {
        }

        /// <inheritdoc />
        public override async Task Create()
        {
            await OneDriveDataSource.Create(ROAMING_SETTINGS_FILENAME);
        }

        /// <inheritdoc />
        public override async Task Delete()
        {
            await OneDriveDataSource.Delete(ROAMING_SETTINGS_FILENAME);
        }

        /// <inheritdoc />
        public override async Task<bool> FileExistsAsync(string filePath)
        {
            var roamingSettings = await OneDriveDataSource.Retrieve<object>(ROAMING_SETTINGS_FILENAME);

            return roamingSettings != null;
        }

        /// <inheritdoc />
        public override async Task<T> ReadFileAsync<T>(string filePath, T @default = default)
        {
            return await OneDriveDataSource.Retrieve<T>(filePath) ?? @default;
        }

        /// <inheritdoc />
        public override async Task<StorageFile> SaveFileAsync<T>(string filePath, T value)
        {
            await OneDriveDataSource.Update(filePath, value);

            // Can't convert DriveItem to StorageFile, so we return null instead.
            return null;
        }

        /// <inheritdoc />
        public override async Task Sync()
        {
            _syncData = await OneDriveDataSource.Retrieve<SyncData>(ROAMING_SETTINGS_FILENAME);
        }
    }
}
