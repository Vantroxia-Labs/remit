namespace AegisEInvoicing.Application.Extensions;

public class StreamingConversionOptions
{
    /// <summary>
    /// Include null values in JSON output (default: false)
    /// </summary>
    public bool IncludeNullValues { get; set; } = false;

    /// <summary>
    /// Specific sheet names to convert (null = all sheets)
    /// </summary>
    public List<string>? SheetNames { get; set; }

    /// <summary>
    /// Custom DateTime format string (default: yyyy-MM-dd)
    /// </summary>
    public string? DateTimeFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Batch size for grouping operations (default: 10000)
    /// Lower = less memory, higher = faster grouping
    /// </summary>
    public int BatchSize { get; set; } = 10000;

    /// <summary>
    /// Report progress every N rows (default: 5000)
    /// </summary>
    public int ProgressReportInterval { get; set; } = 5000;

    /// <summary>
    /// Pretty print JSON with indentation (default: false for performance)
    /// </summary>
    public bool PrettyPrint { get; set; } = false;
}

public class ConversionProgress
{
    public long TotalProcessed { get; set; }
    public bool IsComplete { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public double RowsPerSecond { get; set; }
}
