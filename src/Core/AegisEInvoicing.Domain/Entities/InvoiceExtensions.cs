namespace EInvoiceIntegrator.Domain.Entities;

public static class InvoiceExtensions
{
    public static void SetInvoiceDeliveryPeriod(this Invoice invoice, InvoiceDeliveryPeriod period)
    {
        var property = typeof(Invoice).GetProperty("InvoiceDeliveryPeriod")!;
        property.SetValue(invoice, period);
    }

    public static void SetDocumentReferences(this Invoice invoice,
        DocumentReference? dispatchRef = null,
        DocumentReference? receiptRef = null,
        DocumentReference? originatorRef = null,
        DocumentReference? contractRef = null)
    {
        var dispatchProp = typeof(Invoice).GetProperty("DispatchDocumentReference")!;
        dispatchProp.SetValue(invoice, dispatchRef);
        
        var receiptProp = typeof(Invoice).GetProperty("ReceiptDocumentReference")!;
        receiptProp.SetValue(invoice, receiptRef);
        
        var originatorProp = typeof(Invoice).GetProperty("OriginatorDocumentReference")!;
        originatorProp.SetValue(invoice, originatorRef);
        
        var contractProp = typeof(Invoice).GetProperty("ContractDocumentReference")!;
        contractProp.SetValue(invoice, contractRef);
    }

    public static void AddBillingReference(this Invoice invoice, DocumentReference reference)
    {
        var field = typeof(Invoice).GetField("_billingReferences", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<DocumentReference>)field.GetValue(invoice)!;
        list.Add(reference);
    }

    public static void AddAdditionalDocumentReference(this Invoice invoice, DocumentReference reference)
    {
        var field = typeof(Invoice).GetField("_additionalDocumentReferences", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<DocumentReference>)field.GetValue(invoice)!;
        list.Add(reference);
    }

    public static void AddPaymentMeans(this Invoice invoice, PaymentMeans paymentMeans)
    {
        var field = typeof(Invoice).GetField("_paymentMeans", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<PaymentMeans>)field.GetValue(invoice)!;
        list.Add(paymentMeans);
    }

    public static void AddAllowanceCharge(this Invoice invoice, AllowanceCharge allowanceCharge)
    {
        var field = typeof(Invoice).GetField("_allowanceCharges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<AllowanceCharge>)field.GetValue(invoice)!;
        list.Add(allowanceCharge);
    }

    public static void AddTaxTotal(this Invoice invoice, TaxTotal taxTotal)
    {
        var field = typeof(Invoice).GetField("_taxTotals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<TaxTotal>)field.GetValue(invoice)!;
        list.Add(taxTotal);
    }
}