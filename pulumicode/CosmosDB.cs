using Pulumi.Azure.Core;
using Pulumi.Azure.CosmosDB;
using Pulumi.Azure.CosmosDB.Inputs;

using Random = Pulumi.Random;

namespace pulumicode
{
	public class CosmosDB
	{
		private readonly ResourceGroup _resourceGroup;
		private readonly string _location = "EastUS2";

		// CentralUS, SouthCentralUS, NorthCentralUS, WestUS, EastUS, EastUS2

		public CosmosDB(ResourceGroup resourceGroup)
		{
			_resourceGroup = resourceGroup;
		}


		internal void BuildCosmosDb()
		{
			//var ri = new Random.RandomInteger("ri", new Random.RandomIntegerArgs
			//{
			//	Min = 10000,
			//	Max = 99999,
			//});

			var db = new Account("db", new AccountArgs
			{
				Location = _location,
				ResourceGroupName = _resourceGroup.Name,
				OfferType = "Standard",
				Kind = "GlobalDocumentDB",
				EnableAutomaticFailover = false,
				EnableFreeTier = false,
				EnableMultipleWriteLocations = false,
				ConsistencyPolicy = new AccountConsistencyPolicyArgs
				{
					ConsistencyLevel = "Session",
					MaxIntervalInSeconds = 5,
					MaxStalenessPrefix = 100,
				},
				GeoLocations =
				{
					new AccountGeoLocationArgs
					{
						Location = _location,
						FailoverPriority = 0,
					},
				},
				
			});
		}
	}
}
