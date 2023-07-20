using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.OData.Query;
using System.Text;

namespace BettingEdge.POC.ODataToMongo.Services
{
    [MemoryDiagnoser(true)]
    public class ODataQueryBuilder<TModel>
    {
        private readonly ODataQueryOptions<TModel> _oDataQueryOptions;
        private QueryString projectableQuerySet;
        private QueryString nonProjectableQuerySet;

        public ODataQueryBuilder(ODataQueryOptions<TModel> oDataQueryOptions)
        {
            _oDataQueryOptions = oDataQueryOptions;

            BreakDownQueryOptions();
        }

        [Benchmark]
        public void BreakDownQueryOptions()
        {
            const char QUERY_OPTIONS_PREFIX = '?';
            const char QUERY_OPTIONS_SEPARATOR = '&';
            const char QUERY_OPTIONS_DELIMITER = '=';
            const char QUERY_OPTION_EXPAND_DELIMITER = ',';

            var projectableOptions = new StringBuilder();
            var nonProjectableOptions = new StringBuilder();

            foreach (var e in _oDataQueryOptions.Request.Query)
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
        public ODataQueryOptions<TModel> GetProjectableQueriesOnly()
        {
            var context = _oDataQueryOptions.Context;
            var request = _oDataQueryOptions.Request;
            request.QueryString = projectableQuerySet;
            return (ODataQueryOptions<TModel>)Activator.CreateInstance(_oDataQueryOptions.GetType(), context, request);
        }

        [Benchmark]
        public ODataQueryOptions GetNonProjectableQueriesOnly()
        {
            var context = _oDataQueryOptions.Context;
            var request = _oDataQueryOptions.Request;
            request.QueryString = nonProjectableQuerySet;
            return (ODataQueryOptions<TModel>)Activator.CreateInstance(_oDataQueryOptions.GetType(), context, request);
        }
    }
}
