# Lambda Metadata Extractor - Testing Guide

## Overview

This guide will help you test the Lambda function that automatically extracts metadata from photos and videos uploaded to S3.

## Prerequisites

- Docker and Docker Compose installed
- Node.js installed (for building the Lambda function locally if needed)
- AWS CLI or curl for manual testing

## Setup

### 1. Build the Lambda Docker Image

**IMPORTANT**: You must build the Lambda Docker image BEFORE starting docker compose:

```bash
cd lambda-metadata-extractor
docker build -t lambda-metadata-extractor:latest .
cd ..
```

Or use the helper script:

```bash
./setup-lambda.sh
```

### 2. Start the Services

```bash
docker compose up -d
```

This will start:
- PostgreSQL database
- Mailcatcher
- LocalStack (with S3 and Lambda services)

The LocalStack init scripts will automatically:
- Create the S3 bucket `local-development-bucket`
- Build and deploy the Lambda function
- Configure S3 to trigger the Lambda on file uploads

### 2. Verify Lambda Deployment

Check if the Lambda function was deployed successfully:

```bash
export AWS_DEFAULT_REGION=us-east-1
aws --endpoint-url=http://localhost:4566 lambda list-functions
```

**Note**: Make sure to set `AWS_DEFAULT_REGION=us-east-1` or add `--region us-east-1` to all AWS CLI commands.

You should see `metadata-extractor` in the list.

### 3. Verify S3 Bucket Configuration

Check the bucket notification configuration:

```bash
aws --endpoint-url=http://localhost:4566 s3api get-bucket-notification-configuration --bucket local-development-bucket
```

You should see the Lambda trigger configured for `s3:ObjectCreated:*` events.

## Testing the Metadata Extraction

### Option 1: Upload via Your API

Use your .NET API to:
1. Request a pre-signed upload URL
2. Upload a photo or video using the pre-signed URL
3. Check the S3 object tags to verify metadata extraction

### Option 2: Manual Upload via AWS CLI

1. **Upload a test image:**

```bash
aws --endpoint-url=http://localhost:4566 s3 cp /path/to/your/image.jpg s3://local-development-bucket/test-image.jpg
```

2. **Wait a few seconds for Lambda to process**

3. **Check the object tags:**

```bash
aws --endpoint-url=http://localhost:4566 s3api get-object-tagging --bucket local-development-bucket --key test-image.jpg
```

Expected output for images:
```json
{
  "TagSet": [
    {"Key": "mediaType", "Value": "image"},
    {"Key": "width", "Value": "1920"},
    {"Key": "height", "Value": "1080"},
    {"Key": "format", "Value": "jpeg"},
    {"Key": "fileSize", "Value": "524288"}
  ]
}
```

4. **Upload a test video:**

```bash
aws --endpoint-url=http://localhost:4566 s3 cp /path/to/your/video.mp4 s3://local-development-bucket/test-video.mp4
```

5. **Check the video object tags:**

```bash
aws --endpoint-url=http://localhost:4566 s3api get-object-tagging --bucket local-development-bucket --key test-video.mp4
```

Expected output for videos:
```json
{
  "TagSet": [
    {"Key": "mediaType", "Value": "video"},
    {"Key": "width", "Value": "1920"},
    {"Key": "height", "Value": "1080"},
    {"Key": "duration", "Value": "120.5"},
    {"Key": "format", "Value": "mov,mp4,m4a,3gp,3g2,mj2"},
    {"Key": "fileSize", "Value": "10485760"}
  ]
}
```

## Checking Lambda Logs

To see Lambda execution logs:

```bash
aws --endpoint-url=http://localhost:4566 logs tail /aws/lambda/metadata-extractor --follow
```

Or check Docker logs:

```bash
docker compose logs -f localstack
```

## Troubleshooting

### Lambda Not Triggering

1. **Check if Lambda was deployed:**
   ```bash
   aws --endpoint-url=http://localhost:4566 lambda get-function --function-name metadata-extractor
   ```

2. **Check S3 notification configuration:**
   ```bash
   aws --endpoint-url=http://localhost:4566 s3api get-bucket-notification-configuration --bucket local-development-bucket
   ```

3. **Verify permissions:**
   ```bash
   aws --endpoint-url=http://localhost:4566 lambda get-policy --function-name metadata-extractor
   ```

### Lambda Execution Errors

1. **Check Lambda logs:**
   ```bash
   docker compose logs localstack | grep metadata-extractor
   ```

2. **Test Lambda directly (without S3 trigger):**
   ```bash
   aws --endpoint-url=http://localhost:4566 lambda invoke \
     --function-name metadata-extractor \
     --payload '{"Records":[{"s3":{"bucket":{"name":"local-development-bucket"},"object":{"key":"test-image.jpg"}}}]}' \
     response.json

   cat response.json
   ```

### Rebuilding the Lambda

If you make changes to the Lambda code:

1. **Stop the containers:**
   ```bash
   docker compose down
   ```

2. **Remove the old function.zip:**
   ```bash
   rm lambda-metadata-extractor/function.zip
   rm -rf lambda-metadata-extractor/node_modules
   ```

3. **Restart the containers:**
   ```bash
   docker compose up -d
   ```

   The init script will rebuild and redeploy the Lambda automatically.

## Integration with Your .NET API

Your .NET backend should:

1. **Generate pre-signed upload URLs** for clients
2. **After upload confirmation from client**, check S3 object tags:
   ```csharp
   var taggingRequest = new GetObjectTaggingRequest
   {
       BucketName = "local-development-bucket",
       Key = objectKey
   };
   var taggingResponse = await s3Client.GetObjectTaggingAsync(taggingRequest);
   ```

3. **Parse and store metadata** in your database:
   ```csharp
   var metadata = new MediaMetadata();
   foreach (var tag in taggingResponse.Tagging)
   {
       switch (tag.Key)
       {
           case "mediaType":
               metadata.MediaType = tag.Value;
               break;
           case "width":
               metadata.Width = int.Parse(tag.Value);
               break;
           case "height":
               metadata.Height = int.Parse(tag.Value);
               break;
           case "duration":
               metadata.Duration = double.Parse(tag.Value);
               break;
           case "fileSize":
               metadata.FileSize = long.Parse(tag.Value);
               break;
       }
   }
   ```

## Notes

- Lambda execution in LocalStack may take 5-10 seconds on first invocation (cold start)
- The Lambda uses sharp for image processing and ffmpeg for video processing
- Maximum Lambda timeout is set to 60 seconds
- Maximum Lambda memory is set to 512 MB
- For production deployment to AWS, you'll need to build the Lambda with proper layers or include all dependencies in the zip file

## Supported File Formats

**Images:**
- JPEG/JPG
- PNG
- GIF
- WebP
- BMP
- TIFF
- HEIC

**Videos:**
- MP4
- MOV
- AVI
- MKV
- WebM
- FLV
- M4V
