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
            }
            catch (SerpApiSearchException ex)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(ex.ToString());
            }
            return results ?? new JArray(); //retunrs results or empty array if no results found
        }

        /// <summary>
        /// Calls SerpAPI's Google Shopping Product endpoint to get real retailer offer links
        /// for a specific product identified by its SerpAPI product_id.
        /// Returns the "sellers_results" → "online_sellers" array.
        /// </summary>
        public async Task<JArray> GetProductOffersAsync(string productId)
        {
            Console.WriteLine("[GetProductOffersAsync] product_id: " + productId);
            string apiKey = "de03a78be02d283d9baed511d2e0e45f94c7219b695d180bb97e24a6adbdecdb";

            return await Task.Run(() =>
            {
                Hashtable ht = new Hashtable();
                ht.Add("engine", "google_shopping_product");
                ht.Add("product_id", productId);
                ht.Add("gl", "us");
                ht.Add("hl", "en");

                try
                {
                    GoogleSearch search = new GoogleSearch(ht, apiKey);
                    JObject data = search.GetJson();

                    // Response shape: data["sellers_results"]["online_sellers"]
                    var sellers = data["sellers_results"]?["online_sellers"] as JArray;
                    Console.WriteLine("[GetProductOffersAsync] Offer count: " + (sellers?.Count ?? 0));
                    return sellers ?? new JArray();
                }
                catch (SerpApiSearchException ex)
                {
                    Console.WriteLine("[GetProductOffersAsync] Exception: " + ex.ToString());
                    return new JArray();
                }
            });
        }
        
    }
}