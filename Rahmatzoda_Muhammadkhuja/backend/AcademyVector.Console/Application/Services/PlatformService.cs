using AcademyVector.ConsoleApp.Application.Interfaces;
using AcademyVector.ConsoleApp.Domain.Entities;
using AcademyVector.ConsoleApp.Domain.Exceptions;

namespace AcademyVector.ConsoleApp.Application.Services;

public sealed class PlatformService : IPlatformService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
    {
        "IT и разработка", "Дизайн", "Маркетинг", "Аналитика", "Менеджмент"
    };

    private static readonly HashSet<string> AllowedLevels = new(StringComparer.Ordinal)
    {
        "Начальный", "Средний", "Продвинутый"
    };

    private static readonly Dictionary<string, string> DefaultCovers = new(StringComparer.Ordinal)
    {
        ["IT и разработка"] = "https://cdn.vector-academy.ru/images/course-csharp-oop.jpg",
        ["Дизайн"] = "https://cdn.vector-academy.ru/images/course-uiux-design.jpg",
        ["Маркетинг"] = "https://cdn.vector-academy.ru/images/course-digital-marketing.jpg",
        ["Аналитика"] = "https://cdn.vector-academy.ru/images/course-data-analytics.jpg",
        ["Менеджмент"] = "https://cdn.vector-academy.ru/images/course-product-management.jpg"
    };

    private readonly ICourseDbRepository _repository;
    private readonly IAssessmentService _assessmentService;
    private readonly IWebinarScheduleValidator _webinarValidator;

    public PlatformService(
        ICourseDbRepository repository,
        IAssessmentService assessmentService,
        IWebinarScheduleValidator webinarValidator)
    {
        _repository = repository;
        _assessmentService = assessmentService;
        _webinarValidator = webinarValidator;
    }

    private static string CreateId(string prefix) =>
        $"{prefix}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N")[..5]}";

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed) &&
        (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

    public async Task<PlatformDatabase> GetBootstrapAsync(string role, CancellationToken ct = default)
    {
        var db = await _repository.ReadAsync(ct);
        if (string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase))
        {
            return db;
        }

        // Скрываем правильные индексы ответов на тесты при выдаче данных студенту и гостю
        foreach (var course in db.Courses)
        {
            foreach (var mod in course.Modules)
            {
                foreach (var quiz in mod.Quizzes)
                {
                    foreach (var question in quiz.Questions)
                    {
                        question.CorrectIndex = -1;
                    }
                }
            }
        }

        db.Inquiries = new();

        if (string.Equals(role, "guest", StringComparison.OrdinalIgnoreCase))
        {
            db.Payments = new();
            db.Homeworks = new();
            db.Students = db.Students
                .Select(s => new Student(s.Id, s.FullName, "guest-masked@vector.ru", s.GroupId, s.AvatarInitials, s.TotalPoints, s.CreatedAt))
                .ToList();
        }

        return db;
    }

    public async Task<OopSummaryResponse> GetOopSummaryAsync(CancellationToken ct = default)
    {
        var db = await _repository.ReadAsync(ct);

        // Полиморфная обработка коллекций базовых абстрактных типов
        var users = new List<UserBase>();
        users.AddRange(db.Students);
        users.AddRange(db.Instructors);

        var activities = new List<LearningActivityBase>();
        foreach (var course in db.Courses)
        {
            foreach (var mod in course.Modules)
            {
                activities.AddRange(mod.Lessons);
                activities.AddRange(mod.Quizzes);
                activities.AddRange(mod.Assignments);
            }
        }
        activities.AddRange(db.Webinars);

        var items = new List<OopSummaryItem>();

        foreach (var user in users)
        {
            items.Add(new OopSummaryItem(
                user.Id,
                "Пользователь",
                user.GetType().Name,
                nameof(UserBase),
                user.FullName,
                user.Describe(),
                user.GetMetricLabel(),
                user.GetMetricValue()));
        }

        foreach (var activity in activities)
        {
            items.Add(new OopSummaryItem(
                activity.Id,
                $"Учебная активность ({activity.GetActivityKind()})",
                activity.GetType().Name,
                nameof(LearningActivityBase),
                activity.Title,
                activity.Describe(),
                activity.GetMetricLabel(),
                activity.GetMetricValue()));
        }

        return new OopSummaryResponse(
            items.Count,
            new List<string>
            {
                "DomainEntity -> UserBase -> Student | Instructor",
                "DomainEntity -> LearningActivityBase -> LessonActivity | QuizActivity | AssignmentActivity | WebinarActivity",
                "DomainException -> ValidationDomainException (400) | NotFoundDomainException (404) | ConflictDomainException (409)"
            },
            items);
    }

    public Task<PlatformDatabase> ResetDatabaseAsync(CancellationToken ct = default) =>
        _repository.ResetAsync(ct);

    public Task<EnrollResult> EnrollCourseAsync(string courseId, EnrollRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.StudentId))
        {
            throw new ValidationDomainException("Не указан идентификатор студента.");
        }

        return _repository.UpdateAsync(db =>
        {
            var student = db.Students.FirstOrDefault(s => s.Id == request.StudentId)
                ?? throw new NotFoundDomainException("Студент не найден.");
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");

            if (db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == course.Id))
            {
                throw new ConflictDomainException("Студент уже зачислен на этот курс.", "ALREADY_ENROLLED");
            }

            var now = DateTime.UtcNow.ToString("O");

            if (course.PriceRub > 0 && string.Equals(request.Mode, "application", StringComparison.OrdinalIgnoreCase))
            {
                var pendingExists = db.Payments.Any(p =>
                    p.StudentId == student.Id &&
                    p.CourseId == course.Id &&
                    p.Status == "Ожидает подтверждения");

                if (pendingExists)
                {
                    throw new ConflictDomainException(
                        "Заявка на этот курс уже отправлена и ожидает подтверждения.",
                        "PENDING_APPLICATION_EXISTS");
                }

                var pendingPayment = new PaymentRecord(
                    CreateId("pay"),
                    student.Id,
                    course.Id,
                    course.PriceRub,
                    "Заявка на обучение",
                    "Ожидает подтверждения",
                    now,
                    null);
                db.Payments.Insert(0, pendingPayment);
                return new EnrollResult(false, null, pendingPayment, "Заявка отправлена на подтверждение преподавателю.");
            }

            var payment = new PaymentRecord(
                CreateId("pay"),
                student.Id,
                course.Id,
                course.PriceRub,
                "Мгновенная оплата",
                "Оплачено",
                now,
                now);
            db.Payments.Insert(0, payment);

            var enrollment = new Enrollment(CreateId("enr"), student.Id, course.Id, now);
            db.Enrollments.Add(enrollment);
            course.IncrementStudents();

            var latestMonth = db.MonthlyDynamics.LastOrDefault();
            if (latestMonth != null)
            {
                latestMonth.Enrollments += 1;
            }

            return new EnrollResult(true, enrollment, payment);
        }, ct);
    }

    public Task<CompleteLessonResult> CompleteLessonAsync(
        string courseId,
        string lessonId,
        string studentId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            throw new ValidationDomainException("Не указан студент.");
        }

        return _repository.UpdateAsync(db =>
        {
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");

            var hasLesson = course.Modules.Any(m => m.Lessons.Any(l => l.Id == lessonId));
            if (!hasLesson)
            {
                throw new NotFoundDomainException("Урок не найден в курсе.");
            }

            var enrollment = db.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.CourseId == courseId)
                ?? throw new NotFoundDomainException("Запись студента на курс не найдена.");

            var student = db.Students.FirstOrDefault(s => s.Id == studentId);

            if (enrollment.MarkLessonCompleted(lessonId) && student != null)
            {
                student.AddPoints(25);
            }

            var prevProgress = enrollment.ProgressPercent;
            var newProgress = _assessmentService.CalculateProgress(course, enrollment.CompletedLessonIds);
            var cert = newProgress == 100
                ? _assessmentService.GenerateCertificateNumber(enrollment.Id, course.Id)
                : null;
            enrollment.UpdateProgress(newProgress, cert);

            if (prevProgress < 100 && enrollment.ProgressPercent == 100)
            {
                var latestMonth = db.MonthlyDynamics.LastOrDefault();
                if (latestMonth != null)
                {
                    latestMonth.Graduations += 1;
                }
            }

            return new CompleteLessonResult(enrollment, student?.TotalPoints ?? 0);
        }, ct);
    }

    public Task<SaveLessonNoteResult> SaveLessonNoteAsync(
        string courseId,
        string lessonId,
        SaveLessonNoteRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.StudentId))
        {
            throw new ValidationDomainException("Не указан студент.");
        }

        return _repository.UpdateAsync(db =>
        {
            var enrollment = db.Enrollments.FirstOrDefault(e => e.StudentId == request.StudentId && e.CourseId == courseId)
                ?? throw new NotFoundDomainException("Запись студента на курс не найдена.");

            enrollment.SaveLessonNote(lessonId, request.Note);
            return new SaveLessonNoteResult(true, enrollment.LessonNotes);
        }, ct);
    }

    public Task<GuestInquiry> SubmitInquiryAsync(SubmitInquiryRequest request, CancellationToken ct = default)
    {
        var inquiry = new GuestInquiry(
            CreateId("inq"),
            request.Name,
            request.Email,
            request.Topic,
            request.Message);

        return _repository.UpdateAsync(db =>
        {
            db.Inquiries.Insert(0, inquiry);
            return inquiry;
        }, ct);
    }

    public Task<QuizSubmissionResponse> SubmitQuizAsync(
        string courseId,
        string quizId,
        SubmitQuizRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.StudentId))
        {
            throw new ValidationDomainException("Не указан студент.");
        }

        return _repository.UpdateAsync(db =>
        {
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");

            var quiz = course.Modules.SelectMany(m => m.Quizzes).FirstOrDefault(q => q.Id == quizId)
                ?? throw new NotFoundDomainException("Тест не найден.");

            var enrollment = db.Enrollments.FirstOrDefault(e => e.StudentId == request.StudentId && e.CourseId == courseId)
                ?? throw new NotFoundDomainException("Запись на курс не найдена.");

            enrollment.QuizResults.TryGetValue(quizId, out var prevAttempt);
            var (attempt, details, pointsDelta) = _assessmentService.EvaluateQuiz(quiz, request.Answers, prevAttempt);
            enrollment.RecordQuizResult(quizId, attempt);

            var student = db.Students.FirstOrDefault(s => s.Id == request.StudentId);
            if (student != null && pointsDelta > 0)
            {
                student.AddPoints(pointsDelta);
            }

            return new QuizSubmissionResponse(attempt, details, student?.TotalPoints ?? 0);
        }, ct);
    }

    public Task<HomeworkSubmission> SubmitHomeworkAsync(
        string courseId,
        string assignmentId,
        SubmitHomeworkRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.StudentId) || string.IsNullOrWhiteSpace(request.SubmissionText))
        {
            throw new ValidationDomainException("Заполните текстовое описание решения.");
        }

        if (!string.IsNullOrWhiteSpace(request.SubmissionUrl) && !IsValidHttpUrl(request.SubmissionUrl))
        {
            throw new ValidationDomainException("Ссылка на работу должна начинаться с http:// или https://.");
        }

        return _repository.UpdateAsync(db =>
        {
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");

            var exists = course.Modules.SelectMany(m => m.Assignments).Any(a => a.Id == assignmentId);
            if (!exists)
            {
                throw new NotFoundDomainException("Задание не найдено.");
            }

            if (!db.Enrollments.Any(e => e.StudentId == request.StudentId && e.CourseId == courseId))
            {
                throw new NotFoundDomainException("Необходима запись на курс.");
            }

            var existing = db.Homeworks.FirstOrDefault(h =>
                h.StudentId == request.StudentId &&
                h.CourseId == courseId &&
                h.AssignmentId == assignmentId);

            if (existing != null)
            {
                existing.Resubmit(request.SubmissionText, request.SubmissionUrl);
                return existing;
            }

            var created = new HomeworkSubmission(
                CreateId("sub"),
                request.StudentId,
                courseId,
                assignmentId,
                request.SubmissionText,
                request.SubmissionUrl);
            db.Homeworks.Insert(0, created);
            return created;
        }, ct);
    }

    public Task<Course> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        if (!AllowedCategories.Contains(request.Category))
        {
            throw new ValidationDomainException("Некорректная категория курса.");
        }

        if (!AllowedLevels.Contains(request.Level))
        {
            throw new ValidationDomainException("Некорректный уровень сложности.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ValidationDomainException("Добавьте описание курса.");
        }

        var coverUrl = !string.IsNullOrWhiteSpace(request.CoverUrl) && IsValidHttpUrl(request.CoverUrl)
            ? request.CoverUrl.Trim()
            : DefaultCovers[request.Category];

        return _repository.UpdateAsync(db =>
        {
            if (!db.Instructors.Any(i => i.Id == request.InstructorId))
            {
                throw new NotFoundDomainException("Выбранный преподаватель не найден.");
            }

            var id = CreateId("course");
            var course = new Course(
                id,
                request.Title,
                id,
                request.Category,
                request.Level,
                request.Description,
                coverUrl,
                request.InstructorId,
                request.PriceRub,
                request.DurationHours);
            course.AddModule(new CourseModule(CreateId("mod"), "Модуль 1. Введение и практика", 1));
            db.Courses.Add(course);
            return course;
        }, ct);
    }

    public Task<CourseModule> AddModuleAsync(string courseId, CreateModuleRequest request, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");
            var mod = new CourseModule(CreateId("mod"), request.Title, course.Modules.Count + 1);
            course.AddModule(mod);
            return mod;
        }, ct);
    }

    public Task<LessonActivity> AddLessonAsync(
        string courseId,
        string moduleId,
        UpsertLessonRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ValidationDomainException("Название и основной текст урока обязательны.");
        }

        return _repository.UpdateAsync(db =>
        {
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");
            var mod = course.Modules.FirstOrDefault(m => m.Id == moduleId)
                ?? throw new NotFoundDomainException("Модуль не найден.");

            var lesson = new LessonActivity(
                CreateId("les"),
                request.Title,
                mod.Lessons.Count + 1,
                request.DurationMinutes,
                string.IsNullOrWhiteSpace(request.Summary) ? "Практический материал урока." : request.Summary,
                request.Content,
                request.CodeExample,
                string.IsNullOrWhiteSpace(request.KeyTakeaway) ? "Закрепите теорию на практическом задании." : request.KeyTakeaway);

            mod.AddLesson(lesson);

            foreach (var enr in db.Enrollments.Where(e => e.CourseId == courseId))
            {
                var updated = _assessmentService.CalculateProgress(course, enr.CompletedLessonIds);
                enr.UpdateProgress(updated, updated == 100 ? _assessmentService.GenerateCertificateNumber(enr.Id, courseId) : null);
            }

            return lesson;
        }, ct);
    }

    public Task<LessonActivity> UpdateLessonAsync(
        string courseId,
        string lessonId,
        UpsertLessonRequest request,
        CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");
            var lesson = course.Modules.SelectMany(m => m.Lessons).FirstOrDefault(l => l.Id == lessonId)
                ?? throw new NotFoundDomainException("Урок не найден.");

            lesson.UpdateContent(
                request.Title,
                request.DurationMinutes,
                request.Summary,
                request.Content,
                request.CodeExample,
                request.KeyTakeaway);
            return lesson;
        }, ct);
    }

    public Task<QuizActivity> AddQuizAsync(
        string courseId,
        string moduleId,
        CreateQuizRequest request,
        CancellationToken ct = default)
    {
        if (request.Questions == null || request.Questions.Count == 0)
        {
            throw new ValidationDomainException("Добавьте минимум один вопрос в тест.");
        }

        var questions = request.Questions.Select((q, idx) =>
        {
            var options = q.Options.Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToList();
            if (string.IsNullOrWhiteSpace(q.Text) || options.Count < 2 || q.CorrectIndex < 0 || q.CorrectIndex >= options.Count)
            {
                throw new ValidationDomainException("Каждый вопрос должен содержать текст, минимум 2 варианта и верный индекс ответа.");
            }

            return new QuizQuestion
            {
                Id = CreateId($"q-{idx + 1}"),
                Text = q.Text.Trim(),
                Options = options,
                CorrectIndex = q.CorrectIndex,
                Explanation = string.IsNullOrWhiteSpace(q.Explanation)
                    ? "Верный вариант выбран по материалам урока."
                    : q.Explanation.Trim()
            };
        }).ToList();

        return _repository.UpdateAsync(db =>
        {
            var course = db.Courses.FirstOrDefault(c => c.Id == courseId)
                ?? throw new NotFoundDomainException("Курс не найден.");
            var mod = course.Modules.FirstOrDefault(m => m.Id == moduleId)
                ?? throw new NotFoundDomainException("Модуль не найден.");

            var quiz = new QuizActivity(CreateId("quiz"), request.Title, request.PassingScore, request.PointsReward, questions);
            mod.AddQuiz(quiz);
            return quiz;
        }, ct);
    }

    public Task<HomeworkSubmission> ReviewHomeworkAsync(
        string homeworkId,
        ReviewHomeworkRequest request,
        CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            var hw = db.Homeworks.FirstOrDefault(h => h.Id == homeworkId)
                ?? throw new NotFoundDomainException("Домашнее задание не найдено.");

            var newlyAccepted = hw.Review(request.Status, request.Score, request.TeacherComment);
            if (newlyAccepted)
            {
                var student = db.Students.FirstOrDefault(s => s.Id == hw.StudentId);
                student?.AddPoints(50);
            }

            return hw;
        }, ct);
    }

    public Task<StudyGroup> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            if (!db.Instructors.Any(i => i.Id == request.CuratorId))
            {
                throw new NotFoundDomainException("Куратор не найден.");
            }

            var group = new StudyGroup(
                CreateId("grp"),
                request.Name,
                request.CuratorId,
                string.IsNullOrWhiteSpace(request.Description) ? "Учебная группа потока 2026." : request.Description);
            db.Groups.Add(group);
            return group;
        }, ct);
    }

    public Task<Student> AssignStudentGroupAsync(string studentId, string? groupId, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId)
                ?? throw new NotFoundDomainException("Студент не найден.");

            if (!string.IsNullOrWhiteSpace(groupId) && !db.Groups.Any(g => g.Id == groupId))
            {
                throw new NotFoundDomainException("Учебная группа не найдена.");
            }

            student.AssignToGroup(groupId);
            return student;
        }, ct);
    }

    public Task<WebinarActivity> ScheduleWebinarAsync(CreateWebinarRequest request, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            var instructor = db.Instructors.FirstOrDefault(i => i.Id == request.InstructorId)
                ?? throw new NotFoundDomainException("Преподаватель не найден.");

            if (!db.Courses.Any(c => c.Id == request.CourseId))
            {
                throw new NotFoundDomainException("Курс не найден.");
            }

            var candidate = new WebinarActivity(
                CreateId("web"),
                request.Title,
                request.CourseId,
                request.InstructorId,
                request.StartTime,
                request.EndTime,
                request.MeetingUrl,
                string.IsNullOrWhiteSpace(request.Description) ? "Живое занятие с разбором вопросов." : request.Description);

            _webinarValidator.EnsureNoInstructorConflict(
                db.Webinars,
                request.InstructorId,
                candidate.StartTime,
                candidate.EndTime,
                instructor.FullName);

            db.Webinars.Add(candidate);
            return candidate;
        }, ct);
    }

    public Task DeleteWebinarAsync(string webinarId, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            var idx = db.Webinars.FindIndex(w => w.Id == webinarId);
            if (idx < 0)
            {
                throw new NotFoundDomainException("Вебинар не найден.");
            }

            db.Webinars.RemoveAt(idx);
            return true;
        }, ct);
    }

    public Task<ConfirmPaymentResult> ConfirmPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(db =>
        {
            var payment = db.Payments.FirstOrDefault(p => p.Id == paymentId)
                ?? throw new NotFoundDomainException("Платёж или заявка не найдены.");

            payment.Confirm();

            var enrollment = db.Enrollments.FirstOrDefault(e =>
                e.StudentId == payment.StudentId && e.CourseId == payment.CourseId);

            if (enrollment == null)
            {
                enrollment = new Enrollment(CreateId("enr"), payment.StudentId, payment.CourseId);
                db.Enrollments.Add(enrollment);

                var course = db.Courses.FirstOrDefault(c => c.Id == payment.CourseId);
                course?.IncrementStudents();

                var latestMonth = db.MonthlyDynamics.LastOrDefault();
                if (latestMonth != null)
                {
                    latestMonth.Enrollments += 1;
                }
            }

            return new ConfirmPaymentResult(payment, enrollment);
        }, ct);
    }
}
