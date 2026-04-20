using FluentValidation;

namespace Learnix.Application.Sections.Commands.ReorderSections;

public sealed class ReorderSectionsValidator : AbstractValidator<ReorderSectionsCommand>
{
    public ReorderSectionsValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        
        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(items => items.Count <= 500)
            .WithMessage("Too many sections in reorder payload.");
        
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEmpty();
            item.RuleFor(i => i.Order).GreaterThanOrEqualTo(0);
        });
        // Payload-level uniqueness (duplicate IDs / duplicate orders) is validated in domain
        // alongside set-equality with DB state (ReorderValidation.EnsureValid).
    }
}