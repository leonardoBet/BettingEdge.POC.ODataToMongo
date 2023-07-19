using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Linq;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Mime;

namespace BettingEdge.POC.ODataToMongo.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class TodosController : ControllerBase
	{
		private const string ATLAS_URI = "mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass%20Community&ssl=false";
		MongoClient client;
		IMongoCollection<TodoItem> todoCollection;

		public TodosController()
    {
	    var settings = MongoClientSettings.FromConnectionString(ATLAS_URI);
			settings.LinqProvider = LinqProvider.V3;
			settings.LoggingSettings = new LoggingSettings(LoggerFactory.Create(b =>
			{
				b.AddSimpleConsole();
				b.SetMinimumLevel(LogLevel.Debug);
			}));
			client = new MongoClient(settings);
			todoCollection = client.GetDatabase("ODataPOCDatabase").GetCollection<TodoItem>("TodoList");
    }

		/// <summary>
		/// This is just a test controller to OData queries against MongoDb.
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
		public IActionResult Test1(
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
				foreach (TodoItem result in partialResult)
				{
					list.Add(result);
				}

				partialResult = odataBuilder.GetProjectableQueriesOnly().ApplyTo(list.AsQueryable());
			}

			return Ok(partialResult);
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