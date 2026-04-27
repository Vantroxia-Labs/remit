namespace AegisEInvoicing.Domain.Constants;

public static class ResponseMessages
{
    // Success Messages
    public const string INVOICE_CREATED_SUCCESS = "Invoice created successfully";
    public const string INVOICE_APPROVED_SUCCESS = "Invoice approved successfully";
    public const string INVOICE_UPDATED_SUCCESS = "Invoice updated successfully";
    public const string INVOICE_DELETED_SUCCESS = "Invoice deleted successfully";
    public const string INVOICE_SENT_SUCCESS = "Invoice sent successfully";
    public const string PAYMENT_RECORDED_SUCCESS = "Payment recorded successfully";
    public const string PARTY_CREATED_SUCCESS = "Party created successfully";
    public const string PARTY_UPDATED_SUCCESS = "Party updated successfully";
    public const string BUSINESS_CREATED_SUCCESS = "Business created successfully";
    public const string BUSINESS_UPDATED_SUCCESS = "Business updated successfully";

    public const string INVOICE_ALREADY_VALIDATED = "Invoice Already Validated";

    public const string INVOICE_ALREADY_SIGNED = "Invoice Already Signed";

    public const string INVOICE_ALREADY_TRANSMITTED = "Invoice Already Transmitted";

    //SystemIntegrator
    public const string GENERATE_IRN_SUCCESS = "IRN generated successfully";
    public const string GENERATE_IRN_FAILED = "IRN generation failed";
    public const string GENERATE_QR_CODE_SUCCESS = "QRCode generated successfully";
    public const string GENERATE_QR_CODE_FAILED = "QRCode generation failed";
    public const string INVALID_IRN_FORMAT = "IRN Format is invalid";

    // Error Messages
    public const string INVOICE_NOT_FOUND = "Invoice not found";
    public const string INVOICE_NOT_FOUND_VALIDATED = "Invoice not found or already validated";
    public const string INVOICE_NOT_FOUND_SIGNED = "Invoice not found or already signed";
    public const string INVOICE_NOT_FOUND_TRANSMITTED = "Invoice not found or already transmitted";
    public const string SERVICE_ID_NOT_FOUND = "Service ID for business is not configured. Please contact System Administrator";
    public const string BUSINESS_NOT_FOUND = "Business not found";
    public const string BUSINESS_SERIVCE_CODE_MISSING = "FIR service code is missing";
    public const string BUSINESS_INVOICE_PREFIX_MISSING = "Invoice Prefix is missing from Business configuration";
    public const string BUSINESS_FIRS_CREDENTIALS_NOT_CONFIGURED = "FIRS credentials for business not configured";
    public const string BUSINESS_QR_CODE_KEYS_NOT_CONFIGURED = "QR Code Keys for business not configured";
    public const string BUSINESS_QR_CODE_KEYS_CONFIGURED = "QR Code Keys for business configured";
    public const string PARTY_NOT_FOUND = "Party not found";
    public const string INVALID_TIN_OR_NOT_ENROLLED = "Invalid Buyer TIN or Buyer has not been enrolled on the MBS portal";
    public const string B2C_INVOICE_CANNOT_BE_TRANSMITTED = "B2C invoices cannot be transmitted to the NRS portal";
    public const string B2C_INVOICE_TRANSMISSION_SKIPPED = "B2C invoice validated and signed successfully. Transmission to NRS is not required for B2C invoices";
    public const string TIN_VALID_AND_ENROLLED = "TIN is valid and buyer is enrolled on the MBS portal";
    public const string BUSINESS_ITEM_NOT_FOUND = "Business Item not found";
    public const string INVALID_INVOICE_DATA = "Empty/Invalid invoice data provided";
    public const string INVALID_CUSTOMER_DATA = "Invalid customer data provided";
    public const string INVOICE_ALREADY_PAID = "Invoice is already marked as paid";
    public const string INVOICE_ALREADY_SENT = "Invoice has already been sent";
    public const string UNAUTHORIZED_ACCESS = "Unauthorized access to this resource";
    public const string INSUFFICIENT_PERMISSIONS = "Insufficient permissions to perform this action";

    // Validation Messages
    public const string REQUIRED_FIELD_MISSING = "Required field is missing: {0}";
    public const string INVALID_EMAIL_FORMAT = "Invalid email format";
    public const string INVALID_DATE_FORMAT = "Invalid date format";
    public const string INVALID_AMOUNT = "Amount must be greater than zero";
    public const string INVALID_TAX_RATE = "Tax rate must be between 0 and 100";
    public const string INVOICE_NUMBER_EXISTS = "Invoice number already exists";
    public const string INVOICE_VALIDATION_SUCCESSFUL = "Invoice validated successfully";
    public const string INVOICE_VALIDATION_FAILED = "Invoice validation failed";
    public const string INVOICE_SIGNING_SUCCESSFUL = "Invoice signed successfully";
    public const string INVOICE_NOT_SIGNED = "Invoice Not Signed";
    public const string INVOICE_SIGNING_FAILED = "Invoice signing failed";
    public const string INVOICE_TRANSMISSION_SUCCESSFUL = "Invoice transmitted successfully";
    public const string INVOICE_TRANSMISSION_FAILED = "Invoice transmission failed";
    public const string CUSTOMER_EMAIL_EXISTS = "Customer with this email already exists";
    public const string LOGIN_SUCCESSFUL = "Login successful";
    public const string LOGIN_FAILURE = "EmailAddress/Password is incorrect";
    public const string ACCOUNT_LOCKED = "Account Is Locked";

    // General Messages
    public const string OPERATION_FAILED = "Operation failed. Please try again. Please contact Administrator if problem persists";
    public const string OPERATION_SUCCESSFUL = "Operation Successful";
    public const string OPERATION_PROCESSING = "Operation Processing";
    public const string CREATED = "Created successfully";
    public const string INTERNAL_SERVER_ERROR = "An internal server error occurred";
    public const string DATA_RETRIEVED_SUCCESS = "Data retrieved successfully";
    public const string NO_DATA_FOUND = "No data found";
    public const string INVALID_REQUEST_FORMAT = "Invalid request format";
}
