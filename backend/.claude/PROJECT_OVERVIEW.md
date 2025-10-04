We build Kappi.gg - a photo/video sharing platform for travelers and photographers to easily share photos and videos in original quality without compression.
Our mobile app enables users to easily download all the photos and videos using short links, after downloading the media content is immediately available on the device's gallery.
We focus on building a dotnet core api.

Here's the technical overview:

### Uploading Media

API Client (mobile app or web) has to request a pre-signed upload URL from the API server, then it can upload the media content directly to the blob storage (s3) using the pre-signed URL.

Once the upload is completed, API server has to be notified about the successful upload, so the server can store the metadata in the database. We want to store the media type as well as dimensions (width, height) for photos and videos.

We want to use a lambda function on AWS s3 to extract the metadata (media type, dimensions) upon upload and store it in the s3 object tags.

When photos/videos are requested for viewing/downloading, API server will generate pre-signed download URLs for each media item and also check if we have the metadata stored in the database, if not - fetch it from s3 object tags and store it in the database for future requests.

### Anonymous Access

Anonymous user can upload photos and videos without creating an account, we call it a "gallery", for anonymous users the gallery will be stored for 30 days and can be managed (more photos can be added / removed) only via access key which is returned upon creation of the gallery.
Each gallery has a unique short code (e.g. abc123) that can be shared with others to view and download the media content.

Short code should be generated as short as possible, starting with 4 characters, then if generated code collides with existing code - increase the length by 1, example of a human-readable short code:

```csharp
public static string GenerateShortUrlCode(int length = 4)
    {
        // some chars removed to avoid confusion e.g lI1i0O
        const string chars = "abcdefghkmnopqrstuwxyz23456789";
        var stringChars = new char[length];
        var random = new Random();

        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }
```

Clean-up service should run periodically (every hour) to remove expired galleries and their media content from the database and blob storage (s3).

To view and download the media content, anonymous user needs to provide the short code, no access key is required for viewing/downloading.

### Authenticated Access

Authenticated users can create an account and log in to manage their galleries.
Authenticated users can create multiple galleries, each gallery can have its own set of photos and videos.
Galleries created by authenticated users do not expire, but users can choose to delete them manually.
Authenticated users can decide if they want to share the gallery with others via a short code or keep it private.
Authenticated users can set the password for their galleries to restrict access. (ensure password is properly hashed before storing in the database).
When accessing a password-protected gallery, users must provide the correct password to view or download the media content.

### Other

- Ensure proper logging throughout the application for monitoring and debugging purposes.
- Ensure all endpoints are covered with integration tests. (you can look up how existing tests are implemented in the api.Tests project)
- To run tests please use the following command: `dotnet test api.Tests`
- Please put all the logic related to this project in a separate module called "Kappi" (see how other modules are implemented and wired together)