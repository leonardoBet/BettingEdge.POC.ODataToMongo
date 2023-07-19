using Microsoft.AspNetCore.OData.Query;
using System.Text;
using System;
using BenchmarkDotNet.Attributes;

namespace BettingEdge.POC.ODataToMongo
{
	[MemoryDiagnoser(false)]
	public class ODataQueryBuilder<TModel>
	{
		private readonly ODataQueryOptions<TModel> _oDataQueryOptions;
		private QueryString projectableQuerySet;
		private QueryString nonProjectableQuerySet;

		public ODataQueryBuilder(ODataQueryOptions<TModel> oDataQueryOptions)
		{
			_oDataQueryOptions = oDataQueryOptions;

			BreakDownQueryOptions(_oDataQueryOptions);
		}

		private void BreakDownQueryOptions(ODataQueryOptions<TModel> oDataQueryOptions)
		{
			const char QUERY_OPTIONS_PREFIX = '?';
			const char QUERY_OPTIONS_SEPARATOR = '&';
			const char QUERY_OPTIONS_DELIMITER = '=';
			const char QUERY_OPTION_EXPAND_DELIMITER = ',';

			var projectableOptions = new StringBuilder();
			var nonProjectableOptions = new StringBuilder();

			foreach (var e in oDataQueryOptions.Request.Query)
			{
				if (e.Key.StartsWith("$select") || e.Key.StartsWith("$expand"))//TODO: add more projectable options dynamically
				{
					projectableOptions.Append(QUERY_OPTIONS_SEPARATOR);
					projectableOptions.Append(e.Key);
					projectableOptions.Append(QUERY_OPTIONS_DELIMITER);
					projectableOptions.Append(e.Value);
				}
				else
				{
					nonProjectableOptions.Append(QUERY_OPTIONS_SEPARATOR);
					nonProjectableOptions.Append(e.Key);
					nonProjectableOptions.Append(QUERY_OPTIONS_DELIMITER);
					nonProjectableOptions.Append(e.Value);
				}
			}

			nonProjectableQuerySet = new QueryString($"{QUERY_OPTIONS_PREFIX}{nonProjectableOptions}");
			projectableQuerySet = new QueryString($"{QUERY_OPTIONS_PREFIX}{projectableOptions}");
		}

		[Benchmark]
		public ODataQueryOptions GetProjectableQueriesOnly()
		{
			var req = _oDataQueryOptions.Request;
			//var newUri = new Uri($"{req.Scheme}://{req.Host}{req.Path}{req.QueryString}");
			_oDataQueryOptions.Request.QueryString = projectableQuerySet;
			return (ODataQueryOptions)Activator.CreateInstance(_oDataQueryOptions.GetType(), _oDataQueryOptions.Context, req);
		}

		[Benchmark]
		public ODataQueryOptions GetNonProjectableQueriesOnly()
		{
			var req = _oDataQueryOptions.Request;
			//var newUri = new Uri($"{req.Scheme}://{req.Host}{req.Path}{req.QueryString}");
			_oDataQueryOptions.Request.QueryString = nonProjectableQuerySet;
			return (ODataQueryOptions)Activator.CreateInstance(_oDataQueryOptions.GetType(), _oDataQueryOptions.Context, req);
		}
	}
}
