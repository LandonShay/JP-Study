
namespace JP_Dictionary.Models
{
    public class Sentence
    {
        public string JP { get; set; } = string.Empty;
        public string EN { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public string KeywordsAsString { get; set; } = string.Empty;
        public string RomajiReading = string.Empty;
    }
}
