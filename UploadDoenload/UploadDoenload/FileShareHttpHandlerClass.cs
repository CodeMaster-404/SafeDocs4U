using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using ServiceStack.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace UploadDoenload
{
    public class FileShareHttpHandlerClass:IHttpHandler
    {
        async static Task CreateStoredAccessPolicyAsync(string containerName)
        {
            string connectionString = "";

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
                Id = "mysignedidentifier",
                AccessPolicy = new BlobAccessPolicy
                {
                    StartsOn = DateTimeOffset.UtcNow.AddHours(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                    Permissions = "rw"
                }
            }
        };
                // Set the container's access policy.
                await containerClient.SetAccessPolicyAsync(permissions: signedIdentifiers);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.ErrorCode);
                Console.WriteLine(e.Message);
            }
            finally
            {
                await containerClient.DeleteAsync();
            }
        }
    }

}
