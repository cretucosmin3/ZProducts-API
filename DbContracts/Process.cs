namespace ProductAPI.DbContracts;

[DataContract]
public class Process
{
    [BsonId, DataMember]
    public ObjectId Id { get; set; }

    [DataMember]
    public string Name { get; set; } = default!;

    [DataMember]
    public string ProgressText { get; set; } = default!;

    [DataMember]
    public float Progress { get; set; } = 0;

    [DataMember]
    public float Duration { get; set; } = 0;

    [DataMember]
    public bool Finished { get; set; } = false;

    [DataMember]
    public bool HasError { get; set; } = false;

    [DataMember]
    public string ErrorMessage { get; set; } = default!;

    [DataMember]
    public DateTime StartedAt { get; set; }

    [DataMember]
    public DateTime FinishedAt { get; set; }
}