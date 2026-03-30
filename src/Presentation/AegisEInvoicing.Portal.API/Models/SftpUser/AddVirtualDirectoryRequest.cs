namespace AegisEInvoicing.Portal.API.Models.SftpUser;

public class AddVirtualDirectoryRequest
{
    public string Name { get; set; } = null!;
    public string RelativePath { get; set; } = null!; // e.g., "INCOMING" or "custom/subdir"
    public bool CreatePhysical { get; set; } = true;  // create on disk using maintenance account first
}
