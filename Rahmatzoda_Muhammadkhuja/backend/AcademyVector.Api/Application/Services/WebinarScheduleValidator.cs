using AcademyVector.Api.Application.Interfaces;
using AcademyVector.Api.Domain.Entities;
using AcademyVector.Api.Domain.Exceptions;

namespace AcademyVector.Api.Application.Services;

public sealed class WebinarScheduleValidator : IWebinarScheduleValidator
{
    public void EnsureNoInstructorConflict(
        IEnumerable<WebinarActivity> existingWebinars,
        string instructorId,
        string startTimeIso,
        string endTimeIso,
        string instructorFullName)
    {
        var conflict = existingWebinars.FirstOrDefault(w =>
            w.InstructorId == instructorId && w.OverlapsWith(startTimeIso, endTimeIso));

        if (conflict != null)
        {
            throw new ConflictDomainException(
                $"Конфликт расписания: преподаватель {instructorFullName} уже ведёт занятие «{conflict.Title}» в пересекающееся время.",
                "WEBINAR_TIME_CONFLICT",
                new { conflictingWebinar = conflict });
        }
    }
}
