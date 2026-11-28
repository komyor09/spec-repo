import {CheckSquare, Plus, Square} from 'lucide-react';
import {useState, type FormEvent} from 'react';
import type {ClinicState} from '../types/clinic.ts';
import {PetAvatar} from './PetAvatar.tsx';
import {PetHealthLabel} from './StatusText.tsx';

export function InpatientView({
  state,
  onToggleTask,
  onAddTask,
  onSelectPet,
}: {
  state: ClinicState;
  onToggleTask: (taskId: string) => void;
  onAddTask: (payload: {
    petId: string;
    time: string;
    procedure: string;
    notes: string;
  }) => Promise<string | null>;
  onSelectPet: (petId: string) => void;
}) {
  const hospitalized = state.pets.filter(
    (p) => p.status === 'Стационар' || p.status === 'Наблюдение',
  );

  const [petId, setPetId] = useState(hospitalized[0]?.id || state.pets[0]?.id || '');
  const [time, setTime] = useState('16:00');
  const [procedure, setProcedure] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);

  const handleAdd = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !procedure.trim()) return;
    setSaving(true);
    await onAddTask({
      petId,
      time,
      procedure: procedure.trim(),
      notes: notes.trim(),
    });
    setProcedure('');
    setNotes('');
    setSaving(false);
  };

  return (
    <div className="space-y-6">
      <section className="bg-white border border-slate-200 rounded-xl p-5">
        <h2 className="text-sm font-semibold text-slate-900 mb-3">
          Назначить процедуру в лист наблюдений
        </h2>
        <form
          onSubmit={handleAdd}
          className="grid grid-cols-1 md:grid-cols-12 gap-3 items-end"
        >
          <div className="md:col-span-3">
            <label className="block text-xs text-slate-500 mb-1">Пациент</label>
            <select
              value={petId}
              onChange={(e) => setPetId(e.target.value)}
              className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs text-slate-900"
            >
              {state.pets.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.cageNumber || p.status})
                </option>
              ))}
            </select>
          </div>
          <div className="md:col-span-2">
            <label className="block text-xs text-slate-500 mb-1">Время</label>
            <input
              type="time"
              value={time}
              onChange={(e) => setTime(e.target.value)}
              className="w-full h-9 px-2.5 rounded-lg border border-slate-200 bg-white text-xs font-mono tabular-nums"
            />
          </div>
          <div className="md:col-span-3">
            <label className="block text-xs text-slate-500 mb-1">
              Процедура / Препарат
            </label>
            <input
              type="text"
              value={procedure}
              onChange={(e) => setProcedure(e.target.value)}
              placeholder="Инфузия 30 мл + термометрия"
              className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
              required
            />
          </div>
          <div className="md:col-span-2">
            <label className="block text-xs text-slate-500 mb-1">
              Комментарий
            </label>
            <input
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="После кормления"
              className="w-full h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs"
            />
          </div>
          <div className="md:col-span-2">
            <button
              type="submit"
              disabled={saving}
              className="w-full h-9 inline-flex items-center justify-center gap-1.5 px-3 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 whitespace-nowrap"
            >
              <Plus size={14} />
              Добавить
            </button>
          </div>
        </form>
      </section>

      {hospitalized.length === 0 ? (
        <div className="bg-white border border-slate-200 rounded-xl p-10 text-center text-sm text-slate-500">
          В стационаре и под наблюдением сейчас нет пациентов.
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {hospitalized.map((pet) => {
            const owner = state.owners.find((o) => o.id === pet.ownerId);
            const tasks = state.inpatientTasks.filter(
              (t) => t.petId === pet.id,
            );
            return (
              <section
                key={pet.id}
                className="bg-white border border-slate-200 rounded-xl overflow-hidden flex flex-col"
              >
                <div className="p-5 border-b border-slate-200 flex items-start justify-between gap-4">
                  <div className="flex items-center gap-4">
                    <PetAvatar
                      src={pet.photoUrl}
                      alt={pet.name}
                      size="md"
                    />
                    <div>
                      <div className="flex items-center gap-2.5">
                        <button
                          type="button"
                          onClick={() => onSelectPet(pet.id)}
                          className="text-base font-semibold text-slate-900 hover:text-teal-700"
                        >
                          {pet.name}
                        </button>
                        <PetHealthLabel status={pet.status} />
                      </div>
                      <div className="text-xs text-slate-500 mt-1">
                        {pet.species} · {pet.breed} ·{' '}
                        <span className="font-mono tabular-nums text-slate-700">
                          {pet.weightKg} кг
                        </span>
                        {pet.cageNumber && (
                          <>
                            {' · '}
                            <span className="font-mono font-medium text-slate-900">
                              {pet.cageNumber}
                            </span>
                          </>
                        )}
                      </div>
                      <div className="text-xs text-slate-500 mt-1">
                        Диета: {pet.diet} · Аллергии: {pet.allergies}
                      </div>
                    </div>
                  </div>
                  <div className="text-right text-xs text-slate-500 shrink-0">
                    <div>{owner?.fullName}</div>
                    <div className="tabular-nums">{owner?.phone}</div>
                  </div>
                </div>

                <div className="divide-y divide-slate-100 flex-1">
                  {tasks.length === 0 ? (
                    <div className="p-5 text-xs text-slate-500">
                      Процедуры на текущую смену не назначены.
                    </div>
                  ) : (
                    tasks.map((task) => (
                      <button
                        key={task.id}
                        type="button"
                        onClick={() => onToggleTask(task.id)}
                        className="w-full px-5 py-3 flex items-start gap-3 text-left hover:bg-slate-50 transition-colors"
                      >
                        <span className="mt-0.5 text-teal-700 shrink-0">
                          {task.completed ? (
                            <CheckSquare size={16} />
                          ) : (
                            <Square size={16} className="text-slate-400" />
                          )}
                        </span>
                        <div className="flex-1 min-w-0">
                          <div
                            className={`text-sm font-medium ${
                              task.completed
                                ? 'line-through text-slate-400'
                                : 'text-slate-900'
                            }`}
                          >
                            <span className="font-mono text-xs text-slate-500 mr-2 tabular-nums">
                              {task.time}
                            </span>
                            {task.procedure}
                          </div>
                          {task.notes && (
                            <div className="text-xs text-slate-500 mt-0.5">
                              {task.notes}
                            </div>
                          )}
                        </div>
                      </button>
                    ))
                  )}
                </div>
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}
