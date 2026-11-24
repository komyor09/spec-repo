import {
  AlertTriangle,
  Calculator,
  Minus,
  Plus,
  Search,
  Snowflake,
} from 'lucide-react';
import {useMemo, useState} from 'react';
import type {ClinicState, MedicationCategory} from '../types/clinic.ts';

const CATEGORIES: ('Все' | MedicationCategory)[] = [
  'Все',
  'Антибиотики',
  'Вакцины',
  'Противопаразитарные',
  'Анальгетики и НПВС',
  'Гастропротекторы',
  'Растворы и инфузии',
  'Дерматология и уши',
  'Кардиология',
];

const DOSAGE_PROTOCOLS = [
  {
    id: 'amox',
    label: 'Амоксициллин + клавулановая к-та (Синулокс)',
    dosePerKg: 12.5,
    unit: 'мг',
    frequency: '2 раза в сутки (каждые 12 ч)',
  },
  {
    id: 'melox',
    label: 'Мелоксикам суспензия 1.5 мг/мл (поддерживающая)',
    dosePerKg: 0.1,
    unit: 'мг',
    concentrationMgPerMl: 1.5,
    frequency: '1 раз в сутки после еды',
  },
  {
    id: 'omep',
    label: 'Омепразол Вет',
    dosePerKg: 1.0,
    unit: 'мг',
    frequency: '1–2 раза в сутки за 30 мин до еды',
  },
  {
    id: 'pimo',
    label: 'Пимобендан (Ветмедин)',
    dosePerKg: 0.25,
    unit: 'мг',
    frequency: '2 раза в сутки за 1 час до кормления',
  },
  {
    id: 'inf',
    label: 'Инфузия поддерживающая (Рингера-Локка / NaCl 0.9%)',
    dosePerKg: 50,
    unit: 'мл',
    frequency: 'суточный объем (разделить на 24 ч или 2–3 болюса)',
  },
];

