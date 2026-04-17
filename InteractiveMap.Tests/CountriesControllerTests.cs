using InteractiveMap.Server;
using InteractiveMap.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace InteractiveMap.Tests;

[TestClass]
public sealed class CountriesControllerTests
{
    [TestMethod]
    public async Task Get_ReturnsOkWithOrderedCountries_WhenApiResponseSucceeds()
    {
        var json = @"[
          {
            ""name"": { ""common"": ""Brazil"" },
            ""cca2"": ""BR"",
            ""capital"": [""Brasília""],
            ""region"": ""Americas"",
            ""population"": 214327000,
            ""flag"": ""🇧🇷"",
            ""latlng"": [-10.0, -55.0],
            ""languages"": { ""por"": ""Portuguese"" }
          },
          {
            ""name"": { ""common"": ""Argentina"" },
            ""cca2"": ""AR"",
            ""capital"": [""Buenos Aires""],
            ""region"": ""Americas"",
            ""population"": 45605823,
            ""flag"": ""🇦🇷"",
            ""latlng"": [-34.0, -64.0],
            ""languages"": { ""spa"": ""Spanish"" }
          }
        ]";

        var controller = CreateControllerWithJsonResponse(json, HttpStatusCode.OK);

        var result = await controller.Get();

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType(okResult.Value, typeof(List<Country>));
        var countries = (List<Country>)okResult.Value;

        Assert.IsNotNull(countries);
        Assert.HasCount(2, countries);
        Assert.AreEqual("Argentina", countries[0].Name);
        Assert.AreEqual("Brazil", countries[1].Name);
    }

    [TestMethod]
    public async Task Get_ReturnsEmptyList_WhenApiReturnsEmptyArray()
    {
        var controller = CreateControllerWithJsonResponse("[]", HttpStatusCode.OK);

        var result = await controller.Get();

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType(okResult.Value, typeof(List<Country>));
        var countries = (List<Country>)okResult.Value;

        Assert.IsNotNull(countries);
        Assert.IsEmpty(countries);
    }

    [TestMethod]
    public async Task Get_ReturnsServerError_WhenApiResponseFails()
    {
        var controller = CreateControllerWithJsonResponse("Internal server error", HttpStatusCode.InternalServerError);

        var result = await controller.Get();

        Assert.IsInstanceOfType(result.Result, typeof(ObjectResult));
        var objectResult = (ObjectResult)result.Result;

        Assert.AreEqual(500, objectResult.StatusCode);
        Assert.AreEqual("Failed to fetch countries data", objectResult.Value);
    }

    [TestMethod]
    public async Task Get_ReturnsOnlyValidCountries_WhenOneItemFailsParsing()
    {
        var json = @"[
          {
            ""name"": { ""common"": ""Validland"" },
            ""cca2"": ""VL"",
            ""capital"": [""Valid City""],
            ""region"": ""Test"",
            ""population"": 12345,
            ""flag"": ""🏳️"",
            ""latlng"": [10.0, 20.0],
            ""languages"": { ""eng"": ""English"" }
          },
          {
            ""cca2"": ""XX""
          }
        ]";

        var controller = CreateControllerWithJsonResponse(json, HttpStatusCode.OK);

        var result = await controller.Get();

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType(okResult.Value, typeof(List<Country>));
        var countries = (List<Country>)okResult.Value;

        Assert.IsNotNull(countries);
        Assert.HasCount(1, countries);
        Assert.AreEqual("Validland", countries[0].Name);
    }

    [TestMethod]
    public async Task GetByCode_ReturnsOkWithCountry_WhenApiResponseSucceeds()
    {
        var json = @"[
          {
            ""name"": { ""common"": ""Canada"" },
            ""cca2"": ""CA"",
            ""capital"": [""Ottawa""],
            ""region"": ""Americas"",
            ""population"": 38005238,
            ""flag"": ""🇨🇦"",
            ""latlng"": [56.1304, -106.3468],
            ""languages"": { ""eng"": ""English"", ""fra"": ""French"" }
          }
        ]";

        var controller = CreateControllerWithJsonResponse(json, HttpStatusCode.OK);

        var result = await controller.GetByCode("CA");

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType(okResult.Value, typeof(Country));
        var country = (Country)okResult.Value;

        Assert.IsNotNull(country);
        Assert.AreEqual("Canada", country.Name);
        Assert.AreEqual("CA", country.Code);
        Assert.AreEqual("Ottawa", country.Capital);
        Assert.AreEqual("Americas", country.Region);
    }

    [TestMethod]
    public async Task GetByCode_ReturnsNotFound_WhenApiResponseFails()
    {
        var controller = CreateControllerWithJsonResponse("Country not found", HttpStatusCode.NotFound);

        var result = await controller.GetByCode("XX");

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        var notFoundResult = (NotFoundObjectResult)result.Result;

        Assert.AreEqual("Country with code 'XX' not found", notFoundResult.Value);
    }

    [TestMethod]
    public async Task GetByCode_ReturnsNotFound_WhenApiReturnsEmptyArray()
    {
        var controller = CreateControllerWithJsonResponse("[]", HttpStatusCode.OK);

        var result = await controller.GetByCode("XX");

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Get_ReturnsServerError_WhenHttpClientThrowsException()
    {
        var controller = CreateControllerWithException(new HttpRequestException("Network failure"));

        var result = await controller.Get();

        Assert.IsInstanceOfType(result.Result, typeof(ObjectResult));
        var objectResult = (ObjectResult)result.Result;

        Assert.AreEqual(500, objectResult.StatusCode);
        Assert.AreEqual("An error occurred while fetching countries", objectResult.Value);
    }

    private static CountriesController CreateControllerWithJsonResponse(string json, HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var factory = CreateHttpClientFactory((_, _) => Task.FromResult(response));
        return new CountriesController(factory, CreateLogger());
    }

    private static CountriesController CreateControllerWithException(Exception exception)
    {
        var factory = CreateHttpClientFactory((_, _) => Task.FromException<HttpResponseMessage>(exception));
        return new CountriesController(factory, CreateLogger());
    }

    private static IHttpClientFactory CreateHttpClientFactory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        var handler = new FakeHttpMessageHandler(responder);
        var client = new HttpClient(handler);
        return new TestHttpClientFactory(client);
    }

    private static ILogger<CountriesController> CreateLogger()
    {
        return new LoggerFactory().CreateLogger<CountriesController>();
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responder(request, cancellationToken);
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }
}
