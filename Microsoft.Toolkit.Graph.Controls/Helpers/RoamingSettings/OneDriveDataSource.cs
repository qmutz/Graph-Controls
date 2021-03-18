using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Toolkit.Graph.Providers;

namespace Microsoft.Toolkit.Graph.Helpers.RoamingSettings
{
    /// <summary>
    /// Helpers for interacting with files in the special OneDrive AppRoot folder.
    /// </summary>
    internal static class OneDriveDataSource
    {
        private static GraphServiceClient Graph => ProviderManager.Instance.GlobalProvider?.Graph;

        /// <summary>
        /// OneDrive will not accept empty files, so we cannot simply create a dummy file.
        /// Use Update to create a new file with contents.
        /// </summary>
        /// <param name="fileWithExt">The name of the file with extension.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task Create(string fileWithExt)
        {
            throw new NotImplementedException("Cannot create an empty file. Use Update to create a new file with contents.");
        }

        /// <summary>
        /// Updates or create a new file on the remote with the provided content.
        /// </summary>
        /// <typeparam name="T">The type of object to save.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<DriveItem> Update<T>(string fileWithExt, T fileContents)
        {
            var contents = (fileContents is string stringContents)
                ? stringContents
                : Graph.HttpProvider.Serializer.SerializeObject(fileContents);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));

            return await Graph.Me.Drive.Special.AppRoot.ItemWithPath(fileWithExt).Content.Request().PutAsync<DriveItem>(stream);
        }

        /// <summary>
        /// Get a file from the remote.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<T> Retrieve<T>(string fileWithExt)
        {
            Stream stream = await Graph.Me.Drive.Special.AppRoot.ItemWithPath(fileWithExt).Content.Request().GetAsync();

            return Graph.HttpProvider.Serializer.DeserializeObject<T>(stream);
        }

        /// <summary>
        /// Get a file from the remote.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<string> Retrieve(string fileWithExt)
        {
            Stream stream = await Graph.Me.Drive.Special.AppRoot.ItemWithPath(fileWithExt).Content.Request().GetAsync();

            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Delete the file from the remote.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task Delete(string fileWithExt)
        {
            await Graph.Me.Drive.Special.AppRoot.ItemWithPath(fileWithExt).Request().DeleteAsync();
        }
    }
}