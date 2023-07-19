using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BettingEdge.POC.ODataToMongo
{

	[BsonIgnoreExtraElements]
	public record TodoItem
	{
		[BsonId]
		[BsonRepresentation(BsonType.String)]
		public string id { get; set; }

		[BsonElement("title")]
		public string title { get; set; } = string.Empty;

		[BsonElement("isCompleted")]
		public bool isCompleted { get; set; }
	}
}
