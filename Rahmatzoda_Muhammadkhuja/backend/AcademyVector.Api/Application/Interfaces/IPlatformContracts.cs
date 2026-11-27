using AcademyVector.Api.Domain.Entities;

namespace AcademyVector.Api.Application.Interfaces;

public interface ISeedDataProvider
{
    PlatformDatabase CreateSeedDatabase();
}

public interface ICourseDbRepository
{
    Task<PlatformDatabase> ReadAsync(CancellationToken ct = default);
    Task<T> UpdateAsync<T>(Func<PlatformDatabase, T> mutator, CancellationToken ct = default);
    Task<PlatformDatabase> ResetAsync(CancellationToken ct = default);
}

public interface IWebinarScheduleValidator
{
    void EnsureNoInstructorConflict(
        IEnumerable<WebinarActivity> existingWebinars,
        string instructorId,
        string startTimeIso,
        string endTimeIso,
        string instructorFullName);
}

public interface IAssessmentService
{
    int CalculateProgress(Course course, IReadOnlyCollection<string> completedLessonIds);
    string GenerateCertificateNumber(string enrollmentId, string courseId);
    (QuizAttemptResult Attempt, List<QuizQuestionFeedback> Details, int PointsDelta) EvaluateQuiz(
        QuizActivity quiz,
        IReadOnlyList<int> answers,
        QuizAttemptResult? previousAttempt);
}

public interface IPlatformService
{
    Task<PlatformDatabase> GetBootstrapAsync(string role, CancellationToken ct = default);
    Task<OopSummaryResponse> GetOopSummaryAsync(CancellationToken ct = default);
    Task<PlatformDatabase> ResetDatabaseAsync(CancellationToken ct = default);

    Task<EnrollResult> EnrollCourseAsync(string courseId, EnrollRequest request, CancellationToken ct = default);
    Task<CompleteLessonResult> CompleteLessonAsync(string courseId, string lessonId, string studentId, CancellationToken ct = default);
    Task<SaveLessonNoteResult> SaveLessonNoteAsync(string courseId, string lessonId, SaveLessonNoteRequest request, CancellationToken ct = default);
    Task<GuestInquiry> SubmitInquiryAsync(SubmitInquiryRequest request, CancellationToken ct = default);
    Task<QuizSubmissionResponse> SubmitQuizAsync(string courseId, string quizId, SubmitQuizRequest request, CancellationToken ct = default);
    Task<HomeworkSubmission> SubmitHomeworkAsync(string courseId, string assignmentId, SubmitHomeworkRequest request, CancellationToken ct = default);

    Task<Course> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default);
    Task<CourseModule> AddModuleAsync(string courseId, CreateModuleRequest request, CancellationToken ct = default);
    Task<LessonActivity> AddLessonAsync(string courseId, string moduleId, UpsertLessonRequest request, CancellationToken ct = default);
    Task<LessonActivity> UpdateLessonAsync(string courseId, string lessonId, UpsertLessonRequest request, CancellationToken ct = default);
    Task<QuizActivity> AddQuizAsync(string courseId, string moduleId, CreateQuizRequest request, CancellationToken ct = default);

    Task<HomeworkSubmission> ReviewHomeworkAsync(string homeworkId, ReviewHomeworkRequest request, CancellationToken ct = default);
    Task<StudyGroup> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<Student> AssignStudentGroupAsync(string studentId, string? groupId, CancellationToken ct = default);
    Task<WebinarActivity> ScheduleWebinarAsync(CreateWebinarRequest request, CancellationToken ct = default);
    Task DeleteWebinarAsync(string webinarId, CancellationToken ct = default);
    Task<ConfirmPaymentResult> ConfirmPaymentAsync(string paymentId, CancellationToken ct = default);
}

public sealed record EnrollRequest(string StudentId, string Mode = "instant");
public sealed record EnrollResult(bool Enrolled, Enrollment? Enrollment, PaymentRecord Payment, string? Message = null);
public sealed record CompleteLessonRequest(string StudentId);
public sealed record CompleteLessonResult(Enrollment Enrollment, int StudentTotalPoints);
public sealed record SaveLessonNoteRequest(string StudentId, string Note);
public sealed record SaveLessonNoteResult(bool Ok, Dictionary<string, string> LessonNotes);
public sealed record SubmitInquiryRequest(string Name, string Email, string Topic, string Message);
public sealed record SubmitQuizRequest(string StudentId, List<int> Answers);
public sealed record QuizQuestionFeedback(string QuestionId, int SelectedIndex, int CorrectIndex, bool IsCorrect, string Explanation);
public sealed record QuizSubmissionResponse(QuizAttemptResult Attempt, List<QuizQuestionFeedback> Details, int StudentTotalPoints);
public sealed record SubmitHomeworkRequest(string StudentId, string SubmissionText, string SubmissionUrl);

public sealed record CreateCourseRequest(
    string Title,
    string Category,
    string Level,
    string Description,
    string InstructorId,
    int PriceRub,
    int DurationHours,
    string? CoverUrl);

public sealed record CreateModuleRequest(string Title);

public sealed record UpsertLessonRequest(
    string Title,
    int DurationMinutes,
    string Summary,
    string Content,
    string CodeExample,
    string KeyTakeaway);

public sealed record CreateQuizQuestionDto(string Text, List<string> Options, int CorrectIndex, string Explanation);
public sealed record CreateQuizRequest(string Title, int PassingScore, int PointsReward, List<CreateQuizQuestionDto> Questions);

public sealed record ReviewHomeworkRequest(string Status, int Score, string TeacherComment);
public sealed record CreateGroupRequest(string Name, string CuratorId, string Description);
public sealed record AssignGroupRequest(string? GroupId);

public sealed record CreateWebinarRequest(
    string Title,
    string CourseId,
    string InstructorId,
    string StartTime,
    string EndTime,
    string MeetingUrl,
    string Description);

public sealed record ConfirmPaymentResult(PaymentRecord Payment, Enrollment Enrollment);

public sealed record OopSummaryItem(
    string Id,
    string EntityKind,
    string ClassName,
    string BaseClass,
    string Title,
    string PolymorphicDescription,
    string MetricLabel,
    string MetricValue);

public sealed record OopSummaryResponse(
    int TotalEntities,
    List<string> HierarchyNotes,
    List<OopSummaryItem> Items);
