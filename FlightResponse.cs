using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightDealTracker
{
    public class Rootobject
    {
        public bool status { get; set; }
        public long timestamp { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public Context context { get; set; }
        public Result[] results { get; set; }
    }

    public class Context
    {
        public string status { get; set; }
        public string sessionId { get; set; }
        public int totalResults { get; set; }
    }

    public class Result
    {
        public string id { get; set; }
        public string type { get; set; }
        public Content content { get; set; }
    }

    public class Content
    {
        public Location location { get; set; }
        public Flightquotes flightQuotes { get; set; }
        public Image image { get; set; }
        public Flightroutes flightRoutes { get; set; }
    }

    public class Location
    {
        public string id { get; set; }
        public string skyCode { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public Continent continent { get; set; }
    }

    public class Continent
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class Flightquotes
    {
        public Cheapest cheapest { get; set; }
        public Direct direct { get; set; }
    }

    public class Cheapest
    {
        public string price { get; set; }
        public int rawPrice { get; set; }
        public bool direct { get; set; }
    }

    public class Direct
    {
        public string price { get; set; }
        public int rawPrice { get; set; }
        public bool direct { get; set; }
    }

    public class Image
    {
        public string url { get; set; }
    }

    public class Flightroutes
    {
        public bool directFlightsAvailable { get; set; }
    }

}

