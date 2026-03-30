namespace AegisEInvoicing.FIRSAccessPoint.Exceptions;

public sealed class DecryptionException : Exception
{
    public DecryptionException(string message) : base(message) { }
    public DecryptionException(string message, Exception inner) : base(message, inner) { }
}