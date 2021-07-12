using Azure;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using WebSocket4Net.Command;

namespace UploadDoenload.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlobStorageController : ControllerBase
    {
        //containerName = "filesharecontainer"; 
        string accountName = "filesharecontainer";
        public static bool PolicyCreationStatus;

            [HttpPut("CreateAccessPolicy")]
       
            public async Task<IActionResult> CreateStoredAccessPolicyAsync(string containerName)
            {
                string connectionString = "DefaultEndpointsProtocol=https;AccountName=safedocs4u;AccountKey=6km3JJnntkHwdEj5IE0aFEjKETQmv/tRN5ToUialDJzUJkJBWOjoNUBCEYmR8VSy6fCyv81UUd+IRrH0g7umag==;EndpointSuffix=core.windows.net";

                // Use the connection string to authorize the operation to create the access policy.
                // Azure AD does not support the Set Container ACL operation that creates the policy.
                BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

                
                try
                {
                    await containerClient.CreateIfNotExistsAsync();

                    // Create one or more stored access policies.
                    List<BlobSignedIdentifier> signedIdentifiers = new List<BlobSignedIdentifier>
                    {
                        new BlobSignedIdentifier
                        {
                            Id = "usershareaccesspolicy",
                            AccessPolicy = new BlobAccessPolicy
                            {
                                StartsOn = DateTimeOffset.UtcNow.AddHours(-1),
                                ExpiresOn = DateTimeOffset.UtcNow.AddDays(3),//sets 3 days policy
                                Permissions = "rw"
                            }
                        }

                    };
                    // Set the container's access policy.
                    await containerClient.SetAccessPolicyAsync(permissions: signedIdentifiers);//actually does work
                    if (containerClient.SetAccessPolicyAsync(permissions: signedIdentifiers).IsCompleted)
                    {
                    PolicyCreationStatus = true;
                    }

                    
                }
                catch (RequestFailedException e)
                {
                    Console.WriteLine(e.ErrorCode);
                    Console.WriteLine(e.Message);
                    PolicyCreationStatus = false;
                }
               //need to review this if statement
                if (PolicyCreationStatus == true) 
                {
                    return Content("did not work");
                }
                else 
                {
                    return Content("Hooray it works");
                }


            /////////////////////////////////////////////////
            ////////////////////////////////////////////////
            /////Now get the file url path
            /////Use SAS to authenticate and give access to file
            /////////////////////////////////////////////////
            ////////////////////////////////////////////////
            ///



            //create an instance of the DefaultAzureCredential class.
            //Get an authenticated token credential
            // Construct the blob endpoint from the account name.
            string blobEndpoint = string.Format("https://{0}.blob.core.windows.net", accountName);

            // Create a new Blob service client with Azure AD credentials.
            BlobServiceClient blobClient = new BlobServiceClient(new Uri(blobEndpoint),
                                                                 new DefaultAzureCredential());

            // Get a user delegation key for the Blob service that's valid for seven days.
            // You can use the key to generate any number of shared access signatures over the lifetime of the key.
            Azure.Storage.Blobs.Models.UserDelegationKey key = await blobClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                               DateTimeOffset.UtcNow.AddDays(7));

            // Read the key's properties.
            Console.WriteLine("User delegation key properties:");
            Console.WriteLine("Key signed start: {0}", key.SignedStartsOn);
            Console.WriteLine("Key signed expiry: {0}", key.SignedExpiresOn);
            Console.WriteLine("Key signed object ID: {0}", key.SignedObjectId);
            Console.WriteLine("Key signed tenant ID: {0}", key.SignedTenantId);
            Console.WriteLine("Key signed service: {0}", key.SignedService);
            Console.WriteLine("Key signed version: {0}", key.SignedVersion);

            //Get a user delegation SAS for a blob
            async static Task<Uri> GetUserDelegationSasBlob(BlobClient blobClient)
            {
                BlobServiceClient blobServiceClient =
                    blobClient.GetParentBlobContainerClient().GetParentBlobServiceClient();

                // Get a user delegation key for the Blob service that's valid for 7 days.
                // You can use the key to generate any number of shared access signatures 
                // over the lifetime of the key.
                Azure.Storage.Blobs.Models.UserDelegationKey userDelegationKey =
                    await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                      DateTimeOffset.UtcNow.AddDays(7));

                // Create a SAS token that's also valid for 7 days.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddDays(7)
                };

                // Specify read and write permissions for the SAS.
                sasBuilder.SetPermissions(BlobSasPermissions.Read |
                                          BlobSasPermissions.Write);

                // Add the SAS token to the blob URI.
                BlobUriBuilder blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
                {
                    // Specify the user delegation key.
                    Sas = sasBuilder.ToSasQueryParameters(userDelegationKey,
                                                          blobServiceClient.AccountName)
                };

                Console.WriteLine("Blob user delegation SAS URI: {0}", blobUriBuilder);
                Console.WriteLine();
                return blobUriBuilder.ToUri();
            }








            //string blobName = ("safedocs4u");
            //string containerName = ("filesharecontainer");
            //// Get a reference to a container
            //BlobContainerClient container = new BlobContainerClient(connectionString, containerName);
            //// Get a reference to a blob
            //BlobClient blob = container.GetBlobClient(blobName);


            //// Download file to a given path from Azure storage

            //await blob.DownloadToAsync(downloadPath);

            ////var stream = await blob.OpenReadAsync();
            ////return File(stream, blob.Properties.ContentType, option);

        }

        [HttpPost("CreateSAS")]
        public async Task<IActionResult> CreateSAS() 
        {
            //Create an account SAS
            static string GetAccountSASToken(StorageSharedKeyCredential key)
            {
                // Create a SAS token that's valid for one hour.
                AccountSasBuilder sasBuilder = new AccountSasBuilder()
                {
                    Services = AccountSasServices.Blobs | AccountSasServices.Files,
                    ResourceTypes = AccountSasResourceTypes.Service,
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Protocol = SasProtocol.Https
                };

                sasBuilder.SetPermissions(AccountSasPermissions.Read |
                    AccountSasPermissions.Write);

                // Use the key to get the SAS token.
                string sasToken = sasBuilder.ToSasQueryParameters(key).ToString();

                Console.WriteLine("SAS token for the storage account is: {0}", sasToken);
                Console.WriteLine();

                return sasToken;
            }

            //Use an account SAS from a client
            static void UseAccountSAS(Uri blobServiceUri, string sasToken)
            {
                var blobServiceClient = new BlobServiceClient
                    (new Uri($"{blobServiceUri}?{sasToken}"), null);

                BlobRetentionPolicy retentionPolicy = new BlobRetentionPolicy();
                retentionPolicy.Enabled = true;
                retentionPolicy.Days = 7;

                blobServiceClient.SetProperties(new BlobServiceProperties()
                {
                    HourMetrics = new BlobMetrics()
                    {
                        RetentionPolicy = retentionPolicy,
                        Version = "1.0"
                    },
                    MinuteMetrics = new BlobMetrics()
                    {
                        RetentionPolicy = retentionPolicy,
                        Version = "1.0"
                    },
                    Logging = new BlobAnalyticsLogging()
                    {
                        Write = true,
                        Read = true,
                        Delete = true,
                        RetentionPolicy = retentionPolicy,
                        Version = "1.0"
                    }
                });

                // The permissions granted by the account SAS also permit you to retrieve service properties.

                BlobServiceProperties serviceProperties = blobServiceClient.GetProperties().Value;
                Console.WriteLine(serviceProperties.HourMetrics.RetentionPolicy);
                Console.WriteLine(serviceProperties.HourMetrics.Version);
            }

        }


        private static Uri GetServiceSasUriForContainer(BlobContainerClient containerClient, string storedPolicyName = null)
        {
            // Check whether this BlobContainerClient object has been authorized with Shared Key.
            if (containerClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one hour.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = "c"
                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
                    sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                Uri sasUri = containerClient.GenerateSasUri(sasBuilder);
                Console.WriteLine("SAS URI for blob container is: {0}", sasUri);
                Console.WriteLine();

                return sasUri;
            }
            else
            {
                Console.WriteLine(@"BlobContainerClient must be authorized with Shared Key 
                          credentials to create a service SAS.");
                return null;
            }
        }
       


    }
}
//static void BlobUrl()
//{
//    //var account = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
//    //var cloudBlobClient = account.CreateCloudBlobClient();
//    //var container = cloudBlobClient.GetContainerReference("container-name");
//    //var blob = container.GetBlockBlobReference("test.pdf");
//    //blob.UploadFromFile("File Path ....");//Upload file....
//    //var blobUrl = blob.Uri.AbsoluteUri;

//    var azureConnectionString = CloudConfigurationManager.GetSetting(connectionString);
//    var containerName = ConfigurationManager.AppSettings["filesharecontainer"];
//    if (azureConnectionString == null || containerName == null)
//        return;

//    CloudStorageAccount backupStorageAccount = CloudStorageAccount.Parse(azureConnectionString);
//    var backupBlobClient = backupStorageAccount.CreateCloudBlobClient();
//    var container = backupBlobClient.GetContainerReference(containerName);
//    var blobs = container.ListBlobs(useFlatBlobListing: true);
//    var downloads = blobs.Select(blob => blob.Uri.Segments.Last()).ToList();

//    //var stream = await blob.OpenReadAsync();
//    //return File(stream, blob.Properties.ContentType, option);
//}