import React, { useState } from 'react';
import { Film } from 'lucide-react';

interface PosterImageProps {
  src?: string | null;
  alt: string;
  className?: string;
  /** Text shown inside the placeholder when the image is missing or fails. */
  fallbackTitle?: string;
  eager?: boolean;
}

/** Lazy-loaded poster with a styled placeholder when the URL is missing or broken. */
export const PosterImage: React.FC<PosterImageProps> = ({
  src,
  alt,
  className = '',
  fallbackTitle,
  eager = false
}) => {
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  const broken = !src || failedSrc === src;

  if (broken) {
    return (
      <div
        role="img"
        aria-label={alt}
        className={`flex flex-col items-center justify-center gap-2 p-3 text-center bg-gradient-to-br from-violet-950/80 via-slate-900 to-[#0B0D14] text-slate-400 ${className}`}
      >
        <Film className="w-7 h-7 text-violet-400/70" aria-hidden="true" />
        {fallbackTitle && (
          <span className="text-[11px] font-semibold leading-tight line-clamp-3 text-slate-300">
            {fallbackTitle}
          </span>
        )}
      </div>
    );
  }

  return (
    <img
      src={src}
      alt={alt}
      loading={eager ? 'eager' : 'lazy'}
      decoding="async"
      onError={() => setFailedSrc(src)}
      className={className}
    />
  );
};
