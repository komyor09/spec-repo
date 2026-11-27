import { Award, Download, Printer, X, ShieldCheck } from 'lucide-react';
import type { Course, Enrollment, User } from '../types/platform.ts';
import { formatRuDate } from '../utils/formatters.ts';

interface CertificateModalProps {
  enrollment: Enrollment;
  course: Course;
  student: User;
  instructor?: User;
  onClose: () => void;
}

export function CertificateModal({
  enrollment,
  course,
  student,
  instructor,
  onClose,
}: CertificateModalProps) {
  const certificateCode = enrollment.certificateId ?? 'VEC-2026-0001';
  const completionDate = formatRuDate(enrollment.completedAt ?? new Date().toISOString().slice(0, 10));

  const handleDownloadPng = () => {
    const canvas = document.createElement('canvas');
    canvas.width = 1600;
    canvas.height = 1130;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Background
    ctx.fillStyle = '#0B0F17';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Subtle radial highlight
    const grad = ctx.createRadialGradient(800, 320, 60, 800, 450, 760);
    grad.addColorStop(0, 'rgba(245, 158, 11, 0.14)');
    grad.addColorStop(1, 'rgba(11, 15, 23, 0)');
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Outer & inner frames
    ctx.strokeStyle = 'rgba(245, 158, 11, 0.55)';
    ctx.lineWidth = 4;
    ctx.strokeRect(48, 48, canvas.width - 96, canvas.height - 96);

    ctx.strokeStyle = 'rgba(255, 255, 255, 0.12)';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(68, 68, canvas.width - 136, canvas.height - 136);

    // Brand header
    ctx.fillStyle = '#F59E0B';
    ctx.font = '700 28px "Plus Jakarta Sans", sans-serif';
    ctx.fillText('АКАДЕМИЯ ВЕКТОР', 120, 150);

    ctx.fillStyle = '#94A3B8';
    ctx.font = '500 22px "Manrope", sans-serif';
    ctx.fillText('СЕРТИФИКАТ ОБ ОКОНЧАНИИ ПРОГРАММЫ', 120, 195);

    // Confirm label
    ctx.fillStyle = '#94A3B8';
    ctx.font = '400 24px "Manrope", sans-serif';
    ctx.fillText('Настоящим подтверждается, что', 120, 330);

    // Student name
    ctx.fillStyle = '#F8FAFC';
    ctx.font = '700 64px "Plus Jakarta Sans", sans-serif';
    ctx.fillText(student.name, 120, 415);

    // Course completion line
    ctx.fillStyle = '#CBD5E1';
    ctx.font = '400 26px "Manrope", sans-serif';
    ctx.fillText('успешно завершил(а) полный курс обучения и итоговую аттестацию по программе', 120, 485);

    // Course title (with simple line wrapping)
    ctx.fillStyle = '#FBBF24';
    ctx.font = '700 42px "Plus Jakarta Sans", sans-serif';
    const words = `«${course.title}»`.split(' ');
    let line = '';
    let y = 570;
    for (const word of words) {
      const testLine = line ? `${line} ${word}` : word;
      if (ctx.measureText(testLine).width > 1320) {
        ctx.fillText(line, 120, y);
        line = word;
        y += 54;
      } else {
        line = testLine;
      }
    }
    if (line) {
      ctx.fillText(line, 120, y);
    }

    // Stats line
    ctx.fillStyle = '#94A3B8';
    ctx.font = '500 22px "Manrope", sans-serif';
    ctx.fillText(
      `Направление: ${course.category}   ·   Объём программы: ${course.durationHours} академических часов   ·   Результат: 100%`,
      120,
      740,
    );

    // Divider
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.14)';
    ctx.beginPath();
    ctx.moveTo(120, 860);
    ctx.lineTo(canvas.width - 120, 860);
    ctx.stroke();

    // Footer metadata
    ctx.fillStyle = '#94A3B8';
    ctx.font = '400 20px "Manrope", sans-serif';
    ctx.fillText('Преподаватель курса', 120, 920);
    ctx.fillText('Дата выдачи', 640, 920);
    ctx.fillText('Регистрационный номер', 1120, 920);

    ctx.fillStyle = '#F8FAFC';
    ctx.font = '600 26px "Plus Jakarta Sans", sans-serif';
    ctx.fillText(instructor?.name ?? 'Алексей Соколов', 120, 962);
    ctx.fillText(completionDate, 640, 962);

    ctx.fillStyle = '#F59E0B';
    ctx.font = '600 26px "JetBrains Mono", monospace';
    ctx.fillText(certificateCode, 1120, 962);

    const link = document.createElement('a');
    link.download = `certificate-${certificateCode}.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4 overflow-y-auto">
      <div className="w-full max-w-4xl space-y-4">
        {/* Modal Controls */}
        <div className="flex flex-wrap items-center justify-between gap-3 bg-[#131A26] border border-white/10 rounded-xl px-5 py-3">
          <div className="flex items-center gap-2 text-sm font-medium text-slate-200">
            <Award className="w-4 h-4 text-amber-400" />
            <span>Сертификат об окончании курса</span>
          </div>
          <div className="flex items-center gap-2.5">
            <button
              type="button"
              onClick={handleDownloadPng}
              className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-lg bg-[#1A2332] text-slate-100 hover:bg-[#243044] border border-white/10 transition-colors cursor-pointer"
            >
              <Download className="w-3.5 h-3.5 text-amber-400" />
              <span>Скачать PNG</span>
            </button>
            <button
              type="button"
              onClick={() => window.print()}
              className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
            >
              <Printer className="w-3.5 h-3.5" />
              <span>Печать / PDF</span>
            </button>
            <button
              type="button"
              onClick={onClose}
              aria-label="Закрыть окно сертификата"
              className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-white/5 transition-colors cursor-pointer"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Printable Certificate Sheet */}
        <div
          id="printable-certificate"
          className="relative rounded-2xl bg-[#0B0F17] border-2 border-amber-500/50 p-8 md:p-12 overflow-hidden shadow-2xl"
        >
          <div className="border border-white/10 rounded-xl p-8 md:p-10 space-y-8 relative">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="space-y-1">
                <div className="font-display text-lg font-bold tracking-wider text-amber-400 uppercase">
                  Академия Вектор
                </div>
                <div className="text-xs tracking-widest text-slate-400 uppercase">
                  Сертификат об окончании программы
                </div>
              </div>

              <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-amber-500/10 border border-amber-500/30 text-amber-300">
                <ShieldCheck className="w-4 h-4" />
                <span className="font-mono text-xs font-semibold tabular-nums">
                  {certificateCode}
                </span>
              </div>
            </div>

            <div className="space-y-4 py-4">
              <p className="text-sm text-slate-400">Настоящим подтверждается, что</p>
              <h2 className="font-display text-3xl md:text-5xl font-bold text-slate-50 tracking-tight">
                {student.name}
              </h2>
              <p className="text-base text-slate-300 max-w-2xl leading-relaxed">
                успешно освоил(а) учебную программу, выполнил(а) практические задания и прошёл(ла)
                итоговую аттестацию по курсу:
              </p>
              <div className="font-display text-2xl md:text-3xl font-bold text-amber-400 leading-snug">
                «{course.title}»
              </div>
              <p className="text-xs text-slate-400 pt-1">
                Направление: {course.category} · Уровень: {course.level} · Объём: {course.durationHours} академических часов · Результат: 100%
              </p>
            </div>

            <div className="pt-8 border-t border-white/10 grid grid-cols-1 sm:grid-cols-3 gap-6">
              <div>
                <div className="text-xs text-slate-500 mb-1">Преподаватель курса</div>
                <div className="text-sm font-semibold text-slate-100">
                  {instructor?.name ?? 'Алексей Соколов'}
                </div>
                <div className="text-xs text-slate-400">
                  {instructor?.specialty ?? 'Ведущий эксперт'}
                </div>
              </div>

              <div>
                <div className="text-xs text-slate-500 mb-1">Дата выдачи</div>
                <div className="text-sm font-semibold text-slate-100 font-mono tabular-nums">
                  {completionDate}
                </div>
              </div>

              <div className="sm:text-right">
                <div className="text-xs text-slate-500 mb-1">Статус документа</div>
                <div className="text-sm font-semibold text-emerald-400">
                  Подтверждён академией
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
