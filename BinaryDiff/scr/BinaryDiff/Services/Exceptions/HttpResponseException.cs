using System;

namespace BinaryDiff.Services.Exceptions
{
    [Serializable]
    public class HttpResponseException : Exception
    {
        public HttpResponseException(int status, object value)
        {
            Status = status;
            Value = value;
        }

        public int Status { get; set; }

        public object Value { get; set; }
    }
}
