using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Messaging.Commands.SendMessage;

public sealed class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(ConversationConstants.MessageMaxLength)
            .WithMessage($"Message cannot exceed {ConversationConstants.MessageMaxLength} characters.");
    }
}
