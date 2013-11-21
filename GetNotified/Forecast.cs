using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetNotified
{
    class Forecast
    {
        public string query { get; set; } // cityname, countryname
        public string desc { get; set; }
        public string temp_C { get; set; }
        public string cloudCoverage { get; set; }
        public string weatherIconUrl { get; set; }
        public string windspeedKmph { get; set; }
        public string humidity { get; set; }
    }

    public class ForecastTileObject
    {
        public string cityName { get; set; }
        public string temperature { get; set; }
        public string humidity { get; set; }
        public string windspeed { get; set; }
        public string cloudCoverage { get; set; }
        public string weatherIconUrl { get; set; }
        public string desc { get; set; }
    }
}
