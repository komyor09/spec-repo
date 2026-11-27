import express from 'express';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { createServer as createViteServer } from 'vite';
import {
  findWebinarConflict,
  getBootstrapPayload,
  isValidHttpUrl,
  mutateDatabase,
  parseTimeToMinutes,
  readDatabase,
  recalculateEnrollmentProgress,
  resetDatabaseToSeed,
} from './server/db.ts';
import type {
  Course,
  CourseCategory,
  CourseLevel,
  PaymentStatus,
  SubmissionStatus,
} from './src/types/platform.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const PORT = Number(process.env.PORT) || 3000;

const VALID_CATEGORIES: CourseCategory[] = [
  'IT и разработка',
  'Дизайн',
  'Маркетинг',
  'Аналитика',
  'Менеджмент',
];

const VALID_LEVELS: CourseLevel[] = ['Начальный', 'Средний', 'Продвинутый'];

const DEFAULT_COVER_BY_CATEGORY: Record<CourseCategory, string> = {
  'IT и разработка': '/src/assets/images/course_fullstack_dev_1795752284557.jpg',
  'Дизайн': '/src/assets/images/course_ux_design_1795752285074.jpg',
  'Маркетинг': '/src/assets/images/course_marketing_growth_1795752285105.jpg',
  'Аналитика': '/src/assets/images/course_data_analytics_1795752285308.jpg',
  'Менеджмент': '/src/assets/images/course_product_mgmt_1795752286103.jpg',
};

