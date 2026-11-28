import {AlertTriangle, CheckCircle2, Clock, FlaskConical, Plus} from 'lucide-react';
import {useMemo, useState, type FormEvent} from 'react';
import type {ClinicState, LabTestStatus} from '../types/clinic.ts';

export function LabView({
  state,
  onOrderLabTest,
  onUpdateLabTest,
  onSelectPet,
}: {
  state: ClinicState;
  onOrderLabTest: (payload: {
    petId: string;
    veterinarianId: string;
    testType: string;
    sampleType: string;
  }) => Promise<string | null>;
  onUpdateLabTest: (
    id: string,
    payload: {
      status?: LabTestStatus;
      resultSummary?: string;
      isAbnormal?: boolean;
    },
  ) => Promise<void>;
  onSelectPet: (petId: string) => void;
}) {
  const [statusFilter, setStatusFilter] = useState<'Все' | LabTestStatus>('Все');
  const [showOrderForm, setShowOrderForm] = useState(false);

  const [petId, setPetId] = useState(state.pets[0]?.id || '');
  const [veterinarianId, setVeterinarianId] = useState(
    state.veterinarians[0]?.id || '',
  );
  const [testType, setTestType] = useState(
    'Общий клинический анализ крови (ОАК)',
  );
  const [sampleType, setSampleType] = useState('Венозная кровь (ЭДТА)');

  const [editingId, setEditingId] = useState<string | null>(null);
  const [draftResult, setDraftResult] = useState('');
  const [draftAbnormal, setDraftAbnormal] = useState(false);

  const filtered = useMemo(() => {
    if (statusFilter === 'Все') return state.labTests;
    return state.labTests.filter((t) => t.status === statusFilter);
  }, [state.labTests, statusFilter]);

  const handleOrder = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !testType.trim()) return;
    await onOrderLabTest({
      petId,
      veterinarianId,
      testType: testType.trim(),
      sampleType: sampleType.trim(),
    });
    setShowOrderForm(false);
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-1 p-1 bg-slate-200/70 rounded-lg">
          {(['Все', 'Назначен', 'В работе', 'Готово'] as const).map((st) => (
            <button
              key={st}
              type="button"
              onClick={() => setStatusFilter(st)}
              className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap shrink-0 ${
                statusFilter === st
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              {st}
            </button>
          ))}
        </div>

        <button
          type="button"
          onClick={() => setShowOrderForm((v) => !v)}
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap shrink-0"
        >
          <Plus size={15} />
          Назначить исследование
        </button>
      </div>

      {showOrderForm && (
        <form
          onSubmit={handleOrder}
          className="bg-white border border-slate-200 rounded-xl p-5 grid grid-cols-1 md:grid-cols-12 gap-3 items-end"
        >
          <div className="md:col-span-3">
            <label className="block text-xs text-slate-500 mb-1">Пациент</label>
            <select
              value={petId}
              onChange={(e) => setPetId(e.target.value)}
              className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
            >
              {state.pets.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.species})
                </option>
              ))}
            </select>
          </div>
          <div className="md:col-span-3">
            <label className="block text-xs text-slate-500 mb-1">
              Вид анализа / диагностики
            </label>
            <input
              type="text"
              value={testType}
              onChange={(e) => setTestType(e.target.value)}
              className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
              required
            />
          </div>
          <div className="md:col-span-2">
            <label className="block text-xs text-slate-500 mb-1">
              Биоматериал
            </label>
            <input
              type="text"
              value={sampleType}
              onChange={(e) => setSampleType(e.target.value)}
              className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
            />
          </div>
          <div className="md:col-span-2">
            <label className="block text-xs text-slate-500 mb-1">Врач</label>
            <select
              value={veterinarianId}
              onChange={(e) => setVeterinarianId(e.target.value)}
              className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
            >
              {state.veterinarians.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.fullName}
                </option>
              ))}
            </select>
          </div>
          <div className="md:col-span-2">
            <button
              type="submit"
              className="w-full h-9 px-3 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
            >
              Создать заказ
            </button>
          </div>
        </form>
      )}

      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                <th className="py-3 px-4">Дата</th>
                <th className="py-3 px-4">Пациент</th>
                <th className="py-3 px-4">Исследование</th>
                <th className="py-3 px-4">Заключение / Результат</th>
                <th className="py-3 px-4">Статус</th>
                <th className="py-3 px-4 text-right">Действие</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 text-sm">
              {filtered.map((lab) => {
                const pet = state.pets.find((p) => p.id === lab.petId);
                const vet = state.veterinarians.find(
                  (v) => v.id === lab.veterinarianId,
                );
                const isEditing = editingId === lab.id;
                return (
                  <tr key={lab.id} className="hover:bg-slate-50 align-top">
                    <td className="py-3.5 px-4 font-mono text-xs text-slate-600 tabular-nums whitespace-nowrap">
                      {lab.orderedDate}
                    </td>
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      <button
                        type="button"
                        onClick={() => pet && onSelectPet(pet.id)}
                        className="font-medium text-slate-900 hover:text-teal-700"
                      >
                        {pet?.name}
                      </button>
                      <div className="text-xs text-slate-500">
                        {vet?.fullName}
                      </div>
                    </td>
                    <td className="py-3.5 px-4">
                      <div className="font-medium text-slate-900">
                        {lab.testType}
                      </div>
                      <div className="text-xs text-slate-500">
                        {lab.sampleType}
                      </div>
                    </td>
                    <td className="py-3.5 px-4 max-w-md">
                      {isEditing ? (
                        <div className="space-y-2">
                          <textarea
                            rows={2}
                            value={draftResult}
                            onChange={(e) => setDraftResult(e.target.value)}
                            className="w-full px-2.5 py-1.5 text-xs border border-slate-300 rounded-lg"
                          />
                          <label className="inline-flex items-center gap-2 text-xs text-slate-700">
                            <input
                              type="checkbox"
                              checked={draftAbnormal}
                              onChange={(e) =>
                                setDraftAbnormal(e.target.checked)
                              }
                            />
                            Выявлены отклонения от референса
                          </label>
                          <div className="flex gap-2">
                            <button
                              type="button"
                              onClick={async () => {
                                await onUpdateLabTest(lab.id, {
                                  status: 'Готово',
                                  resultSummary: draftResult,
                                  isAbnormal: draftAbnormal,
                                });
                                setEditingId(null);
                              }}
                              className="px-2.5 py-1 text-xs font-medium text-white bg-teal-700 rounded-md"
                            >
                              Сохранить результат
                            </button>
                            <button
                              type="button"
                              onClick={() => setEditingId(null)}
                              className="px-2.5 py-1 text-xs text-slate-600 border border-slate-200 rounded-md"
                            >
                              Отмена
                            </button>
                          </div>
                        </div>
                      ) : (
                        <div>
                          <div className="text-xs text-slate-700 leading-relaxed">
                            {lab.resultSummary}
                          </div>
                          {lab.isAbnormal && (
                            <div className="mt-1 inline-flex items-center gap-1 text-xs font-medium text-red-700">
                              <AlertTriangle size={12} />
                              Отклонение от нормы
                            </div>
                          )}
                        </div>
                      )}
                    </td>
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      {lab.status === 'Готово' && (
                        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700">
                          <CheckCircle2 size={13} />
                          Готово
                        </span>
                      )}
                      {lab.status === 'В работе' && (
                        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-teal-700">
                          <FlaskConical size={13} />
                          В работе
                        </span>
                      )}
                      {lab.status === 'Назначен' && (
                        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-amber-700">
                          <Clock size={13} />
                          Назначен
                        </span>
                      )}
                    </td>
                    <td className="py-3.5 px-4 text-right whitespace-nowrap">
                      {lab.status === 'Назначен' && (
                        <button
                          type="button"
                          onClick={() =>
                            onUpdateLabTest(lab.id, {status: 'В работе'})
                          }
                          className="px-2.5 py-1 text-xs font-medium text-teal-700 border border-teal-200 bg-teal-50 rounded-md hover:bg-teal-100 mr-2"
                        >
                          Взять в работу
                        </button>
                      )}
                      {lab.status !== 'Готово' && !isEditing && (
                        <button
                          type="button"
                          onClick={() => {
                            setEditingId(lab.id);
                            setDraftResult(lab.resultSummary);
                            setDraftAbnormal(lab.isAbnormal);
                          }}
                          className="px-2.5 py-1 text-xs font-medium text-slate-700 border border-slate-200 rounded-md hover:bg-slate-100"
                        >
                          Внести результат
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
    </div>
  );
}
