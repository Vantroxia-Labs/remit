using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models.InvoiceData;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UploadInvoices;

public sealed record UploadInvoiceCommand(
    List<UploadInvoiceRequest> UploadInvoiceRequest) : IRequest<UploadInvoiceResult>, ITransactionalCommand;
