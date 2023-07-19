using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BettingEdge.POC.ODataToMongo
{

	[BsonIgnoreExtraElements]
	public record TodoItem
	{
		[BsonId]
		[BsonRepresentation(BsonType.String)]
		public string Id { get; set; }

		[BsonElement("title")]
		public string Title { get; set; } = string.Empty;

		[BsonElement("isCompleted")]
		public bool IsCompleted { get; set; }
	}
}
