namespace BinaryDiff.Data.Entities
{
    public class ComparableEncodedData
    {
        public int Id { get; set; }
        public byte[] LeftData { get; set; }
        public byte[] RightData { get; set; }
    }
}
