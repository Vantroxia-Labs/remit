using ExcelDataReader;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AegisEInvoicing.Application.Extensions;

public class StreamingInvoiceConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    static StreamingInvoiceConverter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    #region IFormFile Methods - Direct to List

    /// <summary>
    /// Converts Excel from IFormFile directly to strongly-typed list WITHOUT saving to file
    /// RECOMMENDED: Use this for converting Excel uploads to List&lt;T&gt; in memory
    /// Perfect for API endpoints that receive Excel and return/process typed objects
    /// </summary>
    public static async Task<List<T>> ConvertToTypedListWithGroupingAsync<T>(
        IFormFile excelFile,
        StreamingConversionOptions? options = null,
        IProgress<ConversionProgress>? progress = null)
    {
        var invoices = await ConvertToListWithGroupingAsync(excelFile, "PaymentReference", options, progress);

        var jsonString = JsonSerializer.Serialize(invoices, JsonOptions);
        var typedList = JsonSerializer.Deserialize<List<T>>(jsonString, JsonOptions);

        return typedList ?? new List<T>();
    }

    /// <summary>
    /// Converts Excel from IFormFile to Dictionary list WITHOUT saving to file
    /// Use for in-memory processing or when you need flexibility with the data structure
    /// </summary>
    public static async Task<List<Dictionary<string, object?>>> ConvertToListWithGroupingAsync(
        IFormFile excelFile,
        string groupByField,
        StreamingConversionOptions? options = null,
        IProgress<ConversionProgress>? progress = null)
    {
        if (excelFile == null || excelFile.Length == 0)
            throw new ArgumentException("Excel file is required", nameof(excelFile));

        options ??= new StreamingConversionOptions();

        var invoices = new List<Dictionary<string, object?>>();

        await using var excelStream = excelFile.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(excelStream, new ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.UTF8
        });

        var startTime = DateTime.UtcNow;

        do
        {
            if (options.SheetNames?.Any() == true && !options.SheetNames.Contains(reader.Name))
                continue;

            var sheetInvoices = StreamSheetWithGrouping(reader, null, groupByField, options, progress);
            invoices.AddRange(sheetInvoices);

        } while (reader.NextResult());

        var elapsed = DateTime.UtcNow - startTime;
        progress?.Report(new ConversionProgress
        {
            TotalProcessed = invoices.Count,
            IsComplete = true,
            ElapsedTime = elapsed
        });

        return invoices;
    }

    #endregion

    #region IFormFile Methods - With File Output

    /// <summary>
    /// Converts Excel from IFormFile and BOTH saves to JSON file AND returns the list
    /// Use when you want to keep a file copy for audit/backup purposes
    /// </summary>
    public static async Task<List<Dictionary<string, object?>>> ConvertToJsonFileWithGroupingAsync(
        IFormFile excelFile,
        string jsonPath,
        string groupByField,
        StreamingConversionOptions? options = null,
        IProgress<ConversionProgress>? progress = null)
    {
        if (excelFile == null || excelFile.Length == 0)
            throw new ArgumentException("Excel file is required", nameof(excelFile));

        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentNullException(nameof(jsonPath));

        options ??= new StreamingConversionOptions();

        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var invoices = new List<Dictionary<string, object?>>();

        await using var fileStream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions
        {
            Indented = options.PrettyPrint
        });

        await using var excelStream = excelFile.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(excelStream, new ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.UTF8
        });

        var startTime = DateTime.UtcNow;

        writer.WriteStartArray();

        do
        {
            if (options.SheetNames?.Any() == true && !options.SheetNames.Contains(reader.Name))
                continue;

            var sheetInvoices = StreamSheetWithGrouping(reader, writer, groupByField, options, progress);
            invoices.AddRange(sheetInvoices);

        } while (reader.NextResult());

        writer.WriteEndArray();
        await writer.FlushAsync();

        var elapsed = DateTime.UtcNow - startTime;
        progress?.Report(new ConversionProgress
        {
            TotalProcessed = invoices.Count,
            IsComplete = true,
            ElapsedTime = elapsed
        });

        return invoices;
    }

    /// <summary>
    /// Streams Excel from IFormFile directly to JSON output stream (e.g., HTTP Response)
    /// Use this to return JSON directly to client without saving to disk
    /// Does NOT return a list - writes directly to stream for memory efficiency
    /// </summary>
    public static async Task ConvertToJsonStreamAsync(
        IFormFile excelFile,
        Stream outputStream,
        StreamingConversionOptions? options = null,
        IProgress<ConversionProgress>? progress = null)
    {
        if (excelFile == null || excelFile.Length == 0)
            throw new ArgumentException("Excel file is required", nameof(excelFile));

        if (outputStream == null || !outputStream.CanWrite)
            throw new ArgumentException("Output stream must be writable", nameof(outputStream));

        options ??= new StreamingConversionOptions();

        await using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions
        {
            Indented = options.PrettyPrint,
            SkipValidation = false
        });

        await using var excelStream = excelFile.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(excelStream, new ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.UTF8
        });

        var startTime = DateTime.UtcNow;
        long processedRows = 0;

        writer.WriteStartArray();

        do
        {
            if (options.SheetNames?.Any() == true && !options.SheetNames.Contains(reader.Name))
                continue;

            processedRows += StreamSheetAsync(reader, writer, options, progress, processedRows);

        } while (reader.NextResult());

        writer.WriteEndArray();
        await writer.FlushAsync();

        var elapsed = DateTime.UtcNow - startTime;
        progress?.Report(new ConversionProgress
        {
            TotalProcessed = processedRows,
            IsComplete = true,
            ElapsedTime = elapsed,
            RowsPerSecond = processedRows / elapsed.TotalSeconds
        });
    }

    /// <summary>
    /// Streams Excel from IFormFile to JSON file WITHOUT grouping
    /// Use for simple row-by-row conversion (each row = one invoice)
    /// </summary>
    public static async Task ConvertToJsonFileStreamingAsync(
        IFormFile excelFile,
        string jsonPath,
        StreamingConversionOptions? options = null,
        IProgress<ConversionProgress>? progress = null)
    {
        if (excelFile == null || excelFile.Length == 0)
            throw new ArgumentException("Excel file is required", nameof(excelFile));

        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentNullException(nameof(jsonPath));

        options ??= new StreamingConversionOptions();

        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions
        {
            Indented = options.PrettyPrint,
            SkipValidation = false
        });

        await using var excelStream = excelFile.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(excelStream, new ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.UTF8
        });

        var startTime = DateTime.UtcNow;
        long processedRows = 0;

        writer.WriteStartArray();

        do
        {
            if (options.SheetNames?.Any() == true && !options.SheetNames.Contains(reader.Name))
                continue;

            processedRows += StreamSheetAsync(reader, writer, options, progress, processedRows);

        } while (reader.NextResult());

        writer.WriteEndArray();
        await writer.FlushAsync();

        var elapsed = DateTime.UtcNow - startTime;
        progress?.Report(new ConversionProgress
        {
            TotalProcessed = processedRows,
            IsComplete = true,
            ElapsedTime = elapsed,
            RowsPerSecond = processedRows / elapsed.TotalSeconds
        });
    }

    #endregion

    #region File Path Methods (Original - Backward Compatibility)

    /// <summary>
    /// Original method - Streams Excel from file path to JSON file with minimal memory footprint
    /// Perfect for 1M+ invoices
    /// </summary>
    public static async Task ConvertToJsonFileStreamingAsync(
        string excelPath,
        string jsonPath,
        StreamingConversionOptions? options = null,
        IProgress<ConversionProgress>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(excelPath))
            throw new ArgumentNullException(nameof(excelPath));

        if (!File.Exists(excelPath))
            throw new FileNotFoundException($"Excel file not found: {excelPath}");

        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentNullException(nameof(jsonPath));

        options ??= new StreamingConversionOptions();

        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions
        {
            Indented = options.PrettyPrint,
            SkipValidation = false
        });

        using var excelStream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(excelStream, new ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.UTF8
        });

        var startTime = DateTime.UtcNow;
        long processedRows = 0;

        writer.WriteStartArray();

        do
        {
            if (options.SheetNames?.Any() == true && !options.SheetNames.Contains(reader.Name))
                continue;

            processedRows += StreamSheetAsync(reader, writer, options, progress, processedRows);

        } while (reader.NextResult());

        writer.WriteEndArray();
        await writer.FlushAsync();

        var elapsed = DateTime.UtcNow - startTime;
        progress?.Report(new ConversionProgress
        {
            TotalProcessed = processedRows,
            IsComplete = true,
            ElapsedTime = elapsed,
            RowsPerSecond = processedRows / elapsed.TotalSeconds
        });
    }

    /// <summary>
    /// Original method - Converts with grouping from file path
    /// </summary>
    public static async Task<List<Dictionary<string, object?>>> ConvertToJsonFileWithGroupingAsync(
        string excelPath,
        string jsonPath,
        string groupByField,
        StreamingConversionOptions? options = null,
        IProgress<ConversionProgress>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(excelPath))
            throw new ArgumentNullException(nameof(excelPath));

        if (!File.Exists(excelPath))
            throw new FileNotFoundException($"Excel file not found: {excelPath}");

        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentNullException(nameof(jsonPath));

        options ??= new StreamingConversionOptions();

        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var invoices = new List<Dictionary<string, object?>>();

        await using var fileStream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions
        {
            Indented = options.PrettyPrint
        });

        using var excelStream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(excelStream);

        var startTime = DateTime.UtcNow;

        writer.WriteStartArray();

        do
        {
            if (options.SheetNames?.Any() == true && !options.SheetNames.Contains(reader.Name))
                continue;

            var sheetInvoices = StreamSheetWithGrouping(reader, writer, groupByField, options, progress);
            invoices.AddRange(sheetInvoices);

        } while (reader.NextResult());

        writer.WriteEndArray();
        await writer.FlushAsync();

        var elapsed = DateTime.UtcNow - startTime;
        progress?.Report(new ConversionProgress
        {
            TotalProcessed = invoices.Count,
            IsComplete = true,
            ElapsedTime = elapsed
        });

        return invoices;
    }

    #endregion

    #region Private Helper Methods

    private static long StreamSheetAsync(
        IExcelDataReader reader,
        Utf8JsonWriter writer,
        StreamingConversionOptions options,
        IProgress<ConversionProgress>? progress,
        long currentProgress)
    {
        var headers = ReadHeaders(reader, 3);
        var columnMap = BuildColumnMap(headers);
        long rowsProcessed = 0;
        var reportInterval = options.ProgressReportInterval;

        while (reader.Read())
        {
            if (IsEmptyRow(reader))
                continue;

            var invoice = BuildInvoiceObject(reader, columnMap, options);

            if (invoice != null)
            {
                WriteInvoiceObject(writer, invoice);
                rowsProcessed++;

                if (progress != null && rowsProcessed % reportInterval == 0)
                {
                    progress.Report(new ConversionProgress
                    {
                        TotalProcessed = currentProgress + rowsProcessed,
                        IsComplete = false
                    });
                }
            }
        }

        return rowsProcessed;
    }

    private static List<Dictionary<string, object?>> StreamSheetWithGrouping(
        IExcelDataReader reader,
        Utf8JsonWriter? writer,
        string groupByField,
        StreamingConversionOptions options,
        IProgress<ConversionProgress>? progress)
    {
        var headers = ReadHeaders(reader, 3);
        var columnMap = BuildColumnMap(headers);

        var batchSize = options.BatchSize;
        var batch = new List<Dictionary<string, object?>>(batchSize);
        var allInvoices = new List<Dictionary<string, object?>>();
        long totalProcessed = 0;

        while (reader.Read())
        {
            if (IsEmptyRow(reader))
                continue;

            var row = ReadDataRow(reader, columnMap, options);
            if (row != null && row.Count > 0)
            {
                batch.Add(row);
            }

            if (batch.Count >= batchSize)
            {
                var invoices = ProcessAndWriteBatch(batch, groupByField, writer, options);
                allInvoices.AddRange(invoices);
                totalProcessed += batch.Count;
                batch.Clear();

                progress?.Report(new ConversionProgress
                {
                    TotalProcessed = totalProcessed,
                    IsComplete = false
                });
            }
        }

        if (batch.Count > 0)
        {
            var invoices = ProcessAndWriteBatch(batch, groupByField, writer, options);
            allInvoices.AddRange(invoices);
            totalProcessed += batch.Count;
        }

        return allInvoices;
    }

    private static List<Dictionary<string, object?>> ProcessAndWriteBatch(
        List<Dictionary<string, object?>> batch,
        string groupByField,
        Utf8JsonWriter? writer,
        StreamingConversionOptions options)
    {
        var invoices = new List<Dictionary<string, object?>>();

        var groups = batch.GroupBy(row =>
            row.ContainsKey(groupByField) ? row[groupByField]?.ToString() ?? "unknown" : "unknown"
        );

        foreach (var group in groups)
        {
            var invoice = BuildGroupedInvoice(group.ToList(), options);
            if (invoice != null)
            {
                invoices.Add(invoice);

                // Only write if writer is provided (when saving to file)
                if (writer != null)
                {
                    WriteInvoiceObject(writer, invoice);
                }
            }
        }

        return invoices;
    }

    private static Dictionary<string, object?>? BuildInvoiceObject(
        IExcelDataReader reader,
        Dictionary<int, FieldPath> columnMap,
        StreamingConversionOptions options)
    {
        var invoice = new Dictionary<string, object?>();
        var items = new List<Dictionary<string, object?>>();
        var currentItem = new Dictionary<string, object?>();

        foreach (var (col, fieldPath) in columnMap)
        {
            if (col >= reader.FieldCount)
                continue;

            object? value;
            try
            {
                value = reader.GetValue(col);
                value = ConvertValue(value, options);
            }
            catch
            {
                value = null;
            }

            if (value == null && !options.IncludeNullValues)
                continue;

            if (fieldPath.IsInvoiceItem)
            {
                var itemPath = fieldPath.Parts.Skip(1).ToList();
                SetNestedValue(currentItem, itemPath, value);
            }
            else
            {
                SetNestedValue(invoice, fieldPath.Parts, value);
            }
        }

        if (currentItem.Count > 0)
        {
            items.Add(currentItem);
        }

        if (items.Count > 0)
        {
            invoice["InvoiceItems"] = items;
        }

        return invoice.Count > 0 ? invoice : null;
    }

    private static Dictionary<string, object?>? BuildGroupedInvoice(
        List<Dictionary<string, object?>> rows,
        StreamingConversionOptions options)
    {
        if (rows.Count == 0)
            return null;

        var invoice = new Dictionary<string, object?>();
        var items = new List<Dictionary<string, object?>>();

        // Track document references that should be collected as lists
        var billingReferences = new List<Dictionary<string, object?>>();
        var additionalDocumentReferences = new List<Dictionary<string, object?>>();

        foreach (var row in rows)
        {
            var item = new Dictionary<string, object?>();
            var billingRef = new Dictionary<string, object?>();
            var additionalDocRef = new Dictionary<string, object?>();

            foreach (var (key, value) in row)
            {
                var parts = key.Split('.');

                if (parts[0] == "InvoiceItems")
                {
                    var itemPath = parts.Skip(1).ToList();
                    SetNestedValue(item, itemPath, value);
                }
                else if (parts[0] == "BillingReference")
                {
                    // Collect billing references (each row may have one)
                    var refPath = parts.Skip(1).ToList();
                    SetNestedValue(billingRef, refPath, value);
                }
                else if (parts[0] == "AdditionalDocumentReferences")
                {
                    // Collect additional document references (each row may have one)
                    var refPath = parts.Skip(1).ToList();
                    SetNestedValue(additionalDocRef, refPath, value);
                }
                else if (invoice.Count == 0 || !invoice.ContainsKey(key))
                {
                    // For single document references and other fields, only take from first row
                    SetNestedValue(invoice, parts.ToList(), value);
                }
            }

            if (item.Count > 0)
            {
                items.Add(item);
            }

            if (billingRef.Count > 0 && HasNonNullValues(billingRef))
            {
                billingReferences.Add(billingRef);
            }

            if (additionalDocRef.Count > 0 && HasNonNullValues(additionalDocRef))
            {
                additionalDocumentReferences.Add(additionalDocRef);
            }
        }

        if (items.Count > 0)
        {
            invoice["InvoiceItems"] = items;
        }

        if (billingReferences.Count > 0)
        {
            invoice["BillingReference"] = billingReferences;
        }

        if (additionalDocumentReferences.Count > 0)
        {
            invoice["AdditionalDocumentReferences"] = additionalDocumentReferences;
        }

        return invoice.Count > 0 ? invoice : null;
    }

    private static bool HasNonNullValues(Dictionary<string, object?> dict)
    {
        return dict.Values.Any(v => v != null && (v is not string str || !string.IsNullOrWhiteSpace(str)));
    }

    private static void WriteInvoiceObject(Utf8JsonWriter writer, Dictionary<string, object?> invoice)
    {
        JsonSerializer.Serialize(writer, invoice, JsonOptions);
    }

    private static List<List<string>> ReadHeaders(IExcelDataReader reader, int headerRowCount)
    {
        var headers = new List<List<string>>();

        for (int i = 0; i < headerRowCount; i++)
        {
            if (!reader.Read())
                break;

            var headerRow = new List<string>();
            for (int col = 0; col < reader.FieldCount; col++)
            {
                try
                {
                    var value = reader.GetValue(col)?.ToString()?.Trim() ?? string.Empty;
                    headerRow.Add(value);
                }
                catch
                {
                    headerRow.Add(string.Empty);
                }
            }
            headers.Add(headerRow);
        }

        return headers;
    }

    /// <summary>
    /// Builds column map with support for user's custom template structure:
    /// Row 1 = Main field names (IssueDate, Party, InvoiceItems, etc.)
    /// Row 2 = Sub-field names (Name, Code, Address, TaxCategory, etc.)
    /// Row 3 = Deeply nested field names (Street, City, Name, Percent, etc.) - optional
    ///
    /// Carries forward row1 and row2 values when merged across columns
    /// </summary>
    private static Dictionary<int, FieldPath> BuildColumnMap(List<List<string>> headers)
    {
        var columnMap = new Dictionary<int, FieldPath>();
        var colCount = headers[0].Count;

        // Track the last non-empty values to handle merged cells
        string lastRow1Value = string.Empty;
        string lastRow2Value = string.Empty;

        for (int col = 0; col < colCount; col++)
        {
            var path = new List<string>();
            var row1 = headers.ElementAtOrDefault(0)?[col]?.Trim() ?? string.Empty;
            var row2 = headers.ElementAtOrDefault(1)?[col]?.Trim() ?? string.Empty;
            var row3 = headers.ElementAtOrDefault(2)?[col]?.Trim() ?? string.Empty;

            // Carry forward row1 value if current cell is empty (merged cells)
            if (!string.IsNullOrWhiteSpace(row1))
            {
                lastRow1Value = row1;
                lastRow2Value = string.Empty; // Reset row2 when row1 changes
            }
            else if (!string.IsNullOrWhiteSpace(lastRow1Value))
            {
                row1 = lastRow1Value;
            }

            // Carry forward row2 value if current cell is empty (merged cells)
            if (!string.IsNullOrWhiteSpace(row2))
            {
                lastRow2Value = row2;
            }
            else if (!string.IsNullOrWhiteSpace(lastRow2Value) && !string.IsNullOrWhiteSpace(row1))
            {
                row2 = lastRow2Value;
            }

            // Build the path based on what values are present
            // Row1 is the main field (could be top-level like "IssueDate" or category like "Party", "InvoiceItems")
            // Row2 is the sub-field (like "Name", "Address", "TaxCategory")
            // Row3 is for deeply nested fields (like "Street" under Address, or "Name" under TaxCategory)

            if (!string.IsNullOrWhiteSpace(row1))
                path.Add(row1);

            if (!string.IsNullOrWhiteSpace(row2))
                path.Add(row2);

            if (!string.IsNullOrWhiteSpace(row3))
                path.Add(row3);

            if (path.Count > 0)
            {
                columnMap[col] = new FieldPath
                {
                    Parts = path,
                    IsInvoiceItem = path[0] == "InvoiceItems"
                };
            }
        }

        return columnMap;
    }

    private static Dictionary<string, object?> ReadDataRow(
        IExcelDataReader reader,
        Dictionary<int, FieldPath> columnMap,
        StreamingConversionOptions options)
    {
        var row = new Dictionary<string, object?>();

        foreach (var (col, fieldPath) in columnMap)
        {
            if (col >= reader.FieldCount)
                continue;

            object? value;
            try
            {
                value = reader.GetValue(col);
                value = ConvertValue(value, options);
            }
            catch
            {
                value = null;
            }

            if (value == null && !options.IncludeNullValues)
                continue;

            var key = string.Join(".", fieldPath.Parts);
            row[key] = value;
        }

        return row;
    }

    private static void SetNestedValue(Dictionary<string, object?> root, List<string> path, object? value)
    {
        if (path.Count == 0)
            return;

        var current = root;

        for (var i = 0; i < path.Count - 1; i++)
        {
            var key = path[i];

            if (!current.ContainsKey(key))
            {
                current[key] = new Dictionary<string, object?>();
            }

            if (current[key] is Dictionary<string, object?> dict)
            {
                current = dict;
            }
            else
            {
                return;
            }
        }

        current[path[^1]] = value;
    }

    private static bool IsEmptyRow(IExcelDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            try
            {
                var value = reader.GetValue(i);
                if (value != null && value is not DBNull)
                {
                    var str = value.ToString();
                    if (!string.IsNullOrWhiteSpace(str))
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
        return true;
    }

    private static object? ConvertValue(object? value, StreamingConversionOptions options)
    {
        if (value == null || value is DBNull)
            return null;

        return value switch
        {
            DateTime dt => options.DateTimeFormat != null ? dt.ToString(options.DateTimeFormat) : dt.ToString("yyyy-MM-dd"),
            double d when double.IsNaN(d) || double.IsInfinity(d) => null,
            decimal => value,
            double => value,
            float => value,
            int => value,
            long => value,
            short => value,
            byte => value,
            bool => value,
            string s => s.Trim(),
            _ => value.ToString()?.Trim()
        };
    }

    private class FieldPath
    {
        public List<string> Parts { get; set; } = new();
        public bool IsInvoiceItem { get; set; }
    }

    #endregion
}