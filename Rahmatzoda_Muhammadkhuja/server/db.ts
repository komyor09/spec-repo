import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import type {
  BootstrapPayload,
  Course,
  DashboardSummary,
  DatabaseSchema,
  Enrollment,
  Webinar,
} from '../src/types/platform.ts';
import { createSeedDatabase } from './seedData.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '..');
const DATA_DIR = path.join(ROOT_DIR, 'data');
const DB_FILE = path.join(DATA_DIR, 'courses-db.json');

let writeQueue: Promise<unknown> = Promise.resolve();

function ensureDataDir(): void {
  if (!fs.existsSync(DATA_DIR)) {
    fs.mkdirSync(DATA_DIR, { recursive: true });
  }
}

export function readDatabase(): DatabaseSchema {
  ensureDataDir();
  if (!fs.existsSync(DB_FILE)) {
    const seed = createSeedDatabase();
    writeDatabaseSync(seed);
    return seed;
  }
  try {
    const raw = fs.readFileSync(DB_FILE, 'utf-8');
    const parsed = JSON.parse(raw) as DatabaseSchema;
    if (!parsed || !Array.isArray(parsed.courses) || !Array.isArray(parsed.users)) {
      const seed = createSeedDatabase();
      writeDatabaseSync(seed);
      return seed;
    }
    return parsed;
  } catch {
    const seed = createSeedDatabase();
    writeDatabaseSync(seed);
    return seed;
  }
}

function writeDatabaseSync(db: DatabaseSchema): void {
  ensureDataDir();
  const tmpFile = `${DB_FILE}.${Date.now()}.tmp`;
  fs.writeFileSync(tmpFile, JSON.stringify(db, null, 2), 'utf-8');
  fs.renameSync(tmpFile, DB_FILE);
}

export function mutateDatabase<T>(mutator: (db: DatabaseSchema) => T | Promise<T>): Promise<T> {
  const next = writeQueue.then(async () => {
    const db = readDatabase();
    const result = await mutator(db);
    writeDatabaseSync(db);
    return result;
  });
  writeQueue = next.catch(() => undefined);
  return next;
}

export function resetDatabaseToSeed(): Promise<DatabaseSchema> {
  const next = writeQueue.then(() => {
    const seed = createSeedDatabase();
    writeDatabaseSync(seed);
    return seed;
  });
  writeQueue = next.catch(() => undefined);
  return next;
}

export function isValidHttpUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

export function recalculateEnrollmentProgress(enrollment: Enrollment, course: Course): Enrollment {
  const totalLessons = course.modules.reduce((acc, mod) => acc + mod.lessons.length, 0);
  const totalQuizzes = course.quizzes.length;
  const totalUnits = totalLessons + totalQuizzes;

  const validLessonIds = new Set(course.modules.flatMap((m) => m.lessons.map((l) => l.id)));
  const completedLessonsCount = enrollment.completedLessonIds.filter((id) => validLessonIds.has(id)).length;

  const validQuizIds = new Set(course.quizzes.map((q) => q.id));
  const passedQuizzesCount = Object.values(enrollment.quizAttempts).filter(
    (attempt) => validQuizIds.has(attempt.quizId) && attempt.passed,
  ).length;

  const totalPoints = Object.values(enrollment.quizAttempts).reduce(
    (sum, attempt) => sum + (validQuizIds.has(attempt.quizId) ? attempt.earnedPoints : 0),
    0,
  );

  const progressPercent =
    totalUnits === 0 ? 100 : Math.min(100, Math.round(((completedLessonsCount + passedQuizzesCount) / totalUnits) * 100));

  enrollment.progressPercent = progressPercent;
  enrollment.totalPoints = totalPoints;

  if (progressPercent === 100 && !enrollment.certificateId) {
    const randomCode = Math.floor(1000 + Math.random() * 9000);
    enrollment.certificateId = `VEC-2026-${randomCode}`;
    enrollment.completedAt = new Date().toISOString().slice(0, 10);
  } else if (progressPercent < 100) {
    delete enrollment.certificateId;
    delete enrollment.completedAt;
  }

  return enrollment;
}

export function parseTimeToMinutes(timeStr: string): number | null {
  const match = /^(\d{2}):(\d{2})$/.exec(timeStr.trim());
  if (!match) return null;
  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return null;
  return hours * 60 + minutes;
}

export function findWebinarConflict(
  webinars: Webinar[],
  candidate: {
    instructorId: string;
    date: string;
    startTime: string;
    durationMinutes: number;
    excludeWebinarId?: string;
  },
): Webinar | null {
  const startA = parseTimeToMinutes(candidate.startTime);
  if (startA === null) return null;
  const endA = startA + candidate.durationMinutes;

  for (const existing of webinars) {
    if (candidate.excludeWebinarId && existing.id === candidate.excludeWebinarId) continue;
    if (existing.instructorId !== candidate.instructorId) continue;
    if (existing.date !== candidate.date) continue;

    const startB = parseTimeToMinutes(existing.startTime);
    if (startB === null) continue;
    const endB = startB + existing.durationMinutes;

    // Two intervals [startA, endA) and [startB, endB) overlap iff startA < endB && startB < endA
    if (startA < endB && startB < endA) {
      return existing;
    }
  }
  return null;
}

export function computeDashboardSummary(db: DatabaseSchema): DashboardSummary {
  const activeStudents = db.users.filter((u) => u.role === 'student').length;
  const totalCourses = db.courses.length;
  const pendingHomework = db.submissions.filter((s) => s.status === 'pending').length;
  const confirmedRevenueRub = db.payments
    .filter((p) => p.status === 'paid')
    .reduce((sum, p) => sum + p.amountRub, 0);

  const avgProgress =
    db.enrollments.length === 0
      ? 0
      : Math.round(db.enrollments.reduce((acc, e) => acc + e.progressPercent, 0) / db.enrollments.length);

  return {
    activeStudents,
    totalCourses,
    pendingHomework,
    confirmedRevenueRub,
    averageCompletionPercent: avgProgress,
    upcomingWebinarsCount: db.webinars.length,
  };
}

export function getBootstrapPayload(db: DatabaseSchema): BootstrapPayload {
  return {
    ...db,
    summary: computeDashboardSummary(db),
  };
}
