import type { ApiTokenResponse, TokenRefreshDto, UserInfoResponse } from '~/api/apiSchemas';

const INTERNAL_API_URL = process.env.VITE_PRIVATE_API_URL;
const ACCESS_TOKEN_COOKIE = 'accessToken';
const REFRESH_TOKEN_COOKIE = 'refreshToken';
const TOKEN_EXPIRY_COOKIE = 'tokenExpiry';

interface CookieOptions {
  httpOnly: boolean;
  secure: boolean;
  sameSite: 'lax' | 'strict' | 'none';
  path: string;
  maxAge: number;
}

export function getCookieHeader(name: string, value: string, options: CookieOptions): string {
  const parts = [`${name}=${value}`];

  if (options.httpOnly) parts.push('HttpOnly');
  if (options.secure) parts.push('Secure');
  if (options.sameSite) parts.push(`SameSite=${options.sameSite}`);
  if (options.path) parts.push(`Path=${options.path}`);
  if (options.maxAge) parts.push(`Max-Age=${options.maxAge}`);

  return parts.join('; ');
}

export function parseCookies(request: Request): Record<string, string> {
  const cookieHeader = request.headers.get('Cookie');
  if (!cookieHeader) return {};

  return cookieHeader.split(';').reduce((acc, cookie) => {
    const [name, value] = cookie.trim().split('=');
    if (name && value) {
      acc[name] = value;
    }
    return acc;
  }, {} as Record<string, string>);
}

export function getAccessToken(request: Request): string | null {
  const cookies = parseCookies(request);
  return cookies[ACCESS_TOKEN_COOKIE] || null;
}

export function getRefreshToken(request: Request): string | null {
  const cookies = parseCookies(request);
  return cookies[REFRESH_TOKEN_COOKIE] || null;
}

export function getTokenExpiry(request: Request): number | null {
  const cookies = parseCookies(request);
  const expiry = cookies[TOKEN_EXPIRY_COOKIE];
  return expiry ? parseInt(expiry, 10) : null;
}

export function isTokenExpired(request: Request): boolean {
  const expiry = getTokenExpiry(request);
  if (!expiry) return true;

  // Consider token expired if it will expire in the next 5 minutes
  const now = Date.now();
  const buffer = 5 * 60 * 1000; // 5 minutes
  return now >= (expiry - buffer);
}

export function setAuthCookies(tokenResponse: ApiTokenResponse): string[] {
  const isProduction = process.env.NODE_ENV === 'production';
  const maxAge = 30 * 24 * 60 * 60; // 30 days

  const cookieOptions: CookieOptions = {
    httpOnly: true,
    secure: isProduction,
    sameSite: 'lax',
    path: '/',
    maxAge,
  };

  // Token expires in 30 days (matching refresh token lifetime)
  const expiry = Date.now() + (maxAge * 1000);

  return [
    getCookieHeader(ACCESS_TOKEN_COOKIE, tokenResponse.token, cookieOptions),
    getCookieHeader(REFRESH_TOKEN_COOKIE, tokenResponse.refreshToken, cookieOptions),
    getCookieHeader(TOKEN_EXPIRY_COOKIE, expiry.toString(), cookieOptions),
  ];
}

export function clearAuthCookies(): string[] {
  const cookieOptions: CookieOptions = {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    path: '/',
    maxAge: 0,
  };

  return [
    getCookieHeader(ACCESS_TOKEN_COOKIE, '', cookieOptions),
    getCookieHeader(REFRESH_TOKEN_COOKIE, '', cookieOptions),
    getCookieHeader(TOKEN_EXPIRY_COOKIE, '', cookieOptions),
  ];
}

export async function refreshAccessToken(request: Request): Promise<{
  tokenResponse: ApiTokenResponse | null;
  error: string | null;
}> {
  const refreshToken = getRefreshToken(request);

  if (!refreshToken) {
    return { tokenResponse: null, error: 'No refresh token available' };
  }

  try {
    const response = await fetch(`${INTERNAL_API_URL}/api/token/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken } as TokenRefreshDto),
    });

    if (!response.ok) {
      return { tokenResponse: null, error: `Failed to refresh token: ${response.status}` };
    }

    const tokenResponse = await response.json() as ApiTokenResponse;
    return { tokenResponse, error: null };
  } catch (error) {
    console.error('Token refresh error:', error);
    return { tokenResponse: null, error: error instanceof Error ? error.message : 'Unknown error' };
  }
}

export async function getCurrentUser(request: Request): Promise<{
  user: UserInfoResponse | null;
  error: string | null;
  status: number;
}> {
  const accessToken = getAccessToken(request);

  if (!accessToken) {
    return { user: null, error: 'No access token', status: 401 };
  }

  try {
    const response = await fetch(`${INTERNAL_API_URL}/api/users/me`, {
      method: 'GET',
      headers: {
        'X-API-TOKEN': accessToken,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      return { user: null, error: `Failed to fetch user: ${response.status}`, status: response.status };
    }

    const user = await response.json() as UserInfoResponse;
    return { user, error: null, status: 200 };
  } catch (error) {
    console.error('Get current user error:', error);
    return {
      user: null,
      error: error instanceof Error ? error.message : 'Unknown error',
      status: 500,
    };
  }
}
