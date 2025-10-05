We build Kappi.gg - a photo/video sharing platform for travelers and photographers to easily share photos and videos in original quality without compression.
Our mobile app enables users to easily download all the photos and videos using short links, after downloading the media content is immediately available on the device's gallery.
We focus on building a web app (frontend) with SSR using react router.

Here's the technical overview:

### Uploading Media

Web app has to request a pre-signed upload URL from the API server, then it can upload the media content directly to the blob storage (s3) using the pre-signed URL.

Once the upload is completed, API server has to be notified about the successful upload, so the server can store the metadata in the database. 

### Anonymous Access

Anonymous user can upload photos and videos without creating an account, we call it a "gallery", for anonymous users the gallery will be stored for 30 days and can be managed (more photos can be added / removed) only via access key which is returned upon creation of the gallery.
Each gallery has a unique short code (e.g. abc123) that can be shared with others to view and download the media content.

To view and download the media content, anonymous user needs to provide the short code, no access key is required for viewing/downloading.

Anonymous galleries can be retrieved by /g/{shortcode} page, which will load all the data server-side.

### Authenticated Access

Authenticated users can create an account and log in to manage their galleries.
Authenticated users can create multiple galleries, each gallery can have its own set of photos and videos.
Galleries created by authenticated users do not expire, but users can choose to delete them manually.
Authenticated users can decide if they want to share the gallery with others via a short code or keep it private.
Authenticated users can set the password for their galleries to restrict access. (ensure password is properly hashed before storing in the database).
When accessing a password-protected gallery, users must provide the correct password to view or download the media content.

### Other

- this project was copied from book sharing service, so some of the components are ready like for uploading, you can copy those, but they use old api methods, you need to adjust them for our new api methods.
- please ensure that there are no typescript errors by using `npm run typecheck` command after every feature implemented.