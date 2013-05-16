using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Caching;

namespace OpenTraits.Azure
{
    public class QuickDeployService
    {
        public const string CACHE_KEY = "OpenTraits.Azure.QuickDeployService";
        public const int SLEEPMS_ONFAIL = 50;
        public const int RETRY_COUNT = 10;

        static RegisteredWaitHandle RegisteredWaitHandle;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextCheckSeconds">check again after X seconds</param>
        /// <param name="containerName">what container to sync</param>
        public static void CheckAsync(int nextCheckSeconds, string containerName, string rootPath)
        {
            lock (typeof(QuickDeployService)) {
                
                // clear existing handle if already exists
                if (RegisteredWaitHandle != null)
                    RegisteredWaitHandle.Unregister(null);

                var waitHandle = new AutoResetEvent(false);
                RegisteredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                    waitHandle,
                    // Method to execute
                    (state, timeout) => {
                        WhenTimeComes(containerName, rootPath);
                    },
                    // optional state object to pass to the method
                    null,
                    // Execute the method after 5 seconds
                    TimeSpan.FromSeconds(nextCheckSeconds),
                    // Set this to false to execute it repeatedly every 5 seconds
                    false
                );
            }
        }

        public static void CheckSync(int nextCheckSeconds, string containerName, string rootPath)
        {
            while (true) {
                lock (typeof(QuickDeployService)) {
                    WhenTimeComes(containerName, rootPath);
                }
                Thread.Sleep(nextCheckSeconds * 1000);
            }
        }

        static void WhenTimeComes(string containerName, string rootPath)
        {
            // read storage account configuration settings
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) => {
                configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
            });

            //var storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionString");
            //var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["AzureBlobStorage"]);

            // initialize blob storage
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            var container = blobStorage.GetContainerReference(containerName);
            container.CreateIfNotExist();

            // sync with local file system
            Sync(container, rootPath);
        }

        static void Sync(CloudBlobContainer container, string rootPath)
        {
            // iterate all files from storage
            foreach (CloudBlob item in container.ListBlobs(new BlobRequestOptions() { UseFlatBlobListing = true })) {
                try {
                    var localPath = Path.Combine(rootPath, item.Name);

                    if (File.Exists(localPath) && File.GetLastWriteTimeUtc(localPath).ToFileTime() > item.Properties.LastModifiedUtc.ToFileTime())
                        continue; // local file is more recent

                    if (!Directory.Exists(Path.GetDirectoryName(localPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath));

                    for (var i = 0; i < RETRY_COUNT; i++) {
                        try {
                            using (var fs = File.Open(localPath, FileMode.Create)) {
                                item.DownloadToStream(fs);
                                break; // all OK
                            }
                        } catch (IOException) {
                            // retry again later
                            System.Threading.Thread.Sleep(SLEEPMS_ONFAIL);
                        }
                        //catch (UnauthorizedAccessException) {
                        //    // ??? just catch this silently otherwise IIS will crash
                        //}
                    }
                } catch (Exception ex) {
                    NLog.LogManager.GetLogger("optQuick").Error(string.Format("Unable to process path {0} => {1}", rootPath, item.Name));
                    NLog.LogManager.GetLogger("optQuick").Error(ex);
                    throw;
                }
            }
        }
    }
}
