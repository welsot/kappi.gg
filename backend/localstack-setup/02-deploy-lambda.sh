#!/bin/bash

echo "Creating Lambda function package..."
cd /tmp/lambda-metadata-extractor || exit 1

# Clean up old artifacts
rm -f function.zip

# Create zip with just the handler code (no dependencies needed - AWS SDK is in runtime)
zip -q function.zip index.js package.json

# Check zip size
ZIP_SIZE=$(stat -c%s "function.zip")
echo "Lambda package size: $ZIP_SIZE bytes"

echo "Deploying Lambda function..."
awslocal lambda create-function \
  --function-name metadata-extractor \
  --runtime nodejs20.x \
  --handler index.handler \
  --zip-file fileb:///tmp/lambda-metadata-extractor/function.zip \
  --role arn:aws:iam::000000000000:role/lambda-role \
  --timeout 30 \
  --memory-size 256 \
  --environment Variables="{S3_ENDPOINT=http://localstack:4566,AWS_REGION=us-east-1}"

echo "Waiting for Lambda function to become active..."
awslocal lambda wait function-active-v2 --function-name metadata-extractor

echo "Granting S3 permission to invoke Lambda..."
awslocal lambda add-permission \
  --function-name metadata-extractor \
  --statement-id s3-trigger \
  --action lambda:InvokeFunction \
  --principal s3.amazonaws.com \
  --source-arn arn:aws:s3:::local-development-bucket

echo "Configuring S3 bucket notification..."
awslocal s3api put-bucket-notification-configuration \
  --bucket local-development-bucket \
  --notification-configuration '{
    "LambdaFunctionConfigurations": [
      {
        "Id": "metadata-extraction-trigger",
        "LambdaFunctionArn": "arn:aws:lambda:us-east-1:000000000000:function:metadata-extractor",
        "Events": ["s3:ObjectCreated:*"]
      }
    ]
  }'

echo "Lambda function deployed and S3 trigger configured successfully!"
