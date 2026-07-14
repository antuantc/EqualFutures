using Microsoft.AspNetCore.Identity;

namespace EqualFutures.Web.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    /// <summary>When the account was created. Used by admin tooling to triage registration issues.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

