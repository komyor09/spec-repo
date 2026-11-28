import {Printer, X} from 'lucide-react';
import type {ClinicState, Invoice, Pet} from '../types/clinic.ts';

export type PrintDocumentTarget =
  | {kind: 'pet-extract'; petId: string}
  | {kind: 'invoice'; invoiceId: string};

export function PrintPreviewModal({
  target,
  state,
  onClose,
}: {
  target: PrintDocumentTarget;
  state: ClinicState;
  onClose: () => void;
}) {
  const handlePrint = () => {
    window.print();
  };

  if (target.kind === 'pet-extract') {
    const pet = state.pets.find((p) => p.id === target.petId);
    if (!pet) return null;
    const owner = state.owners.find((o) => o.id === pet.ownerId);
    const records = state.medicalRecords.filter((r) => r.petId === pet.id);
    const vaccines = state.vaccinations.filter((v) => v.petId === pet.id);
    const labs = state.labTests.filter((l) => l.petId === pet.id);

    return (
      <PrintModalFrame
        title={`Медицинская выписка — ${pet.name}`}
        onClose={onClose}
        onPrint={handlePrint}
      >
        <PetExtractSheet
          pet={pet}
          ownerName={owner?.fullName ?? 'Не указан'}
          ownerPhone={owner?.phone ?? '—'}
          loyaltyTier={owner?.loyaltyTier ?? 'Стандарт'}
          state={state}
          records={records}
          vaccines={vaccines}
          labs={labs}
        />
      </PrintModalFrame>
    );
  }

  const invoice = state.invoices.find((i) => i.id === target.invoiceId);
  if (!invoice) return null;
  const owner = state.owners.find((o) => o.id === invoice.ownerId);
  const pet = state.pets.find((p) => p.id === invoice.petId);

  return (
    <PrintModalFrame
      title={`Квитанция по счету ${invoice.id}`}
      onClose={onClose}
      onPrint={handlePrint}
    >
      <InvoiceReceiptSheet
        invoice={invoice}
        ownerName={owner?.fullName ?? 'Не указан'}
        ownerPhone={owner?.phone ?? '—'}
        ownerTier={owner?.loyaltyTier ?? 'Стандарт'}
        pet={pet}
      />
    </PrintModalFrame>
  );
}

function PrintModalFrame({
  title,
  onClose,
  onPrint,
  children,
}: {
  title: string;
  onClose: () => void;
  onPrint: () => void;
  children: React.ReactNode;
}) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4 print:static print:inset-auto print:bg-white print:p-0 print:block"
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      <div className="w-full max-w-3xl bg-white border border-slate-200 rounded-xl shadow-xl overflow-hidden flex flex-col max-h-[90vh] print:max-w-none print:max-h-none print:shadow-none print:border-0 print:rounded-none">
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 bg-slate-50 print:hidden">
          <h2 className="text-sm font-semibold text-slate-900 truncate">
            {title}
          </h2>
          <div className="flex items-center gap-2 shrink-0">
            <button
              type="button"
              onClick={onPrint}
              className="inline-flex items-center gap-2 px-3.5 py-1.5 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap"
            >
              <Printer size={14} />
              Печать / Сохранить PDF
            </button>
            <button
              type="button"
              onClick={onClose}
              className="p-1.5 rounded-lg text-slate-500 hover:text-slate-900 hover:bg-slate-200/70 transition-colors"
              aria-label="Закрыть предпросмотр"
            >
              <X size={18} />
            </button>
          </div>
        </div>
        <div className="p-6 sm:p-8 overflow-y-auto bg-white print:p-0 print:overflow-visible">
          {children}
        </div>
      </div>
    </div>
  );
}

