using FluentResults;
using FluentValidation;
using Learnix.Application.Common.Behaviors;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Domain.UnitTests.Behaviors;

// Shared test doubles 

file record NonGenericCommand : IRequest<Result>;
file record GenericCommand : IRequest<Result<string>>;

file sealed class PassValidator : AbstractValidator<NonGenericCommand> { }

file sealed class FailValidator : AbstractValidator<NonGenericCommand>
{
    public FailValidator()
    {
        RuleFor(x => x).Must(_ => false).WithMessage("field-error");
    }
}

file sealed class SecondFailValidator : AbstractValidator<NonGenericCommand>
{
    public SecondFailValidator()
    {
        RuleFor(x => x).Must(_ => false).WithMessage("second-error");
    }
}

file sealed class GenericPassValidator : AbstractValidator<GenericCommand> { }

file sealed class GenericFailValidator : AbstractValidator<GenericCommand>
{
    public GenericFailValidator()
    {
        RuleFor(x => x).Must(_ => false).WithMessage("generic-field-error");
    }
}

// ResultFailFactory tests 

/// <summary>
/// Tests that <see cref="ResultFailFactory"/> caches per TResponse type
/// and produces correctly typed failed results.
/// </summary>
public class ResultFailFactoryTests
{
    private static ValidationError MakeError() =>
        new(new FluentValidation.Results.ValidationResult(
            [new FluentValidation.Results.ValidationFailure("X", "msg")]));

    [Fact]
    public void Get_Returns_Same_Delegate_Instance_For_Result_On_Repeated_Calls()
    {
        var first = ResultFailFactory.Get<Result>();
        var second = ResultFailFactory.Get<Result>();

        // Reference equality: the same cached Func object is returned
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Get_Returns_Same_Delegate_Instance_For_Generic_Result_On_Repeated_Calls()
    {
        var first = ResultFailFactory.Get<Result<string>>();
        var second = ResultFailFactory.Get<Result<string>>();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Get_Returns_Different_Delegates_For_Different_Response_Types()
    {
        // Delegates are cached per type — different types must produce different delegates
        var forResult = ResultFailFactory.Get<Result>();
        var forStringResult = ResultFailFactory.Get<Result<string>>();
        var forIntResult = ResultFailFactory.Get<Result<int>>();

        forResult.Should().NotBeSameAs(forStringResult);
        forStringResult.Should().NotBeSameAs(forIntResult);
    }

    [Fact]
    public void Factory_For_Result_Produces_Failed_Result_Containing_ValidationError()
    {
        var factory = ResultFailFactory.Get<Result>();
        var result = factory(MakeError());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ValidationError);
    }

    [Fact]
    public void Factory_For_Generic_Result_Produces_Failed_Result_Containing_ValidationError()
    {
        var factory = ResultFailFactory.Get<Result<string>>();
        var result = factory(MakeError());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ValidationError);
    }

    [Fact]
    public void ValidationError_Carries_Original_Failure_Messages()
    {
        var failure = new FluentValidation.Results.ValidationFailure("Name", "Name is required");
        var error = new ValidationError(new FluentValidation.Results.ValidationResult([failure]));

        var factory = ResultFailFactory.Get<Result>();
        var result = factory(error);

        var ve = result.Errors.OfType<ValidationError>().Single();
        ve.ValidationResult.Errors.Should().ContainSingle(f => f.ErrorMessage == "Name is required");
    }
}

// ValidationBehavior tests 

/// <summary>
/// Tests that <see cref="ValidationBehavior{TRequest,TResponse}"/> correctly
/// short-circuits on failure, passes through on success, and aggregates errors
/// from multiple validators.
/// </summary>
public class ValidationBehaviorTests
{
    private static Task<Result> OkNext() => Task.FromResult(Result.Ok());
    private static Task<Result<string>> OkGenericNext() => Task.FromResult(Result.Ok("value"));

    // No validators 

    [Fact]
    public async Task Should_Call_Next_When_No_Validators()
    {
        var behavior = new ValidationBehavior<NonGenericCommand, Result>(
            Enumerable.Empty<IValidator<NonGenericCommand>>());

        var called = false;
        var result = await behavior.Handle(
            new NonGenericCommand(),
            () => { called = true; return OkNext(); },
            default);

        called.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    // Passing validators 

    [Fact]
    public async Task Should_Call_Next_When_Validator_Passes_For_Result()
    {
        var behavior = new ValidationBehavior<NonGenericCommand, Result>(
            [new PassValidator()]);

        var called = false;
        var result = await behavior.Handle(
            new NonGenericCommand(),
            () => { called = true; return OkNext(); },
            default);

        called.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Call_Next_When_Validator_Passes_For_Generic_Result()
    {
        var behavior = new ValidationBehavior<GenericCommand, Result<string>>(
            [new GenericPassValidator()]);

        var called = false;
        var result = await behavior.Handle(
            new GenericCommand(),
            () => { called = true; return OkGenericNext(); },
            default);

        called.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
    }

    // Failing validators 

    [Fact]
    public async Task Should_Return_Failed_Result_Without_Calling_Next_When_Validator_Fails()
    {
        var behavior = new ValidationBehavior<NonGenericCommand, Result>(
            [new FailValidator()]);

        var called = false;
        var result = await behavior.Handle(
            new NonGenericCommand(),
            () => { called = true; return OkNext(); },
            default);

        called.Should().BeFalse("next must not be called when validation fails");
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ValidationError);
    }

    [Fact]
    public async Task Should_Return_Failed_Generic_Result_Without_Calling_Next_When_Validator_Fails()
    {
        var behavior = new ValidationBehavior<GenericCommand, Result<string>>(
            [new GenericFailValidator()]);

        var called = false;
        var result = await behavior.Handle(
            new GenericCommand(),
            () => { called = true; return OkGenericNext(); },
            default);

        called.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ValidationError);
    }

    [Fact]
    public async Task Should_Include_Failure_Message_In_ValidationError()
    {
        var behavior = new ValidationBehavior<NonGenericCommand, Result>(
            [new FailValidator()]);

        var result = await behavior.Handle(new NonGenericCommand(), OkNext, default);

        var ve = result.Errors.OfType<ValidationError>().Single();
        ve.ValidationResult.Errors.Should().ContainSingle(f => f.ErrorMessage == "field-error");
    }

    // Multiple validators 

    [Fact]
    public async Task Should_Aggregate_Failures_From_Multiple_Validators()
    {
        var behavior = new ValidationBehavior<NonGenericCommand, Result>(
            [new FailValidator(), new SecondFailValidator()]);

        var result = await behavior.Handle(new NonGenericCommand(), OkNext, default);

        result.IsFailed.Should().BeTrue();
        var ve = result.Errors.OfType<ValidationError>().Single();
        var messages = ve.ValidationResult.Errors.Select(f => f.ErrorMessage).ToList();
        // Both validators must have contributed at least one failure each
        messages.Should().Contain("field-error");
        messages.Should().Contain("second-error");
    }

    [Fact]
    public async Task Should_Pass_When_One_Of_Multiple_Validators_Passes_But_None_Fails()
    {
        // Both pass — next should be called
        var behavior = new ValidationBehavior<NonGenericCommand, Result>(
            [new PassValidator(), new PassValidator()]);

        var called = false;
        await behavior.Handle(
            new NonGenericCommand(),
            () => { called = true; return OkNext(); },
            default);

        called.Should().BeTrue();
    }
}
