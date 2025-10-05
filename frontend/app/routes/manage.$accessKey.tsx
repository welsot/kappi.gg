import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import type { Route } from './+types/manage.$accessKey';
import {
  ArrowUpTrayIcon,
  TrashIcon,
  LinkIcon,
  PhotoIcon,
  ClockIcon,
} from '@heroicons/react/24/outline';
import {
  anonymousGalleryRequestUploadUrl,
  anonymousGalleryConfirmUpload,
  deleteAnonymousGalleryMedia,
  getAnonymousGalleryByShortCode,
} from '~/api/apiComponents';
import type { AnonymousGalleryDto, MediaDto } from '~/api/apiSchemas';
import { useGalleryStore } from '~/stores/galleryStore';
import { Footer } from '~/components/Footer';
import { format } from 'date-fns';

export function meta({ params }: Route.MetaArgs) {
  return [
    { title: `Manage Gallery ${params.accessKey} - Kappi.gg` },
    { name: 'description', content: 'Manage your gallery photos and videos' },
  ];
}

export async function loader({ params }: Route.LoaderArgs) {
  return { accessKey: params.accessKey };
}

export default function ManageGallery({ params }: Route.ComponentProps) {
  const { accessKey } = params;
  const navigate = useNavigate();
  const { getGalleryByAccessKey } = useGalleryStore();

  const [gallery, setGallery] = useState<AnonymousGalleryDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [copied, setCopied] = useState(false);

  const galleryInfo = accessKey ? getGalleryByAccessKey(accessKey) : null;

  const fetchGalleryData = async () => {
    if (!galleryInfo) {
      setError('Gallery not found. Access key may be invalid or expired.');
      setIsLoading(false);
      return;
    }

    try {
      const galleryData = await getAnonymousGalleryByShortCode({
        pathParams: { shortCode: galleryInfo.shortCode },
      });

      setGallery(galleryData);
      setError(null);
    } catch (err) {
      console.error('Failed to fetch gallery:', err);
      setError('Failed to load gallery. It may have expired or been deleted.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (!accessKey) {
      setError('Invalid access key');
      setIsLoading(false);
      return;
    }

    fetchGalleryData();
  }, [accessKey, galleryInfo?.shortCode]);

  const handleUploadMore = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const files = event.target.files;
    if (!files || !files.length || !accessKey) return;

    setIsUploading(true);
    setError(null);

    try {
      for (let i = 0; i < files.length; i++) {
        const file = files[i];

        // Get upload URL
        const uploadUrlResponse = await anonymousGalleryRequestUploadUrl({
          body: {
            fileName: file.name,
            contentType: file.type || 'application/octet-stream',
          },
          pathParams: { accessKey },
        });

        // Upload to S3
        const uploadResponse = await fetch(uploadUrlResponse.uploadUrl, {
          method: 'PUT',
          body: file,
          headers: {
            'Content-Type': file.type || 'application/octet-stream',
          },
        });

        if (!uploadResponse.ok) {
          throw new Error(`Failed to upload ${file.name}`);
        }

        // Confirm upload
        await anonymousGalleryConfirmUpload({
          body: { mediaId: uploadUrlResponse.mediaId },
          pathParams: { accessKey },
        });
      }

      // Refresh gallery data
      await fetchGalleryData();
    } catch (err) {
      console.error('Upload error:', err);
      setError(
        err instanceof Error ? err.message : 'Failed to upload files'
      );
    } finally {
      setIsUploading(false);
    }
  };

  const handleDeleteMedia = async (mediaId: string) => {
    if (!accessKey || !confirm('Delete this file?')) return;

    try {
      await deleteAnonymousGalleryMedia({
        pathParams: { accessKey, mediaId },
      });

      // Refresh gallery data
      await fetchGalleryData();
    } catch (err) {
      console.error('Delete error:', err);
      setError('Failed to delete file');
    }
  };

  const copyShareLink = () => {
    if (!gallery) return;
    const shareUrl = `${window.location.origin}/g/${gallery.shortCode}`;
    navigator.clipboard.writeText(shareUrl).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="w-10 h-10 border-t-2 border-b-2 border-purple-600 rounded-full animate-spin"></div>
      </div>
    );
  }

  if (error || !gallery) {
    return (
      <div className="min-h-screen flex flex-col">
        <main className="flex-grow flex items-center justify-center">
          <div className="max-w-md w-full mx-auto p-8">
            <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
              <h2 className="text-xl font-bold text-red-800 mb-2">
                Error
              </h2>
              <p className="text-red-700 mb-4">
                {error || 'Gallery not found'}
              </p>
              <button
                onClick={() => navigate('/')}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700"
              >
                Go Home
              </button>
            </div>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  const expiryDate = new Date(gallery.expiresAt);
  const isExpired = expiryDate < new Date();

  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      <main className="flex-grow py-8">
        <div className="max-w-6xl mx-auto px-4">
          {/* Gallery Info */}
          <div className="bg-white rounded-xl shadow-lg p-6 mb-6">
            <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
              <div>
                <h1 className="text-2xl font-bold text-gray-800 mb-2">
                  Gallery: <span className="text-purple-600">{gallery.shortCode}</span>
                </h1>
                <div className="flex items-center gap-4 text-sm text-gray-600">
                  <div className="flex items-center gap-1">
                    <PhotoIcon className="w-4 h-4" />
                    <span>{gallery.media.totalCount} files</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <ClockIcon className="w-4 h-4" />
                    <span>
                      Expires {format(expiryDate, 'MMM d, yyyy')}
                    </span>
                  </div>
                </div>
              </div>

              <div className="flex gap-2">
                <button
                  onClick={copyShareLink}
                  className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  <LinkIcon className="w-5 h-5" />
                  {copied ? 'Copied!' : 'Share Link'}
                </button>

                <label className="flex items-center gap-2 px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 cursor-pointer">
                  <ArrowUpTrayIcon className="w-5 h-5" />
                  {isUploading ? 'Uploading...' : 'Add More'}
                  <input
                    type="file"
                    multiple
                    accept="image/*,video/*"
                    onChange={handleUploadMore}
                    className="hidden"
                    disabled={isUploading || isExpired}
                  />
                </label>
              </div>
            </div>
          </div>

          {/* Media Grid */}
          {gallery.media.totalCount === 0 ? (
            <div className="bg-white rounded-xl shadow-lg p-12 text-center">
              <PhotoIcon className="w-16 h-16 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-gray-700 mb-2">
                No files yet
              </h3>
              <p className="text-gray-500">
                Upload photos and videos to get started
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {gallery.media.media.map((media) => (
                <MediaCard
                  key={media.id}
                  media={media}
                  onDelete={() => handleDeleteMedia(media.id)}
                />
              ))}
            </div>
          )}
        </div>
      </main>
      <Footer />
    </div>
  );
}

