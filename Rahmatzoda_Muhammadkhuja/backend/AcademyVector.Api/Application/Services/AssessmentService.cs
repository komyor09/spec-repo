using AcademyVector.Api.Application.Interfaces;
using AcademyVector.Api.Domain.Entities;

namespace AcademyVector.Api.Application.Services;

public sealed class AssessmentService : IAssessmentService
{
    public int CalculateProgress(Course course, IReadOnlyCollection<string> completedLessonIds)
    {
        var allLessonIds = course.Modules
            .SelectMany(m => m.Lessons)
            .Select(l => l.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (allLessonIds.Count == 0)
        {
            return 0;
        }

        var completedCount = completedLessonIds
            .Distinct(StringComparer.Ordinal)
            .Count(id => allLessonIds.Contains(id));

        return Math.Min(100, (int)Math.Round((double)completedCount / allLessonIds.Count * 100));
    }

    public string GenerateCertificateNumber(string enrollmentId, string courseId)
    {
        var source = $"{enrollmentId}-{courseId}";
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in source)
            {
                hash ^= ch;
                hash *= 16777619;
            }

            var code = (int)(Math.Abs((int)hash) % 9000) + 1000;
            return $"VEC-2026-{code}";
        }
    }

    public (QuizAttemptResult Attempt, List<QuizQuestionFeedback> Details, int PointsDelta) EvaluateQuiz(
        QuizActivity quiz,
        IReadOnlyList<int> answers,
        QuizAttemptResult? previousAttempt)
    {
        var details = new List<QuizQuestionFeedback>();
        var correctCount = 0;
        var normalizedAnswers = new List<int>();

        for (var i = 0; i < quiz.Questions.Count; i++)
        {
            var question = quiz.Questions[i];
            var selected = i < answers.Count ? answers[i] : -1;
            var isCorrect = selected == question.CorrectIndex;
            if (isCorrect)
            {
                correctCount++;
            }

            normalizedAnswers.Add(selected);
            details.Add(new QuizQuestionFeedback(
                question.Id,
                selected,
                question.CorrectIndex,
                isCorrect,
                question.Explanation));
        }

        var percentScore = quiz.Questions.Count > 0
            ? (int)Math.Round((double)correctCount / quiz.Questions.Count * 100)
            : 0;
        var passed = percentScore >= quiz.PassingScore;
        var pointsEarned = passed ? quiz.PointsReward : 0;
        var previousPoints = previousAttempt?.PointsEarned ?? 0;
        var pointsDelta = Math.Max(0, pointsEarned - previousPoints);

        var attempt = new QuizAttemptResult
        {
            QuizId = quiz.Id,
            Score = percentScore,
            MaxScore = 100,
            Passed = passed,
            PointsEarned = Math.Max(pointsEarned, previousPoints),
            Answers = normalizedAnswers,
            SubmittedAt = DateTime.UtcNow.ToString("O")
        };

        return (attempt, details, pointsDelta);
    }
}
