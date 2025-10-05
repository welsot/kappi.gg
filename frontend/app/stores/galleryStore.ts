import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import type { StateCreator } from 'zustand';

export interface GalleryAccessInfo {
  galleryId: string;
  shortCode: string;
  accessKey: string;
  expiresAt: string;
  createdAt: string;
}

interface PersistentGalleryStore {
  // Store recent galleries the user has created
  recentGalleries: GalleryAccessInfo[];
  currentAccessKey: string | null;

  addGallery: (gallery: GalleryAccessInfo) => void;
  setCurrentAccessKey: (accessKey: string) => void;
  clearCurrentAccessKey: () => void;
  getGalleryByAccessKey: (accessKey: string) => GalleryAccessInfo | undefined;
  removeExpiredGalleries: () => void;
}

const storeConfig: StateCreator<PersistentGalleryStore> = (set, get) => ({
  recentGalleries: [],
  currentAccessKey: null,

  addGallery: (gallery: GalleryAccessInfo) => {
    set((state) => {
      // Check if gallery already exists
      const exists = state.recentGalleries.some(
        (g) => g.galleryId === gallery.galleryId
      );

      if (exists) {
        return state;
      }

      // Keep only last 10 galleries
      const updated = [gallery, ...state.recentGalleries].slice(0, 10);
      return { recentGalleries: updated };
    });
  },

  setCurrentAccessKey: (accessKey: string) => {
    set({ currentAccessKey: accessKey });
  },

  clearCurrentAccessKey: () => {
    set({ currentAccessKey: null });
  },

  getGalleryByAccessKey: (accessKey: string) => {
    return get().recentGalleries.find((g) => g.accessKey === accessKey);
  },

  removeExpiredGalleries: () => {
    set((state) => {
      const now = new Date();
      const valid = state.recentGalleries.filter((g) => {
        return new Date(g.expiresAt) > now;
      });
      return { recentGalleries: valid };
    });
  },
});

const isBrowser = typeof window !== 'undefined';

export const useGalleryStore = isBrowser
  ? create<PersistentGalleryStore>()(
      persist(storeConfig, {
        name: 'kappi-gallery-storage',
        storage: createJSONStorage(() => localStorage),
      })
    )
  : create<PersistentGalleryStore>()(storeConfig);
