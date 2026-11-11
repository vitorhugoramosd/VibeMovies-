import React, { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';

const FOCUSABLE =
  'a[href], area[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), iframe, [tabindex]:not([tabindex="-1"])';

interface ModalProps {
  open: boolean;
  onClose: () => void;
  /** Accessible name (used when no visible title id is given). */
  label?: string;
  labelledBy?: string;
  children: React.ReactNode;
  /** Classes for the dialog panel (width, padding...). */
  panelClassName?: string;
  /** Show the default floating close button. */
  showCloseButton?: boolean;
  /** Element that should receive focus when the modal opens. */
  initialFocusRef?: React.RefObject<HTMLElement>;
}

interface StackEntry {
  panel: React.RefObject<HTMLDivElement>;
  close: React.MutableRefObject<() => void>;
}

/** Open dialogs, top-most last. Esc and focus containment only apply to the top one. */
const modalStack: StackEntry[] = [];
let globalListenersInstalled = false;

function installGlobalListeners() {
  if (globalListenersInstalled) return;
  globalListenersInstalled = true;
  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Escape' || modalStack.length === 0) return;
    e.preventDefault();
    modalStack[modalStack.length - 1].close.current();
  });
  // Keep focus inside the top-most dialog (e.g. when a focused button becomes disabled).
  document.addEventListener('focusin', (e) => {
    const top = modalStack[modalStack.length - 1];
    const panel = top?.panel.current;
    if (panel && e.target instanceof Node && !panel.contains(e.target)) {
      panel.focus({ preventScroll: true });
    }
  });
}

/**
 * Accessible dialog: portal, Esc to close, focus trap, focus restore,
 * backdrop click to close and body scroll lock.
 */
export const Modal: React.FC<ModalProps> = ({
  open,
  onClose,
  label,
  labelledBy,
  children,
  panelClassName = 'w-full max-w-md p-6',
  showCloseButton = true,
  initialFocusRef
}) => {
  const panelRef = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);
  onCloseRef.current = onClose;

  useEffect(() => {
    if (!open) return;
    installGlobalListeners();
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const entry: StackEntry = { panel: panelRef, close: onCloseRef };
    modalStack.push(entry);
    document.body.style.overflow = 'hidden';

    const focusTimer = window.setTimeout(() => {
      const panel = panelRef.current;
      if (!panel) return;
      const target =
        initialFocusRef?.current ?? panel.querySelector<HTMLElement>('[data-autofocus]:not([disabled])') ?? panel;
      target.focus({ preventScroll: true });
      if (!panel.contains(document.activeElement)) panel.focus({ preventScroll: true });
    }, 0);

    return () => {
      window.clearTimeout(focusTimer);
      const idx = modalStack.indexOf(entry);
      if (idx >= 0) modalStack.splice(idx, 1);
      if (modalStack.length === 0) {
        document.body.style.overflow = '';
      }
      if (previouslyFocused && document.contains(previouslyFocused)) {
        previouslyFocused.focus({ preventScroll: true });
      }
    };
  }, [open, initialFocusRef]);

  if (!open) return null;

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (e.key !== 'Tab' || !panelRef.current) return;
    e.stopPropagation();
    const focusables = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
      (el) => el.offsetParent !== null || el === document.activeElement
    );
    if (focusables.length === 0) {
      e.preventDefault();
      panelRef.current.focus();
      return;
    }
    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    const active = document.activeElement;
    if (e.shiftKey && (active === first || active === panelRef.current)) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && active === last) {
      e.preventDefault();
      first.focus();
    }
  };

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4 bg-black/80 backdrop-blur-sm animate-fade-in"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onCloseRef.current();
      }}
      onKeyDown={handleKeyDown}
    >
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label={labelledBy ? undefined : label}
        aria-labelledby={labelledBy}
        tabIndex={-1}
        className={`relative max-h-[92vh] overflow-y-auto rounded-3xl bg-[#11131E] border border-slate-800 shadow-2xl focus:outline-none animate-pop-in ${panelClassName}`}
      >
        {showCloseButton && (
          <button
            type="button"
            onClick={() => onCloseRef.current()}
            aria-label="Fechar"
            className="absolute top-3 right-3 z-20 p-2 rounded-full bg-black/60 text-slate-200 hover:bg-violet-600 hover:text-white transition-colors"
          >
            <X className="w-4 h-4" aria-hidden="true" />
          </button>
        )}
        {children}
      </div>
    </div>,
    document.body
  );
};

/** True while any modal dialog is open (used by global keyboard shortcuts). */
export function isAnyModalOpen(): boolean {
  return modalStack.length > 0;
}
