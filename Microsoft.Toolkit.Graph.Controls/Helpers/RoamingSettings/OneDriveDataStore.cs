using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Toolkit.Graph.Controls.Common;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Storage;

namespace Microsoft.Toolkit.Graph.Helpers.RoamingSettings
{
    /// <summary>
    /// A DataStore for managing roaming settings in OneDrive.
    /// </summary>
    public class OneDriveDataStore : IRoamingSettingsDataStore
    {
        /// <summary>
        /// Create a new file in the OneDrive special AppRoot folder.
        /// </summary>
        /// <param name="fileWithExt">The file name with extension.</param>
        /// <returns>A task upon completion.</returns>
        public static async Task Create(string fileWithExt)
        {
            await OneDriveDataSource.Create(fileWithExt);
        }

        /// <summary>
        /// Get the contents of a file deserialized into a specified type.
        /// </summary>
        /// <typeparam name="T">The type of object contained in the file.</typeparam>
        /// <param name="fileWithExt">The file name with extension.</param>
        /// <returns>A task with the object deserialized from the file contents.</returns>
        public static async Task<T> Retrieve<T>(string fileWithExt)
        {
            return await OneDriveDataSource.Retrieve<T>(fileWithExt);
        }

        /// <summary>
        /// Get the contents of a file as a string.
        /// </summary>
        /// <param name="fileWithExt">The file name with extension.</param>
        /// <returns>A task with the file contents as a string.</returns>
        public static async Task<string> Retrieve(string fileWithExt)
        {
            return await OneDriveDataSource.Retrieve(fileWithExt);
        }

        /// <summary>
        /// Update the contents of a remote file.
        /// </summary>
        /// <typeparam name="T">The type of object to store in the file.</typeparam>
        /// <param name="fileWithExt">The name of the file with extension.</param>
        /// <param name="fileContents">The object to store in the file.</param>
        /// <returns>A task upon completion.</returns>
        public static async Task Update<T>(string fileWithExt, T fileContents)
        {
            await OneDriveDataSource.Update(fileWithExt, fileContents);
        }

        /// <summary>
        /// Delete a remote file.
        /// </summary>
        /// <param name="fileWithExt">The file name with extension.</param>
        /// <returns>A task upon completion.</returns>
        public static async Task Delete(string fileWithExt)
        {
            await OneDriveDataSource.Delete(fileWithExt);
        }

        /// <inheritdoc />
        public IDictionary<string, object> Settings => _syncData;

        private string _fileWithExtension;
        private IDictionary<string, object> _syncData;
        private IObjectSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneDriveDataStore"/> class.
        /// </summary>
        public OneDriveDataStore(string fileWithExt, IObjectSerializer objectSerializer = null, bool autoSync = false)
        {
            _fileWithExtension = fileWithExt;
            _serializer = objectSerializer ?? new JsonObjectSerializer();
            _syncData = new Dictionary<string, object>();

            if (autoSync)
            {
                Task.Run(Sync);
            }
        }

        /// <inheritdoc />
        public async Task Create()
        {
            await Create(_fileWithExtension);
        }

        /// <inheritdoc />
        public async Task Delete()
        {
            await Delete(_fileWithExtension);
        }

        /// <inheritdoc />
        public async Task Sync()
        {
            _syncData = await OneDriveDataSource.Retrieve<IDictionary<string, object>>(_fileWithExtension);
        }

        /// <inheritdoc />
        public virtual bool KeyExists(string key)
        {
            return Settings.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool KeyExists(string compositeKey, string key)
        {
            if (KeyExists(compositeKey))
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Settings[compositeKey];
                if (composite != null)
                {
                    return composite.ContainsKey(key);
                }
            }

            return false;
        }

        /// <inheritdoc />
        public T Read<T>(string key, T @default = default)
        {
            if (!Settings.TryGetValue(key, out object value) || value == null)
            {
                return @default;
            }

            return _serializer.Deserialize<T>((string)value);
        }

        /// <inheritdoc />
        public T Read<T>(string compositeKey, string key, T @default = default)
        {
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Settings[compositeKey];
            if (composite != null)
            {
                string value = (string)composite[key];
                if (value != null)
                {
                    return _serializer.Deserialize<T>(value);
                }
            }

            return @default;
        }

        /// <inheritdoc />
        public void Save<T>(string key, T value)
        {
            // Set the local cache
            Settings[key] = _serializer.Serialize(value);

            // Send an update to the remote.
            Task.Run(() => Update(_fileWithExtension, Settings));
        }

        /// <inheritdoc />
        public void Save<T>(string compositeKey, IDictionary<string, T> values)
        {
            if (KeyExists(compositeKey))
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Settings[compositeKey];

                foreach (KeyValuePair<string, T> setting in values)
                {
                    if (composite.ContainsKey(setting.Key))
                    {
                        composite[setting.Key] = _serializer.Serialize(setting.Value);
                    }
                    else
                    {
                        composite.Add(setting.Key, _serializer.Serialize(setting.Value));
                    }
                }

                Settings[compositeKey] = composite;
            }
            else
            {
                ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
                foreach (KeyValuePair<string, T> setting in values)
                {
                    composite.Add(setting.Key, _serializer.Serialize(setting.Value));
                }

                Settings[compositeKey] = composite;
            }

            Task.Run(() => Update(_fileWithExtension, Settings));
        }

        /// <inheritdoc />
        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var roamingSettings = await Retrieve(filePath);
                return roamingSettings != null;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<T> ReadFileAsync<T>(string filePath, T @default = default)
        {
            return await Retrieve<T>(filePath) ?? @default;
        }

        /// <inheritdoc />
        public async Task<string> ReadFileAsync(string filePath, string @default = default)
        {
            return await Retrieve(filePath) ?? @default;
        }

        /// <inheritdoc />
        public async Task<StorageFile> SaveFileAsync<T>(string filePath, T value)
        {
            await Update(filePath, value);

            // Can't convert DriveItem to StorageFile, so we return null instead.
            return null;
        }
    }
}