interface MediaCardProps {
  media: MediaDto;
  onDelete: () => void;
}

function MediaCard({ media, onDelete }: MediaCardProps) {
  const isVideo = media.mediaType?.startsWith('video/');

  return (
    <div className="group relative bg-white rounded-lg shadow overflow-hidden hover:shadow-lg transition-shadow">
      <div className="aspect-square bg-gray-100 flex items-center justify-center">
        {isVideo ? (
          <video
            src={media.downloadUrl}
            className="w-full h-full object-cover"
          />
        ) : (
          <img
            src={media.downloadUrl}
            alt=""
            className="w-full h-full object-cover"
            loading="lazy"
          />
        )}
      </div>

      <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-40 transition-opacity flex items-center justify-center opacity-0 group-hover:opacity-100">
        <div className="flex gap-2">
          <a
            href={media.downloadUrl}
            download
            className="p-2 bg-white rounded-full hover:bg-gray-100"
            onClick={(e) => e.stopPropagation()}
          >
            <ArrowUpTrayIcon className="w-5 h-5 text-gray-700 rotate-180" />
          </a>
          <button
            onClick={(e) => {
              e.stopPropagation();
              onDelete();
            }}
            className="p-2 bg-white rounded-full hover:bg-red-50"
          >
            <TrashIcon className="w-5 h-5 text-red-600" />
          </button>
        </div>
      </div>
    </div>
  );
}
