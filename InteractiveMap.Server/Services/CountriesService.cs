public class CountriesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CountriesController> _logger;

    public CountriesService(IHttpClientFactory httpClientFactory, ILogger<CountriesController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<Country>> GetCountriesAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://restcountries.com/v3.1/all");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var countries = JsonSerializer.Deserialize<List<Country>>(json);
                return countries ?? new List<Country>();
            }
            else
            {
                _logger.LogError("Failed to fetch countries. Status code: {StatusCode}", response.StatusCode);
                throw new Exception("An error occurred while fetching countries");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching countries");
            throw new Exception("An error occurred while fetching countries");
        }
    }   