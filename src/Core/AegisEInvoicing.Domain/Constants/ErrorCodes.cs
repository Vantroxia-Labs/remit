namespace AegisEInvoicing.Domain.Constants;

/// <summary>
/// Standardized error codes for consistent error handling across the application
/// Format: ERR_[CATEGORY]_[NUMBER]
/// </summary>
public static class ErrorCodes
{
    #region Authentication & Authorization Errors (ERR_AUTH_xxx)

    /// <summary>
    /// API key header is missing from the request
    /// </summary>
    public const string MISSING_API_KEY = "ERR_AUTH_001";

    /// <summary>
    /// API key is invalid or expired
    /// </summary>
    public const string INVALID_API_KEY = "ERR_AUTH_002";

    /// <summary>
    /// Required header is missing from the request
    /// </summary>
    public const string MISSING_REQUIRED_HEADER = "ERR_AUTH_003";

    /// <summary>
    /// Content-Type header is invalid or missing
    /// </summary>
    public const string INVALID_CONTENT_TYPE = "ERR_AUTH_004";

    /// <summary>
    /// User is not authenticated
    /// </summary>
    public const string UNAUTHORIZED = "ERR_AUTH_005";

    /// <summary>
    /// User does not have sufficient permissions
    /// </summary>
    public const string FORBIDDEN = "ERR_AUTH_006";

    #endregion

    #region Business Errors (ERR_BUS_xxx)

    /// <summary>
    /// Business entity not found
    /// </summary>
    public const string BUSINESS_NOT_FOUND = "ERR_BUS_001";

    /// <summary>
    /// Business is inactive or suspended
    /// </summary>
    public const string BUSINESS_INACTIVE = "ERR_BUS_002";

    /// <summary>
    /// Business does not have a valid FIRS Service ID
    /// </summary>
    public const string BUSINESS_SERVICE_ID_MISSING = "ERR_BUS_003";

    /// <summary>
    /// Business FIRS credentials not configured
    /// </summary>
    public const string BUSINESS_FIRS_NOT_CONFIGURED = "ERR_BUS_004";

    /// <summary>
    /// Business QR code keys not configured
    /// </summary>
    public const string BUSINESS_QR_KEYS_NOT_CONFIGURED = "ERR_BUS_005";

    /// <summary>
    /// Business certificate or public key missing
    /// </summary>
    public const string BUSINESS_CERTIFICATE_MISSING = "ERR_BUS_006";

    /// <summary>
    /// Business invoice prefix missing
    /// </summary>
    public const string BUSINESS_PREFIX_MISSING = "ERR_BUS_007";

    /// <summary>
    /// Access denied to the specified business
    /// </summary>
    public const string BUSINESS_ACCESS_DENIED = "ERR_BUS_008";

    #endregion

    #region Party Errors (ERR_PARTY_xxx)

    /// <summary>
    /// Party entity not found
    /// </summary>
    public const string PARTY_NOT_FOUND = "ERR_PARTY_001";

    /// <summary>
    /// Party with the same email already exists
    /// </summary>
    public const string PARTY_DUPLICATE_EMAIL = "ERR_PARTY_002";

    /// <summary>
    /// Party with the same Tax Identification Number already exists
    /// </summary>
    public const string PARTY_DUPLICATE_TIN = "ERR_PARTY_003";

    /// <summary>
    /// Invalid party data provided
    /// </summary>
    public const string PARTY_INVALID_DATA = "ERR_PARTY_004";

    #endregion

    #region Invoice Errors (ERR_INV_xxx)

    /// <summary>
    /// Invoice entity not found
    /// </summary>
    public const string INVOICE_NOT_FOUND = "ERR_INV_001";

    /// <summary>
    /// Invoice with duplicate IRN already exists
    /// </summary>
    public const string INVOICE_DUPLICATE_IRN = "ERR_INV_002";

    /// <summary>
    /// Invalid IRN format provided
    /// </summary>
    public const string INVALID_IRN_FORMAT = "ERR_INV_003";

    /// <summary>
    /// Business item not found
    /// </summary>
    public const string BUSINESS_ITEM_NOT_FOUND = "ERR_INV_004";

    /// <summary>
    /// Invoice amount exceeds maximum allowed limit
    /// </summary>
    public const string INVOICE_AMOUNT_EXCEEDED = "ERR_INV_005";

    /// <summary>
    /// Invoice date is invalid or out of acceptable range
    /// </summary>
    public const string INVOICE_DATE_INVALID = "ERR_INV_006";

