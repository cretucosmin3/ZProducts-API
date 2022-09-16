namespace ProductAPI.Models;

[DataContract]
public class Person
{
    [BsonId]
    [DataMember]
    public ObjectId Id { get; set; }

    [DataMember]
    public string Name { get; set; }
}