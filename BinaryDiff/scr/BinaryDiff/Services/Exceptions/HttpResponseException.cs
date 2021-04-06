using System;

namespace BinaryDiff.Services.Exceptions
{
    public class HttpResponseException : Exception
    {
        public HttpResponseException(int status, object value)
        {
            Status = status;
            Value = value;
        }

        public int Status { get; set; } = 500;

        public object Value { get; set; }
    }
}
