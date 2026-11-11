import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { Toast } from '../components/Toast';

type ToastTone = 'success' | 'error' | 'info';

interface ToastState {
  id: number;
  message: string;
  tone: ToastTone;
}

type ShowToast = (message: string, tone?: ToastTone) => void;

const ToastContext = createContext<ShowToast | null>(null);

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [toast, setToast] = useState<ToastState | null>(null);
  const timerRef = useRef<number | null>(null);

  const show = useCallback<ShowToast>((message, tone = 'success') => {
    if (timerRef.current) window.clearTimeout(timerRef.current);
    const id = Date.now();
    setToast({ id, message, tone });
    timerRef.current = window.setTimeout(() => {
      setToast((prev) => (prev?.id === id ? null : prev));
    }, 3200);
  }, []);

  useEffect(
    () => () => {
      if (timerRef.current) window.clearTimeout(timerRef.current);
    },
    []
  );

  return (
    <ToastContext.Provider value={show}>
      {children}
      <Toast message={toast?.message ?? null} tone={toast?.tone ?? 'success'} toastKey={toast?.id ?? 0} onDismiss={() => setToast(null)} />
    </ToastContext.Provider>
  );
};

export function useToast(): ShowToast {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used inside <ToastProvider>');
  return ctx;
}
