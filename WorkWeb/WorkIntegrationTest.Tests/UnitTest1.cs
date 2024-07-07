namespace WorkIntegrationTest.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Newtonsoft.Json;
using System.Text;
using WorkIntegrationTest.Web.Controllers;

public class UnitTest1 : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public UnitTest1(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Test1()
    {
        // Arrange
        var requestObject = new Request
        {
            Value = "Test"
        };
        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestObject), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/test/execute", jsonContent);

        // Assert
        response.EnsureSuccessStatusCode();

        var responseObject = JsonConvert.DeserializeObject<Response>(await response.Content.ReadAsStringAsync());

        Assert.Equal("Test", responseObject.Value);
    }
}
