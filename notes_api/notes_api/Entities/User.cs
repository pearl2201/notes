using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotesApi.Entities;

public class User : BaseAuditableEntity
{


    [Key]
    public int Id { get; set; }
    [MinLength(8)]
    public string UserName { get; set; }
    [JsonIgnore]
    [MinLength(8)]
    public string PasswordHash { get; set; }
    [MinLength(8)]
    public string DisplayName { get; set; }

    public string Avatar { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [JsonIgnore]
    public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

}