import { useEffect, useRef, useState } from 'react';
import {
  ArrowUpTrayIcon,
  CheckCircleIcon,
  ExclamationCircleIcon,
  PhotoIcon,
  XMarkIcon,
} from '@heroicons/react/24/outline';
import {
  createGallery,
  galleryRequestUploadUrl,
  galleryConfirmUpload,
} from '~/api/apiComponents';
import { useUploadStore } from '~/stores/uploadStore';

const ACCEPTED_FILE_TYPES = [
  'image/jpeg',
  'image/jpg',
  'image/png',
  'image/gif',
  'image/webp',
  'image/heic',
  'image/heif',
  'video/mp4',
  'video/quicktime',
  'video/x-msvideo',
  'video/x-matroska',
];

export function AuthenticatedGalleryUploader() {
  const [error, setError] = useState<string | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  const [isPublic, setIsPublic] = useState(true);
  const [passwordEnabled, setPasswordEnabled] = useState(false);
  const [password, setPassword] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const {
    uploadQueue,
    isUploading,
    addFiles,
    updateFileProgress,
    updateFileStatus,
    removeFile,
    clearAll,
    setIsUploading,
  } = useUploadStore();

  const [galleryInfo, setGalleryInfo] = useState<{
    id: string;
    shortCode: string;
  } | null>(null);

  // Create authenticated gallery on mount
  useEffect(() => {
    const initGallery = async () => {
      try {
        const response = await createGallery({
          body: {
            isPublic: true,
            password: null,
          },
        });

        setGalleryInfo({
          id: response.id,
          shortCode: response.shortCode,
        });
      } catch (err) {
        console.error('Failed to create gallery:', err);
        setError('Failed to create gallery. Please refresh the page.');
      } finally {
        setIsInitializing(false);
      }
    };

    initGallery();
  }, []);

  const handleSettingsChange = async () => {
    if (!galleryInfo) return;

    try {
      // Update gallery settings when public/password settings change
      const response = await createGallery({
        body: {
          isPublic,
          password: passwordEnabled && isPublic ? password : null,
        },
      });

      setGalleryInfo({
        id: response.id,
        shortCode: response.shortCode,
      });
    } catch (err) {
      console.error('Failed to update gallery settings:', err);
      setError('Failed to update gallery settings.');
    }
  };

  const uploadFile = async (fileData: {
    id: string;
    file: File;
  }): Promise<void> => {
    if (!galleryInfo) return;

    try {
      updateFileStatus(fileData.id, 'uploading');

      // Get signed upload URL
      const uploadUrlResponse = await galleryRequestUploadUrl({
        body: {
          fileName: fileData.file.name,
          contentType: fileData.file.type || 'application/octet-stream',
        },
        pathParams: {
          id: galleryInfo.id,
        },
      });

      // Upload file to S3
      const uploadResponse = await fetch(uploadUrlResponse.uploadUrl, {
        method: 'PUT',
        body: fileData.file,
        headers: {
          'Content-Type': fileData.file.type || 'application/octet-stream',
        },
      });

      if (!uploadResponse.ok) {
        throw new Error('Failed to upload file to storage');
      }

      updateFileProgress(fileData.id, 100);

      // Confirm upload
      await galleryConfirmUpload({
        body: {
          mediaId: uploadUrlResponse.mediaId,
        },
        pathParams: {
          id: galleryInfo.id,
        },
      });

      updateFileStatus(
        fileData.id,
        'completed',
        undefined,
        uploadUrlResponse.mediaId
      );
    } catch (err) {
      console.error('Upload error:', err);
      updateFileStatus(
        fileData.id,
        'error',
        err instanceof Error ? err.message : 'Upload failed'
      );
    }
  };

  const handleFileChange = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const files = event.target.files;
    if (!files || !files.length || !galleryInfo) return;

    const fileArray = Array.from(files);
    addFiles(fileArray);

    // Clear input
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  // Auto-upload files in queue
  useEffect(() => {
    const pendingFiles = uploadQueue.filter((f) => f.status === 'pending');

    if (pendingFiles.length > 0 && !isUploading) {
      setIsUploading(true);
      setError(null);

      (async () => {
        for (const fileData of pendingFiles) {
          await uploadFile(fileData);
        }
        setIsUploading(false);
      })();
    }
  }, [uploadQueue, isUploading]);

  const handleButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();

    if (isUploading || !galleryInfo) return;

    const droppedFiles = e.dataTransfer.files;
    if (!droppedFiles || !droppedFiles.length) return;

    if (fileInputRef.current) {
      const dataTransfer = new DataTransfer();
      for (let i = 0; i < droppedFiles.length; i++) {
        dataTransfer.items.add(droppedFiles[i]);
      }
      fileInputRef.current.files = dataTransfer.files;
      const event = new Event('change', { bubbles: true });
      fileInputRef.current.dispatchEvent(event);
    }
  };

  const handleViewGallery = () => {
    if (galleryInfo) {
      window.location.href = `/g/${galleryInfo.shortCode}`;
    }
  };

  const completedCount = uploadQueue.filter(
    (f) => f.status === 'completed'
  ).length;
  const errorCount = uploadQueue.filter((f) => f.status === 'error').length;
  const allCompleted =
    uploadQueue.length > 0 &&
    completedCount + errorCount === uploadQueue.length &&
    !isUploading;

  if (isInitializing) {
    return (
      <div className="w-full max-w-4xl mx-auto">
        <div className="bg-white p-8 rounded-xl shadow-lg border-2 border-purple-100">
          <div className="flex flex-col items-center justify-center py-12">
            <div className="w-10 h-10 border-t-2 border-b-2 border-purple-600 rounded-full animate-spin mb-3"></div>
            <p className="text-gray-600">Initializing gallery...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full max-w-4xl mx-auto">
      <div className="bg-white p-8 rounded-xl shadow-lg border-2 border-purple-100">
        {error && (
          <div className="mb-6 p-4 bg-red-50 rounded-lg border border-red-200 flex items-center gap-3 text-red-700">
            <ExclamationCircleIcon className="w-6 h-6 text-red-500" />
            <p>{error}</p>
          </div>
        )}

        <div className="text-center mb-8">
          <h2 className="text-2xl font-bold text-gray-800 mb-3">
            <b>Upload</b> Photos & Videos
          </h2>
          <p className="text-gray-700 text-lg">
            Share your memories in <b>original quality</b>
          </p>
          {galleryInfo && (
            <p className="text-sm text-gray-500 mt-2">
              Gallery Code: <b className="text-purple-600">{galleryInfo.shortCode}</b>
            </p>
          )}
        </div>

        {/* Gallery Settings */}
        <div className="mb-6 p-4 bg-gray-50 rounded-lg border border-gray-200">
          <h3 className="text-sm font-semibold text-gray-700 mb-3">Gallery Settings</h3>

          {/* Public/Private Switch */}
          <div className="flex items-center justify-between mb-3">
            <label htmlFor="public-switch" className="text-sm text-gray-700">
              Public Gallery
            </label>
            <button
              id="public-switch"
              type="button"
              onClick={() => {
                setIsPublic(!isPublic);
                if (isPublic) {
                  setPasswordEnabled(false);
                }
              }}
              className={`${
                isPublic ? 'bg-purple-600' : 'bg-gray-300'
              } relative inline-flex h-6 w-11 items-center rounded-full transition-colors`}
            >
              <span
                className={`${
                  isPublic ? 'translate-x-6' : 'translate-x-1'
                } inline-block h-4 w-4 transform rounded-full bg-white transition-transform`}
              />
            </button>
          </div>

          {/* Password Protection Switch */}
          {isPublic && (
            <div className="flex items-center justify-between mb-3">
              <label htmlFor="password-switch" className="text-sm text-gray-700">
                Password Protection
              </label>
              <button
                id="password-switch"
                type="button"
                onClick={() => setPasswordEnabled(!passwordEnabled)}
                className={`${
                  passwordEnabled ? 'bg-purple-600' : 'bg-gray-300'
                } relative inline-flex h-6 w-11 items-center rounded-full transition-colors`}
              >
                <span
                  className={`${
                    passwordEnabled ? 'translate-x-6' : 'translate-x-1'
                  } inline-block h-4 w-4 transform rounded-full bg-white transition-transform`}
                />
              </button>
            </div>
          )}

          {/* Password Input */}
          {isPublic && passwordEnabled && (
            <div className="mt-3">
              <label htmlFor="password-input" className="block text-sm text-gray-700 mb-1">
                Gallery Password
              </label>
              <input
                id="password-input"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter password"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
              />
            </div>
          )}

          <button
            onClick={handleSettingsChange}
            className="mt-3 w-full px-4 py-2 bg-purple-600 text-white rounded-lg text-sm font-medium hover:bg-purple-700 transition-colors"
          >
            Apply Settings
          </button>
        </div>

        {allCompleted && completedCount > 0 ? (
          <div className="text-center py-8">
            <CheckCircleIcon className="w-20 h-20 text-green-500 mx-auto mb-4" />
            <h3 className="text-xl font-semibold text-gray-800 mb-3">
              Upload Complete!
            </h3>
            <p className="text-gray-600 mb-6">
              Successfully uploaded {completedCount} file
              {completedCount !== 1 ? 's' : ''}
              {errorCount > 0 && ` (${errorCount} failed)`}
            </p>
            <button
              onClick={handleViewGallery}
              className="px-8 py-3 bg-purple-600 text-white rounded-lg font-bold hover:bg-purple-700"
            >
              View Gallery
            </button>
          </div>
        ) : (
          <div>
            <div
              className={`
                border-2 border-dashed rounded-xl p-10 mb-6 text-center cursor-pointer
                transition-colors duration-200
                ${
                  isUploading
                    ? 'bg-gray-50 border-gray-300'
                    : 'border-purple-300 hover:border-purple-500 bg-purple-50'
                }
              `}
              onClick={handleButtonClick}
              onDragOver={(e) => {
                e.preventDefault();
                e.stopPropagation();
              }}
              onDrop={handleDrop}
            >
              <div className="flex flex-col items-center justify-center">
                <ArrowUpTrayIcon className="w-16 h-16 text-purple-500 mb-3" />
                <p className="text-lg font-medium text-gray-800 mb-1">
                  Drag your files here or click to browse
                </p>
                <p className="text-gray-500 text-sm">
                  Photos and videos in original quality
                </p>
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept={ACCEPTED_FILE_TYPES.join(',')}
                onChange={handleFileChange}
                className="hidden"
                disabled={isUploading || !galleryInfo}
                multiple
              />
            </div>

            {uploadQueue.length > 0 && (
              <div className="mt-4">
                <div className="flex justify-between items-center mb-2">
                  <h4 className="text-sm font-medium text-gray-700">
                    Files ({uploadQueue.length})
                  </h4>
                  {allCompleted && (
                    <button
                      onClick={() => clearAll()}
                      className="text-sm text-gray-500 hover:text-gray-700"
                    >
                      Clear
                    </button>
                  )}
                </div>
                <div className="border rounded-lg overflow-hidden max-h-60 overflow-y-auto">
                  {uploadQueue.map((fileData) => (
                    <div
                      key={fileData.id}
                      className="px-4 py-3 border-b last:border-0 flex justify-between items-center"
                    >
                      <div className="flex items-center flex-1 min-w-0">
                        <PhotoIcon className="w-5 h-5 text-gray-500 mr-2 flex-shrink-0" />
                        <span className="text-sm text-gray-800 truncate">
                          {fileData.file.name}
                        </span>
                      </div>
                      <div className="flex items-center gap-2 ml-4">
                        {fileData.status === 'uploading' && (
                          <div className="w-4 h-4 border-t-2 border-b-2 border-purple-600 rounded-full animate-spin"></div>
                        )}
                        {fileData.status === 'completed' && (
                          <CheckCircleIcon className="w-5 h-5 text-green-500" />
                        )}
                        {fileData.status === 'error' && (
                          <ExclamationCircleIcon className="w-5 h-5 text-red-500" />
                        )}
                        {fileData.status === 'pending' && (
                          <span className="text-xs text-gray-400">Pending</span>
                        )}
                        {(fileData.status === 'pending' ||
                          fileData.status === 'error') && (
                          <button
                            onClick={() => removeFile(fileData.id)}
                            className="text-gray-400 hover:text-gray-600"
                          >
                            <XMarkIcon className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
