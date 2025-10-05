import { index, route, type RouteConfig } from '@react-router/dev/routes';

export default [
  index('routes/home.tsx'),
  route('/upload', 'routes/upload.tsx'),
  route('/login', 'routes/login.tsx'),
  route('/register', 'routes/register.tsx'),
  route('/dashboard', 'routes/dashboard.tsx'),
  route('/manage/:accessKey', 'routes/manage.$accessKey.tsx'),
  route('/g/:shortCode', 'routes/g.$shortCode.tsx'),
] satisfies RouteConfig;
