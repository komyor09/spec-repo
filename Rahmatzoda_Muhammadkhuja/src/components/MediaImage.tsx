import { useState } from 'react';
import { BookOpen, User as UserIcon } from 'lucide-react';
import { getInitials } from '../utils/formatters.ts';

interface MediaImageProps {
  src?: string;
  alt: string;
  className?: string;
  kind?: 'cover' | 'avatar';
  fallbackName?: string;
}

export function MediaImage({
  src,
  alt,
  className = '',
  kind = 'cover',
  fallbackName,
}: MediaImageProps) {
  const [failed, setFailed] = useState(false);

  if (!src || failed) {
    if (kind === 'avatar') {
      return (
        <div
          aria-label={alt}
          className={`flex items-center justify-center bg-[#1A2332] border border-white/10 text-amber-400 font-semibold select-none ${className}`}
        >
          {fallbackName ? (
            <span className="text-xs tracking-wide">{getInitials(fallbackName)}</span>
          ) : (
            <UserIcon className="w-4 h-4" />
          )}
        </div>
      );
    }

    return (
      <div
        aria-label={alt}
        className={`flex flex-col items-center justify-center bg-gradient-to-br from-[#1A2332] via-[#131A26] to-[#0B0F17] border-b border-white/[0.08] text-slate-400 px-4 text-center ${className}`}
      >
        <BookOpen className="w-7 h-7 text-amber-400/80 mb-2" />
        <span className="text-xs font-medium text-slate-300 line-clamp-2">{alt}</span>
      </div>
    );
  }

  return (
    <img
      src={src}
      alt={alt}
      referrerPolicy="no-referrer"
      onError={() => setFailed(true)}
      className={className}
    />
  );
}
