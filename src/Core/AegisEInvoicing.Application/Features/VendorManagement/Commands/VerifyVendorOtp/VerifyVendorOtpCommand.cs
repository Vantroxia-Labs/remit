using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.VerifyVendorOtp;

public record VerifyVendorOtpCommand(string Token, string Otp) : IRequest<VendorPortalVerifyResult>, ITransactionalCommand;
