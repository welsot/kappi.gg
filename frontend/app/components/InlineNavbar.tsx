import { Link } from 'react-router';
import { useCurrentUser } from '~/context/UserContext';
import type { UserDto } from '~/api/apiSchemas';

export function InlineNavbar() {
  const { currentUser } = useCurrentUser();

  return (
    <div className="max-w-4xl mx-auto mb-8 flex justify-center">
      <div className="backdrop-blur-md bg-white/30 border border-white/40 rounded-full shadow-lg px-4 py-2">
        {currentUser ? <UserInlineNavbar user={currentUser} /> : <AnonInlineNavbar />}
      </div>
    </div>
  );
}

function UserInlineNavbar({ user }: { user: UserDto }) {
  const username = user.email.split('@')[0];

  return (
    <div className="flex items-center gap-3">
      <div className="flex items-center gap-2 px-2">
        <svg className="w-4 h-4 text-purple-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
        </svg>
        <span className="text-sm font-medium text-gray-800">{username}</span>
      </div>
      <Link
        prefetch="intent"
        to="/dashboard"
        className="flex items-center gap-1.5 px-3 py-1.5 bg-purple-600 text-white text-sm font-medium rounded-full hover:bg-purple-700 transition-all hover:shadow-md"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
        </svg>
        Dashboard
      </Link>
    </div>
  );
}

function AnonInlineNavbar() {
  return (
    <div className="flex items-center gap-2">
      <Link
        prefetch="intent"
        to="/login"
        className="flex items-center gap-1.5 px-3 py-1.5 text-gray-800 text-sm font-medium rounded-full hover:bg-white/50 transition-all"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" />
        </svg>
        Log In
      </Link>
      <Link
        prefetch="intent"
        to="/signup"
        className="flex items-center gap-1.5 px-3 py-1.5 bg-purple-600 text-white text-sm font-medium rounded-full hover:bg-purple-700 transition-all hover:shadow-md"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
        </svg>
        Sign Up
      </Link>
    </div>
  );
}