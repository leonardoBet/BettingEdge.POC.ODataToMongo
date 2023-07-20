using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Linq;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Mime;
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using Microsoft.Azure.Documents.OData.Sql;
using MongoDB.Bson;
using BettingEdge.POC.ODataToMongo.Models;
using BettingEdge.POC.ODataToMongo.Services;

namespace BettingEdge.POC.ODataToMongo.Controllers
{
    [ApiController]
	[Route("[controller]")]
	public class TodosController : ControllerBase
	{
		private const string ATLAS_URI = "mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass%20Community&ssl=false";
		IMongoCollection<TodoItem> todoCollection;
		Container cosmosContainer;

		public TodosController()
    {
	    var settings = MongoClientSettings.FromConnectionString(ATLAS_URI);
			settings.LinqProvider = LinqProvider.V3;
			settings.LoggingSettings = new LoggingSettings(LoggerFactory.Create(b =>
			{
				b.AddSimpleConsole();
				b.SetMinimumLevel(LogLevel.Debug);
			}));
			MongoClient mongoClient = new MongoClient(settings);
			todoCollection = mongoClient.GetDatabase("ODataPOCDatabase").GetCollection<TodoItem>("TodoList");

			// New instance of CosmosClient class
			CosmosClient comsClient = new(
				accountEndpoint: "https://localhost:8081/",
				"C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
			);

			Database database = comsClient.GetDatabase(id: "ODataPOCDatabase");
			cosmosContainer = database.GetContainer(id: "TodoList");

		}

		/// <summary>
		/// This is just a test controller to test OData queries against MongoDb.
		/// </summary>
		/// <returns></returns>
		[EnableQuery]
		[HttpGet("[action]")]
		public async Task<IActionResult> Control()
		{
			var collection = await todoCollection.FindAsync(_ => true);
			return Ok(collection.ToList());
		}

		/// <summary>
		/// This controller aims to test the ODataQueryBuilder using same configuration as BettingEdge API.
		/// </summary>
		/// <param name="pageSize"></param>
		/// <param name="pagePath"></param>
		/// <returns></returns>
		[HttpGet("[action]")]
		[Produces(MediaTypeNames.Application.Json)]
		[Consumes(MediaTypeNames.Application.Json)]
		[SwaggerOperation("GetTodos")]
		[SwaggerResponse(200, "Todos response", typeof(TodoItem))]
		public IActionResult Mongo(
			[FromQuery(Name = "pageSize")] int? pageSize = null,
			[FromQuery(Name = "pagePath")] string pagePath = null
		)
		{
			var context = GetQueryContext<TodoItem>();
			var oDataQueryOptions = new ODataQueryOptions<TodoItem>(context, HttpContext.Request);

			var odataBuilder = new ODataQueryBuilder<TodoItem>(oDataQueryOptions);

			var partialResult = odataBuilder.GetNonProjectableQueriesOnly().ApplyTo(todoCollection.AsQueryable());

			if (oDataQueryOptions.IsSupportedQueryOption("select") && !string.IsNullOrEmpty(oDataQueryOptions.RawValues.Select))
			{
				var list = new List<TodoItem>();
				foreach (TodoItem result in partialResult)//Forcing materialization of the query 
				{
					list.Add(result);
				}

				partialResult = odataBuilder.GetProjectableQueriesOnly().ApplyTo(list.AsQueryable());
			}


			return Ok(partialResult);
		}

		/// <summary>
		/// This controller aims to test the ODataQueryBuilder using same configuration as BettingEdge API.
		/// </summary>
		/// <param name="pageSize"></param>
		/// <param name="pagePath"></param>
		/// <returns></returns>
		[HttpGet("[action]")]
		[Produces(MediaTypeNames.Application.Json)]
		[Consumes(MediaTypeNames.Application.Json)]
		[SwaggerOperation("GetTodos")]
		[SwaggerResponse(200, "Todos response", typeof(TodoItem))]
		public IActionResult Cosmos(
			[FromQuery(Name = "pageSize")] int? pageSize = null,
			[FromQuery(Name = "pagePath")] string pagePath = null
		)
		{
			var context = GetQueryContext<TodoItem>();
			var oDataQueryOptions = new ODataQueryOptions<TodoItem>(context, HttpContext.Request);
			var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());

			var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL);
			var feed = cosmosContainer.GetItemQueryIterator<TodoItem>(sqlQuery);

			var list = new List<TodoItem>();
			while (feed.HasMoreResults)
			{
				FeedResponse<TodoItem> response = feed.ReadNextAsync().Result;

				// Iterate query results
				foreach (TodoItem item in response)
				{
					list.Add(item);
				}
			}
			return Ok(list);
		}

		/// <summary>
		/// Extracted from BettingEdge API as it is.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		static ODataQueryContext GetQueryContext<T>()
		{
			var type = typeof(T);
			ODataQueryContext result;
			var builder = new ODataConventionModelBuilder();
			var entityTypeConfiguration = builder.AddEntityType(type);
			builder.AddEntitySet(type.Name, entityTypeConfiguration);
			var edmModels = builder.GetEdmModel();
			result = new ODataQueryContext(edmModels, type, new ODataPath());

			return result;
		}
	}
}