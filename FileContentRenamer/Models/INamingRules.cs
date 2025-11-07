namespace FileContentRenamer.Models
{
  /// <summary>
  /// Interface for managing a collection of naming rules and generating filenames
  /// </summary>
  public interface INamingRules
  {
    /// <summary>
    /// Collection of all naming rules
    /// </summary>
    List<NamingRule> Rules { get; set; }

    /// <summary>
    /// Default service name to use if no rules match
    /// </summary>
    string DefaultServiceName { get; set; }

    /// <summary>
    /// Default payment method to use if not specified by a rule
    /// </summary>
    string DefaultPaymentMethod { get; set; }

    /// <summary>
    /// Finds the first rule that matches the content
    /// </summary>
    /// <param name="content">The document content to check against rules</param>
    /// <returns>The first matching rule, or null if no rule matches</returns>
    NamingRule? FindMatchingRule(string content);

    /// <summary>
    /// Determines the service name and payment method based on the content
    /// </summary>
    /// <param name="content">The document content</param>
    /// <param name="date">The date to use in the filename (format: yyyy-MM-dd)</param>
    /// <returns>Formatted filename in the pattern: service_date_paymentmethod</returns>
    string GenerateFilename(string content, string date);
  }
}
