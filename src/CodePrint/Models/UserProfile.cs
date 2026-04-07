namespace CodePrint.Models;

public enum MembershipTier { Free, Professional, Enterprise }

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string MaskedPhone => Phone.Length >= 7
        ? Phone[..3] + "****" + Phone[^4..]
        : Phone;
    public string? AvatarPath { get; set; }
    public MembershipTier Tier { get; set; } = MembershipTier.Free;
    public int TemplateCount { get; set; }
    public int TemplateLimit => Tier switch
    {
        MembershipTier.Free => 50,
        MembershipTier.Professional => 500,
        MembershipTier.Enterprise => int.MaxValue,
        _ => 50
    };
    public string? ReferralCode { get; set; }
}
