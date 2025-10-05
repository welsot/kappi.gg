import { useCurrentUser } from '~/context/UserContext';
import type { ReactElement } from 'react';

export function UserProfile(): ReactElement {
  const { currentUser } = useCurrentUser();

  if (!currentUser) {
    return <div>Not logged in</div>;
  }

  return (
    <div>
      <h2>User Profile</h2>
      <p>Email: {currentUser.email}</p>
      <p>ID: {currentUser.id}</p>
    </div>
  );
}
