using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

public class Party : AuditableAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string Description {  get; private set; } = null!;

    [EmailAddress]
    public string Email { get; private set; } = null!;
    public TIN TaxIdentificationNumber { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public Guid BusinessID { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    
    // Collections
    private readonly List<Invoice> _invoices = [];
    public IReadOnlyCollection<Invoice> Invoices => _invoices.AsReadOnly();

    // Private constructor for EF Core
    private Party() { }

    // Private constructor for factory methods
    private Party(
        string name,
        string phone,
        string email,
        TIN taxIdentificationNumber,
        Address address,
        Guid businessId,
        string description)
    {
        Name = name;
        Phone = phone;
        Email = email;
        TaxIdentificationNumber = taxIdentificationNumber;
        Address = address;
        BusinessID = businessId;
        Description = description;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a new Party with all required information
    /// </summary>
    public static Party Create(
        string name,
        string phone,
        string email,
        TIN taxIdentificationNumber,
        Address address,
        Guid businessId,
        string description)
    {
        ValidateRequiredFields(name, phone, email, taxIdentificationNumber, address, businessId, description);

        var party = new Party(name, phone, email, taxIdentificationNumber, address, businessId, description);

        // Add domain event if needed
        // party.AddDomainEvent(new PartyCreatedDomainEvent(party.Id, party.Name));

        return party;
    }

    /// <summary>
    /// Creates a new Party with Business entity reference
    /// </summary>
    public static Party CreateWithBusiness(
        string name,
        string phone,
        string email,
        TIN taxIdentificationNumber,
        Address address,
        Business business,
        string description)
    {
        if (business == null)
            throw new ArgumentNullException(nameof(business));

        var party = Create(name, phone, email, taxIdentificationNumber, address, business.Id, description);
        party.Business = business;

        return party;
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Updates the party's contact information
    /// </summary>
    public void UpdateContactInfo(string phone, string email)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        var oldPhone = Phone;
        var oldEmail = Email;

        Phone = phone;
        Email = email;
    }

    /// <summary>
    /// Updates the party's name
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty", nameof(newName));

        if (newName.Length > 200) // Assuming max length
            throw new ArgumentException("Name cannot exceed 200 characters", nameof(newName));

        var oldName = Name;
        Name = newName;
    }

    /// <summary>
    /// Updates the party's address
    /// </summary>
    public void UpdateAddress(Address newAddress)
    {
        if (newAddress == null)
            throw new ArgumentNullException(nameof(newAddress));

        var oldAddress = Address;
        Address = newAddress;
    }

    /// <summary>
    /// Updates the tax identification number
    /// </summary>
    public void UpdateTaxIdentificationNumber(TIN newTIN)
    {
        if (newTIN == null)
            throw new ArgumentNullException(nameof(newTIN));

        var oldTIN = TaxIdentificationNumber;
        TaxIdentificationNumber = newTIN;
    }
   

    /// <summary>
    /// Checks if the party belongs to the specified business
    /// </summary>
    public bool BelongsToBusiness(Guid businessId)
    {
        return BusinessID == businessId;
    }
    

    /// <summary>
    /// Gets the party's full display name with business context
    /// </summary>
    public string GetDisplayName()
    {
        return Business != null ? $"{Name} ({Business.Name})" : Name;
    }   

    #endregion

    #region Private Helper Methods

    private static void ValidateRequiredFields(
        string name,
        string phone,
        string email,
        TIN taxIdentificationNumber,
        Address address,
        Guid businessId,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required", nameof(phone));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (taxIdentificationNumber is null)
            throw new ArgumentNullException(nameof(taxIdentificationNumber));

        if (address is null)
            throw new ArgumentNullException(nameof(address));

        if(description is null)
            throw new ArgumentException("Description is required", nameof(description));

        if (businessId == Guid.Empty)
            throw new ArgumentException("Business ID cannot be empty", nameof(businessId));
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
