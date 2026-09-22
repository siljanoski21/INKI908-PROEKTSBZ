using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;

namespace TravelWeb.Pages
{
    public class RecommendationItem
    {
        public string Destination { get; set; } = "";
        public string Type { get; set; } = "";
        public string Climate { get; set; } = "";
        public int MinBudgetEUR { get; set; }
        public int MaxBudgetEUR { get; set; }
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        // Смештај
        public string HotelName { get; set; } = "";
        public string HotelType { get; set; } = "";
        public int HotelPrice { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly string _jsonPath = "knowledgebase.json";

        public RecommendationItem? TopRecommendation { get; set; }
        public List<RecommendationItem> OtherRecommendations { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Budget { get; set; } = 1000;

        [BindProperty(SupportsGet = true)]
        public string? SelectedType { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string? SelectedClimate { get; set; } = "All";

        public bool IsSearched { get; set; } = false;

        public void OnGet(int? budget, string? type, string? climate)
        {
            if (!budget.HasValue) return;

            IsSearched = true;
            Budget = budget;
            SelectedType = type ?? "All";
            SelectedClimate = climate ?? "All";

            if (!System.IO.File.Exists(_jsonPath)) return;

            string jsonContent = System.IO.File.ReadAllText(_jsonPath);
            JObject kb = JObject.Parse(jsonContent);
            JArray recommendations = (JArray)kb["Recommendations"]!;

            List<RecommendationItem> validMatches = new();

            foreach (JObject rec in recommendations)
            {
                string recType = rec["Type"]?.ToString() ?? "";
                string recClimate = rec["Climate"]?.ToString() ?? "";
                int recMin = rec["MinBudgetEUR"]?.ToObject<int>() ?? 0;
                int recMax = rec["MaxBudgetEUR"]?.ToObject<int>() ?? int.MaxValue;

                // Филтрирање: Ако е избрано "All", се занемарува тој филтер
                bool matchesBudget = budget >= recMin;
                bool matchesType = string.IsNullOrEmpty(type) || type.Equals("All", StringComparison.OrdinalIgnoreCase) || recType.Equals(type, StringComparison.OrdinalIgnoreCase);
                bool matchesClimate = string.IsNullOrEmpty(climate) || climate.Equals("All", StringComparison.OrdinalIgnoreCase) || recClimate.Equals(climate, StringComparison.OrdinalIgnoreCase);

                if (matchesBudget && matchesType && matchesClimate)
                {
                    var item = new RecommendationItem
                    {
                        Destination = rec["Destination"]?.ToString() ?? "",
                        Type = recType,
                        Climate = recClimate,
                        MinBudgetEUR = recMin,
                        MaxBudgetEUR = recMax,
                        Description = rec["Description"]?.ToString() ?? "",
                        ImageUrl = rec["Image"]?.ToString() ?? ""
                    };

                    var acc = rec["Accommodation"];
                    if (acc != null)
                    {
                        item.HotelName = acc["Name"]?.ToString() ?? "";
                        item.HotelType = acc["Type"]?.ToString() ?? "";
                        item.HotelPrice = acc["PricePerNightEUR"]?.ToObject<int>() ?? 0;
                    }

                    validMatches.Add(item);
                }
            }

            if (validMatches.Any())
            {
                // Најдобрата дестинација за тој буџет
                TopRecommendation = validMatches.OrderByDescending(r => r.MinBudgetEUR).First();

                // Сите останати достапни дестинации во ценовниот ранг
                OtherRecommendations = validMatches.Where(r => r != TopRecommendation).OrderBy(r => r.MinBudgetEUR).ToList();
            }
        }
    }
}