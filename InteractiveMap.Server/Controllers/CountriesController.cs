using InteractiveMap.Infrastructure;
using InteractiveMap.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InteractiveMap.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CountriesController> _logger;

        public CountriesController(IHttpClientFactory httpClientFactory, ILogger<CountriesController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet(Name = "GetAllCountries")]
        public async Task<ActionResult<IEnumerable<Country>>> Get(JsonHelper jsonHelper)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://restcountries.com/v3.1/all?fields=name,cca2,capital,region,population,flag,latlng,languages");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch countries from REST Countries API");
                    return StatusCode(500, "Failed to fetch countries data");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var rawCountries = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent, options);

                if (rawCountries == null || !rawCountries.Any())
                {
                    return Ok(new List<Country>());
                }

                var countries = new List<Country>();

                foreach (var item in rawCountries)
                {
                    try
                    {
                        var country = new Country
                        {
                            Name = jsonHelper.GetJsonStringValue(item, "name", "common") ?? "Unknown",
                            Code = jsonHelper.GetJsonStringValue(item, "cca2") ?? string.Empty,
                            Capital = jsonHelper.GetJsonArrayFirstString(item, "capital") ?? "N/A",
                            Region = jsonHelper.GetJsonStringValue(item, "region") ?? "Unknown",
                            Population = jsonHelper.GetJsonLongValue(item, "population"),
                            Flag = jsonHelper.GetJsonStringValue(item, "flag") ?? "🌍",
                            Latitude = jsonHelper.GetJsonDoubleValue(item, "latlng", 0),
                            Longitude = jsonHelper.GetJsonDoubleValue(item, "latlng", 1)
                        };

                        // Extract languages
                        var languagesObj = item.GetProperty("languages");
                        var languages = new List<string>();
                        if (languagesObj.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var lang in languagesObj.EnumerateObject())
                            {
                                languages.Add(lang.Value.GetString() ?? string.Empty);
                            }
                        }
                        country.Languages = languages.ToArray();

                        countries.Add(country);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error parsing country data: {ex.Message}");
                        continue;
                    }
                }

                return Ok(countries.OrderBy(c => c.Name).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching countries: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching countries");
            }
        }

        [HttpGet("{code}", Name = "GetCountryByCode")]
        public async Task<ActionResult<Country>> GetByCode(JsonHelper jsonHelper, string code)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"https://restcountries.com/v3.1/alpha/{code}?fields=name,cca2,capital,region,population,flag,latlng,languages");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound($"Country with code '{code}' not found");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var rawCountries = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent, options);

                if (rawCountries == null || !rawCountries.Any())
                {
                    return NotFound();
                }

                var item = rawCountries[0];
                var country = new Country
                {
                    Name = jsonHelper.GetJsonStringValue(item, "name", "common") ?? "Unknown",
                    Code = jsonHelper.GetJsonStringValue(item, "cca2") ?? string.Empty,
                    Capital = jsonHelper.GetJsonArrayFirstString(item, "capital") ?? "N/A",
                    Region = jsonHelper.GetJsonStringValue(item, "region") ?? "Unknown",
                    Population = jsonHelper.GetJsonLongValue(item, "population"),
                    Flag = jsonHelper.GetJsonStringValue(item, "flag") ?? "🌍",
                    Latitude = jsonHelper.GetJsonDoubleValue(item, "latlng", 0),
                    Longitude = jsonHelper.GetJsonDoubleValue(item, "latlng", 1)
                };

                return Ok(country);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching country: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching the country");
            }
        }

       
    }
}