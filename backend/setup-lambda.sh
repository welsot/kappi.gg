#!/bin/bash

# This script builds the Lambda Docker image before starting docker compose
# Run this script before `docker compose up`

echo "Building Lambda Docker image..."
cd lambda-metadata-extractor || exit 1

# Build Docker image for Lambda
docker build -t lambda-metadata-extractor:latest .

echo "Lambda Docker image built successfully!"
echo "Now you can run: docker compose up -d"
