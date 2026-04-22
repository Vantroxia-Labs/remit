using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBusinessItem;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBulkBusinessItem;

public class CreateBulkBusinessItemCommandHandler : IRequestHandler<CreateBulkBusinessItemCommand, BulkItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateBulkBusinessItemCommandHandler> _logger;
    public CreateBulkBusinessItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ILogger<CreateBulkBusinessItemCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BulkItemResult> Handle(CreateBulkBusinessItemCommand request, CancellationToken cancellationToken)
    {
        List<BusinessItem> businessItems = new List<BusinessItem>();
        try
        {
            if (!_currentUser.UserId.HasValue)
            {
                _logger.LogWarning("Unauthorized attempt to create business item");
                throw new AuthenticationException("User authentication required");
            }

            if (!_currentUser.BusinessId.HasValue)
            {
                _logger.LogWarning("Business not found");
                throw new ForbiddenException("Business not found");
            }

            // Verify business exists
            var businessExists = await _context.Businesses
                .AnyAsync(b => b.Id == _currentUser.BusinessId.Value, cancellationToken);

            if (!businessExists)
            {
                _logger.LogWarning("Attempt to create business item for non-existent business {BusinessId}", _currentUser.BusinessId.Value);
                throw new NotFoundException("Business not found");
            }

            // Read and validate Excel file
            var readFile = await ReadFile(request.file);

            if (!readFile.IsSuccessful)
            {
                _logger.LogWarning("Failed to read Excel file for business {BusinessId}: {Error}", _currentUser.BusinessId.Value, readFile.ErrorMessage);
                throw new UnprocessableEntityException(readFile.ErrorMessage);
            }

            // Check for duplicates within the uploaded file
            var duplicateNamesInFile = readFile.Items
                .GroupBy(i => i.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNamesInFile.Any())
            {
                var duplicateList = string.Join(", ", duplicateNamesInFile.Select(n => $"'{n}'"));
                _logger.LogWarning("Duplicate item names found in uploaded file for business {BusinessId}: {Duplicates}", _currentUser.BusinessId.Value, duplicateList);
                throw new ConflictException($"The uploaded file contains duplicate item names: {duplicateList}. Each item name must be unique.");
            }

            // Get existing business items for the current business
            var existingItemNames = await _context.BusinessItems
                .Where(bi => bi.BusinessID == _currentUser.BusinessId.Value && !bi.IsDeleted)
                .Select(bi => bi.Name)
                .ToListAsync(cancellationToken);

            // Check for duplicates against existing database items
            var duplicateNamesInDb = readFile.Items
                .Where(i => existingItemNames.Contains(i.Name.Trim(), StringComparer.OrdinalIgnoreCase))
                .Select(i => i.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (duplicateNamesInDb.Any())
            {
                var duplicateList = string.Join(", ", duplicateNamesInDb.Select(n => $"'{n}'"));
                _logger.LogWarning("Duplicate item names found in database for business {BusinessId}: {Duplicates}", _currentUser.BusinessId.Value, duplicateList);
                throw new ConflictException($"The following item names already exist in your business: {duplicateList}. Please use different names or update the existing items.");
            }

            foreach (var item in readFile.Items)
            {
                // Create value objects
                var serviceCode = ServiceCode.Create(item.Service.Code, item.Service.Name);

                var businessItem = BusinessItem.Create(
                    _currentUser.BusinessId.Value,
                    item.Name,
                    item.ItemType,
                    serviceCode,
                    Guid.Empty,
                    item.ItemDescription,
                    item.UnitPrice);

                businessItems.Add(businessItem);
            }

            // Save to database
            await _context.BusinessItems.AddRangeAsync(businessItems);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created {Count} business items for business {BusinessId} (skipped {ErrorCount} blank rows)",
                readFile.totalSuccessfulUploadedCount, _currentUser.BusinessId.Value, readFile.totalErrorsUploadedCount);

            return new BulkItemResult(true, $"Successfully created {readFile.totalSuccessfulUploadedCount} business items.");
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (ConflictException)
        {
            throw;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating bulk business items: {Message}", ex.Message);
            throw new UnprocessableEntityException("Invalid data in the uploaded file. Please verify all fields are correctly formatted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating bulk business items: {Message}", ex.Message);
            throw new UnprocessableEntityException("We could not perform the action at this time. Please try again later.");
        }
    }

    private async Task<BulkItemDtos> ReadFile(IFormFile file)
    {
        BulkItemDtos response = new BulkItemDtos();
        int successfulUpload = 0;
        int expectedUpload = 0;
        string currentLocation = string.Empty;
        int totalBlankCells = 0;
        string blankMessage = string.Empty;
        string blankCells = string.Empty;
        var list = new List<CreateBulkItemRequest>();
        string uploadType = file.Name;
        string fileName = file.FileName;
        HashSet<string> allowedFileTypes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "xlsx", "xls"
            };

        FileInfo fileInfo = new FileInfo(file.Name);

        //check file extension
        string[] fileArr = fileName.Split('.').Skip(1).ToArray();

        foreach (var ext in fileArr)
        {
            if (!allowedFileTypes.Contains(ext))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file extension is not a valid Excel type.", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };
        }


        if (!fileName.EndsWith(".xlsx") && !fileName.EndsWith(".xls"))
            return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file not an Excel type", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };


        //check file size
        if (file.Length > 2000000)
            return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file is greater than 2MB", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };


        string sFileExtension = Path.GetExtension(fileName).ToLower();
        ISheet sheet;

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;
            if (sFileExtension == ".xls")
            {
                HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook  
            }
            else
            {
                XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
            }
            IRow headerRow = sheet.GetRow(0); //Get Header Row
            int cellCount = headerRow.LastCellNum;

            expectedUpload = sheet.PhysicalNumberOfRows - 1;

            if (cellCount != 7)
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Only (7) columns are allowed in the document", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (expectedUpload is 0)
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} No Record Found", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[0].StringCellValue.ToLower().Trim(), "Name", StringComparison.OrdinalIgnoreCase))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[1].StringCellValue.ToLower().Trim(), "ItemType", StringComparison.OrdinalIgnoreCase))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[2].StringCellValue.ToLower().Trim(), "Code", StringComparison.OrdinalIgnoreCase))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[3].StringCellValue.ToLower().Trim(), "CodeDescription", StringComparison.OrdinalIgnoreCase))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[4].StringCellValue.ToLower().Trim(), "ItemName", StringComparison.OrdinalIgnoreCase))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[5].StringCellValue.ToLower().Trim(), "ItemDescription", StringComparison.OrdinalIgnoreCase))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[6].StringCellValue.ToLower().Trim(), "UnitPrice", StringComparison.OrdinalIgnoreCase))
                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };


            try
            {
                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
                {
                    IRow row = sheet.GetRow(i);

                    if (row is null) continue;

                    if (row.Cells.All(d => d.CellType == CellType.Blank))
                    {
                        blankCells = string.IsNullOrEmpty(blankCells) ? $"The following row(s) are blank: {i}" : $"{blankCells}, {i}";

                        totalBlankCells++;

                        continue;
                    }

                    var cells = row.Cells.Take(10).ToList();

                    currentLocation = $"at row {i}.";

                    string? name = cells[0] is null ? "" : cells[0].ToString();
                    string? itemTypeStr = cells[1] is null ? "" : cells[1].ToString();
                    string? code = cells[2] is null ? "" : cells[2].ToString();
                    string? codeDescription = cells[3] is null ? "" : cells[3].ToString();
                    string? itemName = cells[4] is null ? "" : cells[4].ToString();
                    string? itemDesc = cells[5] is null ? "" : cells[5].ToString();
                    string? unitPrice = cells[6] is null ? "" : cells[6].ToString();

                    currentLocation = "at saving records into database";

                    if (!Enum.TryParse<AegisEInvoicing.Domain.Enums.ItemType>(itemTypeStr, ignoreCase: true, out var parsedItemType))
                        parsedItemType = AegisEInvoicing.Domain.Enums.ItemType.Service;

                    if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(codeDescription) || !string.IsNullOrWhiteSpace(itemName) || !string.IsNullOrWhiteSpace(itemDesc) || !string.IsNullOrWhiteSpace(unitPrice))
                    {
                        list.Add(new CreateBulkItemRequest()
                        {
                            ItemDescription = itemDesc!,
                            Name = name!,
                            ItemName = itemName!,
                            UnitPrice = Convert.ToDecimal(unitPrice),
                            ItemType = parsedItemType,
                            Service = new CreateBulkItemServiceCodeRequest() { Name = codeDescription!, Code = code! }
                        });
                        successfulUpload++;
                    }
                    else
                        totalBlankCells++;
                }

                if (totalBlankCells > 0)
                {
                    blankMessage = $"{totalBlankCells} blank/empty row(s). {blankCells}";
                }

                if (successfulUpload > 0)
                {
                    return new BulkItemDtos() { IsSuccessful = true, ErrorMessage = string.Empty, totalErrorsUploadedCount = totalBlankCells, totalSuccessfulUploadedCount = successfulUpload, Items = list };
                }

                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = "No successful upload found", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            }
            catch (Exception ex)
            {

                _logger.LogError($"CreateBulkBusinessItemCommandHandler::ReadFile: {ex}");

                return new BulkItemDtos() { IsSuccessful = false, ErrorMessage = $"{successfulUpload} out of {expectedUpload} rows uploaded successfully.\n Something went wrong at row {successfulUpload + 1}. \n  Ensure the excel sheet is completely filled with no empty column/cells.\n {blankMessage}", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };
            }
        }
    }
}
