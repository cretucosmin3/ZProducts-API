namespace ProductAPI.DbContracts;

[DataContract]
public class Site
{
    [BsonId, DataMember]
    public ObjectId Id { get; set; }

    [DataMember]
    public string IndexParent { get; set; } = default!;

    [DataMember]
    public string Domain { get; set; } = default!;

    [DataMember]
    public string Url { get; set; } = default!;

    [DataMember]
    public float Price { get; set; }

    [DataMember]
    public Dictionary<string, float> PriceHistory { get; set; } = default!;

    [DataMember]
    public DateTime LastUpdate { get; set; }
}