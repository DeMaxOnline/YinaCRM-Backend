using System;
using System.Collections.Generic;
using System.Text;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace Yina.Common.Validation;

public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    public ValidationResult()
    {
    }

    public ValidationResult(IEnumerable<ValidationError> errors)
    {
        if (errors is null)
        {
            return;
        }

        _errors.AddRange(errors);
    }

    public IReadOnlyList<ValidationError> Errors => _errors;

    public bool IsValid => _errors.Count == 0;

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(params ValidationError[] errors) => new(errors);

    public ValidationResult Add(ValidationError error)
    {
        _errors.Add(error);
        return this;
    }

    public ValidationResult AddRange(IEnumerable<ValidationError> errors)
    {
        _errors.AddRange(errors);
        return this;
    }

    public ValidationResult Merge(ValidationResult? other)
    {
        if (other is null)
        {
            return this;
        }

        _errors.AddRange(other._errors);
        return this;
    }

    public ValidationResult WithPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) || _errors.Count == 0)
        {
            return this;
        }

        for (var i = 0; i < _errors.Count; i++)
        {
            _errors[i] = _errors[i].WithPrefix(prefix);
        }

        return this;
    }

    public Error ToError(string code = "VALIDATION_FAILED")
    {
        if (IsValid)
        {
            return Error.None;
        }

        var builder = new StringBuilder();
        builder.Append(_errors.Count).Append(" validation error(s): ");
        for (var i = 0; i < _errors.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("; ");
            }

            builder.Append(_errors[i]);
        }

        return Error.Create(code, builder.ToString(), 400);
    }

    public Result<T> ToResult<T>(T value)
        => IsValid ? Result<T>.Success(value) : Result<T>.Failure(ToError());
}
