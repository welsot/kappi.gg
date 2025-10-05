import { apiTokenRefresh } from '~/api/apiComponents';

/**
 * Get a cookie value by name on the client side
 */
function getCookie(name: string): string | null {
  if (typeof document === 'undefined') return null;

  const value = `; ${document.cookie}`;
  const parts = value.split(`; ${name}=`);

  if (parts.length === 2) {
    return parts.pop()?.split(';').shift() || null;
  }

  return null;
}

/**
 * Client-side token refresh mechanism
 * This function attempts to refresh the access token using the refresh token stored in cookies
 */
export async function refreshClientToken(): Promise<boolean> {
  try {
    const refreshToken = getCookie('refreshToken');

    if (!refreshToken) {
      console.warn('[Client] No refresh token found in cookies');
      return false;
    }

    // Call the refresh endpoint with the refresh token from cookies
    const tokenResponse = await apiTokenRefresh({
      body: {
        refreshToken,
      },
    });

    // Token refresh was successful
    // The server will set new cookies automatically via Set-Cookie headers
    console.info('[Client] Token refreshed successfully');
    return true;
  } catch (error) {
    console.error('[Client] Failed to refresh token:', error);
    return false;
  }
}
