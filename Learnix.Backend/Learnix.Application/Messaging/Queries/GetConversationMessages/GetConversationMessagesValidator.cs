using FluentValidation;

using Learnix.Application.Common.Constants;

namespace Learnix.Application.Messaging.Queries.GetConversationMessages;

internal sealed class GetConversationMessagesValidator : AbstractValidator<GetConversationMessagesQuery>
{
    public GetConversationMessagesValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip must be greater than or equal to 0.");

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .WithMessage("Take must be greater than 0.")
            .LessThanOrEqualTo(PaginationConstants.MaxPageSize)
            .WithMessage($"Take cannot exceed {PaginationConstants.MaxPageSize}.");
    }
}
