import { create } from 'zustand';

export interface UploadingFile {
  id: string;
  file: File;
  progress: number;
  status: 'pending' | 'uploading' | 'completed' | 'error';
  error?: string;
  mediaId?: string;
}

interface UploadStore {
  uploadQueue: UploadingFile[];
  isUploading: boolean;

  addFiles: (files: File[]) => void;
  updateFileProgress: (id: string, progress: number) => void;
  updateFileStatus: (
    id: string,
    status: UploadingFile['status'],
    error?: string,
    mediaId?: string
  ) => void;
  removeFile: (id: string) => void;
  clearCompleted: () => void;
  clearAll: () => void;
  setIsUploading: (isUploading: boolean) => void;
}

export const useUploadStore = create<UploadStore>((set) => ({
  uploadQueue: [],
  isUploading: false,

  addFiles: (files) => {
    const newFiles: UploadingFile[] = files.map((file) => ({
      id: crypto.randomUUID(),
      file,
      progress: 0,
      status: 'pending',
    }));

    set((state) => ({
      uploadQueue: [...state.uploadQueue, ...newFiles],
    }));
  },

  updateFileProgress: (id, progress) => {
    set((state) => ({
      uploadQueue: state.uploadQueue.map((f) =>
        f.id === id ? { ...f, progress } : f
      ),
    }));
  },

  updateFileStatus: (id, status, error, mediaId) => {
    set((state) => ({
      uploadQueue: state.uploadQueue.map((f) =>
        f.id === id ? { ...f, status, error, mediaId } : f
      ),
    }));
  },

  removeFile: (id) => {
    set((state) => ({
      uploadQueue: state.uploadQueue.filter((f) => f.id !== id),
    }));
  },

  clearCompleted: () => {
    set((state) => ({
      uploadQueue: state.uploadQueue.filter((f) => f.status !== 'completed'),
    }));
  },

  clearAll: () => {
    set({ uploadQueue: [] });
  },

  setIsUploading: (isUploading) => {
    set({ isUploading });
  },
}));
