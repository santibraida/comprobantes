namespace FileContentRenamer.Models
{
  /// <summary>
  /// Interface for a naming rule that determines service names and payment methods based on content
  /// </summary>
  public interface INamingRule
  {
    /// <summary>
    /// The name of the rule to be used for logging
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// List of keywords that all must be present for this rule to match
    /// </summary>
    List<string> Keywords { get; set; }

    /// <summary>
    /// Service name to use in the filename (e.g., "muni_quilmes", "high_school", etc.)
    /// </summary>
    string ServiceName { get; set; }

    /// <summary>
    /// Payment method to use in the filename (e.g., "santander", "efectivo", etc.)
    /// </summary>
    string PaymentMethod { get; set; }

    /// <summary>
    /// Optional date override in yyyy-MM-dd format for cases where OCR fails
    /// </summary>
    string? DateOverride { get; set; }

    /// <summary>
    /// Checks if this rule matches the provided content
    /// </summary>
    /// <param name="content">The text content to check against the rule's keywords</param>
    /// <returns>True if the rule matches, false otherwise</returns>
    bool Matches(string content);

    /// <summary>
    /// Validates that the rule is properly configured
    /// </summary>
    /// <returns>True if the rule is valid, false otherwise</returns>
    bool IsValid();
  }
}
