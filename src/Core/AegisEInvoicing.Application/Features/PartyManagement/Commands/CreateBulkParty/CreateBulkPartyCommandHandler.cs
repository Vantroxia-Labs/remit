using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateParty;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;
using System.Diagnostics;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateBulkParty;

public class CreateBulkPartyCommandHandler : IRequestHandler<CreateBulkPartyCommand, BulkPartyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateBulkPartyCommandHandler> _logger;
    public CreateBulkPartyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<CreateBulkPartyCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BulkPartyResult> Handle(CreateBulkPartyCommand request, CancellationToken cancellationToken)
    {
        List<Party> parties = new List<Party>();
        List<Party> updatedParty = new List<Party>();
        bool isUpdateRequired = false;
        bool isNewRecord = false;
        try
        {
                if (!_currentUser.UserId.HasValue)
                {
                    _logger.LogWarning("Unauthorized attempt to create party");
                    throw new AuthenticationException("User authentication required");
                }

                if (!_currentUser.BusinessId.HasValue)
                {
                    _logger.LogWarning("Business Not Found");
                    throw new NotFoundException("Business Not Found");
                }

                // Verify business exists
                var businessExists = await _context.Businesses
                    .AnyAsync(b => b.Id == _currentUser.BusinessId.Value, cancellationToken);

                if (!businessExists)
                {
                    _logger.LogWarning("Attempt to create party for non-existent business {BusinessId}", _currentUser.BusinessId.Value);
                    throw new NotFoundException("Business Not Found");
                }

                var readFile = await ReadFile(request.file);

                if(!readFile.IsSuccessful)
                    return new BulkPartyResult(false, readFile.ErrorMessage);

                foreach (var item in readFile.Parties)
                {
                    //check if bulk party exist and if exist update
                    var fetchParty = await _context.Parties.FirstOrDefaultAsync(p => p.BusinessID == _currentUser.BusinessId.Value && p.TaxIdentificationNumber.Value == item.TaxIdentificationNumber);

                    if(fetchParty is not null)
                    {
                        var updateAddress = Address.Create(
                         item.Address.Street!,
                         item.Address.City!,
                         item.Address.State!,
                         item.Address.Country!,
                         item.Address.PostalCode ?? string.Empty);

                        var updateTin = TIN.Create(item.TaxIdentificationNumber!);

                        fetchParty.UpdateAddress(updateAddress);
                        fetchParty.UpdateContactInfo(item.Phone, item.Email);
                        fetchParty.UpdateTaxIdentificationNumber(updateTin);

                        updatedParty.Add(fetchParty);
                        isUpdateRequired = true;
                    }
                    else
                    {
                        // Create value objects
                        var tin = TIN.Create(item.TaxIdentificationNumber!);

                        var address = Address.Create(
                            item.Address.Street!,
                            item.Address.City!,
                            item.Address.State!,
                            item.Address.Country!,
                            item.Address.PostalCode ?? string.Empty);

                        // Create Party entity
                        var party = Party.Create(
                            item.Name!,
                            item.Phone!,
                            item.Email!,
                            tin,
                            address,
                            _currentUser.BusinessId.Value,
                            item.Description!);

                        party.MarkAsCreated(_currentUser.UserId.Value);

                        parties.Add(party);
                        isNewRecord = true;
                    }
                }

                // Save to database
                if(isUpdateRequired)
                    _context.Parties.UpdateRange(updatedParty);

                if(isNewRecord)
                    await _context.Parties.AddRangeAsync(parties);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created {count} successful parties for business {BusinessId} and {count_2} for unsuccessful parties", readFile.totalSuccessfulUploadedCount, _currentUser.BusinessId.Value, readFile.totalErrorsUploadedCount);

            return new BulkPartyResult(true, "Request successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating party: {Message}", ex.Message);

            return new BulkPartyResult(false, "Something went wrong");
        }
    }

    private async Task<BulkPartyDtos> ReadFile(IFormFile file)
    {
        BulkPartyDtos response = new BulkPartyDtos();
        int successfulUpload = 0;
        int expectedUpload = 0;
        string currentLocation = string.Empty;
        int totalBlankCells_InvalidNin = 0;
        string blankMessage = string.Empty;
        string blankCells = string.Empty;
        var list = new List<CreateBulkPartyRequest>();
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
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file extension is not a valid Excel type.", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };
        }


        if (!fileName.EndsWith(".xlsx") && !fileName.EndsWith(".xls"))
            return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file not an Excel type", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };


        //check file size
        if (file.Length > 2000000)
            return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file is greater than 2MB", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };


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

            if (cellCount != 10)
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Only (1)  column are allowed in the document", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (expectedUpload is 0)
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} No Record Found", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[0].StringCellValue.ToLower().Trim(), "Name", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[1].StringCellValue.ToLower().Trim(), "Description", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[2].StringCellValue.ToLower().Trim(), "Phone", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[3].StringCellValue.ToLower().Trim(), "Email", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[4].StringCellValue.ToLower().Trim(), "TaxIdentificationNumber", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[5].StringCellValue.ToLower().Trim(), "Street", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[6].StringCellValue.ToLower().Trim(), "City", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[7].StringCellValue.ToLower().Trim(), "State", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[8].StringCellValue.ToLower().Trim(), "Country", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            if (!string.Equals(headerRow.Cells[9].StringCellValue.ToLower().Trim(), "PostalCode", StringComparison.OrdinalIgnoreCase))
                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{uploadType} Uploaded file does not contain the required columns", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };


            try
            {
                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
                {
                    IRow row = sheet.GetRow(i);

                    if (row is null) continue;

                    if (row.Cells.All(d => d.CellType == CellType.Blank))
                    {
                        blankCells = string.IsNullOrEmpty(blankCells) ? $"The following row(s) are blank: {i}" : $"{blankCells}, {i}";

                        totalBlankCells_InvalidNin++;

                        continue;
                    }

                    var cells = row.Cells.Take(10).ToList();

                    currentLocation = $"at row {i}.";

                    string? name = cells[0] is null ? "" : cells[0].ToString();
                    string? desc = cells[1] is null ? "" : cells[1].ToString();
                    string? phone = cells[2] is null ? "" : cells[2].ToString();
                    string? email = cells[3] is null ? "" : cells[3].ToString();
                    string? taxId = cells[4] is null ? "" : cells[4].ToString();
                    string? street = cells[5] is null ? "" : cells[5].ToString();
                    string? city = cells[6] is null ? "" : cells[6].ToString();
                    string? state = cells[7] is null ? "" : cells[7].ToString();
                    string? country = cells[8] is null ? "" : cells[8].ToString();
                    string? postalCode = cells[9] is null ? "" : cells[9].ToString();

                    currentLocation = "at saving records into database";
                   

                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(desc))
                    {
                        list.Add(new CreateBulkPartyRequest() 
                        { 
                            Description = desc, 
                            Name = name, 
                            Phone = phone!, 
                            Email = email!, 
                            TaxIdentificationNumber = taxId!, 
                            Address = new CreateBulkPartyAddressRequest() 
                            { 
                                City = city!,
                                Country = country!,
                                PostalCode = postalCode!,
                                State = state!,
                                Street = street!
                            } });
                        successfulUpload++;
                    }
                    else
                        totalBlankCells_InvalidNin++;
                }

                if (totalBlankCells_InvalidNin > 0)
                {
                    blankMessage = $"{totalBlankCells_InvalidNin} blank/empty row(s). {blankCells}";
                }

                if (successfulUpload > 0)
                {
                    return new BulkPartyDtos() { IsSuccessful = true, ErrorMessage = string.Empty, totalErrorsUploadedCount = totalBlankCells_InvalidNin, totalSuccessfulUploadedCount = successfulUpload, Parties = list };
                }

                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = "No successful upload found", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };

            }
            catch (Exception ex)
            {

                _logger.LogError($"CreateBulkPartyCommandHandler::ReadFile: {ex}");

                return new BulkPartyDtos() { IsSuccessful = false, ErrorMessage = $"{successfulUpload} out of {expectedUpload} rows uploaded successfully.\n Something went wrong at row {successfulUpload + 1}. \n  Ensure the excel sheet is completely filled with no empty column/cells.\n {blankMessage}", totalErrorsUploadedCount = 0, totalSuccessfulUploadedCount = 0 };
            }
        }
    }
}
