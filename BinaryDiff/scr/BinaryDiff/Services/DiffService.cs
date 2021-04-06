using BinaryDiff.Data.Contracts;
using BinaryDiff.Data.Entities;
using BinaryDiff.ServiceModel;
using BinaryDiff.Services.Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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
        /// <param name="encodedData">The comparable object data of a side. Should be a Json Base64 Encoded</param>
        /// <param name="diffDataSide">The side that the data should be stored in the comparable object. Left or Right</param>
        public void SetData(int id, string encodedData, DiffDataSide diffDataSide)
        {
            var decodedString = DecodeFromBase64String(encodedData);
            ValidateJson(decodedString);

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
                comparableObject.LeftEncodedData = encodedData;
            }
            else
            {
                comparableObject.RightEncodedData = encodedData;
            }

            _comparableEncodedDataRepository.Update(comparableObject);
        }

        /// <summary>
        /// Decode from a Base64 Encoded string
        /// </summary>
        /// <param name="base64EncodedString">The Base64 encoded string</param>
        /// <returns>The decode result string</returns>
        /// <exception cref="HttpResponseException">Thrown when the string received is not a valid Base64 
        /// Encoded string</exception>
        private string DecodeFromBase64String(string base64EncodedString)
        {
            try
            {
                var byteArray = Convert.FromBase64String(base64EncodedString);
                var decodedString = Encoding.UTF8.GetString(byteArray);

                return decodedString;
            }
            catch
            {
                throw new HttpResponseException((int)HttpStatusCode.BadRequest, "Not a valid base64 encoding");
            }
        }

        /// <summary>
        /// Validates if the string is a well formed valid json. It does not check any json schema
        /// </summary>
        /// <param name="jsonString">The supposed json string</param>
        /// <exception cref="HttpResponseException">Thrown when the string received is not a valid json string</exception>
        private void ValidateJson(string jsonString)
        {
            try
            {
                var result = JObject.Parse(jsonString);
            }
            catch
            {
                throw new HttpResponseException((int)HttpStatusCode.BadRequest, "Not a valid json string");
            }
        }

        /// <summary>
        /// Compares both sides of a comparable object and return the detais of the comparison
        /// </summary>
        /// <param name="id">The Id of the comparable object</param>
        /// <returns>Returns a DiffResult objetc containing the detais of the comparison</returns>
        /// <exception cref="HttpResponseException">Thrown when there is no comparable object with the Id informed 
        /// or when the comparable object does not have data in one of its side (left/right)</exception>
        public DiffResult GetDiff(int id)
        {
            var comparableObject = _comparableEncodedDataRepository.Get(id);

            ValidateComparableObject(comparableObject, id);

            var diffResult = new DiffResult();
            diffResult.Result =
                (comparableObject.LeftEncodedData == comparableObject.RightEncodedData)
                    ? DiffResultType.Equal
                    : (comparableObject.LeftEncodedData.Length == comparableObject.RightEncodedData.Length)
                        ? DiffResultType.EqualSize
                        : DiffResultType.DifferentSize;
            if (diffResult.Result == DiffResultType.EqualSize)
            {
                diffResult.DiffDetails = GetDiffDetails(comparableObject);
            }

            return diffResult;
        }

        /// <summary>
        /// Get the list with the differences between both sides in the comparable object. Should be used only 
        /// when the sides of the comparable object are different and with the same size
        /// </summary>
        /// <param name="comparableObject">The object to be compared</param>
        /// <returns>A list with the initial positions and lengths where the sides are different</returns>
        private IEnumerable<DiffResultDetail> GetDiffDetails(ComparableEncodedData comparableObject)
        {
            var diffResultetails = new List<DiffResultDetail>();
            var lastDifferentPosition = -1;
            var firstDifferentPosition = -1;
            var previousIsEqual = true;

            for (var i = 0; i < comparableObject.LeftEncodedData.Length; i++)
            {
                if (comparableObject.LeftEncodedData[i] != comparableObject.RightEncodedData[i])
                {
                    if (previousIsEqual)
                    {
                        firstDifferentPosition = i;
                    }
                    lastDifferentPosition = i;
                    previousIsEqual = false;
                }
                if (comparableObject.LeftEncodedData[i] == comparableObject.RightEncodedData[i])
                {
                    if (!previousIsEqual)
                    {
                        diffResultetails.Add(
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
                diffResultetails.Add(
                    new DiffResultDetail
                    {
                        Offset = firstDifferentPosition,
                        Length = lastDifferentPosition + 1 - firstDifferentPosition
                    });
            }

            return diffResultetails;
        }

        /// <summary>
        /// Valdates if the comparable objetc exists and if it has both sides to compara
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
            if (String.IsNullOrEmpty(comparableObject.LeftEncodedData))
            {
                throw new HttpResponseException((int)HttpStatusCode.NotFound, $"Object with index '{comparableObject.Id}' have no left data");
            }
            if (String.IsNullOrEmpty(comparableObject.RightEncodedData))
            {
                throw new HttpResponseException((int)HttpStatusCode.NotFound, $"Object with index '{comparableObject.Id}' have no right data");
            }
        }
    }
}