    /// <summary>
    /// Invoice has no line items
    /// </summary>
    public const string INVOICE_NO_ITEMS = "ERR_INV_007";

    /// <summary>
    /// Invoice number already exists
    /// </summary>
    public const string INVOICE_NUMBER_EXISTS = "ERR_INV_008";

    /// <summary>
    /// Invoice is already validated
    /// </summary>
    public const string INVOICE_ALREADY_VALIDATED = "ERR_INV_009";

    /// <summary>
    /// Invoice is already signed
    /// </summary>
    public const string INVOICE_ALREADY_SIGNED = "ERR_INV_010";

    /// <summary>
    /// Invoice is already transmitted
    /// </summary>
    public const string INVOICE_ALREADY_TRANSMITTED = "ERR_INV_011";

    /// <summary>
    /// Business item category mismatch
    /// </summary>
    public const string BUSINESS_ITEM_CATEGORY_MISMATCH = "ERR_INV_012";

    /// <summary>
    /// Invalid currency code
    /// </summary>
    public const string INVALID_CURRENCY = "ERR_INV_013";

    #endregion

    #region Validation Errors (ERR_VAL_xxx)

    /// <summary>
    /// General validation failure
    /// </summary>
    public const string VALIDATION_FAILED = "ERR_VAL_001";

    /// <summary>
    /// Required field is missing
    /// </summary>
    public const string REQUIRED_FIELD_MISSING = "ERR_VAL_002";

    /// <summary>
    /// Invalid data format
    /// </summary>
    public const string INVALID_FORMAT = "ERR_VAL_003";

    /// <summary>
    /// Invalid email format
    /// </summary>
    public const string INVALID_EMAIL = "ERR_VAL_004";

    /// <summary>
    /// Invalid phone number format
    /// </summary>
    public const string INVALID_PHONE = "ERR_VAL_005";

    /// <summary>
    /// Invalid TIN format
    /// </summary>
    public const string INVALID_TIN = "ERR_VAL_006";

    /// <summary>
    /// Invalid date format or range
    /// </summary>
    public const string INVALID_DATE = "ERR_VAL_007";

    /// <summary>
    /// Invalid amount (must be greater than zero)
    /// </summary>
    public const string INVALID_AMOUNT = "ERR_VAL_008";

    /// <summary>
    /// Invalid quantity (must be greater than zero)
    /// </summary>
    public const string INVALID_QUANTITY = "ERR_VAL_009";

    #endregion

    #region Item Category Errors (ERR_CAT_xxx)

    /// <summary>
    /// Item category not found
    /// </summary>
    public const string ITEM_CATEGORY_NOT_FOUND = "ERR_CAT_001";

    /// <summary>
    /// Item category already exists
    /// </summary>
    public const string ITEM_CATEGORY_EXISTS = "ERR_CAT_002";

    #endregion

    #region System Errors (ERR_SYS_xxx)

    /// <summary>
    /// Internal server error
    /// </summary>
    public const string INTERNAL_ERROR = "ERR_SYS_001";

    /// <summary>
    /// Database operation failed
    /// </summary>
    public const string DATABASE_ERROR = "ERR_SYS_002";

    /// <summary>
    /// External service call failed
    /// </summary>
    public const string EXTERNAL_SERVICE_ERROR = "ERR_SYS_003";

    /// <summary>
    /// Service unavailable or timeout
    /// </summary>
    public const string SERVICE_UNAVAILABLE = "ERR_SYS_004";

    /// <summary>
    /// Configuration error
    /// </summary>
    public const string CONFIGURATION_ERROR = "ERR_SYS_005";

    #endregion

    #region FIRS Integration Errors (ERR_FIRS_xxx)

    /// <summary>
    /// FIRS API connection failed
    /// </summary>
    public const string FIRS_CONNECTION_FAILED = "ERR_FIRS_001";

    /// <summary>
    /// FIRS API returned an error
    /// </summary>
    public const string FIRS_API_ERROR = "ERR_FIRS_002";

    /// <summary>
    /// FIRS validation failed
    /// </summary>
    public const string FIRS_VALIDATION_FAILED = "ERR_FIRS_003";

    /// <summary>
    /// FIRS signing failed
    /// </summary>
    public const string FIRS_SIGNING_FAILED = "ERR_FIRS_004";

    /// <summary>
    /// FIRS transmission failed
    /// </summary>
    public const string FIRS_TRANSMISSION_FAILED = "ERR_FIRS_005";

    #endregion
}
