using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Abstractions.Validation.Attributes;

/// <summary>
/// 校验字符串不为空且不全为空白字符
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public NotWhiteSpaceAttribute()
        : base("The {0} field must not be empty or whitespace.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text))
            return ValidationResult.Success;

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }
}
