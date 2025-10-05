import type { Route } from './+types/g.$shortCode';
import { getAnonymousGalleryByShortCode } from '~/api/apiComponents';
import type { AnonymousGalleryDto, MediaDto } from '~/api/apiSchemas';
import { Footer } from '~/components/Footer';
import { PhotoIcon, ArrowDownTrayIcon } from '@heroicons/react/24/outline';
import { format } from 'date-fns';
import { useState, useEffect } from 'react';

export function meta({ params }: Route.MetaArgs) {
  return [
    { title: `Gallery ${params.shortCode} - Kappi.gg` },
    {
      name: 'description',
      content: 'View and download photos and videos',
    },
  ];
}

export default function GalleryView({ params }: Route.ComponentProps) {
  const { shortCode } = params;
  const [gallery, setGallery] = useState<AnonymousGalleryDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedMedia, setSelectedMedia] = useState<MediaDto | null>(null);

  useEffect(() => {
    const fetchGallery = async () => {
      try {
        const galleryData = await getAnonymousGalleryByShortCode({
          pathParams: { shortCode },
        });
        setGallery(galleryData);
      } catch (err) {
        console.error('Failed to fetch gallery:', err);
        setError('Gallery not found or has expired');
      } finally {
        setIsLoading(false);
      }
    };

    fetchGallery();
  }, [shortCode]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="w-12 h-12 border-4 border-purple-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-gray-600">Loading gallery...</p>
        </div>
      </div>
    );
  }

  if (error || !gallery) {
    return (
      <div className="min-h-screen flex flex-col bg-gray-50">
        <main className="flex-grow flex items-center justify-center">
          <div className="bg-white rounded-xl shadow-lg p-8 max-w-md mx-4 text-center">
            <PhotoIcon className="w-16 h-16 text-red-500 mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-gray-800 mb-2">
              Gallery Not Found
            </h2>
            <p className="text-gray-600 mb-6">
              {error || 'This gallery does not exist or has expired'}
            </p>
            <a
              href="/"
              className="inline-block px-6 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700"
            >
              Go Home
            </a>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  const expiryDate = new Date(gallery.expiresAt);

  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      <main className="flex-grow py-8">
        <div className="max-w-6xl mx-auto px-4">
          {/* Gallery Header */}
          <div className="bg-white rounded-xl shadow-lg p-6 mb-6 text-center">
            <h1 className="text-3xl font-bold text-gray-800 mb-2">
              Gallery: <span className="text-purple-600">{gallery.shortCode}</span>
            </h1>
            <div className="flex items-center justify-center gap-6 text-sm text-gray-600">
              <div className="flex items-center gap-1">
                <PhotoIcon className="w-5 h-5" />
                <span>
                  {gallery.media.totalCount} file
                  {gallery.media.totalCount !== 1 ? 's' : ''}
                </span>
              </div>
              <div>
                Expires on {format(expiryDate, 'MMMM d, yyyy')}
              </div>
            </div>
          </div>

          {/* Media Grid */}
          {gallery.media.totalCount === 0 ? (
            <div className="bg-white rounded-xl shadow-lg p-12 text-center">
              <PhotoIcon className="w-16 h-16 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-gray-700 mb-2">
                No files in this gallery
              </h3>
              <p className="text-gray-500">
                This gallery is empty or the files have been removed
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {gallery.media.media.map((media) => (
                <MediaThumbnail
                  key={media.id}
                  media={media}
                  onClick={() => setSelectedMedia(media)}
                />
              ))}
            </div>
          )}
        </div>
      </main>

      {/* Media Viewer Modal */}
      {selectedMedia && (
        <MediaViewer
          media={selectedMedia}
          onClose={() => setSelectedMedia(null)}
        />
      )}

      <Footer />
    </div>
  );
}

interface MediaThumbnailProps {
  media: MediaDto;
  onClick: () => void;
}

function MediaThumbnail({ media, onClick }: MediaThumbnailProps) {
  const [imageError, setImageError] = useState(false);
  const [imageLoaded, setImageLoaded] = useState(false);
  const isVideo = media.mediaType?.startsWith('video/');

  return (
    <div
      onClick={onClick}
      className="group relative bg-white rounded-lg shadow overflow-hidden hover:shadow-lg transition-shadow cursor-pointer"
    >
      {/* Hover overlay */}
      <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-20 transition-opacity pointer-events-none" />
      <div className="aspect-square bg-gray-100 flex items-center justify-center relative">
        {isVideo ? (
          <div className="relative w-full h-full">
            <video
              src={media.downloadUrl}
              className="w-full h-full object-cover"
              preload="metadata"
            />
            <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-30 pointer-events-none">
              <div className="w-12 h-12 rounded-full bg-white bg-opacity-80 flex items-center justify-center">
                <svg
                  className="w-6 h-6 text-gray-800 ml-1"
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path d="M8 5v14l11-7z" />
                </svg>
              </div>
            </div>
          </div>
        ) : imageError ? (
          <PhotoIcon className="w-12 h-12 text-gray-400" />
        ) : (
          <>
            {!imageLoaded && (
              <div className="absolute inset-0 flex items-center justify-center">
                <div className="w-8 h-8 border-2 border-purple-600 border-t-transparent rounded-full animate-spin"></div>
              </div>
            )}
            <img
              src={media.downloadUrl}
              alt=""
              className="w-full h-full object-cover"
              loading="lazy"
              onLoad={() => setImageLoaded(true)}
              onError={() => {
                console.error('Failed to load image:', media.downloadUrl);
                setImageError(true);
              }}
            />
          </>
        )}
      </div>
    </div>
  );
}

interface MediaViewerProps {
  media: MediaDto;
  onClose: () => void;
}

function MediaViewer({ media, onClose }: MediaViewerProps) {
  const isVideo = media.mediaType?.startsWith('video/');

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-90 z-50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div
        className="relative max-w-7xl max-h-full"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Close button */}
        <button
          onClick={onClose}
          className="absolute -top-12 right-0 text-white hover:text-gray-300 text-lg"
        >
          ✕ Close
        </button>

        {/* Media content */}
        <div className="bg-black rounded-lg overflow-hidden">
          {isVideo ? (
            <video
              src={media.downloadUrl}
              controls
              className="max-w-full max-h-[80vh] mx-auto"
              autoPlay
            />
          ) : (
            <img
              src={media.downloadUrl}
              alt=""
              className="max-w-full max-h-[80vh] mx-auto"
            />
          )}
        </div>

        {/* Download button */}
        <div className="mt-4 text-center">
          <a
            href={media.downloadUrl}
            download
            className="inline-flex items-center gap-2 px-6 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700"
          >
            <ArrowDownTrayIcon className="w-5 h-5" />
            Download
          </a>
        </div>
      </div>
    </div>
  );
}
