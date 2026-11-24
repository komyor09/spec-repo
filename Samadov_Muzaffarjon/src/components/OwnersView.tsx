import {CheckCircle2, Clock, Plus, Receipt, Search, UserCheck} from 'lucide-react';
import {useMemo, useState, type FormEvent} from 'react';
import type {ClinicState, InvoiceStatus} from '../types/clinic.ts';

export function OwnersView({
  state,
  onOpenNewOwner,
  onSelectPet,
  onOpenOwnerPortal,
  onCreateInvoice,
  onUpdateInvoiceStatus,
}: {
  state: ClinicState;
  onOpenNewOwner: () => void;
  onSelectPet: (petId: string) => void;
  onOpenOwnerPortal: (ownerId: string) => void;
  onCreateInvoice: (payload: {
    ownerId: string;
    petId: string;
    title: string;
    amountRub: number;
  }) => Promise<void>;
  onUpdateInvoiceStatus: (id: string, status: InvoiceStatus) => Promise<void>;
}) {
  const [subTab, setSubTab] = useState<'owners' | 'invoices'>('owners');
  const [search, setSearch] = useState('');
  const [showInvoiceForm, setShowInvoiceForm] = useState(false);

  const [invPetId, setInvPetId] = useState(state.pets[0]?.id || '');
  const [invTitle, setInvTitle] = useState('');
  const [invAmount, setInvAmount] = useState('2400');

  const filteredOwners = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return state.owners;
    return state.owners.filter(
      (o) =>
        o.fullName.toLowerCase().includes(q) ||
        o.phone.toLowerCase().includes(q) ||
        o.email.toLowerCase().includes(q),
    );
  }, [state.owners, search]);

  const handleInvoiceSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const pet = state.pets.find((p) => p.id === invPetId);
    if (!pet || !invTitle.trim()) return;
    await onCreateInvoice({
      ownerId: pet.ownerId,
      petId: pet.id,
      title: invTitle.trim(),
      amountRub: Number(invAmount) || 0,
    });
    setInvTitle('');
    setShowInvoiceForm(false);
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-1 p-1 bg-slate-200/70 rounded-lg">
          <button
            type="button"
            onClick={() => setSubTab('owners')}
            className={`px-3.5 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap ${
              subTab === 'owners'
                ? 'bg-white text-slate-900 shadow-sm'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            База владельцев ({state.owners.length})
          </button>
          <button
            type="button"
            onClick={() => setSubTab('invoices')}
            className={`px-3.5 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap ${
              subTab === 'invoices'
                ? 'bg-white text-slate-900 shadow-sm'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            Счета и оплаты ({state.invoices.length})
          </button>
        </div>

        {subTab === 'owners' ? (
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-64">
              <Search
                size={15}
                className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
              />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Поиск по ФИО, телефону..."
                className="w-full h-10 pl-9 pr-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
              />
            </div>

            <button
              type="button"
              onClick={onOpenNewOwner}
              className="inline-flex items-center gap-2 px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap shrink-0"
            >
              <Plus size={15} />
              Добавить владельца
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setShowInvoiceForm((v) => !v)}
            className="inline-flex items-center gap-2 px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap shrink-0"
          >
            <Receipt size={15} />
            Выставить счет
          </button>
        )}
      </div>

      {subTab === 'owners' && (
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                  <th className="py-3 px-4">Владелец</th>
                  <th className="py-3 px-4">Контакты</th>
                  <th className="py-3 px-4">Адрес</th>
                  <th className="py-3 px-4">Питомцы</th>
                  <th className="py-3 px-4 text-right">Личный кабинет</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 text-sm">
                {filteredOwners.map((owner) => {
                  const ownerPets = state.pets.filter(
                    (p) => p.ownerId === owner.id,
                  );
                  return (
                    <tr key={owner.id} className="hover:bg-slate-50">
                      <td className="py-3.5 px-4">
                        <div className="font-medium text-slate-900">
                          {owner.fullName}
                        </div>
                        <div className="text-xs text-slate-500 tabular-nums">
                          {owner.loyaltyTier} · с {owner.registeredAt}
                        </div>
                      </td>
                      <td className="py-3.5 px-4">
                        <div className="text-slate-900 tabular-nums">
                          {owner.phone}
                        </div>
                        <div className="text-xs text-slate-500">
                          {owner.email}
                        </div>
                      </td>
                      <td className="py-3.5 px-4 text-slate-600 text-xs max-w-xs">
                        {owner.address || '—'}
                      </td>
                      <td className="py-3.5 px-4">
                        {ownerPets.length === 0 ? (
                          <span className="text-xs text-slate-400">
                            Нет питомцев
                          </span>
                        ) : (
                          <div className="flex flex-wrap gap-2">
                            {ownerPets.map((pet) => (
                              <button
                                key={pet.id}
                                type="button"
                                onClick={() => onSelectPet(pet.id)}
                                className="text-xs font-medium text-teal-700 hover:underline"
                              >
                                {pet.name} ({pet.species})
                              </button>
                            ))}
                          </div>
                        )}
                      </td>
                      <td className="py-3.5 px-4 text-right">
                        <button
                          type="button"
                          onClick={() => onOpenOwnerPortal(owner.id)}
                          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-700 border border-slate-200 rounded-lg hover:bg-slate-100 whitespace-nowrap"
                        >
                          <UserCheck size={14} />
                          Открыть кабинет
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {subTab === 'invoices' && (
        <div className="space-y-4">
          {showInvoiceForm && (
            <form
              onSubmit={handleInvoiceSubmit}
              className="bg-white border border-slate-200 rounded-xl p-5 grid grid-cols-1 md:grid-cols-12 gap-3 items-end"
            >
              <div className="md:col-span-3">
                <label className="block text-xs text-slate-500 mb-1">
                  Пациент и владелец
                </label>
                <select
                  value={invPetId}
                  onChange={(e) => setInvPetId(e.target.value)}
                  className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
                >
                  {state.pets.map((p) => {
                    const o = state.owners.find((own) => own.id === p.ownerId);
                    return (
                      <option key={p.id} value={p.id}>
                        {p.name} — {o?.fullName}
                      </option>
                    );
                  })}
                </select>
              </div>
              <div className="md:col-span-5">
                <label className="block text-xs text-slate-500 mb-1">
                  Наименование услуг / препаратов
                </label>
                <input
                  type="text"
                  value={invTitle}
                  onChange={(e) => setInvTitle(e.target.value)}
                  placeholder="Осмотр дерматолога + Суролан 15 мл"
                  className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
                  required
                />
              </div>
              <div className="md:col-span-2">
                <label className="block text-xs text-slate-500 mb-1">
                  Сумма (₽)
                </label>
                <input
                  type="number"
                  min="0"
                  value={invAmount}
                  onChange={(e) => setInvAmount(e.target.value)}
                  className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs font-mono tabular-nums"
                  required
                />
              </div>
              <div className="md:col-span-2">
                <button
                  type="submit"
                  className="w-full h-9 px-3 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
                >
                  Создать счет
                </button>
              </div>
            </form>
          )}

          <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                  <th className="py-3 px-4">Дата</th>
                  <th className="py-3 px-4">Владелец и питомец</th>
                  <th className="py-3 px-4">Состав счета</th>
                  <th className="py-3 px-4 text-right">Сумма</th>
                  <th className="py-3 px-4">Статус</th>
                  <th className="py-3 px-4 text-right">Оплата</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 text-sm">
                {state.invoices.map((inv) => {
                  const owner = state.owners.find((o) => o.id === inv.ownerId);
                  const pet = state.pets.find((p) => p.id === inv.petId);
                  return (
                    <tr key={inv.id} className="hover:bg-slate-50">
                      <td className="py-3 px-4 font-mono text-xs tabular-nums whitespace-nowrap">
                        {inv.date}
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        <div className="font-medium text-slate-900">
                          {owner?.fullName}
                        </div>
                        <div className="text-xs text-slate-500">
                          {pet?.name} ({pet?.species})
                        </div>
                      </td>
                      <td className="py-3 px-4 text-xs text-slate-700">
                        {inv.title}
                      </td>
                      <td className="py-3 px-4 text-right font-mono text-xs font-semibold text-slate-900 tabular-nums whitespace-nowrap">
                        {inv.amountRub.toLocaleString('ru-RU')} ₽
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        {inv.status === 'Оплачен' ? (
                          <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700">
                            <CheckCircle2 size={13} />
                            Оплачен
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1.5 text-xs font-medium text-amber-700">
                            <Clock size={13} />
                            Ожидает оплаты
                          </span>
                        )}
                      </td>
                      <td className="py-3 px-4 text-right whitespace-nowrap">
                        {inv.status === 'Ожидает оплаты' && (
                          <button
                            type="button"
                            onClick={() =>
                              onUpdateInvoiceStatus(inv.id, 'Оплачен')
                            }
                            className="px-2.5 py-1 text-xs font-medium text-teal-700 border border-teal-200 bg-teal-50 rounded-md hover:bg-teal-100"
                          >
                            Отметить оплаченным
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
