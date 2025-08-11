using System.Text.RegularExpressions;
using Serilog;

namespace FileContentRenamer.Models
{
    public class NamingRule
    {
        /// <summary>
        /// The name of the rule to be used for logging
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// List of keywords that all must be present for this rule to match
        /// </summary>
        public List<string> Keywords { get; set; } = [];
        
        /// <summary>
        /// Service name to use in the filename (e.g., "muni_quilmes", "high_school", etc.)
        /// </summary>
        public string ServiceName { get; set; } = "";
        
        /// <summary>
        /// Payment method to use in the filename (e.g., "santander", "efectivo", etc.)
        /// </summary>
        public string PaymentMethod { get; set; } = "";
        
        /// <summary>
        /// Checks if this rule matches the provided content
        /// </summary>
        /// <param name="content">The text content to check against the rule's keywords</param>
        /// <returns>True if the rule matches, false otherwise</returns>
        public bool Matches(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Log.Debug("Rule '{RuleName}' not checked - content is empty", Name);
                return false;
            }
                
            if (Keywords == null || Keywords.Count == 0)
            {
                Log.Debug("Rule '{RuleName}' not checked - no keywords defined", Name);
                return false;
            }
                
            // Convert content to lowercase for easier comparison
            string lowerContent = content.ToLowerInvariant();
            
            // Check each keyword and log which ones are missing
            var missingKeywords = new List<string>();

            foreach (var keyword in Keywords)
            {
                if (!lowerContent.Contains(keyword.ToLowerInvariant()))
                {
                    missingKeywords.Add(keyword);
                }
            }
            
            bool allKeywordsMatch = missingKeywords.Count == 0;
            
            if (allKeywordsMatch)
            {
                Log.Debug("Rule '{RuleName}' matched content with keywords: {Keywords}", Name, string.Join(", ", Keywords));
            }
            else if (missingKeywords.Count < Keywords.Count)
            {
                // Some keywords matched but not all
                Log.Debug("Rule '{RuleName}' partially matched. Missing keywords: {MissingKeywords}", 
                    Name, string.Join(", ", missingKeywords));
            }
            
            return allKeywordsMatch;
        }
    }
    
    public class NamingRules
    {
        /// <summary>
        /// Collection of all naming rules
        /// </summary>
        public List<NamingRule> Rules { get; set; } = [];
        
        /// <summary>
        /// Default service name to use if no rules match
        /// </summary>
        public string DefaultServiceName { get; set; } = "";
        
        /// <summary>
        /// Default payment method to use if not specified by a rule
        /// </summary>
        public string DefaultPaymentMethod { get; set; } = "";
        
        /// <summary>
        /// Constructor with default parameters
        /// </summary>
        public NamingRules() { }
        
        /// <summary>
        /// Constructor that explicitly sets default values
        /// </summary>
        public NamingRules(string defaultServiceName, string defaultPaymentMethod)
        {
            DefaultServiceName = defaultServiceName;
            DefaultPaymentMethod = defaultPaymentMethod;
            
            Log.Debug("NamingRules initialized with DefaultServiceName: {DefaultServiceName}, DefaultPaymentMethod: {DefaultPaymentMethod}",
                DefaultServiceName, DefaultPaymentMethod);
        }
        
        /// <summary>
        /// Finds the first rule that matches the content
        /// </summary>
        /// <param name="content">The document content to check against rules</param>
        /// <returns>The first matching rule, or null if no rule matches</returns>
        public NamingRule? FindMatchingRule(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Log.Warning("Cannot find matching rule - content is empty");
                return null;
            }
                
            foreach (var rule in Rules.ToList())
            {
                Log.Debug("Checking rule: '{RuleName}', Keywords: {Keywords})", 
                    rule.Name, string.Join(", ", rule.Keywords));
                    
                if (rule.Matches(content))
                {
                    Log.Debug("Found matching rule: '{RuleName}'", rule.Name);
                    return rule;
                }
            }
            
            Log.Warning("No matching rule found for content");
            return null;
        }
        
        /// <summary>
        /// Determines the service name and payment method based on the content
        /// </summary>
        /// <param name="content">The document content</param>
        /// <param name="date">The date to use in the filename (format: yyyy-MM-dd)</param>
        /// <returns>Formatted filename in the pattern: service_date_paymentmethod</returns>
        public string GenerateFilename(string content, string date)
        {
            var rule = FindMatchingRule(content);
            
            string serviceName = rule?.ServiceName ?? DefaultServiceName;
            // Only use the rule's PaymentMethod if it's not empty
            string paymentMethod = (!string.IsNullOrEmpty(rule?.PaymentMethod)) ? rule.PaymentMethod : DefaultPaymentMethod;
            
            if (rule != null)
            {
                Log.Information("Using rule '{RuleName}' for naming. Service: {Service}, Payment: {Payment}", 
                    rule.Name, serviceName, paymentMethod);
                
                // Add explicit logging about payment method
                if (string.IsNullOrEmpty(rule.PaymentMethod))
                {
                    Log.Debug("Rule '{RuleName}' doesn't specify PaymentMethod, using default: {DefaultPayment}", 
                        rule.Name, DefaultPaymentMethod);
                }
            }
            else
            {
                Log.Warning("No matching rule found, using defaults. Service: {Service}, Payment: {Payment}", 
                    DefaultServiceName, DefaultPaymentMethod);
            }
            
            return $"{serviceName}_{date}_{paymentMethod}";
        }
    }
}
