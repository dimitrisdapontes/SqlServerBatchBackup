using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SqlServerBatchBackup
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach(ConnectionStringSettings x in ConfigurationManager.ConnectionStrings)
            {                
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(x.ConnectionString);

                Console.WriteLine($"Processing database {builder.InitialCatalog}");

                if (CheckConnection(builder))
                {
                    try
                    {
                        BackupDatabases(builder).Wait();
                    }
                    catch (Exception ex)
                    {
                        Logger.Write($"Error while backing up database {builder.InitialCatalog}. Ex={ex}", "Backup File");
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether a connection to the database can be made
        /// (true = connection OK, otherwise false)
        /// </summary>
        /// <param name="builder">The builder that contains all the info about the connection to the database</param>
        private static bool CheckConnection(SqlConnectionStringBuilder builder)
        {
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch(Exception ex)
                {
                    Logger.Write($"Unable to open connection to {builder.InitialCatalog}. Ex={ex}", "CheckConnection");
                    return false;
                }
            }
        }

        /// <summary>
        /// Creates a backup for a database
        /// </summary>
        /// <param name="builder">The builder that contains all the info about the connection to the database</param>
        static async Task BackupDatabases(SqlConnectionStringBuilder builder)
        {
            var backupFolder = ConfigurationManager.AppSettings["BackupFolder"];
            var backupPath = Path.Combine(backupFolder, $"{builder.InitialCatalog}-{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.bak");

            // create backup
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string sql = $"BACKUP DATABASE {builder.InitialCatalog} TO DISK='{backupPath}'";

                using (var command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    command.CommandTimeout = connection.ConnectionTimeout;
                    command.ExecuteNonQuery();
                }

                Console.WriteLine($"\t Database {builder.InitialCatalog} backup OK");
            }

            // zip the backup file
            string zipFileName = $"{Path.GetFileNameWithoutExtension(backupPath)}.zip";
            string zipFilePath = Path.Combine(backupFolder, zipFileName);
            using (FileStream fs = new FileStream(zipFilePath, FileMode.Create))
            using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                arch.CreateEntryFromFile(backupPath, Path.GetFileName(backupPath));

                Console.WriteLine($"\t Database {builder.InitialCatalog} backup zipped OK");
            }

            if (DropboxRepository.SendToDropbox)
            {
                // send zipped backup to dropbox
                var dropboxRepository = new DropboxRepository();
                await dropboxRepository.Save(zipFileName, File.ReadAllBytes(zipFilePath), builder.InitialCatalog);

                Console.WriteLine($"\t Database {builder.InitialCatalog} zipped backup sent to Dropbox OK");
            }
            
            // delete backup files
            File.Delete(backupPath);
            File.Delete(zipFilePath);
        }
    }
}
