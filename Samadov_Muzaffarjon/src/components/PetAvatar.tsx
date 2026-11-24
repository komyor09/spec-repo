import {PawPrint, Stethoscope} from 'lucide-react';
import {useState} from 'react';

interface PetAvatarProps {
  src?: string;
  alt: string;
  size?: 'sm' | 'md' | 'lg';
  variant?: 'pet' | 'doctor';
}

export function PetAvatar({
  src,
  alt,
  size = 'md',
  variant = 'pet',
}: PetAvatarProps) {
  const [failed, setFailed] = useState(false);

  const dims =
    size === 'sm'
      ? 'w-9 h-9 rounded-lg'
      : size === 'lg'
        ? 'w-20 h-20 rounded-xl'
        : 'w-11 h-11 rounded-lg';

  const iconSize = size === 'lg' ? 28 : size === 'sm' ? 16 : 18;

  if (!src || failed) {
    return (
      <div
        className={`${dims} shrink-0 bg-slate-100 border border-slate-200 flex items-center justify-center text-slate-500`}
        aria-label={alt}
      >
        {variant === 'doctor' ? (
          <Stethoscope size={iconSize} strokeWidth={1.75} />
        ) : (
          <PawPrint size={iconSize} strokeWidth={1.75} />
        )}
      </div>
    );
  }

  return (
    <img
      src={src}
      alt={alt}
      referrerPolicy="no-referrer"
      onError={() => setFailed(true)}
      className={`${dims} shrink-0 object-cover border border-slate-200 bg-slate-100`}
    />
  );
}
