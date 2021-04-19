using BinaryDiff.Data.Contracts;
using BinaryDiff.Data.Entities;
using BinaryDiff.ServiceModel;
using BinaryDiff.Services.Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace BinaryDiff.Services
{
    public class DiffService
    {
        private readonly IComparableEncodedDataRepository _comparableEncodedDataRepository;

        public DiffService(IComparableEncodedDataRepository comparableEncodedDataRepository)
        {
            _comparableEncodedDataRepository = comparableEncodedDataRepository;
        }

        /// <summary>
        /// Set one side of a comparable object
        /// </summary>
        /// <param name="id">The comparable object Id</param>
        /// <param name="encodedData">The comparable object data of a side. Should be a Base64 Encoded binary data</param>
        /// <param name="diffDataSide">The side that the data should be stored in the comparable object. Left or Right</param>
        /// <returns>Returns the resulted ComparableEncodedData object</returns>
        /// <exception cref="HttpResponseException">Thrown when the 'encodedData' is not a valid Base64 Encoded string</exception>
        public ComparableEncodedData SetData(int id, string encodedData, DiffDataSide diffDataSide)
        {
            
            var byteArray = DecodeFromBase64String(encodedData);

            var comparableObject = _comparableEncodedDataRepository.Get(id);

            if (comparableObject == null)
            {
                comparableObject = 
                    new ComparableEncodedData()
                    {
                        Id = id,
                    };
            }

            if (diffDataSide == DiffDataSide.Left)
            {
                comparableObject.LeftData = byteArray;
            }
            else
            {
                comparableObject.RightData = byteArray;
            }

            _comparableEncodedDataRepository.Update(comparableObject);

            return comparableObject;
        }

        /// <summary>
        /// Get the binary byte array decoded from a Bese64 encoded string
        /// </summary>
        /// <param name="base64EncodedString">The supposed Base64 encoded binary data</param>
        /// <returns>Returns the byte array resulted from the decode</returns>
        /// <exception cref="HttpResponseException">Thrown when the string received is not a valid Base64 
        /// Encoded string</exception>
        private byte[] DecodeFromBase64String(string base64EncodedString)
        {
            try
            {
                return Convert.FromBase64String(base64EncodedString);
            }
            catch
            {
                throw new HttpResponseException((int)HttpStatusCode.BadRequest, "Not a valid base64 encoding");
            }
        }

        /// <summary>
        /// Compares both sides of a comparable object and return the details of the comparison
        /// </summary>
        /// <param name="id">The Id of the comparable object</param>
        /// <returns>Returns a DiffResult object containing the details of the comparison</returns>
        /// <exception cref="HttpResponseException">Thrown when there is no comparable object with the Id informed 
        /// or when the comparable object does not have data in one of its side (left/right)</exception>
        public DiffResult GetDiff(int id)
        {
            var comparableObject = _comparableEncodedDataRepository.Get(id);

            ValidateComparableObject(comparableObject, id);


            if (comparableObject.LeftData.Length != comparableObject.RightData.Length)
            {
                return new DiffResult { Result = DiffResultType.DifferentSize };
            }

            var diffDetails = GetDiffDetails(comparableObject);
            if (!diffDetails.Any())
            {
                return new DiffResult { Result = DiffResultType.Equal };
            }

            return new DiffResult 
            {
                Result = DiffResultType.EqualSize,
                DiffDetails = diffDetails
            };
        }

        /// <summary>
        /// Get the list with the differences between the binary data of both sides in the comparable object. 
        /// Should be used only when the sides of the comparable object are different and with the same size
        /// </summary>
        /// <param name="comparableObject">The object to be compared</param>
        /// <returns>A list with the initial positions and lengths where the sides are different</returns>
        private IEnumerable<DiffResultDetail> GetDiffDetails(ComparableEncodedData comparableObject)
        {
            var diffResultDetails = new List<DiffResultDetail>();
            var lastDifferentPosition = int.MinValue;
            var firstDifferentPosition = int.MinValue;
            var previousIsEqual = true;

            var leftBinaryData = comparableObject.LeftData;
            var rightBinaryData = comparableObject.RightData;

            for (var i = 0; i < leftBinaryData.Length; i++)
            {
                if (leftBinaryData[i] != rightBinaryData[i])
                {
                    if (previousIsEqual)
                    {
                        firstDifferentPosition = i;
                    }
                    lastDifferentPosition = i;
                    previousIsEqual = false;
                }
                if (leftBinaryData[i] == rightBinaryData[i])
                {
                    if (!previousIsEqual)
                    {
                        diffResultDetails.Add(
                            new DiffResultDetail 
                            { 
                                Offset = firstDifferentPosition, 
                                Length = lastDifferentPosition + 1 - firstDifferentPosition
                            });
                    }
                    previousIsEqual = true;
                }
            }

            if (!previousIsEqual)
            {
                diffResultDetails.Add(
                    new DiffResultDetail
                    {
                        Offset = firstDifferentPosition,
                        Length = lastDifferentPosition + 1 - firstDifferentPosition
                    });
            }

            return diffResultDetails;
        }

        /// <summary>
        /// Validates if the comparable object exists and if it has both sides to compare
        /// </summary>
        /// <param name="comparableObject">The comparable object to be validated</param>
        /// <exception cref="HttpResponseException">Thrown when the comparable object is null
        /// or when the comparable object does not have data in one of its side (left/right)</exception>
        private void ValidateComparableObject(ComparableEncodedData comparableObject, int id)
        {
            if (comparableObject == null)
            {
                throw new HttpResponseException((int)HttpStatusCode.NotFound, $"Object with index '{id}' was not found");
            }
            if (!(comparableObject.LeftData?.Any() ?? false))
            {
                throw new HttpResponseException((int)HttpStatusCode.NotFound, $"Object with index '{comparableObject.Id}' have no left data");
            }
            if (!(comparableObject.RightData?.Any() ?? false))
            {
                throw new HttpResponseException((int)HttpStatusCode.NotFound, $"Object with index '{comparableObject.Id}' have no right data");
            }
        }
    }
}
