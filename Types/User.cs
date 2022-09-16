namespace ProductAPI.Types;

[DataContract]
public class User
{
    [BsonId]
    [DataMember]
    public ObjectId Id { get; set; }

    [DataMember]
    public string Username { get; set; } = string.Empty;

    [DataMember]
    public byte[] PasswordHash { get; set; }

    [DataMember]
    public byte[] PasswordSalt { get; set; }

    [DataMember]
    public string RefreshToken { get; set; } = string.Empty;

    [DataMember]
    public DateTime TokenCreated { get; set; }

    [DataMember]
    public DateTime TokenExpires { get; set; }
}