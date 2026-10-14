using System.ComponentModel.DataAnnotations;

namespace FileSystemBrowser.Models
{
  public record MoveOperation(string? Name, string? RelativePath) : IValidatableObject
  {
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      // Check to make sure that only one of Name or RelativePath is specified.
      // If both are null or both are non-null, return a validation error.
      if ((Name is null) == (RelativePath is null))
      {
        yield return new ValidationResult("Specify exactly one of 'Name' or 'RelativePath'.", new[] { nameof(Name), nameof(RelativePath) });
      }

      if (Name is not null && (string.IsNullOrWhiteSpace(Name) || Name.IndexOfAny(['/', '\\']) >= 0))
        yield return new ValidationResult(
            "A name must be non-empty and cannot contain path separators.", [nameof(Name)]);

      if (RelativePath is not null && string.IsNullOrWhiteSpace(RelativePath))
        yield return new ValidationResult(
            "A relative path must be non-empty.", [nameof(RelativePath)]);


    }
  }
}