function makeId(prefix: string): string {
  return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`;
}

async function startServer() {
  const app = express();
  app.use(express.json({ limit: '1mb' }));

  // Ensure initial DB exists on startup
  readDatabase();

  // Healthcheck
  app.get('/api/health', (_req, res) => {
    res.json({ ok: true, status: 'healthy', timestamp: new Date().toISOString() });
  });

  // Bootstrap all platform state
  app.get('/api/bootstrap', (_req, res) => {
    const db = readDatabase();
    res.json(getBootstrapPayload(db));
  });

  // Reset database to initial Russian demo seed
  app.post('/api/reset', async (_req, res) => {
    try {
      const db = await resetDatabaseToSeed();
      res.json(getBootstrapPayload(db));
    } catch {
      res.status(500).json({ error: 'Не удалось сбросить демонстрационные данные.' });
    }
  });

  // Enroll student in course (immediate or via payment application)
  app.post('/api/enrollments', async (req, res) => {
    const { studentId, courseId, payNow = true, paymentMethod = 'Банковская карта' } = req.body ?? {};
    if (typeof studentId !== 'string' || typeof courseId !== 'string') {
      res.status(400).json({ error: 'Необходимо указать студента и курс.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const student = db.users.find((u) => u.id === studentId && u.role === 'student');
        const course = db.courses.find((c) => c.id === courseId);
        if (!student || !course) {
          return { status: 404 as const, body: { error: 'Студент или курс не найдены.' } };
        }

        const existing = db.enrollments.find(
          (e) => e.studentId === studentId && e.courseId === courseId,
        );
        if (existing) {
          return { status: 409 as const, body: { error: 'Студент уже записан на этот курс.' } };
        }

        const today = new Date().toISOString().slice(0, 10);

        if (course.priceRub > 0 && payNow === false) {
          const existingPending = db.payments.find(
            (p) => p.studentId === studentId && p.courseId === courseId && p.status === 'pending',
          );
          if (existingPending) {
            return {
              status: 409 as const,
              body: { error: 'Заявка на этот курс уже ожидает подтверждения оплаты.' },
            };
          }
          const payment = {
            id: makeId('pay'),
            studentId,
            courseId,
            amountRub: course.priceRub,
            status: 'pending' as PaymentStatus,
            paymentMethod: String(paymentMethod).slice(0, 80) || 'Заявка на обучение',
            createdAt: today,
            updatedAt: today,
          };
          db.payments.unshift(payment);
          return {
            status: 201 as const,
            body: {
              pendingPayment: true,
              payment,
              data: getBootstrapPayload(db),
            },
          };
        }

        if (course.priceRub > 0) {
          db.payments.unshift({
            id: makeId('pay'),
            studentId,
            courseId,
            amountRub: course.priceRub,
            status: 'paid',
            paymentMethod: String(paymentMethod).slice(0, 80) || 'Онлайн-оплата',
            createdAt: today,
            updatedAt: today,
          });
        }

        const newEnrollment = {
          id: makeId('enr'),
          studentId,
          courseId,
          enrolledAt: today,
          completedLessonIds: [],
          quizAttempts: {},
          progressPercent: 0,
          totalPoints: 0,
        };
        recalculateEnrollmentProgress(newEnrollment, course);
        db.enrollments.push(newEnrollment);
        course.studentsCount += 1;

        return {
          status: 201 as const,
          body: {
            pendingPayment: false,
            enrollment: newEnrollment,
            data: getBootstrapPayload(db),
          },
        };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Ошибка при записи на курс.' });
    }
  });

  // Toggle lesson completion & recalculate progress
  app.post('/api/enrollments/:id/lessons/:lessonId/toggle', async (req, res) => {
    const { id, lessonId } = req.params;
    try {
      const result = await mutateDatabase((db) => {
        const enrollment = db.enrollments.find((e) => e.id === id);
        if (!enrollment) {
          return { status: 404 as const, body: { error: 'Запись на курс не найдена.' } };
        }
        const course = db.courses.find((c) => c.id === enrollment.courseId);
        if (!course) {
          return { status: 404 as const, body: { error: 'Курс не найден.' } };
        }
        const lessonExists = course.modules.some((m) => m.lessons.some((l) => l.id === lessonId));
        if (!lessonExists) {
          return { status: 404 as const, body: { error: 'Урок не найден в курсе.' } };
        }

        const idx = enrollment.completedLessonIds.indexOf(lessonId);
        if (idx >= 0) {
          enrollment.completedLessonIds.splice(idx, 1);
        } else {
          enrollment.completedLessonIds.push(lessonId);
        }

        recalculateEnrollmentProgress(enrollment, course);
        return {
          status: 200 as const,
          body: { enrollment, data: getBootstrapPayload(db) },
        };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось обновить статус урока.' });
    }
  });

  // Submit quiz answers, evaluate on server, award points & recalculate progress
  app.post('/api/enrollments/:id/quizzes/:quizId/submit', async (req, res) => {
    const { id, quizId } = req.params;
    const { answers } = req.body ?? {};
    if (!Array.isArray(answers)) {
      res.status(400).json({ error: 'Ответы на тест должны быть переданы массивом.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const enrollment = db.enrollments.find((e) => e.id === id);
        if (!enrollment) {
          return { status: 404 as const, body: { error: 'Запись на курс не найдена.' } };
        }
        const course = db.courses.find((c) => c.id === enrollment.courseId);
        if (!course) {
          return { status: 404 as const, body: { error: 'Курс не найден.' } };
        }
        const quiz = course.quizzes.find((q) => q.id === quizId);
        if (!quiz || quiz.questions.length === 0) {
          return { status: 404 as const, body: { error: 'Тест не найден.' } };
        }

        const normalizedAnswers = quiz.questions.map((_, i) =>
          typeof answers[i] === 'number' ? answers[i] : -1,
        );
        const correctCount = quiz.questions.reduce(
          (acc, q, i) => (normalizedAnswers[i] === q.correctIndex ? acc + 1 : acc),
          0,
        );
        const scorePercent = Math.round((correctCount / quiz.questions.length) * 100);
        const passed = scorePercent >= quiz.passingScore;
        const previousAttempt = enrollment.quizAttempts[quiz.id];
        const earnedPoints = passed
          ? quiz.pointsReward
          : previousAttempt?.passed
            ? quiz.pointsReward
            : 0;

        const attempt = {
          quizId: quiz.id,
          scorePercent,
          passed: passed || Boolean(previousAttempt?.passed),
          earnedPoints,
          selectedAnswers: normalizedAnswers,
          completedAt: new Date().toISOString(),
        };

        enrollment.quizAttempts[quiz.id] = attempt;
        recalculateEnrollmentProgress(enrollment, course);

        return {
          status: 200 as const,
          body: { attempt, enrollment, data: getBootstrapPayload(db) },
        };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Ошибка при проверке теста.' });
    }
  });

  // Student submits homework
  app.post('/api/submissions', async (req, res) => {
    const { courseId, studentId, answerText, projectUrl } = req.body ?? {};
    if (typeof courseId !== 'string' || typeof studentId !== 'string') {
      res.status(400).json({ error: 'Укажите курс и студента.' });
      return;
    }
    const cleanAnswer = typeof answerText === 'string' ? answerText.trim() : '';
    if (cleanAnswer.length < 10) {
      res.status(400).json({ error: 'Ответ на задание должен содержать минимум 10 символов.' });
      return;
    }
    const cleanUrl = typeof projectUrl === 'string' ? projectUrl.trim() : '';
    if (cleanUrl && !isValidHttpUrl(cleanUrl)) {
      res.status(400).json({ error: 'Ссылка должна начинаться с http:// или https://.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const course = db.courses.find((c) => c.id === courseId);
        const student = db.users.find((u) => u.id === studentId && u.role === 'student');
        if (!course || !student) {
          return { status: 404 as const, body: { error: 'Курс или студент не найдены.' } };
        }

        const submission = {
          id: makeId('sub'),
          courseId,
          studentId,
          answerText: cleanAnswer.slice(0, 4000),
          ...(cleanUrl ? { projectUrl: cleanUrl.slice(0, 500) } : {}),
          status: 'pending' as SubmissionStatus,
          submittedAt: new Date().toISOString(),
        };

        db.submissions.unshift(submission);
        return {
          status: 201 as const,
          body: { submission, data: getBootstrapPayload(db) },
        };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось отправить домашнее задание.' });
    }
  });

  // Instructor reviews homework
  app.patch('/api/submissions/:id/review', async (req, res) => {
    const { id } = req.params;
    const { status, score, instructorComment, reviewerId } = req.body ?? {};
    const validStatuses: SubmissionStatus[] = ['pending', 'approved', 'revision'];
    if (!validStatuses.includes(status)) {
      res.status(400).json({ error: 'Некорректный статус проверки.' });
      return;
    }
    const numericScore = score === undefined || score === null ? undefined : Number(score);
    if (numericScore !== undefined && (Number.isNaN(numericScore) || numericScore < 0 || numericScore > 100)) {
      res.status(400).json({ error: 'Оценка должна быть числом от 0 до 100.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const submission = db.submissions.find((s) => s.id === id);
        if (!submission) {
          return { status: 404 as const, body: { error: 'Работа студента не найдена.' } };
        }
        submission.status = status;
        if (numericScore !== undefined) submission.score = Math.round(numericScore);
        if (typeof instructorComment === 'string') {
          submission.instructorComment = instructorComment.trim().slice(0, 2000);
        }
        submission.reviewedAt = new Date().toISOString();
        if (typeof reviewerId === 'string') {
          submission.reviewerId = reviewerId;
        }
        return {
          status: 200 as const,
          body: { submission, data: getBootstrapPayload(db) },
        };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось сохранить проверку задания.' });
    }
  });

  // Instructor creates a new course
  app.post('/api/courses', async (req, res) => {
    const {
      title,
      subtitle,
      category,
      level,
      durationHours,
      priceRub,
      instructorId,
      coverImage,
      firstModuleTitle,
      firstLessonTitle,
      firstLessonSummary,
      firstLessonContent,
      homeworkTitle,
      homeworkDescription,
    } = req.body ?? {};

    const cleanTitle = typeof title === 'string' ? title.trim() : '';
    const cleanSubtitle = typeof subtitle === 'string' ? subtitle.trim() : '';
    if (cleanTitle.length < 4 || cleanSubtitle.length < 8) {
      res.status(400).json({ error: 'Укажите название (от 4 символов) и описание курса.' });
      return;
    }
    if (!VALID_CATEGORIES.includes(category)) {
      res.status(400).json({ error: 'Выберите корректную категорию курса.' });
      return;
    }
    if (!VALID_LEVELS.includes(level)) {
      res.status(400).json({ error: 'Выберите корректный уровень сложности.' });
      return;
    }

    const hours = Number(durationHours);
    const price = Number(priceRub ?? 0);
    if (Number.isNaN(hours) || hours < 1 || hours > 500) {
      res.status(400).json({ error: 'Длительность курса должна быть от 1 до 500 часов.' });
      return;
    }
    if (Number.isNaN(price) || price < 0 || price > 1000000) {
      res.status(400).json({ error: 'Укажите корректную стоимость курса.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const instructor =
          db.users.find((u) => u.id === instructorId && u.role === 'instructor') ??
          db.users.find((u) => u.role === 'instructor');
        if (!instructor) {
          return { status: 400 as const, body: { error: 'Преподаватель не найден.' } };
        }

        const modId = makeId('mod');
        const lesId = makeId('les');
        const safeCover =
          typeof coverImage === 'string' && coverImage.startsWith('/src/assets/images/')
            ? coverImage
            : DEFAULT_COVER_BY_CATEGORY[category as CourseCategory];

        const newCourse: Course = {
          id: makeId('crs'),
          title: cleanTitle.slice(0, 140),
          subtitle: cleanSubtitle.slice(0, 320),
          category,
          level,
          durationHours: Math.round(hours),
          rating: 5.0,
          studentsCount: 0,
          priceRub: Math.round(price),
          coverImage: safeCover,
          instructorId: instructor.id,
          createdAt: new Date().toISOString().slice(0, 10),
          modules: [
            {
              id: modId,
              title:
                typeof firstModuleTitle === 'string' && firstModuleTitle.trim()
                  ? firstModuleTitle.trim().slice(0, 120)
                  : 'Модуль 1. Введение и базовые принципы',
              description: 'Стартовый модуль программы с практическими материалами.',
              lessons: [
                {
                  id: lesId,
                  title:
                    typeof firstLessonTitle === 'string' && firstLessonTitle.trim()
                      ? firstLessonTitle.trim().slice(0, 140)
                      : 'Урок 1. Ключевые понятия и постановка задач',
                  durationMinutes: 25,
                  summary:
                    typeof firstLessonSummary === 'string' && firstLessonSummary.trim()
                      ? firstLessonSummary.trim().slice(0, 220)
                      : 'Знакомство со структурой курса и инструментами работы.',
                  content: [
                    typeof firstLessonContent === 'string' && firstLessonContent.trim()
                      ? firstLessonContent.trim()
                      : 'Добро пожаловать на курс! В этом вводном занятии вы познакомитесь с программой обучения, критериями оценки и финальным проектом.',
                  ],
                  keyTakeaway: 'Сформулируйте личную цель на курс и изучите структуру первого модуля.',
                },
              ],
            },
          ],
          quizzes: [],
          homeworkTask: {
            title:
              typeof homeworkTitle === 'string' && homeworkTitle.trim()
                ? homeworkTitle.trim().slice(0, 120)
                : 'Итоговая практическая работа',
            description:
              typeof homeworkDescription === 'string' && homeworkDescription.trim()
                ? homeworkDescription.trim().slice(0, 600)
                : 'Подготовьте практический кейс по материалам первого модуля и приложите пояснения к решению.',
            deliverableHint: 'Отправьте текстовый отчёт или ссылку на выполненное задание.',
          },
        };

        db.courses.unshift(newCourse);
        return {
          status: 201 as const,
          body: { course: newCourse, data: getBootstrapPayload(db) },
        };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось создать курс.' });
    }
  });

  // Instructor adds a module to course
  app.post('/api/courses/:id/modules', async (req, res) => {
    const { id } = req.params;
    const { title, description } = req.body ?? {};
    const cleanTitle = typeof title === 'string' ? title.trim() : '';
    if (cleanTitle.length < 3) {
      res.status(400).json({ error: 'Название модуля должно быть не короче 3 символов.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const course = db.courses.find((c) => c.id === id);
        if (!course) {
          return { status: 404 as const, body: { error: 'Курс не найден.' } };
        }
        const mod = {
          id: makeId('mod'),
          title: cleanTitle.slice(0, 140),
          description:
            typeof description === 'string' && description.trim()
              ? description.trim().slice(0, 280)
              : 'Практический блок программы.',
          lessons: [],
        };
        course.modules.push(mod);
        return { status: 201 as const, body: { module: mod, data: getBootstrapPayload(db) } };
      });
      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Ошибка добавления модуля.' });
    }
  });

  // Instructor adds a lesson to a module
  app.post('/api/courses/:id/modules/:moduleId/lessons', async (req, res) => {
    const { id, moduleId } = req.params;
    const { title, durationMinutes, summary, content, keyTakeaway, codeExample } = req.body ?? {};
    const cleanTitle = typeof title === 'string' ? title.trim() : '';
    if (cleanTitle.length < 3) {
      res.status(400).json({ error: 'Укажите название урока (от 3 символов).' });
      return;
    }

    const duration = Number(durationMinutes ?? 25);
    if (Number.isNaN(duration) || duration < 1 || duration > 300) {
      res.status(400).json({ error: 'Длительность урока должна быть от 1 до 300 минут.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const course = db.courses.find((c) => c.id === id);
        if (!course) {
          return { status: 404 as const, body: { error: 'Курс не найден.' } };
        }
        const mod = course.modules.find((m) => m.id === moduleId);
        if (!mod) {
          return { status: 404 as const, body: { error: 'Модуль не найден.' } };
        }

        const paragraphs =
          typeof content === 'string' && content.trim()
            ? content
                .split('\n')
                .map((p) => p.trim())
                .filter(Boolean)
            : ['Материал урока подготовлен преподавателем.'];

        const lesson = {
          id: makeId('les'),
          title: cleanTitle.slice(0, 150),
          durationMinutes: Math.round(duration),
          summary:
            typeof summary === 'string' && summary.trim()
              ? summary.trim().slice(0, 240)
              : 'Ключевой разбор темы модуля.',
          content: paragraphs,
          ...(typeof codeExample === 'string' && codeExample.trim()
            ? { codeExample: codeExample.trim().slice(0, 2000) }
            : {}),
          keyTakeaway:
            typeof keyTakeaway === 'string' && keyTakeaway.trim()
              ? keyTakeaway.trim().slice(0, 240)
              : 'Закрепите материал практическим заданием.',
        };

        mod.lessons.push(lesson);

        // Recalculate progress for all students enrolled in this course
        for (const enr of db.enrollments) {
          if (enr.courseId === course.id) {
            recalculateEnrollmentProgress(enr, course);
          }
        }

        return { status: 201 as const, body: { lesson, data: getBootstrapPayload(db) } };
      });
      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось добавить урок.' });
    }
  });

  // Instructor edits an existing lesson
  app.put('/api/courses/:id/lessons/:lessonId', async (req, res) => {
    const { id, lessonId } = req.params;
    const { title, durationMinutes, summary, content, keyTakeaway } = req.body ?? {};
    const cleanTitle = typeof title === 'string' ? title.trim() : '';
    if (cleanTitle.length < 3) {
      res.status(400).json({ error: 'Название урока должно быть не короче 3 символов.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const course = db.courses.find((c) => c.id === id);
        if (!course) {
          return { status: 404 as const, body: { error: 'Курс не найден.' } };
        }
        const lesson = course.modules.flatMap((m) => m.lessons).find((l) => l.id === lessonId);
        if (!lesson) {
          return { status: 404 as const, body: { error: 'Урок не найден.' } };
        }

        lesson.title = cleanTitle.slice(0, 150);
        if (durationMinutes !== undefined) {
          const d = Number(durationMinutes);
          if (!Number.isNaN(d) && d >= 1 && d <= 300) {
            lesson.durationMinutes = Math.round(d);
          }
        }
        if (typeof summary === 'string' && summary.trim()) {
          lesson.summary = summary.trim().slice(0, 240);
        }
        if (typeof content === 'string' && content.trim()) {
          lesson.content = content
            .split('\n')
            .map((p) => p.trim())
            .filter(Boolean);
        }
        if (typeof keyTakeaway === 'string' && keyTakeaway.trim()) {
          lesson.keyTakeaway = keyTakeaway.trim().slice(0, 240);
        }

        return { status: 200 as const, body: { lesson, data: getBootstrapPayload(db) } };
      });
      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось обновить урок.' });
    }
  });

  // Instructor adds a quiz or question to course
  app.post('/api/courses/:id/quizzes', async (req, res) => {
    const { id } = req.params;
    const { title, moduleId, passingScore = 70, pointsReward = 40, questions } = req.body ?? {};
    const cleanTitle = typeof title === 'string' ? title.trim() : '';
    if (cleanTitle.length < 3) {
      res.status(400).json({ error: 'Укажите название теста.' });
      return;
    }
    if (!Array.isArray(questions) || questions.length === 0) {
      res.status(400).json({ error: 'Добавьте хотя бы один вопрос в тест.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const course = db.courses.find((c) => c.id === id);
        if (!course) {
          return { status: 404 as const, body: { error: 'Курс не найден.' } };
        }
        const targetModuleId =
          typeof moduleId === 'string' && course.modules.some((m) => m.id === moduleId)
            ? moduleId
            : course.modules[0]?.id ?? '';

        const parsedQuestions = questions.map((q: Record<string, unknown>, index: number) => {
          const opts = Array.isArray(q.options)
            ? q.options.map((o) => String(o).trim()).filter(Boolean)
            : [];
          const correctIdx = Number(q.correctIndex ?? 0);
          return {
            id: makeId(`q${index + 1}`),
            prompt: String(q.prompt ?? '').trim() || `Вопрос ${index + 1}`,
            options: opts.length >= 2 ? opts : ['Вариант А', 'Вариант Б'],
            correctIndex:
              correctIdx >= 0 && correctIdx < (opts.length || 2) ? correctIdx : 0,
            explanation:
              String(q.explanation ?? '').trim() || 'Верный ответ подтверждён материалами курса.',
          };
        });

        const newQuiz = {
          id: makeId('quiz'),
          title: cleanTitle.slice(0, 140),
          moduleId: targetModuleId,
          passingScore: Math.min(100, Math.max(10, Number(passingScore) || 70)),
          pointsReward: Math.min(500, Math.max(5, Number(pointsReward) || 40)),
          questions: parsedQuestions,
        };

        course.quizzes.push(newQuiz);
        for (const enr of db.enrollments) {
          if (enr.courseId === course.id) {
            recalculateEnrollmentProgress(enr, course);
          }
        }

        return { status: 201 as const, body: { quiz: newQuiz, data: getBootstrapPayload(db) } };
      });
      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось создать тест.' });
    }
  });

  // Schedule a webinar with instructor time-conflict validation
  app.post('/api/webinars', async (req, res) => {
    const {
      title,
      courseId,
      instructorId,
      date,
      startTime,
      durationMinutes = 60,
      meetingUrl,
      description,
    } = req.body ?? {};

    const cleanTitle = typeof title === 'string' ? title.trim() : '';
    if (cleanTitle.length < 3) {
      res.status(400).json({ error: 'Укажите название вебинара (от 3 символов).' });
      return;
    }
    if (typeof date !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(date)) {
      res.status(400).json({ error: 'Укажите дату в формате ГГГГ-ММ-ДД.' });
      return;
    }
    if (typeof startTime !== 'string' || parseTimeToMinutes(startTime) === null) {
      res.status(400).json({ error: 'Укажите время начала в формате ЧЧ:ММ.' });
      return;
    }
    const duration = Number(durationMinutes);
    if (Number.isNaN(duration) || duration < 15 || duration > 360) {
      res.status(400).json({ error: 'Длительность занятия должна быть от 15 до 360 минут.' });
      return;
    }
    const cleanMeetingUrl =
      typeof meetingUrl === 'string' && meetingUrl.trim()
        ? meetingUrl.trim()
        : 'https://meet.vector-academy.ru/live-room';
    if (!isValidHttpUrl(cleanMeetingUrl)) {
      res.status(400).json({ error: 'Ссылка на трансляцию должна начинаться с http:// или https://.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const instructor = db.users.find(
          (u) => u.id === instructorId && u.role === 'instructor',
        );
        const course = db.courses.find((c) => c.id === courseId);
        if (!instructor || !course) {
          return { status: 404 as const, body: { error: 'Преподаватель или курс не найдены.' } };
        }

        const conflict = findWebinarConflict(db.webinars, {
          instructorId,
          date,
          startTime,
          durationMinutes: duration,
        });

        if (conflict) {
          const conflictStart = parseTimeToMinutes(conflict.startTime) ?? 0;
          const conflictEndMin = conflictStart + conflict.durationMinutes;
          const endHours = String(Math.floor(conflictEndMin / 60)).padStart(2, '0');
          const endMins = String(conflictEndMin % 60).padStart(2, '0');
          return {
            status: 409 as const,
            body: {
              error: `Конфликт расписания: преподаватель ${instructor.name} уже ведёт занятие «${conflict.title}» (${conflict.date}, ${conflict.startTime}–${endHours}:${endMins}).`,
              conflictingWebinar: conflict,
            },
          };
        }

        const webinar = {
          id: makeId('web'),
          title: cleanTitle.slice(0, 140),
          courseId,
          instructorId,
          date,
          startTime,
          durationMinutes: Math.round(duration),
          meetingUrl: cleanMeetingUrl.slice(0, 500),
          description:
            typeof description === 'string' && description.trim()
              ? description.trim().slice(0, 320)
              : 'Онлайн-консультация с ответами на вопросы студентов.',
        };

        db.webinars.push(webinar);
        db.webinars.sort((a, b) => `${a.date} ${a.startTime}`.localeCompare(`${b.date} ${b.startTime}`));

        return { status: 201 as const, body: { webinar, data: getBootstrapPayload(db) } };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось запланировать вебинар.' });
    }
  });

  // Delete webinar
  app.delete('/api/webinars/:id', async (req, res) => {
    const { id } = req.params;
    try {
      const result = await mutateDatabase((db) => {
        const idx = db.webinars.findIndex((w) => w.id === id);
        if (idx < 0) {
          return { status: 404 as const, body: { error: 'Вебинар не найден.' } };
        }
        db.webinars.splice(idx, 1);
        return { status: 200 as const, body: { data: getBootstrapPayload(db) } };
      });
      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось удалить вебинар.' });
    }
  });

  // Create study group
  app.post('/api/groups', async (req, res) => {
    const { name, curatorId, scheduleNote } = req.body ?? {};
    const cleanName = typeof name === 'string' ? name.trim() : '';
    if (cleanName.length < 3) {
      res.status(400).json({ error: 'Название группы должно быть не короче 3 символов.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const curator =
          db.users.find((u) => u.id === curatorId && u.role === 'instructor') ??
          db.users.find((u) => u.role === 'instructor');
        if (!curator) {
          return { status: 400 as const, body: { error: 'Куратор не найден.' } };
        }
        const group = {
          id: makeId('grp'),
          name: cleanName.slice(0, 100),
          curatorId: curator.id,
          startDate: new Date().toISOString().slice(0, 10),
          scheduleNote:
            typeof scheduleNote === 'string' && scheduleNote.trim()
              ? scheduleNote.trim().slice(0, 80)
              : 'Вт / Чт · 19:00 МСК',
        };
        db.groups.push(group);
        return { status: 201 as const, body: { group, data: getBootstrapPayload(db) } };
      });
      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось создать группу.' });
    }
  });

  // Assign student to group
  app.patch('/api/users/:id/group', async (req, res) => {
    const { id } = req.params;
    const { groupId } = req.body ?? {};
    try {
      const result = await mutateDatabase((db) => {
        const student = db.users.find((u) => u.id === id && u.role === 'student');
        if (!student) {
          return { status: 404 as const, body: { error: 'Студент не найден.' } };
        }
        if (groupId && !db.groups.some((g) => g.id === groupId)) {
          return { status: 404 as const, body: { error: 'Группа не найдена.' } };
        }
        student.groupId = groupId || undefined;
        return { status: 200 as const, body: { data: getBootstrapPayload(db) } };
      });
      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось обновить группу студента.' });
    }
  });

  // Update payment status (automatically activates enrollment when marked 'paid')
  app.patch('/api/payments/:id', async (req, res) => {
    const { id } = req.params;
    const { status } = req.body ?? {};
    const validStatuses: PaymentStatus[] = ['paid', 'pending', 'cancelled'];
    if (!validStatuses.includes(status)) {
      res.status(400).json({ error: 'Некорректный статус оплаты.' });
      return;
    }

    try {
      const result = await mutateDatabase((db) => {
        const payment = db.payments.find((p) => p.id === id);
        if (!payment) {
          return { status: 404 as const, body: { error: 'Заявка на оплату не найдена.' } };
        }
        payment.status = status;
        payment.updatedAt = new Date().toISOString().slice(0, 10);

        if (status === 'paid') {
          const alreadyEnrolled = db.enrollments.some(
            (e) => e.studentId === payment.studentId && e.courseId === payment.courseId,
          );
          if (!alreadyEnrolled) {
            const course = db.courses.find((c) => c.id === payment.courseId);
            const newEnrollment = {
              id: makeId('enr'),
              studentId: payment.studentId,
              courseId: payment.courseId,
              enrolledAt: payment.updatedAt,
              completedLessonIds: [],
              quizAttempts: {},
              progressPercent: 0,
              totalPoints: 0,
            };
            if (course) {
              recalculateEnrollmentProgress(newEnrollment, course);
              course.studentsCount += 1;
            }
            db.enrollments.push(newEnrollment);
          }
        }

        return { status: 200 as const, body: { payment, data: getBootstrapPayload(db) } };
      });

      res.status(result.status).json(result.body);
    } catch {
      res.status(500).json({ error: 'Не удалось обновить статус оплаты.' });
    }
  });

  // Vite dev middleware vs production static assets
  if (process.env.NODE_ENV !== 'production') {
    const vite = await createViteServer({
      server: { middlewareMode: true },
      appType: 'spa',
    });
    app.use(vite.middlewares);
  } else {
    const distDir = path.join(__dirname, 'dist');
    app.use(express.static(distDir));
    app.get('*', (_req, res) => {
      const indexPath = path.join(distDir, 'index.html');
      if (fs.existsSync(indexPath)) {
        res.sendFile(indexPath);
      } else {
        res.status(404).send('Сборка приложения не найдена. Выполните npm run build.');
      }
    });
  }

  app.listen(PORT, '0.0.0.0', () => {
    console.log(`Академия Вектор запущена: http://localhost:${PORT}`);
  });
}

startServer();
