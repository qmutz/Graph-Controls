using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Net.Authentication;
using CommunityToolkit.Net.Graph.Extensions;
using Microsoft.Graph;

namespace CommunityToolkit.Uwp.Graph.Helpers.RoamingSettings
{
    /// <summary>
    /// Helpers for interacting with files in the special OneDrive AppRoot folder.
    /// </summary>
    internal static class OneDriveDataSource
    {
        private static GraphServiceClient Graph => ProviderManager.Instance.GlobalProvider?.Graph();

        /// <summary>
        /// Updates or create a new file on the remote with the provided content.
        /// </summary>
        /// <typeparam name="T">The type of object to save.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<DriveItem> Update<T>(string userId, string fileWithExt, T fileContents)
        {
            var contents = (fileContents is string stringContents)
                ? stringContents
                : Graph.HttpProvider.Serializer.SerializeObject(fileContents);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));

            return await Graph.Users[userId].Drive.Special.AppRoot.ItemWithPath(fileWithExt).Content.Request().PutAsync<DriveItem>(stream);
        }

        /// <summary>
        /// Get a file from the remote.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<T> Retrieve<T>(string userId, string fileWithExt)
        {
            Stream stream = await Graph.Users[userId].Drive.Special.AppRoot.ItemWithPath(fileWithExt).Content.Request().GetAsync();

            if (typeof(T) == typeof(object) || Type.GetTypeCode(typeof(T)) != TypeCode.Object)
            {
                // Skip deserialization for primitives.
                string contents = await new StreamReader(stream).ReadToEndAsync();
                return (T)Convert.ChangeType(contents, typeof(T));
            }

            return Graph.HttpProvider.Serializer.DeserializeObject<T>(stream);
        }

        /// <summary>
        /// Delete the file from the remote.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task Delete(string userId, string fileWithExt)
        {
            await Graph.Users[userId].Drive.Special.AppRoot.ItemWithPath(fileWithExt).Request().DeleteAsync();
        }
    }
}