using BinaryDiff.ServiceModel;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BinarryDiff.Test.Integration
{
    public class BinaryDiffIntegrationTest: IClassFixture<CustomWebApplicationFactory<BinaryDiff.Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<BinaryDiff.Startup> _factory;

        public BinaryDiffIntegrationTest(CustomWebApplicationFactory<BinaryDiff.Startup> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task BinaryDiff_pass_equal()
        {
            // Arrange
            var jsonStr = "{\"key\": \"value\"}";
            var jsonB64Str = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonStr));
            var requestContent = new StringContent(jsonB64Str, Encoding.UTF8, "application/json");

            // Act
            var responseLeft = await _client.PutAsync("v1/diff/1/left", requestContent);
            var responseRight = await _client.PutAsync("v1/diff/1/right", requestContent);
            var response = await _client.GetAsync("v1/diff/1");

            var responseContent = JsonConvert.DeserializeObject<DiffResult>(response.Content.ReadAsStringAsync().Result);
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, responseLeft.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseRight.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(DiffResultType.Equal, responseContent.Result);
        }

        [Fact]
        public async Task BinaryDiff_pass_differentSize()
        {
            // Arrange
            var jsonLeft = "{\"key\": \"value\"}";
            var jsonB64Left = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonLeft));
            var requestContentLeft = new StringContent(jsonB64Left, Encoding.UTF8, "application/json");

            var jsonRight = "{\"key\": \"different_size\"}";
            var jsonB64Right = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonRight));
            var requestContentRight = new StringContent(jsonB64Right, Encoding.UTF8, "application/json");

            // Act
            var responseLeft = await _client.PutAsync("v1/diff/1/left", requestContentLeft);
            var responseRight = await _client.PutAsync("v1/diff/1/right", requestContentRight);
            var response = await _client.GetAsync("v1/diff/1");

            var responseContent = JsonConvert.DeserializeObject<DiffResult>(response.Content.ReadAsStringAsync().Result);

            // Assert
            Assert.Equal(HttpStatusCode.OK, responseLeft.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseRight.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(DiffResultType.DifferentSize, responseContent.Result);
        }

        [Fact]
        public async Task BinaryDiff_pass_equalSize()
        {
            // Arrange
            var jsonLeft = "{\"key_123\": \"value_abc\"}";
            var jsonB64Left = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonLeft));
            var requestContentLeft = new StringContent(jsonB64Left, Encoding.UTF8, "application/json");

            var jsonRight = "{\"key_456\": \"value_bcd\"}";
            var jsonB64Right = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonRight));
            var requestContentRight = new StringContent(jsonB64Right, Encoding.UTF8, "application/json");

            // Act
            var responseLeft = await _client.PutAsync("v1/diff/1/left", requestContentLeft);
            var responseRight = await _client.PutAsync("v1/diff/1/right", requestContentRight);
            var response = await _client.GetAsync("v1/diff/1");

            var responseContent = JsonConvert.DeserializeObject<DiffResult>(response.Content.ReadAsStringAsync().Result);

            // Assert
            Assert.Equal(HttpStatusCode.OK, responseLeft.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseRight.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(DiffResultType.EqualSize, responseContent.Result);
            Assert.Equal(2, responseContent.DiffDetails.Count());
            Assert.Equal(8, responseContent.DiffDetails.ElementAt(0).Offset);
            Assert.Equal(4, responseContent.DiffDetails.ElementAt(0).Length);
            Assert.Equal(26, responseContent.DiffDetails.ElementAt(1).Offset);
            Assert.Equal(4, responseContent.DiffDetails.ElementAt(1).Length);
        }

        [Fact]
        public async Task BinaryDiff_fail_invalidJsonLeft()
        {
            // Arrange
            var jsonLeft = "{key_123 value_abc{{";
            var jsonB64Left = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonLeft));
            var requestContentLeft = new StringContent(jsonB64Left, Encoding.UTF8, "application/json");

            // Act
            var responseLeft = await _client.PutAsync("v1/diff/1/left", requestContentLeft);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, responseLeft.StatusCode);
        }

        [Fact]
        public async Task BinaryDiff_fail_invalidBase64Encoding()
        {
            // Arrange
            var content = "kdfjasdfj$%#$¨@358t5469245%@";
            var requestContentLeft = new StringContent(content, Encoding.UTF8, "application/json");

            // Act
            var responseLeft = await _client.PutAsync("v1/diff/1/left", requestContentLeft);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, responseLeft.StatusCode);
        }

        [Fact]
        public async Task BinaryDiff_fail_missingLeftSide()
        {
            // Arrange
            var jsonRight = "{\"key_456\": \"value_bcd\"}";
            var jsonB64Right = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonRight));
            var requestContentRight = new StringContent(jsonB64Right, Encoding.UTF8, "application/json");

            // Act
            var responseRight = await _client.PutAsync("v1/diff/33/right", requestContentRight);
            var response = await _client.GetAsync("v1/diff/33");

            // Assert
            Assert.Equal(HttpStatusCode.OK, responseRight.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task BinaryDiff_fail_missingRightSide()
        {
            // Arrange
            var jsonRight = "{\"key_456\": \"value_bcd\"}";
            var jsonB64Right = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonRight));
            var requestContentRight = new StringContent(jsonB64Right, Encoding.UTF8, "application/json");

            // Act
            var responseRight = await _client.PutAsync("v1/diff/34/left", requestContentRight);
            var response = await _client.GetAsync("v1/diff/34");

            // Assert
            Assert.Equal(HttpStatusCode.OK, responseRight.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task BinaryDiff_fail_idNotFound()
        {
            // Arrange

            // Act
            var response = await _client.GetAsync("v1/diff/111");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
