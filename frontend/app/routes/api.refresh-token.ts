import { refreshAccessToken, setAuthCookies } from '~/utils/auth.server';

export async function action({ request }: { request: Request }) {
  if (request.method !== 'POST') {
    return new Response('Method not allowed', { status: 405 });
  }

  const { tokenResponse, error } = await refreshAccessToken(request);

  if (error || !tokenResponse) {
    console.error('[API] Token refresh failed:', error);
    return new Response(
      JSON.stringify({ success: false, error: error || 'Token refresh failed' }),
      {
        status: 401,
        headers: {
          'Content-Type': 'application/json',
        },
      }
    );
  }

  // Set new auth cookies
  const cookieHeaders = setAuthCookies(tokenResponse);
  const headers = new Headers({
    'Content-Type': 'application/json',
  });

  cookieHeaders.forEach((cookie) => {
    headers.append('Set-Cookie', cookie);
  });

  return new Response(
    JSON.stringify({ success: true }),
    {
      status: 200,
      headers,
    }
  );
}
