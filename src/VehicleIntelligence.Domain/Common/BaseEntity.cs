namespace VehicleIntelligence.Domain.Common;

/// <summary>
/// Base entity class providing common identity and auditing properties.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
}
