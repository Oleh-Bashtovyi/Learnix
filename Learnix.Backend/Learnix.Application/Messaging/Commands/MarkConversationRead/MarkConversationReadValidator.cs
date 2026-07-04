using FluentValidation;

namespace Learnix.Application.Messaging.Commands.MarkConversationRead;

internal sealed class MarkConversationReadValidator : AbstractValidator<MarkConversationReadCommand>
{
    public MarkConversationReadValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
