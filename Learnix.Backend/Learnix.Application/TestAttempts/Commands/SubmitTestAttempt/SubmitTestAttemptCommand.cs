using FluentResults;
using MediatR;

namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

public sealed record SubmitTestAttemptCommand(
    Guid CourseId,
    Guid LessonId,
    Guid AttemptId,
    IReadOnlyList<SubmittedAnswerDto> Answers) : IRequest<Result<SubmitTestAttemptResponse>>;

public sealed record SubmittedAnswerDto(
    int QuestionOrder,
    List<int> SelectedOptionOrders,
    string? TextValue);
