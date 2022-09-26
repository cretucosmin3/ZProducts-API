namespace ProductAPI.DbContracts;

[DataContract]
public class SearchIndex
{
    [BsonId, DataMember]
    public ObjectId Id { get; set; }

    [DataMember]
    public string TextToSearch { get; set; } = default!;

    [DataMember]
    public string TitleKeywords { get; set; } = default!;

    [DataMember]
    public double MaxSites { get; set; }

    [DataMember]
    public bool UseGoogle { get; set; }

    [DataMember]
    public bool UseYahoo { get; set; }

    [DataMember]
    public bool UseBing { get; set; }

    [DataMember]
    public int SitesIndexed { get; set; }

    [DataMember]
    public DateTime LastUpdate { get; set; }
}