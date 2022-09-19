namespace ProductAPI.DbContracts;

[DataContract]
public class User
{
    [BsonId]
    [DataMember]
    public ObjectId Id { get; set; }

    [DataMember]
    public string Email { get; set; } = string.Empty;

    [DataMember]
    public string Role { get; set; } = string.Empty;

    [DataMember]
    public byte[] PasswordHash { get; set; } = default!;

    [DataMember]
    public byte[] PasswordSalt { get; set; } = default!;

    [DataMember]
    public string RefreshToken { get; set; } = string.Empty;

    [DataMember]
    public DateTime TokenCreated { get; set; }

    [DataMember]
    public DateTime TokenExpires { get; set; }
}