function PetExtractSheet({
  pet,
  ownerName,
  ownerPhone,
  loyaltyTier,
  state,
  records,
  vaccines,
  labs,
}: {
  pet: Pet;
  ownerName: string;
  ownerPhone: string;
  loyaltyTier: string;
  state: ClinicState;
  records: ClinicState['medicalRecords'];
  vaccines: ClinicState['vaccinations'];
  labs: ClinicState['labTests'];
}) {
  const today = new Date().toISOString().slice(0, 10);

  return (
    <div className="space-y-6 text-slate-900 text-sm">
      <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-300 pb-4">
        <div>
          <div className="text-lg font-bold tracking-tight text-slate-900">
            Ветеринарная клиника «ВетМед»
          </div>
          <div className="text-xs text-slate-500 mt-0.5">
            Выписка из электронной медицинской карты пациента
          </div>
        </div>
        <div className="text-right text-xs text-slate-600 font-mono tabular-nums">
          <div>Дата формирования: {today}</div>
          <div>Чип: {pet.microchipNumber}</div>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 border border-slate-200 rounded-lg p-4 text-xs">
        <div className="space-y-1.5">
          <div className="font-semibold text-slate-900 text-sm">
            {pet.name} · {pet.species}
          </div>
          <div>
            <span className="text-slate-500">Порода и окрас: </span>
            {pet.breed}, {pet.color}
          </div>
          <div>
            <span className="text-slate-500">Пол / дата рождения: </span>
            {pet.gender}
            {pet.sterilized ? ' (стерилизован)' : ''} ·{' '}
            <span className="tabular-nums">{pet.birthDate}</span>
          </div>
          <div>
            <span className="text-slate-500">Вес / группа крови: </span>
            <span className="font-mono tabular-nums">{pet.weightKg} кг</span> ·{' '}
            {pet.bloodType}
          </div>
        </div>
        <div className="space-y-1.5">
          <div className="font-semibold text-slate-900 text-sm">
            Владелец: {ownerName}
          </div>
          <div className="tabular-nums">
            <span className="text-slate-500">Телефон: </span>
            {ownerPhone} · {loyaltyTier}
          </div>
          <div>
            <span className="text-slate-500">Аллергии: </span>
            {pet.allergies}
          </div>
          <div>
            <span className="text-slate-500">Рацион: </span>
            {pet.diet}
          </div>
        </div>
      </div>

      <div className="space-y-3">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          Клинические осмотры и назначения ({records.length})
        </h3>
        {records.length === 0 ? (
          <p className="text-xs text-slate-500">Записи осмотров отсутствуют.</p>
        ) : (
          <div className="space-y-3">
            {records.map((rec) => {
              const vet = state.veterinarians.find(
                (v) => v.id === rec.veterinarianId,
              );
              return (
                <div
                  key={rec.id}
                  className="border border-slate-200 rounded-lg p-3.5 space-y-2 text-xs"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-2">
                    <span className="font-semibold text-slate-900">
                      {rec.visitDate} — {rec.diagnosis}
                    </span>
                    <span className="font-mono text-slate-600 tabular-nums">
                      T {rec.temperatureC.toFixed(1)} °C · {rec.weightKg} кг ·
                      ЧСС {rec.heartRateBpm} уд/м
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-500">Врач: </span>
                    {vet?.fullName ?? 'Врач клиники'}
                  </div>
                  <div>
                    <span className="text-slate-500">Жалобы: </span>
                    {rec.complaints}
                  </div>
                  <div>
                    <span className="text-slate-500">Рекомендации: </span>
                    {rec.treatmentPlan}
                  </div>
                  {rec.prescriptions.length > 0 && (
                    <div className="pt-1">
                      <span className="text-slate-500 font-medium">
                        Назначения:{' '}
                      </span>
                      {rec.prescriptions
                        .map(
                          (rx) =>
                            `${rx.medicationName} (${rx.dosage}, ${rx.frequency}, ${rx.durationDays} дн.)`,
                        )
                        .join('; ')}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {vaccines.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Вакцинация и профилактика ({vaccines.length})
          </h3>
          <table className="w-full text-left border-collapse text-xs border border-slate-200">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-500">
                <th className="py-2 px-3">Тип</th>
                <th className="py-2 px-3">Препарат</th>
                <th className="py-2 px-3">Серия</th>
                <th className="py-2 px-3">Дата</th>
                <th className="py-2 px-3">Следующая</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {vaccines.map((v) => (
                <tr key={v.id}>
                  <td className="py-1.5 px-3">{v.kind}</td>
                  <td className="py-1.5 px-3 font-medium">{v.vaccineName}</td>
                  <td className="py-1.5 px-3 font-mono tabular-nums">
                    {v.batchNumber}
                  </td>
                  <td className="py-1.5 px-3 font-mono tabular-nums">
                    {v.administeredDate}
                  </td>
                  <td className="py-1.5 px-3 font-mono tabular-nums">
                    {v.nextDueDate}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {labs.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Лабораторные и диагностические исследования ({labs.length})
          </h3>
          <div className="divide-y divide-slate-200 border border-slate-200 rounded-lg text-xs">
            {labs.map((lab) => (
              <div key={lab.id} className="p-2.5 flex justify-between gap-4">
                <div>
                  <span className="font-medium">{lab.testType}</span> —{' '}
                  <span className="text-slate-600">{lab.resultSummary}</span>
                </div>
                <span className="font-mono text-slate-500 shrink-0 tabular-nums">
                  {lab.orderedDate} ({lab.status})
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="pt-4 border-t border-slate-200 flex items-center justify-between text-xs text-slate-500">
        <span>Дежурный врач клиники «ВетМед»</span>
        <span>Подпись: _____________________</span>
      </div>
    </div>
  );
}

function InvoiceReceiptSheet({
  invoice,
  ownerName,
  ownerPhone,
  ownerTier,
  pet,
}: {
  invoice: Invoice;
  ownerName: string;
  ownerPhone: string;
  ownerTier: string;
  pet?: Pet;
}) {
  return (
    <div className="space-y-6 text-slate-900 text-sm">
      <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-300 pb-4">
        <div>
          <div className="text-lg font-bold tracking-tight text-slate-900">
            Ветеринарная клиника «ВетМед»
          </div>
          <div className="text-xs text-slate-500 mt-0.5">
            Квитанция на оплату ветеринарных услуг и препаратов
          </div>
        </div>
        <div className="text-right text-xs font-mono text-slate-600 tabular-nums">
          <div>Счёт № {invoice.id}</div>
          <div>Дата: {invoice.date}</div>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 border border-slate-200 rounded-lg p-4 text-xs">
        <div className="space-y-1">
          <div className="text-slate-500">Плательщик</div>
          <div className="font-semibold text-slate-900 text-sm">{ownerName}</div>
          <div className="tabular-nums text-slate-600">
            {ownerPhone} · Программа: {ownerTier}
          </div>
        </div>
        <div className="space-y-1">
          <div className="text-slate-500">Пациент</div>
          <div className="font-semibold text-slate-900 text-sm">
            {pet ? `${pet.name} (${pet.species})` : 'Пациент клиники'}
          </div>
          {pet && (
            <div className="text-slate-600 font-mono tabular-nums">
              {pet.breed} · Чип {pet.microchipNumber}
            </div>
          )}
        </div>
      </div>

      <table className="w-full text-left border-collapse border border-slate-200 text-xs">
        <thead>
          <tr className="bg-slate-50 border-b border-slate-200 text-slate-500">
            <th className="py-2.5 px-4">Наименование услуг и препаратов</th>
            <th className="py-2.5 px-4 text-right">Сумма</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td className="py-3.5 px-4 text-slate-900">{invoice.title}</td>
            <td className="py-3.5 px-4 text-right font-mono font-semibold text-slate-900 tabular-nums whitespace-nowrap">
              {invoice.amountRub.toLocaleString('ru-RU')} ₽
            </td>
          </tr>
        </tbody>
        <tfoot>
          <tr className="border-t border-slate-300 bg-slate-50">
            <td className="py-3 px-4 font-semibold text-slate-900">
              Итого к оплате ({invoice.status})
            </td>
            <td className="py-3 px-4 text-right font-mono text-base font-bold text-slate-900 tabular-nums whitespace-nowrap">
              {invoice.amountRub.toLocaleString('ru-RU')} ₽
            </td>
          </tr>
        </tfoot>
      </table>

      <div className="pt-4 border-t border-slate-200 flex items-center justify-between text-xs text-slate-500">
        <span>Администратор / Касса «ВетМед»</span>
        <span>М.П. / Подпись: _____________________</span>
      </div>
    </div>
  );
}
