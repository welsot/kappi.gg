import { Link, redirect } from 'react-router';
import type { Route } from './+types/dashboard';
import { Navbar } from '~/components/Navbar';
import { Footer } from '~/components/Footer';
import { useCurrentUser } from '~/context/UserContext';
import { getAccessToken } from '~/utils/auth.server';
import { PlusIcon, PhotoIcon } from '@heroicons/react/24/outline';

export async function loader({ request }: Route.LoaderArgs) {
  const accessToken = getAccessToken(request);

  if (!accessToken) {
    // Redirect to login with current URL as redirectTo
    const url = new URL(request.url);
    return redirect(`/login?redirectTo=${url.pathname}`);
  }

  return {};
}

export default function Dashboard() {
  const { currentUser } = useCurrentUser();

  if (!currentUser) {
    return null;
  }

  return (
    <div className="flex flex-col min-h-screen">
      <Navbar />

      <main className="flex-grow bg-gradient-to-b from-purple-50 to-white">
        <div className="container mx-auto px-4 py-16 md:py-24">
          <div className="max-w-4xl mx-auto">
            <div className="bg-white p-8 rounded-2xl shadow-lg border border-purple-100 mb-8">
              <h1 className="text-3xl font-bold text-gray-900 mb-2">Welcome back!</h1>
              <p className="text-gray-600 mb-6">Manage your galleries and share your moments</p>

              <div className="bg-purple-50 p-6 rounded-lg border border-purple-200">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Your Account</h2>
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Email:</span>
                    <span className="text-sm font-medium text-gray-900">{currentUser.email}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">User ID:</span>
                    <span className="text-sm font-mono text-gray-700">{currentUser.id}</span>
                  </div>
                </div>
              </div>
            </div>

            <div className="bg-white p-8 rounded-2xl shadow-lg border border-purple-100">
              <h2 className="text-2xl font-bold text-gray-900 mb-6">Quick Actions</h2>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Link
                  prefetch="intent"
                  to="/gallery/new"
                  className="flex flex-col items-center justify-center p-8 border-2 border-dashed border-purple-300 rounded-xl hover:border-purple-500 hover:bg-purple-50 transition-all duration-200 group"
                >
                  <div className="w-16 h-16 bg-purple-100 rounded-full flex items-center justify-center mb-4 group-hover:bg-purple-200 transition-colors duration-200">
                    <PlusIcon className="h-8 w-8 text-purple-600" />
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">Create New Gallery</h3>
                  <p className="text-sm text-gray-600 text-center">
                    Upload and share your photos and videos
                  </p>
                </Link>

                <Link
                  prefetch="intent"
                  to="/galleries"
                  className="flex flex-col items-center justify-center p-8 border-2 border-purple-200 rounded-xl hover:border-purple-500 hover:bg-purple-50 transition-all duration-200 group"
                >
                  <div className="w-16 h-16 bg-purple-100 rounded-full flex items-center justify-center mb-4 group-hover:bg-purple-200 transition-colors duration-200">
                    <PhotoIcon className="h-8 w-8 text-purple-600" />
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">My Galleries</h3>
                  <p className="text-sm text-gray-600 text-center">
                    View and manage your existing galleries
                  </p>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}
