using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.OData.UriParser;

namespace BettingEdge.POC.ODataToMongo
{
	public class CustomEnableQueryAttribute : EnableQueryAttribute
	{
		public override void ValidateQuery(HttpRequest request, ODataQueryOptions queryOptions)
		{
			if (queryOptions.Filter != null)
			{
				queryOptions.Filter.Validator = new MyFilterValidator();
			}

			if (queryOptions.SelectExpand != null)
			{
				queryOptions.SelectExpand.Validator = new MySelectExpandValidator();
			}

			base.ValidateQuery(request, queryOptions);
		}

	}

	public class MySelectExpandValidator : SelectExpandQueryValidator
	{

		public override void Validate(SelectExpandQueryOption filterOption, ODataValidationSettings validationSettings)
		{
			ValidateRangeVariable(filterOption.SelectExpandClause, validationSettings);

			base.Validate(filterOption, validationSettings);
		}

		protected void ValidateRangeVariable(SelectExpandClause rangeVariable, ODataValidationSettings settings)
		{
			// Add your custom logic to Validate RangeVariable
		}

	}

	public class MyFilterValidator : FilterQueryValidator
	{


		public override void Validate(FilterQueryOption filterOption, ODataValidationSettings validationSettings)
		{
			ValidateRangeVariable(filterOption.FilterClause.RangeVariable, validationSettings);

			base.Validate(filterOption, validationSettings);
		}


		protected void ValidateRangeVariable(RangeVariable rangeVariable, ODataValidationSettings settings)
		{
			// Add your custom logic to Validate RangeVariable
		}

	}
}
