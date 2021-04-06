using BinaryDiff.Data.Contracts;
using BinaryDiff.Data.Entities;
using BinaryDiff.ServiceModel;
using BinaryDiff.Services;
using BinaryDiff.Services.Exceptions;
using Moq;
using System;
using System.Linq;
using System.Net;
using System.Text;
using Xunit;

namespace BinaryDiff.Test.Unit
{
    public class BinaryDiffUnitTest
    {
        private readonly Mock<IComparableEncodedDataRepository> _repositoryMock;
        private readonly DiffService _diffService;

        public BinaryDiffUnitTest()
        {
            _repositoryMock = new Mock<IComparableEncodedDataRepository>();
            _diffService = new DiffService(_repositoryMock.Object);
        }

        [Fact]
        public void BinaryDiff_pass_equal()
        {
            // Arrange
            var content = new ComparableEncodedData()
            {
                Id = 1,
                LeftEncodedData = "eyJrZXlfMTIzIjogInZhbHVlX2FiYyJ9",
                RightEncodedData = "eyJrZXlfMTIzIjogInZhbHVlX2FiYyJ9"
            };
            _repositoryMock
                .Setup(m => m.Get(1))
                .Returns(content);

            // Act
            var response = _diffService.GetDiff(1);

            // Assert
            Assert.Equal(DiffResultType.Equal, response.Result);
        }

        [Fact]
        public void BinaryDiff_pass_differentSize()
        {
            // Arrange
            var content = new ComparableEncodedData()
            {
                Id = 1,
                LeftEncodedData = "eyJrZXlfMTIzIjogInZhbHVlX2FiYyJ9",
                RightEncodedData = "eyJrZXlfMTIzIjogInZhbHVlX2FiYyInZhbHVlX2FiYyJ9"
            };
            _repositoryMock
                .Setup(m => m.Get(1))
                .Returns(content);

            // Act
            var response = _diffService.GetDiff(1);

            // Assert
            Assert.Equal(DiffResultType.DifferentSize, response.Result);
        }

        [Fact]
        public void BinaryDiff_pass_equalSize()
        {
            // Arrange
            var content = new ComparableEncodedData()
            {
                Id = 1,
                LeftEncodedData = "eyJrZXlfMTIzIjogInZhbHVlX2FiYyJ9",
                RightEncodedData = "eyJrZXlfNDU2IjogInZhbHVlX2JjZCJ9"
            };
            _repositoryMock
                .Setup(m => m.Get(1))
                .Returns(content);

            // Act
            var response = _diffService.GetDiff(1);

            // Assert
            Assert.Equal(DiffResultType.EqualSize, response.Result);
            Assert.Equal(2, response.DiffDetails.Count());
            Assert.Equal(8, response.DiffDetails.ElementAt(0).Offset);
            Assert.Equal(4, response.DiffDetails.ElementAt(0).Length);
            Assert.Equal(26, response.DiffDetails.ElementAt(1).Offset);
            Assert.Equal(4, response.DiffDetails.ElementAt(1).Length);
        }

        [Fact]
        public void BinaryDiff_fail_invalidJsonLeft()
        {
            // Arrange
            var jsonLeft = "{key_123 value_abc{{";
            var jsonB64Left = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonLeft));

            // Act / Assert
            var exception = Assert.Throws<HttpResponseException>(() => _diffService.SetData(1, jsonB64Left, DiffDataSide.Left));
            Assert.Equal((int)HttpStatusCode.BadRequest, exception.Status);
        }

        [Fact]
        public void BinaryDiff_fail_invalidBase64Encoding()
        {
            // Arrange
            var content = "kdfjasdfj$%#$¨@358t5469245%@";

            // Act / Assert
            var exception = Assert.Throws<HttpResponseException>(() => _diffService.SetData(1, content, DiffDataSide.Left));
            Assert.Equal((int)HttpStatusCode.BadRequest, exception.Status);
        }

        [Fact]
        public void BinaryDiff_fail_missingLeftSide()
        {
            // Arrange
            var content = new ComparableEncodedData()
            {
                Id = 1,
                RightEncodedData = "eyJrZXlfMTIzIjogInZhbHVlX2FiYyJ9"
            };
            _repositoryMock
                .Setup(m => m.Get(1))
                .Returns(content);

            // Act / Assert
            var exception = Assert.Throws<HttpResponseException>(() => _diffService.GetDiff(1));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.Status);
        }

        [Fact]
        public void BinaryDiff_fail_missingRightSide()
        {
            // Arrange
            var content = new ComparableEncodedData()
            {
                Id = 1,
                LeftEncodedData = "eyJrZXlfMTIzIjogInZhbHVlX2FiYyJ9"
            };
            _repositoryMock
                .Setup(m => m.Get(1))
                .Returns(content);

            // Act / Assert
            var exception = Assert.Throws<HttpResponseException>(() => _diffService.GetDiff(1));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.Status);
        }

        [Fact]
        public void BinaryDiff_fail_idNotFound()
        {
            // Arrange
            ComparableEncodedData content = null;
            _repositoryMock
                .Setup(m => m.Get(1))
                .Returns(content);

            // Act / Assert
            var exception = Assert.Throws<HttpResponseException>(() => _diffService.GetDiff(1));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.Status);
        }
    }
}
