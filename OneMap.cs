using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Rock.Address;

namespace sg.carmel.Address.OneMap
{
    /// <summary>
    /// Singapore Address Standardization and Geocoding service from <a href="https://onemap.sg/">OneMap</a>.
    /// </summary>
    [Description( "Singapore Address Standardization and Geocoding service from OneMap" )]
    [Export( typeof( VerificationComponent ) )]
    [ExportMetadata( "ComponentName", "OneMap" )]
    public class OneMap : VerificationComponent
    {
        private const String UnitNumberRegex = @"#\d+-\w+";

        /// <summary>
        /// Gets a value indicating whether OneMap supports standardization.
        /// </summary>
        public override bool SupportsStandardization => true;

        /// <summary>
        /// Gets a value indicating whether OneMap supports geocoding.
        /// </summary>
        public override bool SupportsGeocoding => true;

        /// <summary>
        /// Verifies a given location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="resultMsg">The result message.</param>
        /// <returns>Result of the verification.</returns>
        public override VerificationResult Verify( Rock.Model.Location location, out string resultMsg )
        {
            if ( location.Country != "SG" || string.IsNullOrEmpty( location.PostalCode ) )
            {
                resultMsg = "No match";
                return VerificationResult.None;
            }

            var query = String.Format( "{0} {1} Singapore {2}", location.Street1, location.Street2, location.PostalCode );

            // Remove unit number from the query since OneMap does return results with it.
            query = Regex.Replace( query, UnitNumberRegex, "" );

            // Remove "blk" from query since OneMap does not return results with it.
            query = Regex.Replace( query, @"\bblk\b", "", RegexOptions.IgnoreCase );

            var client = new RestClient( "https://developers.onemap.sg" );
            var request = new RestRequest( "commonapi/search" )
                .AddParameter( "returnGeom", "Y" )
                .AddParameter( "getAddrDetails", "Y" )
                .AddParameter( "searchVal", query );
            var restResponse = client.Get( request );

            if ( restResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                resultMsg = restResponse.StatusDescription;
                return VerificationResult.ConnectionError;
            }

            var result = JObject.Parse( restResponse.Content )["results"]
                .Children()
                .Select( r => r.ToObject<OneMapSearchResult>() )
                .Where( r => r.Postal.Equals( location.PostalCode.Trim() ) )
                .FirstOrDefault();

            if ( result == null )
            {
                resultMsg = "No match";
                return VerificationResult.None;
            }

            var textInfo = CultureInfo.CurrentCulture.TextInfo;

            // Extract unit number and add it to Street2.
            var matches = new Regex( UnitNumberRegex ).Matches( String.Format( "{0} {1}", location.Street1, location.Street2 ) );
            location.Street2 = matches.Count == 1 ? matches[0].Value : null;

            location.PostalCode = result.Postal;
            location.Street1 = String.Format( "{0} {1}", result.BlockNumber.ToUpper(), textInfo.ToTitleCase( result.RoadName.ToLower() ) );
            location.County = null;
            location.City = null;
            location.State = null;
            location.SetLocationPointFromLatLong( result.Latitude, result.Longitude );

            resultMsg = "Match";
            return VerificationResult.Standardized | VerificationResult.Geocoded;
        }
    }

    class OneMapSearchResult
    {
        [JsonProperty( "BLK_NO", Required = Required.Always )]
        public string BlockNumber { get; set; }

        [JsonProperty( "ROAD_NAME", Required = Required.Always )]
        public string RoadName { get; set; }

        [JsonProperty( "POSTAL", Required = Required.Always )]
        public string Postal { get; set; }

        [JsonProperty( "LATITUDE", Required = Required.Always )]
        public double Latitude { get; set; }

        [JsonProperty( "LONGITUDE", Required = Required.Always )]
        public double Longitude { get; set; }
    }
}