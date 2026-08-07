using FluentValidation;

namespace Learnix.Application.Messaging.Commands.UnblockConversation;

internal sealed class UnblockConversationValidator : AbstractValidator<UnblockConversationCommand>
{
    public UnblockConversationValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
