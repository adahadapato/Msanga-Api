namespace Msanga.Domain.Entities;

/// <summary>
/// Another name a school is known by (table CommunityAliases): "FGCIE", "UNILAG", "KC Lagos". Added only through
/// <see cref="Community.AddAlias"/>.
/// </summary>
public sealed class CommunityAlias : Entity<Guid>
{
    /// <summary>Required by EF Core.</summary>
    private CommunityAlias() { }

    internal CommunityAlias(Guid communityId, string name, string searchKey) : base(Guid.CreateVersion7())
    {
        CommunityId = communityId;
        Name = name;
        SearchKey = searchKey;
    }

    /// <summary>The school it belongs to.</summary>
    public Guid CommunityId { get; private set; }

    /// <summary>The name, as people write it ("FGCIE").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Its search key ("fgcie").</summary>
    public string SearchKey { get; private set; } = string.Empty;
}
