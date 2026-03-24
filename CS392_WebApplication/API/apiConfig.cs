// dotnet add package google-search-results-dotnet
using System;
using System.Collections;
using SerpApi;
using Newtonsoft.Json.Linq;

//FREE TRIAL ONLY INCLUDED 250 MAX SEARCHES PER MONTH, DONT ABUSE
//serpapi.com
//Use entity FC to store API results in DB, then display on frontend, saves searches and makes quicker search

namespace CS392_WebApplication.API
{
    public class apiConfig
    {
        public JArray SearchProducts(string query)
        {
            Console.WriteLine("[SearchProducts] Called with query: " + query);
            String apiKey = "de03a78be02d283d9baed511d2e0e45f94c7219b695d180bb97e24a6adbdecdb";

            Hashtable ht = new Hashtable();
            ht.Add("engine", "google_shopping"); //search engine
            ht.Add("q", query); //search query 
            ht.Add("gl", "us"); //country
            ht.Add("hl", "en"); //language
            //ht.Add is parameters that tell API
            // what kind of search to run
            //EX: ht.Add("ParamterName", "ParameterValue");
            //Other examples 
            //ht.Add("location", "Austin, Texas, United States"); //location for localized results
            //ht.Add("google_domain", "google.com"); //google domain 

            JObject data = null;
            JArray results = null;
            try
            {
                GoogleSearch search = new GoogleSearch(ht, apiKey); //create search object with parameters and API key
                data = search.GetJson(); //sends request and returns response in JSON format
                results = (JArray)data["shopping_results"]; //reads results
                Console.WriteLine("[SearchProducts] Result count: " + (results?.Count ?? 0));

                /*foreach (JObject result in results ?? new JArray())
                {
                    var title = (result["title"]);      
                    var description = (result["description"]);              
                    var price = (result["extracted_price"]);
                    var link = (result["serpapi_link"]);
                    var image = (result["thumbnail"]);
                    var source = (result["source"]);
                    var sourceLogo = (result["source_logo"]);
                    var rating = (result["rating"]);
                    var reviews = (result["reviews"]);
                    //loop extracts each result
                }*/
            }
            catch (SerpApiSearchException ex)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(ex.ToString());
            }
            return results ?? new JArray(); //retunrs results or empty array if no results found
        }
        
    }
}