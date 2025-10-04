// AWS SDK v3 is available in the Lambda runtime environment
import { S3Client, GetObjectCommand, PutObjectTaggingCommand } from '@aws-sdk/client-s3';

const s3Client = new S3Client({
  region: process.env.AWS_REGION || 'us-east-1',
  ...(process.env.S3_ENDPOINT && { endpoint: process.env.S3_ENDPOINT, forcePathStyle: true })
});

/**
 * Extract image dimensions from JPEG files
 */
function extractJpegDimensions(buffer) {
  let offset = 2; // Skip SOI marker

  while (offset < buffer.length) {
    // Check for marker
    if (buffer[offset] !== 0xFF) break;

    const marker = buffer[offset + 1];
    offset += 2;

    // SOF markers contain dimensions
    if (marker >= 0xC0 && marker <= 0xCF && marker !== 0xC4 && marker !== 0xC8 && marker !== 0xCC) {
      const height = buffer.readUInt16BE(offset + 3);
      const width = buffer.readUInt16BE(offset + 5);
      return { width, height };
    }

    // Skip segment
    const segmentLength = buffer.readUInt16BE(offset);
    offset += segmentLength;
  }

  return null;
}

/**
 * Extract image dimensions from PNG files
 */
function extractPngDimensions(buffer) {
  // PNG signature is 8 bytes, IHDR chunk follows
  if (buffer.length < 24) return null;

  const width = buffer.readUInt32BE(16);
  const height = buffer.readUInt32BE(20);
  return { width, height };
}

/**
 * Extract basic metadata from buffer based on file signature
 */
function extractMetadata(buffer, contentType, key) {
  const fileSize = buffer.length;
  let metadata = {
    fileSize: fileSize.toString(),
    mediaType: 'unknown',
    format: ''
  };

  // Detect file type by magic bytes
  if (buffer[0] === 0xFF && buffer[1] === 0xD8 && buffer[2] === 0xFF) {
    // JPEG
    metadata.mediaType = 'image';
    metadata.format = 'jpeg';
    const dimensions = extractJpegDimensions(buffer);
    if (dimensions) {
      metadata.width = dimensions.width.toString();
      metadata.height = dimensions.height.toString();
    }
  } else if (buffer[0] === 0x89 && buffer[1] === 0x50 && buffer[2] === 0x4E && buffer[3] === 0x47) {
    // PNG
    metadata.mediaType = 'image';
    metadata.format = 'png';
    const dimensions = extractPngDimensions(buffer);
    if (dimensions) {
      metadata.width = dimensions.width.toString();
      metadata.height = dimensions.height.toString();
    }
  } else if (buffer.slice(4, 12).toString() === 'ftypmp42' ||
             buffer.slice(4, 12).toString() === 'ftypisom' ||
             buffer.slice(4, 12).toString() === 'ftypMSNV' ||
             buffer.slice(4, 12).toString() === 'ftypM4V ') {
    // MP4/M4V video
    metadata.mediaType = 'video';
    metadata.format = 'mp4';
    // For videos, we'll just store file size for now
    // Full metadata extraction would require parsing MOOV atom which is complex
  } else if (buffer.slice(0, 4).toString() === 'RIFF' && buffer.slice(8, 12).toString() === 'AVI ') {
    // AVI video
    metadata.mediaType = 'video';
    metadata.format = 'avi';
  } else if (contentType) {
    // Fallback to content type
    if (contentType.startsWith('image/')) {
      metadata.mediaType = 'image';
      metadata.format = contentType.split('/')[1];
    } else if (contentType.startsWith('video/')) {
      metadata.mediaType = 'video';
      metadata.format = contentType.split('/')[1];
    }
  } else {
    // Fallback to file extension
    const ext = key.split('.').pop().toLowerCase();
    const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'heic'];
    const videoExts = ['mp4', 'mov', 'avi', 'mkv', 'webm', 'm4v'];

    if (imageExts.includes(ext)) {
      metadata.mediaType = 'image';
      metadata.format = ext;
    } else if (videoExts.includes(ext)) {
      metadata.mediaType = 'video';
      metadata.format = ext;
    }
  }

  return metadata;
}

/**
 * Lambda handler
 */
export const handler = async (event) => {
  console.log('Received event:', JSON.stringify(event, null, 2));

  try {
    for (const record of event.Records) {
      const bucket = record.s3.bucket.name;
      const key = decodeURIComponent(record.s3.object.key.replace(/\+/g, ' '));
      const contentType = record.s3.object.contentType;

      console.log(`Processing: s3://${bucket}/${key}`);

      // Download file (read first 64KB for metadata extraction)
      const getCommand = new GetObjectCommand({
        Bucket: bucket,
        Key: key,
        Range: 'bytes=0-65535' // Only download first 64KB
      });

      const response = await s3Client.send(getCommand);
      const chunks = [];
      for await (const chunk of response.Body) {
        chunks.push(chunk);
      }
      const buffer = Buffer.concat(chunks);

      // For file size, we need the full object metadata
      const fullSize = record.s3.object.size || buffer.length;

      // Extract metadata
      const metadata = extractMetadata(buffer, contentType, key);
      metadata.fileSize = fullSize.toString();

      console.log('Extracted metadata:', metadata);

      // Convert to S3 tags
      const tags = Object.entries(metadata)
        .filter(([_, value]) => value) // Only include non-empty values
        .map(([Key, Value]) => ({ Key, Value }));

      // Update S3 object tags
      const putTagsCommand = new PutObjectTaggingCommand({
        Bucket: bucket,
        Key: key,
        Tagging: { TagSet: tags }
      });

      await s3Client.send(putTagsCommand);
      console.log(`Successfully tagged: s3://${bucket}/${key}`);
    }

    return {
      statusCode: 200,
      body: JSON.stringify({ message: 'Metadata extraction completed' })
    };
  } catch (error) {
    console.error('Error:', error);
    throw error;
  }
};
