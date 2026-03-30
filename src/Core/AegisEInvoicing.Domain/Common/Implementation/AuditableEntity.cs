namespace AegisEInvoicing.Domain.Common.Implementation;

/// <summary>
/// Base class for auditable entities
/// </summary>
public abstract class AuditableEntity : Entity
{
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Factory method for creating a new entity
    protected static T CreateNew<T>(Guid createdBy) where T : AuditableEntity, new()
    {
        return new T
        {
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false
        };
    }

    // Factory method for creating with explicit timestamp
    protected static T CreateNew<T>(Guid createdBy, DateTimeOffset createdAt) where T : AuditableEntity, new()
    {
        return new T
        {
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            IsDeleted = false
        };
    }

    public void MarkAsCreated(Guid createdBy)
    {
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        IsDeleted = false;
    }

    // Instance method to mark as updated
    public void MarkAsUpdated(Guid? updatedBy = null)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Instance method to mark as updated with explicit timestamp
    public void MarkAsUpdated(Guid? updatedBy, DateTimeOffset updatedAt)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    // Instance method to soft delete
    public void MarkAsDeleted(Guid? deletedBy = null)
    {
        DeletedBy = deletedBy;
        DeletedAt = DateTimeOffset.UtcNow;
        IsDeleted = true;
    }

    // Instance method to soft delete with explicit timestamp
    public void MarkAsDeleted(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        DeletedBy = deletedBy;
        DeletedAt = deletedAt;
        IsDeleted = true;
    }

    // Instance method to restore (undo soft delete)
    public void MarkAsRestored()
    {
        DeletedBy = null;
        DeletedAt = null;
        IsDeleted = false;
    }

    // Check if entity has been modified
    public bool HasBeenModified => UpdatedAt.HasValue;
}

