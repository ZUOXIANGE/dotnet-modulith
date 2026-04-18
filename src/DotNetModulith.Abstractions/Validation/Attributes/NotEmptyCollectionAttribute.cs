using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Abstractions.Validation.Attributes;

/// <summary>
/// 校验集合非空且至少包含一个元素
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotEmptyCollectionAttribute : ValidationAttribute
{
    public NotEmptyCollectionAttribute()
        : base("The {0} collection must contain at least one item.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IEnumerable enumerable)
        {
            foreach (var _ in enumerable)
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }
}