export function PharmacyView({
  state,
  onAdjustStock,
  onOpenNewMedication,
}: {
  state: ClinicState;
  onAdjustStock: (medicationId: string, delta: number) => void;
  onOpenNewMedication: () => void;
}) {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState<'Все' | MedicationCategory>('Все');
  const [lowStockOnly, setLowStockOnly] = useState(false);

  const [showCalc, setShowCalc] = useState(false);
  const [calcPetId, setCalcPetId] = useState<string>('custom');
  const [calcWeight, setCalcWeight] = useState<string>('10');
  const [protocolId, setProtocolId] = useState<string>(DOSAGE_PROTOCOLS[0].id);

  const activeWeight = useMemo(() => {
    if (calcPetId !== 'custom') {
      const pet = state.pets.find((p) => p.id === calcPetId);
      if (pet) return pet.weightKg;
    }
    return Math.max(0.1, Number(calcWeight) || 0);
  }, [calcPetId, calcWeight, state.pets]);

  const activeProtocol =
    DOSAGE_PROTOCOLS.find((p) => p.id === protocolId) ?? DOSAGE_PROTOCOLS[0];
  const calculatedDose = activeWeight * activeProtocol.dosePerKg;
  const calculatedMl = activeProtocol.concentrationMgPerMl
    ? calculatedDose / activeProtocol.concentrationMgPerMl
    : null;

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return state.medications.filter((med) => {
      if (category !== 'Все' && med.category !== category) return false;
      if (lowStockOnly && med.stockQty > med.minThreshold) return false;
      if (!q) return true;
      return (
        med.name.toLowerCase().includes(q) ||
        med.activeIngredient.toLowerCase().includes(q) ||
        med.manufacturer.toLowerCase().includes(q)
      );
    });
  }, [state.medications, search, category, lowStockOnly]);

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex flex-wrap items-center gap-3 flex-1">
          <div className="relative w-72">
            <Search
              size={15}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
            />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Препарат, ДВ или производитель..."
              className="w-full h-10 pl-9 pr-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>

          <select
            value={category}
            onChange={(e) =>
              setCategory(e.target.value as 'Все' | MedicationCategory)
            }
            className="h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-800 focus:outline-none focus:border-teal-700"
          >
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {c === 'Все' ? 'Все категории' : c}
              </option>
            ))}
          </select>

          <button
            type="button"
            onClick={() => setLowStockOnly((v) => !v)}
            className={`h-10 px-3.5 text-xs font-medium rounded-lg border transition-colors whitespace-nowrap ${
              lowStockOnly
                ? 'bg-amber-50 border-amber-300 text-amber-800'
                : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
            }`}
          >
            Только дефицит
          </button>

          <button
            type="button"
            onClick={() => setShowCalc((v) => !v)}
            className={`h-10 px-3.5 inline-flex items-center gap-1.5 text-xs font-medium rounded-lg border transition-colors whitespace-nowrap ${
              showCalc
                ? 'bg-teal-50 border-teal-300 text-teal-800'
                : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
            }`}
          >
            <Calculator size={14} />
            Калькулятор доз
          </button>
        </div>

        <button
          type="button"
          onClick={onOpenNewMedication}
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap shrink-0"
        >
          <Plus size={15} />
          Новый препарат
        </button>
      </div>

      {showCalc && (
        <section className="bg-white border border-slate-200 rounded-xl p-5 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-900">
              Калькулятор дозировок по весу пациента
            </h3>
            <span className="text-xs text-slate-500">
              Справочный расчет для назначения
            </span>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-end">
            <div className="md:col-span-3">
              <label className="block text-xs text-slate-500 mb-1">
                Пациент из базы
              </label>
              <select
                value={calcPetId}
                onChange={(e) => setCalcPetId(e.target.value)}
                className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs text-slate-900"
              >
                <option value="custom">Ввести вес вручную</option>
                {state.pets.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} ({p.weightKg} кг)
                  </option>
                ))}
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="block text-xs text-slate-500 mb-1">
                Вес (кг)
              </label>
              <input
                type="number"
                step="0.05"
                min="0.05"
                disabled={calcPetId !== 'custom'}
                value={calcPetId === 'custom' ? calcWeight : activeWeight}
                onChange={(e) => setCalcWeight(e.target.value)}
                className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white disabled:bg-slate-50 text-xs font-mono tabular-nums"
              />
            </div>
            <div className="md:col-span-4">
              <label className="block text-xs text-slate-500 mb-1">
                Препарат / Протокол
              </label>
              <select
                value={protocolId}
                onChange={(e) => setProtocolId(e.target.value)}
                className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs text-slate-900"
              >
                {DOSAGE_PROTOCOLS.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.label} ({p.dosePerKg} {p.unit}/кг)
                  </option>
                ))}
              </select>
            </div>
            <div className="md:col-span-3 bg-slate-50 border border-slate-200 rounded-lg px-4 py-2">
              <div className="text-[11px] text-slate-500">Разовая доза</div>
              <div className="text-sm font-mono font-semibold text-teal-800 tabular-nums">
                {calculatedDose.toFixed(2)} {activeProtocol.unit}
                {calculatedMl !== null && (
                  <span className="text-slate-600 font-normal">
                    {' '}
                    ({calculatedMl.toFixed(2)} мл)
                  </span>
                )}
              </div>
              <div className="text-[11px] text-slate-500 truncate">
                {activeProtocol.frequency}
              </div>
            </div>
          </div>
        </section>
      )}

      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        {filtered.length === 0 ? (
          <div className="p-10 text-center text-sm text-slate-500">
            Препараты по выбранным условиям не найдены.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                  <th className="py-3 px-4">Наименование и ДВ</th>
                  <th className="py-3 px-4">Категория и производитель</th>
                  <th className="py-3 px-4">Хранение и серия</th>
                  <th className="py-3 px-4">Годен до</th>
                  <th className="py-3 px-4 text-right">Цена</th>
                  <th className="py-3 px-4 text-right">Остаток</th>
                  <th className="py-3 px-4 text-right">Движение</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 text-sm">
                {filtered.map((med) => {
                  const isLow = med.stockQty <= med.minThreshold;
                  const isCold = med.storageCondition.includes('+2');
                  return (
                    <tr key={med.id} className="hover:bg-slate-50">
                      <td className="py-3 px-4">
                        <div className="font-medium text-slate-900">
                          {med.name}
                        </div>
                        <div className="text-xs text-slate-500">
                          {med.activeIngredient}
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="text-slate-800">{med.category}</div>
                        <div className="text-xs text-slate-500">
                          {med.dosageForm} · {med.manufacturer}
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <div
                          className={`text-xs inline-flex items-center gap-1 ${
                            isCold ? 'text-sky-700 font-medium' : 'text-slate-700'
                          }`}
                        >
                          {isCold && <Snowflake size={12} />}
                          {med.storageCondition}
                        </div>
                        <div className="text-xs font-mono text-slate-500 tabular-nums">
                          {med.batchNumber} ·{' '}
                          {med.prescriptionRequired
                            ? 'Рецептурный'
                            : 'Без рецепта'}
                        </div>
                      </td>
                      <td className="py-3 px-4 font-mono text-xs text-slate-600 tabular-nums whitespace-nowrap">
                        {med.expiryDate}
                      </td>
                      <td className="py-3 px-4 text-right font-mono text-xs text-slate-900 tabular-nums whitespace-nowrap">
                        {med.unitPriceRub.toLocaleString('ru-RU')} ₽
                      </td>
                      <td className="py-3 px-4 text-right whitespace-nowrap">
                        <div
                          className={`inline-flex items-center gap-1.5 font-mono text-xs font-semibold tabular-nums ${
                            isLow ? 'text-amber-700' : 'text-slate-900'
                          }`}
                        >
                          {isLow && <AlertTriangle size={13} />}
                          {med.stockQty} {med.unit}
                        </div>
                        <div className="text-xs text-slate-400 tabular-nums">
                          мин. {med.minThreshold}
                        </div>
                      </td>
                      <td className="py-3 px-4 text-right whitespace-nowrap">
                        <div className="inline-flex items-center gap-1.5">
                          <button
                            type="button"
                            onClick={() => onAdjustStock(med.id, -1)}
                            disabled={med.stockQty <= 0}
                            className="h-8 w-8 inline-flex items-center justify-center rounded-md border border-slate-200 bg-white text-slate-700 hover:bg-slate-100 disabled:opacity-40"
                            title="Списать 1 единицу"
                            aria-label={`Списать 1 ${med.name}`}
                          >
                            <Minus size={14} />
                          </button>
                          <button
                            type="button"
                            onClick={() => onAdjustStock(med.id, 5)}
                            className="h-8 px-2.5 inline-flex items-center gap-1 rounded-md border border-slate-200 bg-white text-xs font-medium text-slate-700 hover:bg-slate-100 tabular-nums"
                            title="Приход 5 единиц"
                          >
                            <Plus size={13} />5
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
