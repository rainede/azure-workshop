#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<ImageDTO> outQ, TraceWriter log)
{
    // Read the content of the request  
    string WebInputURI = await req.Content.ReadAsAsync<string>();

    // throw an error if inputuri is null
    if (WebInputURI == null)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a URI in the json request body");
    }

    // Create the output object to pop on the queue and populate it, the ICollector binding should wire up the actual queue write for us.
    ImageDTO myq = new ImageDTO()
    {

        OutputSAS = GetBlobSasUri(Guid.NewGuid().ToString()),
        InputURI = WebInputURI

    };
 
    outQ.Add(myq);

    return req.CreateResponse(HttpStatusCode.Accepted, myq.OutputSAS);
      
} 
 
public class ImageDTO
{

    public string InputURI {get; set;}
    public string OutputSAS {get; set;} 
 
}

public static string GetBlobSasUri(string blobname)
{

    //Parse the connection string and return a reference to the storage account.
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("journeyfunctionstorage_STORAGE"));

    //Create the blob client object.
    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

    //Get a reference to a container to use for the sample code, and create it if it does not exist.
    CloudBlobContainer container = blobClient.GetContainerReference("images");
    container.CreateIfNotExists();

    //Get a reference to a blob within the container.
    CloudBlockBlob blob = container.GetBlockBlobReference(blobname);

    //Set the expiry time and permissions for the blob.
    //In this case, the start time is specified as a few minutes in the past, to mitigate clock skew.
    //The shared access signature will be valid immediately.
    SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
    sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
    sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
    sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

    //Generate the shared access signature on the blob, setting the constraints directly on the signature.
    string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

    //Return the URI string for the container, including the SAS token.
    return blob.Uri + sasBlobToken;
}
