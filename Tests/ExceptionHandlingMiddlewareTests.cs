using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Xunit;
using System.Threading.Tasks;
using System.Net;
using WebApiApp.Middleware;


namespace WebApiApp.Tests
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public async Task Middleware_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var webHostBuilder = new WebHostBuilder()
                .ConfigureServices(services => { })
                .Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();

                    app.Run(context =>
                    {
                        throw new Exception("Test exception");
                    });
                });

            var server = new TestServer(webHostBuilder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Middleware_ReturnsNotFound_OnKeyNotFoundException()
        {
            // Arrange
            var webHostBuilder = new WebHostBuilder()
                .ConfigureServices(services => { })
                .Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();

                    app.Run(context =>
                    {
                        throw new KeyNotFoundException("Test KeyNotFoundException");
                    });
                });

            var server = new TestServer(webHostBuilder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
