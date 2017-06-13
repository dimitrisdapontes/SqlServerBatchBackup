using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace SqlServerBatchBackup
{
    public class DropboxRepository
    {
        /// <summary>
        /// The dropbox api key used in communication with dropbox
        /// </summary>
        private static string DROPBOX_API_KEY
        {
            get
            {
                return ConfigurationManager.AppSettings["DropboxApiKey"];
            }
        }

        /// <summary>
        /// Indicates whether the backup should be send to Dropbox
        /// </summary>
        internal static bool SendToDropbox
        {
            get
            {
                bool send;
                bool.TryParse(ConfigurationManager.AppSettings["SendToDropbox"], out send);
                return send;
            }
        }

        /// <summary>
        /// Saves a file in Dropbox
        /// </summary>
        /// <param name="fileName">The name of the file to be saved in Dropbox (file name with extension)</param>
        /// <param name="content">The actual file to save</param>
        /// <param name="container">The folder in which the file will be saved</param>
        public async Task Save(string fileName, byte[] content, string container)
        {
            if (!SendToDropbox) return;

            using (var memoryStream = new MemoryStream(content))
            {
                using (var dropboxClient = new DropboxClient(DROPBOX_API_KEY))
                {
                    try
                    {
                        await dropboxClient.Files.UploadAsync(
                            $"/{container}/{fileName}",
                            WriteMode.Overwrite.Instance,
                            body: memoryStream);
                    }
                    catch (ApiException<UploadError> apiEx)
                    {
                        Logger.Write($"Upload error while uploading {fileName} to dropbox. Ex={apiEx}", "DropboxRepository-Save");
                    }
                    catch (Exception ex)
                    {
                        Logger.Write($"Error while uploading {fileName} to dropbox. Ex={ex}", "DropboxRepository-Save");
                    }
                }
            }
        }
    }
}
