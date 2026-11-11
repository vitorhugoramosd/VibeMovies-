import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api, getStoredUser, setStoredAuth } from '../services/api';
import { AuthResponseDto, UpdateProfileRequest } from '../types/api';

interface AuthContextValue {
  user: AuthResponseDto | null;
  /** True until the initial session restore / demo login finished. */
  initializing: boolean;
  login: (email: string, password: string) => Promise<AuthResponseDto>;
  register: (name: string, email: string, password: string, city: string) => Promise<AuthResponseDto>;
  logout: () => void;
  updateProfile: (body: UpdateProfileRequest) => Promise<AuthResponseDto>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const DEMO_EMAIL = 'demo@vibemovies.app';
const DEMO_PASSWORD = 'vibe1234';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthResponseDto | null>(() => getStoredUser());
  const [initializing, setInitializing] = useState(true);

  const persist = useCallback((auth: AuthResponseDto | null) => {
    setStoredAuth(auth);
    setUser(auth);
  }, []);

  // Restore the session (refreshing profile fields like myProviders) or log the demo account in.
  useEffect(() => {
    let cancelled = false;
    const init = async () => {
      try {
        const stored = getStoredUser();
        if (stored) {
          const me = await api.getMe();
          if (!cancelled) {
            persist({
              ...stored,
              name: me.name || stored.name,
              defaultCity: me.defaultCity || stored.defaultCity,
              preferredRegion: me.preferredRegion || stored.preferredRegion,
              myProviders: me.myProviders ?? stored.myProviders ?? []
            });
          }
        } else {
          const auth = await api.login(DEMO_EMAIL, DEMO_PASSWORD);
          if (!cancelled) persist(auth);
        }
      } catch {
        // Offline / demo account missing — stay anonymous.
      } finally {
        if (!cancelled) setInitializing(false);
      }
    };
    init();
    return () => {
      cancelled = true;
    };
  }, [persist]);

  const login = useCallback(
    async (email: string, password: string) => {
      const auth = await api.login(email, password);
      persist(auth);
      return auth;
    },
    [persist]
  );

  const register = useCallback(
    async (name: string, email: string, password: string, city: string) => {
      const auth = await api.register(name, email, password, city);
      persist(auth);
      return auth;
    },
    [persist]
  );

  const logout = useCallback(() => persist(null), [persist]);

  const updateProfile = useCallback(
    async (body: UpdateProfileRequest) => {
      const me = await api.updateMe(body);
      const current = getStoredUser();
      if (!current) throw new Error('Faça login para editar o perfil.');
      const next: AuthResponseDto = {
        ...current,
        name: me.name,
        defaultCity: me.defaultCity,
        preferredRegion: me.preferredRegion,
        myProviders: me.myProviders
      };
      persist(next);
      return next;
    },
    [persist]
  );

  const value = useMemo(
    () => ({ user, initializing, login, register, logout, updateProfile }),
    [user, initializing, login, register, logout, updateProfile]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
