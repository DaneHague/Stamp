namespace StampBlazor.Models;

public class CollectionMember
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public int UserId { get; set; }
    public CollectionRole Role { get; set; } = CollectionRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public User? User { get; set; }
}

public enum CollectionRole
{
    Owner = 1,
    Admin = 2,
    Member = 3
}