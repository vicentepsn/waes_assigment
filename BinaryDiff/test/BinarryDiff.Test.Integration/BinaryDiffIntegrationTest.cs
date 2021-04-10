using BinaryDiff.Model;
using BinaryDiff.ServiceModel;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BinarryDiff.Test.Integration
{
    public class BinaryDiffIntegrationTest: IClassFixture<CustomWebApplicationFactory<BinaryDiff.Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<BinaryDiff.Startup> _factory;
        private readonly string _baseSerializedJson;
        private readonly string _sameSizeSerializedJson;
        private readonly string _differentSizeSerializedJson;

        public BinaryDiffIntegrationTest(CustomWebApplicationFactory<BinaryDiff.Startup> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var baseTestObject = new TestObject
            {
                Id = 1,
                DoubleValue1 = 1.1,
                DoubleValue2 = 2.2,
                SomeText = "Some text"
            };

            var sameSizeTestObject = new TestObject
            {
                Id = 2,
                DoubleValue1 = 1.2,
                DoubleValue2 = 2.3,
                SomeText = "Diff text"
            };

            var differentSizeTestObject = new TestObject
            {
                Id = 3,
                DoubleValue1 = 1.2,
                DoubleValue2 = 2.3,
                SomeText = "Some different longer text"
            };

            var baseEncodedData = SerializeToString(baseTestObject);
            var sameSizeEncodedData = SerializeToString(sameSizeTestObject);
            var differentSizeEncodedData = SerializeToString(differentSizeTestObject);

            _baseSerializedJson = JsonConvert.SerializeObject(new DiffPayload { EncodedBinaryData = baseEncodedData });
            _sameSizeSerializedJson = JsonConvert.SerializeObject(new DiffPayload { EncodedBinaryData = sameSizeEncodedData });
            _differentSizeSerializedJson = JsonConvert.SerializeObject(new DiffPayload { EncodedBinaryData = differentSizeEncodedData });
        }

        private static string SerializeToString<TData>(TData data)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        [Fact]
        public async Task BinaryDiff_pass_equal()
        {
            // Arrange
            var requestContent = new StringContent(_baseSerializedJson, Encoding.UTF8, "application/json");

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
            var requestContentLeft = new StringContent(_baseSerializedJson, Encoding.UTF8, "application/json");

            var requestContentRight = new StringContent(_differentSizeSerializedJson, Encoding.UTF8, "application/json");

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
            var requestContentLeft = new StringContent(_baseSerializedJson, Encoding.UTF8, "application/json");

            var requestContentRight = new StringContent(_sameSizeSerializedJson, Encoding.UTF8, "application/json");

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
            Assert.Equal(4, responseContent.DiffDetails.Count());
            Assert.Equal(272, responseContent.DiffDetails.ElementAt(0).Offset);
            Assert.Equal(1, responseContent.DiffDetails.ElementAt(0).Length);
            Assert.Equal(276, responseContent.DiffDetails.ElementAt(1).Offset);
            Assert.Equal(7, responseContent.DiffDetails.ElementAt(1).Length);
            Assert.Equal(284, responseContent.DiffDetails.ElementAt(2).Offset);
            Assert.Equal(7, responseContent.DiffDetails.ElementAt(2).Length);
            Assert.Equal(298, responseContent.DiffDetails.ElementAt(3).Offset);
            Assert.Equal(4, responseContent.DiffDetails.ElementAt(3).Length);
        }

        [Fact]
        public async Task BinaryDiff_fail_invalidBase64Encoding()
        {
            // Arrange
            var content = JsonConvert.SerializeObject(new DiffPayload { EncodedBinaryData = "kdfjasdfj$%#$¨@358t5469245%@" });
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
            var requestContentRight = new StringContent(_baseSerializedJson, Encoding.UTF8, "application/json");

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
            var requestContentLeft = new StringContent(_baseSerializedJson, Encoding.UTF8, "application/json");

            // Act
            var responseLeft = await _client.PutAsync("v1/diff/34/left", requestContentLeft);
            var response = await _client.GetAsync("v1/diff/34");

            // Assert
            Assert.Equal(HttpStatusCode.OK, responseLeft.StatusCode);
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
