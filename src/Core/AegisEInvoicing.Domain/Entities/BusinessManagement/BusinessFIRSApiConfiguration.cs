using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

public class BusinessFIRSApiConfiguration : AuditableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid FIRSApiConfigurationId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public FIRSApiConfiguration FIRSApiConfiguration { get; private set; } = null!;

    private BusinessFIRSApiConfiguration() { } // EF Constructor

    private BusinessFIRSApiConfiguration(
       Guid businessId,
       Guid firsApiConfigurationId)
    {
        BusinessId = businessId;
        FIRSApiConfigurationId = firsApiConfigurationId;
    }

    public static BusinessFIRSApiConfiguration Create(
       Guid businessId,
       Guid firsApiConfigurationId)
    {
        var businessFIRSConfig = new BusinessFIRSApiConfiguration(
            businessId,
            firsApiConfigurationId);

        return businessFIRSConfig;
    }

    public void Update(Guid firsApiConfigurationId)
    {
        FIRSApiConfigurationId = firsApiConfigurationId;
    }
}