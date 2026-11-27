using System.Text.Encodings.Web;
using System.Text.Json;
using AcademyVector.Api.Application.Interfaces;
using AcademyVector.Api.Application.Services;
using AcademyVector.Api.Infrastructure;
using AcademyVector.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:3000");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// Регистрация зависимостей через интерфейсы (DI)
builder.Services.AddSingleton<ISeedDataProvider, SeedDataProvider>();
builder.Services.AddSingleton<ICourseDbRepository, JsonCourseDbRepository>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IWebinarScheduleValidator, WebinarScheduleValidator>();
builder.Services.AddScoped<IPlatformService, PlatformService>();

var app = builder.Build();

app.UseMiddleware<DomainExceptionMiddleware>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Академия Вектор — ASP.NET Core 8 Web API",
    subject = "ООП на C# (.NET 8)",
    storage = "data/courses-db.json",
    endpoints = new[]
    {
        "GET /api/health",
        "GET /api/platform/oop-summary",
        "GET /api/bootstrap?role=guest|student|teacher",
        "POST /api/inquiries",
        "POST /api/courses/{courseId}/enroll",
        "POST /api/courses/{courseId}/lessons/{lessonId}/complete",
        "POST /api/courses/{courseId}/lessons/{lessonId}/note",
        "POST /api/courses/{courseId}/quizzes/{quizId}/submit",
        "POST /api/courses/{courseId}/assignments/{assignmentId}/submit",
        "POST /api/courses",
        "POST /api/courses/{courseId}/modules",
        "POST /api/courses/{courseId}/modules/{moduleId}/lessons",
        "PUT /api/courses/{courseId}/lessons/{lessonId}",
        "POST /api/courses/{courseId}/modules/{moduleId}/quizzes",
        "PATCH /api/homeworks/{homeworkId}/review",
        "POST /api/groups",
        "PATCH /api/students/{studentId}/group",
        "POST /api/webinars",
        "DELETE /api/webinars/{webinarId}",
        "PATCH /api/payments/{paymentId}/confirm",
        "POST /api/reset"
    }
}));

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok", engine = "ASP.NET Core 8" }));

api.MapGet("/bootstrap", async (string? role, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.GetBootstrapAsync(role ?? "student", ct)));

api.MapGet("/platform/oop-summary", async (IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.GetOopSummaryAsync(ct)));

api.MapPost("/reset", async (IPlatformService service, CancellationToken ct) =>
    Results.Ok(new { ok = true, db = await service.ResetDatabaseAsync(ct) }));

api.MapPost("/courses/{courseId}/enroll", async (string courseId, EnrollRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Created($"/api/courses/{courseId}/enroll", await service.EnrollCourseAsync(courseId, request, ct)));

api.MapPost("/courses/{courseId}/lessons/{lessonId}/complete", async (string courseId, string lessonId, CompleteLessonRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.CompleteLessonAsync(courseId, lessonId, request.StudentId, ct)));

api.MapPost("/courses/{courseId}/lessons/{lessonId}/note", async (string courseId, string lessonId, SaveLessonNoteRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.SaveLessonNoteAsync(courseId, lessonId, request, ct)));

api.MapPost("/inquiries", async (SubmitInquiryRequest request, IPlatformService service, CancellationToken ct) =>
{
    var created = await service.SubmitInquiryAsync(request, ct);
    return Results.Created($"/api/inquiries/{created.Id}", created);
});

api.MapPost("/courses/{courseId}/quizzes/{quizId}/submit", async (string courseId, string quizId, SubmitQuizRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.SubmitQuizAsync(courseId, quizId, request, ct)));

api.MapPost("/courses/{courseId}/assignments/{assignmentId}/submit", async (string courseId, string assignmentId, SubmitHomeworkRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.SubmitHomeworkAsync(courseId, assignmentId, request, ct)));

api.MapPost("/courses", async (CreateCourseRequest request, IPlatformService service, CancellationToken ct) =>
{
    var created = await service.CreateCourseAsync(request, ct);
    return Results.Created($"/api/courses/{created.Id}", created);
});

api.MapPost("/courses/{courseId}/modules", async (string courseId, CreateModuleRequest request, IPlatformService service, CancellationToken ct) =>
{
    var created = await service.AddModuleAsync(courseId, request, ct);
    return Results.Created($"/api/courses/{courseId}/modules/{created.Id}", created);
});

api.MapPost("/courses/{courseId}/modules/{moduleId}/lessons", async (string courseId, string moduleId, UpsertLessonRequest request, IPlatformService service, CancellationToken ct) =>
{
    var created = await service.AddLessonAsync(courseId, moduleId, request, ct);
    return Results.Created($"/api/courses/{courseId}/lessons/{created.Id}", created);
});

api.MapPut("/courses/{courseId}/lessons/{lessonId}", async (string courseId, string lessonId, UpsertLessonRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.UpdateLessonAsync(courseId, lessonId, request, ct)));

api.MapPost("/courses/{courseId}/modules/{moduleId}/quizzes", async (string courseId, string moduleId, CreateQuizRequest request, IPlatformService service, CancellationToken ct) =>
{
    var created = await service.AddQuizAsync(courseId, moduleId, request, ct);
    return Results.Created($"/api/courses/{courseId}/quizzes/{created.Id}", created);
});

api.MapPatch("/homeworks/{homeworkId}/review", async (string homeworkId, ReviewHomeworkRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.ReviewHomeworkAsync(homeworkId, request, ct)));

api.MapPost("/groups", async (CreateGroupRequest request, IPlatformService service, CancellationToken ct) =>
{
    var created = await service.CreateGroupAsync(request, ct);
    return Results.Created($"/api/groups/{created.Id}", created);
});

api.MapPatch("/students/{studentId}/group", async (string studentId, AssignGroupRequest request, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.AssignStudentGroupAsync(studentId, request.GroupId, ct)));

api.MapPost("/webinars", async (CreateWebinarRequest request, IPlatformService service, CancellationToken ct) =>
{
    var created = await service.ScheduleWebinarAsync(request, ct);
    return Results.Created($"/api/webinars/{created.Id}", created);
});

api.MapDelete("/webinars/{webinarId}", async (string webinarId, IPlatformService service, CancellationToken ct) =>
{
    await service.DeleteWebinarAsync(webinarId, ct);
    return Results.Ok(new { ok = true });
});

api.MapPatch("/payments/{paymentId}/confirm", async (string paymentId, IPlatformService service, CancellationToken ct) =>
    Results.Ok(await service.ConfirmPaymentAsync(paymentId, ct)));

app.Run();
