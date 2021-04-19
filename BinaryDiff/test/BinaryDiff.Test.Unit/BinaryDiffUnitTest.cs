using BinaryDiff.Data.Contracts;
using BinaryDiff.Data.Entities;
using BinaryDiff.ServiceModel;
using BinaryDiff.Services;
using BinaryDiff.Services.Exceptions;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace BinaryDiff.Test.Unit
{
    public class BinaryDiffUnitTest
    {
        private readonly Mock<IComparableEncodedDataRepository> _repositoryMock;
        private readonly DiffService _diffService;
        private readonly byte[] _baseBinaryData;
        private readonly byte[] _sameSizeBinaryData;
        private readonly byte[] _differentSizeBinaryData;

        public BinaryDiffUnitTest()
        {
            _repositoryMock = new Mock<IComparableEncodedDataRepository>();
            _diffService = new DiffService(_repositoryMock.Object);

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

            _baseBinaryData = SerializeToByteArray(baseTestObject);
            _sameSizeBinaryData = SerializeToByteArray(sameSizeTestObject);
            _differentSizeBinaryData = SerializeToByteArray(differentSizeTestObject);
        }

        private static byte[] SerializeToByteArray<TData>(TData data)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        [Fact]
        public void BinaryDiff_pass_equal()
        {
            // Arrange
            var content = new ComparableEncodedData()
            {
                Id = 1,
                LeftData = _baseBinaryData,
                RightData = _baseBinaryData
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
                LeftData = _baseBinaryData,
                RightData = _differentSizeBinaryData
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
                LeftData = _baseBinaryData,
                RightData = _sameSizeBinaryData
            };
            _repositoryMock
                .Setup(m => m.Get(1))
                .Returns(content);

            // Act
            var response = _diffService.GetDiff(1);

            // Assert
            Assert.Equal(DiffResultType.EqualSize, response.Result);
            Assert.Equal(4, response.DiffDetails.Count());
            Assert.Equal(256, response.DiffDetails.ElementAt(0).Offset);
            Assert.Equal(1, response.DiffDetails.ElementAt(0).Length);
            Assert.Equal(260, response.DiffDetails.ElementAt(1).Offset);
            Assert.Equal(7, response.DiffDetails.ElementAt(1).Length);
            Assert.Equal(268, response.DiffDetails.ElementAt(2).Offset);
            Assert.Equal(7, response.DiffDetails.ElementAt(2).Length);
            Assert.Equal(282, response.DiffDetails.ElementAt(3).Offset);
            Assert.Equal(4, response.DiffDetails.ElementAt(3).Length);
        }

        [Fact]
        public void BinaryDiff_pass_setDataLeft()
        {
            // Arrange
            var id = 1;
            var content = Convert.ToBase64String(_baseBinaryData);
            ComparableEncodedData resultContent = null;
            _repositoryMock
                .Setup(m => m.Get(id))
                .Returns(resultContent);

            // Act
            var result = _diffService.SetData(id, content, DiffDataSide.Left);

            // Assert
            Assert.Equal(id, result.Id);
            Assert.True(result.LeftData.SequenceEqual(_baseBinaryData));
        }

        [Fact]
        public void BinaryDiff_pass_setDataRight()
        {
            // Arrange
            var id = 1;
            var content = Convert.ToBase64String(_baseBinaryData);
            ComparableEncodedData resultContent = null;
            _repositoryMock
                .Setup(m => m.Get(id))
                .Returns(resultContent);

            // Act
            var result = _diffService.SetData(id, content, DiffDataSide.Right);

            // Assert
            Assert.Equal(id, result.Id);
            Assert.True(result.RightData.SequenceEqual(_baseBinaryData));
        }

        [Fact]
        public void BinaryDiff_pass_setDataLeft_existingObject()
        {
            // Arrange
            var id = 1;
            var content = Convert.ToBase64String(_baseBinaryData);
            ComparableEncodedData resultContent = 
                new ComparableEncodedData
                {
                    Id = id,
                    RightData = _sameSizeBinaryData
                };
            _repositoryMock
                .Setup(m => m.Get(id))
                .Returns(resultContent);

            // Act
            var result = _diffService.SetData(id, content, DiffDataSide.Left);

            // Assert
            Assert.Equal(id, result.Id);
            Assert.True(result.LeftData.SequenceEqual(_baseBinaryData));
            Assert.True(result.RightData.SequenceEqual(_sameSizeBinaryData));
        }

        [Fact]
        public void BinaryDiff_pass_setDataRight_existingObject()
        {
            // Arrange
            var id = 1;
            var content = Convert.ToBase64String(_baseBinaryData);
            ComparableEncodedData resultContent =
                new ComparableEncodedData
                {
                    Id = id,
                    LeftData = _sameSizeBinaryData
                };
            _repositoryMock
                .Setup(m => m.Get(id))
                .Returns(resultContent);

            // Act
            var result = _diffService.SetData(id, content, DiffDataSide.Right);

            // Assert
            Assert.Equal(id, result.Id);
            Assert.True(result.RightData.SequenceEqual(_baseBinaryData));
            Assert.True(result.LeftData.SequenceEqual(_sameSizeBinaryData));
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
                RightData = _baseBinaryData
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
                LeftData = _baseBinaryData
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
