using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using CS392_WebApplication.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using SerpApi;
using Newtonsoft.Json.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CS392_WebApplication.Pages.ProductPages.Catalog
{
    public class catalogListModel
    {
        public static readonly HashSet<string> AllowedSearches = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {

                "WRITING", "pens","pencils","markers","highlighters","chalk","crayons","ink tools","calligraphy tools","stationery","pen pencil marker",

                "PAPER", "paper","notebooks","notepads","cards","sticky notes","printer paper","construction paper","sketch paper","graph paper",

                "ORGANIZATION", "binders","folders","dividers","clips","bands","tabs","sheet protectors","document holders","file organizers",

                "CUTTING", "scissors","cutting tools","paper cutters","craft knives",

                "ADHESIVES", "glue","tape","adhesive dots","rubber cement","mounting adhesives",

                "CORRECTION", "erasers","correction fluid","correction tape","ink removers",

                "MEASURING", "rulers","protractors","compasses","measuring tools","geometry tools","drafting tools",

                "MATH TOOLS", "calculators","abacus","graphing tools","math templates",

                "ART SUPPLIES", "paints","brushes","pastels","charcoal","drawing tools","palettes","canvas","art boards",

                "SCIENCE SUPPLIES", "lab tools","lab containers","microscope accessories","dissection tools","science safety equipment",

                "PRESENTATION", "poster boards","display boards","presentation folders","flip charts","easels","presentation pads",

                "STUDY TOOLS", "flashcards","study guides","note cards","bookmarks","page markers",

                "STORAGE", "pencil cases","supply boxes","desk organizers","storage containers","portable organizers",

                "DIGITAL SCHOOL SUPPLIES", "flash drives","external storage","stylus tools","device cases","headphones",

                "BOARD SUPPLIES", "whiteboards","chalkboards","dry erase tools","board erasers",

                "CLASSROOM MANAGEMENT", "name tags","hall passes","assignment trays","classroom timers","reward charts",

                // MongoDB categories
                "Technology", "laptop","tablet","calculator","graphing calculator","flash drive",
                "external storage","mouse","headphones","charger","printer","device cases","stylus tools",

                "Basic Essentials", "water bottle","lunch box","backpack keychain","umbrella",
                "stickers","book cover","locker mirror",

                "stationery", "writing tools", "paper products", "notebooks", "binders", "folders", 
                "desk supplies", "art supplies", "coloring supplies", "geometry tools", "calculators", 
                "backpacks", "lunch supplies", "organization supplies", "adhesives", "cutting tools", 
                "classroom supplies", "study aids", "electronics", "labels"
        };
    
    }
}
