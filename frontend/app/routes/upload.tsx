import type { Route } from './+types/upload';
import { AnonymousGalleryUploader } from '~/components/AnonymousGalleryUploader';
import { AuthenticatedGalleryUploader } from '~/components/AuthenticatedGalleryUploader';
import { Footer } from '~/components/Footer';
import { useCurrentUser } from '~/context/UserContext';

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
  const { currentUser } = useCurrentUser();

  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      <main className="flex-grow py-8">
        {currentUser ? <AuthenticatedGalleryUploader /> : <AnonymousGalleryUploader />}
      </main>
      <Footer />
    </div>
  );
}
