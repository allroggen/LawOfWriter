namespace LawOfWriter.DTO;

public class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public string? Name { get; set; }

    public string? Vorname { get; set; }

    public DateTime? Bday { get; set; }

    public string? Iban { get; set; }

    public DateTime? Created { get; set; }

    public string? Createdby { get; set; }

    public DateTime? Changed { get; set; }

    public string? Changedby { get; set; }

    public string? Nickname { get; set; }

    public bool IsGuest { get; set; }
}