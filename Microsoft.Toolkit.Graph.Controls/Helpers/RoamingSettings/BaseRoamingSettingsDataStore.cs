using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Toolkit.Graph.Controls.Common;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Storage;

namespace Microsoft.Toolkit.Graph.Helpers.RoamingSettings
{
    /// <summary>
    /// A base class that supports interation with roaming settings data.
    /// </summary>
    public abstract class BaseRoamingSettingsDataStore : IRoamingSettingsDataStore
    {
        /// <inheritdoc />
        public abstract IDictionary<string, object> Settings { get; }

        private IObjectSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRoamingSettingsDataStore"/> class.
        /// </summary>
        /// <param name="objectSerializer"></param>
        public BaseRoamingSettingsDataStore(IObjectSerializer objectSerializer)
        {
            _serializer = objectSerializer ?? new JsonObjectSerializer();
        }

        /// <inheritdoc />
        public abstract Task Create();

        /// <inheritdoc />
        public abstract Task Delete();

        /// <inheritdoc />
        public abstract Task<bool> FileExistsAsync(string filePath);

        /// <inheritdoc />
        public virtual bool KeyExists(string key)
        {
            return Settings.ContainsKey(key);
        }

        /// <inheritdoc />
        public virtual bool KeyExists(string compositeKey, string key)
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
        public virtual T Read<T>(string key, T @default = default)
        {
            if (!Settings.TryGetValue(key, out object value) || value == null)
            {
                return @default;
            }

            return _serializer.Deserialize<T>((string)value);
        }

        /// <inheritdoc />
        public virtual T Read<T>(string compositeKey, string key, T @default = default)
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
        public abstract Task<T> ReadFileAsync<T>(string filePath, T @default = default);

        /// <inheritdoc />
        public virtual void Save<T>(string key, T value)
        {
            Settings[key] = _serializer.Serialize(value);

            // TODO: Sync back to remote.
        }

        /// <inheritdoc />
        public virtual void Save<T>(string compositeKey, IDictionary<string, T> values)
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

            // TODO: Sync back to remote.
        }

        /// <inheritdoc />
        public abstract Task<StorageFile> SaveFileAsync<T>(string filePath, T value);

        /// <inheritdoc />
        public abstract Task Sync();
    }
}
