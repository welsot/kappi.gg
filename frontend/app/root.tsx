import {
  isRouteErrorResponse,
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
  useLoaderData,
  useNavigation,
} from 'react-router';

import type { Route } from './+types/root';
import './app.css';
import type { UserDto } from '~/api/apiSchemas';
import { CurrentUserProvider } from './context/UserContext';
import { AnimatePresence, motion } from 'framer-motion';
import {
  isTokenExpired,
  refreshAccessToken,
  getCurrentUser,
  setAuthCookies,
} from '~/utils/auth.server';
import { useEffect } from 'react';
import { storageService } from '~/utils/storage';
import { api } from '~/utils/api';

interface PlausibleOptions {
  callback?: () => void;
  props?: Record<string, any>;
}

declare global {
  interface Window {
    plausible?: (eventName: string, options?: PlausibleOptions) => void;
  }
}

export const links: Route.LinksFunction = () => [
  { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
  {
    rel: 'preconnect',
    href: 'https://fonts.gstatic.com',
    crossOrigin: 'anonymous',
  },
  {
    rel: 'stylesheet',
    href: 'https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap',
  },
];

export async function loader({ request }: Route.LoaderArgs) {
  let currentUser: UserDto | null = null;
  const headers = new Headers();
  let kappiAccessToken: string|null = null;

  // Check if token is expired and refresh if needed
  if (isTokenExpired(request)) {
    const { tokenResponse, error: refreshTokenError } = await refreshAccessToken(request);

    if (tokenResponse) {
      // Set new auth cookies
      const cookieHeaders = setAuthCookies(tokenResponse);
      cookieHeaders.forEach((cookie) => {
        headers.append('Set-Cookie', cookie);
      });

      // Use the user from the token response
      currentUser = tokenResponse.user;
      kappiAccessToken = tokenResponse.token;
    } else {
      console.warn('[TokenExpired] Failed to refresh token:', refreshTokenError);
    }
  } else {
    // Token is still valid, fetch current user
    const { user, error: getUserError } = await getCurrentUser(request);

    if (user) {
      currentUser = user.user;
    } else {
      console.warn('Failed to fetch current user:', getUserError);
      console.warn('[NotExpired] Trying to refresh token');

      /**
       * Try refresh token START
       */
      const { tokenResponse, error } = await refreshAccessToken(request);

      if (tokenResponse) {
        // Set new auth cookies
        const cookieHeaders = setAuthCookies(tokenResponse);
        cookieHeaders.forEach((cookie) => {
          headers.append('Set-Cookie', cookie);
        });

        // Use the user from the token response
        currentUser = tokenResponse.user;
        kappiAccessToken = tokenResponse.token;
        console.info('[NotExpired] Successfully refreshed token');
      } else {
        console.warn('Failed to refresh token:', error);
      }
      /**
       * Try refresh token END
       */
    }
  }

  return {
    currentUser: currentUser,
    kappiAccessToken: kappiAccessToken,
    headers: headers
  };
}

export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
    <head>
      <meta charSet="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <Meta />
      <Links />
      <script defer data-domain="kappi.gg" src="https://plausible.welsot.com/js/script.tagged-events.js"></script>
    </head>
    <body>
    {children}
    <ScrollRestoration />
    <Scripts />
    </body>
    </html>
  );
}

export default function App() {
  const { currentUser, kappiAccessToken } = useLoaderData<typeof loader>();
  const navigation = useNavigation();
  const isLoading = navigation.state === 'loading';

  useEffect(() => {
    if (kappiAccessToken) {
      storageService.setApiToken(kappiAccessToken);
      api.setAuthToken(kappiAccessToken);
    }
  }, [kappiAccessToken]);

  return (
    <CurrentUserProvider currentUser={currentUser}>
      <div className="relative">
        <AnimatePresence>
          {isLoading && (
            <>
              <motion.div
                className="fixed top-0 left-0 right-0 h-1 bg-blue-600 z-50"
                initial={{ scaleX: 0, transformOrigin: 'left' }}
                animate={{ scaleX: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.6, ease: 'easeInOut' }}
              />
              <motion.div
                className="fixed top-0 left-0 w-full h-full bg-black/5 backdrop-blur-[1px] z-40 pointer-events-none"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.3 }}
              />
            </>
          )}
        </AnimatePresence>
        <Outlet />
      </div>
    </CurrentUserProvider>
  );
}

export function ErrorBoundary({ error }: Route.ErrorBoundaryProps) {
  let message = 'Oops!';
  let details = 'An unexpected error occurred.';
  let stack: string | undefined;

  if (isRouteErrorResponse(error)) {
    message = error.status === 404 ? '404' : 'Error';
    details =
      error.status === 404 ? 'The requested page could not be found.' : error.statusText || details;
  } else if (import.meta.env.DEV && error && error instanceof Error) {
    details = error.message;
    stack = error.stack;
  }

  return (
    <main className="pt-16 p-4 container mx-auto">
      <h1>{message}</h1>
      <p>{details}</p>
      {stack && (
        <pre className="w-full p-4 overflow-x-auto">
          <code>{stack}</code>
        </pre>
      )}
    </main>
  );
}
