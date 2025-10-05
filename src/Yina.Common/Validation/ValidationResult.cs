using System;
using System.Collections.Generic;
using System.Text;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace Yina.Common.Validation;

/// <summary>Aggregates validation errors for a request or object.</summary>
public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    /// <summary>Initializes an empty result.</summary>
    public ValidationResult()
    {
    }

    /// <summary>Initializes with an existing collection of errors.</summary>
    public ValidationResult(IEnumerable<ValidationError> errors)
    {
        if (errors is null)
        {
            return;
        }

        _errors.AddRange(errors);
    }

    /// <summary>Gets the collected validation errors.</summary>
    public IReadOnlyList<ValidationError> Errors => _errors;

    /// <summary>Gets a value indicating whether the validation succeeded.</summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>Creates a successful validation result.</summary>
    public static ValidationResult Success() => new();

    /// <summary>Creates a validation result from the provided <paramref name="errors"/>.</summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new(errors);

    /// <summary>Adds a single <paramref name="error"/> to the result.</summary>
    public ValidationResult Add(ValidationError error)
    {
        _errors.Add(error);
        return this;
    }

    /// <summary>Adds a range of <paramref name="errors"/> to the result.</summary>
    public ValidationResult AddRange(IEnumerable<ValidationError> errors)
    {
        _errors.AddRange(errors);
        return this;
    }

    /// <summary>Merges errors from another <paramref name="other"/> result.</summary>
    public ValidationResult Merge(ValidationResult? other)
    {
        if (other is null)
        {
            return this;
        }

        _errors.AddRange(other._errors);
        return this;
    }

    /// <summary>Adds the specified <paramref name="prefix"/> to error field names.</summary>
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

    /// <summary>Converts collected errors into a single <see cref="Error"/>.</summary>
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

    /// <summary>Returns either a successful or failed <see cref="Result{T}"/> depending on <see cref="IsValid"/>.</summary>
    public Result<T> ToResult<T>(T value)
        => IsValid ? Result<T>.Success(value) : Result<T>.Failure(ToError());
}




