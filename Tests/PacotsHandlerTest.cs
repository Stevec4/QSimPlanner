using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{

    [TestClass()]
	public class PacotsHandlerTest
	{

		//[TestMethod()]
		//public void LoadPacotsWpts()
		//{
		//	PrepareTest();

		//	PacotsHandler pacotsManager = new PacotsHandler();
		//	pacotsManager.GetAllTracks();
		//	pacotsManager.AddToWptList();

		//}

		//[TestMethod()]
		//public void LoadAndFindWpts()
		//{
		//	LoadPacotsWpts();

		//	const string orig = "RCTP";
		//	const string dest = "KSFO";

		//	string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
		//	string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).Last();

		//	var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

		//	string rte = genRoute.ToString(Route.RouteDisplayOption.AirportToAirport);
		//	Debug.WriteLine("Generated route: " + rte);
		//	Debug.WriteLine("Distance = {0} nm\n", genRoute.TotalDis);


		//}

	}
}
