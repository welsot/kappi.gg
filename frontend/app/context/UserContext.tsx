import { createContext, useContext, type ReactNode } from 'react';
import type { UserDto } from '~/api/apiSchemas';

type CurrentUserContextType = {
  currentUser: UserDto | null;
};

const CurrentUserContext = createContext<CurrentUserContextType | undefined>(undefined);

type CurrentUserProviderProps = {
  currentUser: UserDto | null;
  children: ReactNode;
};

export function CurrentUserProvider({ currentUser, children }: CurrentUserProviderProps) {
  return (
    <CurrentUserContext.Provider value={{ currentUser }}>
      {children}
    </CurrentUserContext.Provider>
  );
}

export function useCurrentUser(): CurrentUserContextType {
  const context = useContext(CurrentUserContext);
  if (context === undefined) {
    throw new Error('useCurrentUser must be used within a CurrentUserProvider');
  }

  return context;
}
