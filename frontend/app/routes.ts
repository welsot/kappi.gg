import { index, route, type RouteConfig } from '@react-router/dev/routes';

export default [
  index('routes/home.tsx'),
  route('/how-to-send-epub-books-to-kobo', 'routes/how-to-kobo.tsx'),
] satisfies RouteConfig;
