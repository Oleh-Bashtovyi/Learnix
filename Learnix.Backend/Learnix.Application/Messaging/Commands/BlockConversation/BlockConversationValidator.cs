using FluentValidation;

namespace Learnix.Application.Messaging.Commands.BlockConversation;

internal sealed class BlockConversationValidator : AbstractValidator<BlockConversationCommand>
{
    public BlockConversationValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
