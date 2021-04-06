using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace BinaryDiff.ServiceModel
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DiffResultType
    {
        [EnumMember(Value = "Equal")]
        Equal,
        [EnumMember(Value = "Different size")]
        DifferentSize,
        [EnumMember(Value = "Equal size")]
        EqualSize,
    }
}
