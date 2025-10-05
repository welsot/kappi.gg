import type { Route } from './+types/upload';
import { GalleryUploader } from '~/components/GalleryUploader';
import { Footer } from '~/components/Footer';

export function meta({}: Route.MetaArgs) {
  return [
    { title: 'Upload Photos & Videos - Kappi.gg' },
    {
      name: 'description',
      content: 'Upload and share photos and videos in original quality',
    },
  ];
}

export default function Upload() {
  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      <main className="flex-grow py-8">
        <GalleryUploader />
      </main>
      <Footer />
    </div>
  );
}
