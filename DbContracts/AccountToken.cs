namespace ProductAPI.DbContracts;

[DataContract]
public class AccountToken
{
    [BsonId]
    [DataMember]
    public ObjectId Id { get; set; }

    [DataMember]
    public string AccessCode { get; set; } = default!;

    [DataMember]
    public string Role { get; set; } = "Admin";
}