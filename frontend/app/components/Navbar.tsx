import { Link } from 'react-router';
import { useCurrentUser } from '~/context/UserContext';

export function Navbar() {
  const { currentUser } = useCurrentUser();

  return (
    <nav className="bg-white py-4 shadow-sm text-gray-900">
      <div className="container mx-auto px-4 flex justify-between items-center">
        <div className="flex items-center">
          <Link prefetch={'intent'} to={'/'} className="font-bold text-xl text-purple-600">
            Kappi.gg
          </Link>
        </div>

        <div className="hidden md:flex space-x-6 items-center">
          <Link prefetch={'intent'} to={'/'} className="text-gray-700 hover:text-purple-600">
            Home
          </Link>
          <Link prefetch={'intent'} to={'/upload'} className="text-gray-700 hover:text-purple-600">
            Upload
          </Link>
          {currentUser && (
            <div className="text-gray-700 flex items-center gap-2">
              <span className="text-sm">
                {currentUser.email}
              </span>
            </div>
          )}
        </div>
      </div>
    </nav>
  );
}
