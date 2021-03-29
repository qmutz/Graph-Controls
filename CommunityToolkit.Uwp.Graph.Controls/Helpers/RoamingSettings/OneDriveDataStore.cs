using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Uwp.Graph.Common;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Storage;

namespace CommunityToolkit.Uwp.Graph.Helpers.RoamingSettings
{
    /// <summary>
    /// A DataStore for managing roaming settings in OneDrive.
    /// </summary>
    public class OneDriveDataStore : IRoamingSettingsDataStore
    {
        /// <summary>
        /// Get the contents of a file deserialized into a specified type.
        /// </summary>
        /// <typeparam name="T">The type of object contained in the file.</typeparam>
        /// <param name="userId">The id of the target user.</param>
        /// <param name="fileWithExt">The file name with extension.</param>
        /// <returns>A task with the object deserialized from the file contents.</returns>
        public static async Task<T> Retrieve<T>(string userId, string fileWithExt)
        {
            return await OneDriveDataSource.Retrieve<T>(userId, fileWithExt);
        }

        /// <summary>
        /// Update the contents of a remote file.
        /// </summary>
        /// <typeparam name="T">The type of object to store in the file.</typeparam>
        /// <param name="userId">The id of the target user.</param>
        /// <param name="fileWithExt">The name of the file with extension.</param>
        /// <param name="fileContents">The object to store in the file.</param>
        /// <returns>A task upon completion.</returns>
        public static async Task Update<T>(string userId, string fileWithExt, T fileContents)
        {
            await OneDriveDataSource.Update(userId, fileWithExt, fileContents);
        }

        /// <summary>
        /// Delete a remote file.
        /// </summary>
        /// <param name="userId">The id of the target user.</param>
        /// <param name="fileWithExt">The file name with extension.</param>
        /// <returns>A task upon completion.</returns>
        public static async Task Delete(string userId, string fileWithExt)
        {
            await OneDriveDataSource.Delete(userId, fileWithExt);
        }

        /// <inheritdoc />
        public bool AutoSync { get; }

        /// <summary>
        /// Gets the id of the Graph User.
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// Gets the cached key value pairs.
        /// </summary>
        public IDictionary<string, object> Cache { get; private set; }

        private readonly string _fileWithExtension;
        private readonly IObjectSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneDriveDataStore"/> class.
        /// </summary>
        public OneDriveDataStore(string userId, string fileWithExt, IObjectSerializer objectSerializer = null, bool autoSync = false)
        {
            UserId = userId;
            _fileWithExtension = fileWithExt;
            _serializer = objectSerializer ?? new JsonObjectSerializer();
            AutoSync = autoSync;

            Cache = null;
        }

        /// <inheritdoc />
        public Task Create()
        {
            InitCache();

            // We cannot create an empty file in OneDrive, so we will simply init the cache.
            // The file will be created lazily, whenever the data is synced up.
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task Delete()
        {
            // Clear the cache
            Cache = null;

            if (AutoSync)
            {
                // Delete the remote.
                await Delete(UserId, _fileWithExtension);
            }
        }

        /// <inheritdoc />
        public async Task Sync()
        {
            // Get the remote
            var remoteData = await OneDriveDataSource.Retrieve<IDictionary<string, object>>(UserId, _fileWithExtension);

            if (Cache != null)
            {
                // Send updates for all local values, overwriting the remote.
                foreach (string key in Cache.Keys.ToList())
                {
                    if (!remoteData.ContainsKey(key) || !EqualityComparer<object>.Default.Equals(remoteData[key], Cache[key]))
                    {
                        Save(key, Cache[key]);
                    }
                }
            }

            if (remoteData.Keys.Count > 0)
            {
                InitCache();
            }

            // Update local cache with additions from remote
            foreach (string key in remoteData.Keys.ToList())
            {
                if (!Cache.ContainsKey(key))
                {
                    Cache.Add(key, remoteData[key]);
                }
            }
        }

        /// <inheritdoc />
        public virtual bool KeyExists(string key)
        {
            return Cache != null && Cache.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool KeyExists(string compositeKey, string key)
        {
            if (KeyExists(compositeKey))
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Cache[compositeKey];
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
            if (Cache != null && Cache.TryGetValue(key, out object value))
            {
                try
                {
                    return _serializer.Deserialize<T>((string)value);
                }
                catch
                {
                    // Primitive types can't be deserialized.
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }

            return @default;
        }

        /// <inheritdoc />
        public T Read<T>(string compositeKey, string key, T @default = default)
        {
            if (Cache != null)
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Cache[compositeKey];
                if (composite != null)
                {
                    object value = composite[key];
                    if (value != null)
                    {
                        try
                        {
                            return _serializer.Deserialize<T>((string)value);
                        }
                        catch
                        {
                            // Primitive types can't be deserialized.
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                    }
                }
            }

            return @default;
        }

        /// <inheritdoc />
        public void Save<T>(string key, T value)
        {
            InitCache();

            // Skip serialization for primitives.
            if (typeof(T) == typeof(object) || Type.GetTypeCode(typeof(T)) != TypeCode.Object)
            {
                // Update the cache
                Cache[key] = value;
            }
            else
            {
                // Update the cache
                Cache[key] = _serializer.Serialize(value);
            }

            if (AutoSync)
            {
                // Update the remote
                Task.Run(() => Update(UserId, _fileWithExtension, Cache));
            }
        }

        /// <inheritdoc />
        public void Save<T>(string compositeKey, IDictionary<string, T> values)
        {
            InitCache();

            if (KeyExists(compositeKey))
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Cache[compositeKey];

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

                // Update the cache
                Cache[compositeKey] = composite;
            }
            else
            {
                ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
                foreach (KeyValuePair<string, T> setting in values)
                {
                    composite.Add(setting.Key, _serializer.Serialize(setting.Value));
                }

                // Update the cache
                Cache[compositeKey] = composite;
            }

            if (AutoSync)
            {
                // Update the remote
                Task.Run(() => Update(UserId, _fileWithExtension, Cache));
            }
        }

        /// <inheritdoc />
        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var roamingSettings = await Retrieve<string>(UserId, filePath);
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
            return await Retrieve<T>(UserId, filePath) ?? @default;
        }

        /// <inheritdoc />
        public async Task<StorageFile> SaveFileAsync<T>(string filePath, T value)
        {
            await Update(UserId, filePath, value);

            // Can't convert DriveItem to StorageFile, so we return null instead.
            return null;
        }

        private void InitCache()
        {
            if (Cache == null)
            {
                Cache = new Dictionary<string, object>();
            }
        }
    }
}
