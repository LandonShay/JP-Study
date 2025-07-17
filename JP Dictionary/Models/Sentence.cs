
namespace JP_Dictionary.Models
{
    public class Sentence
    {
        public int JPId { get; set; }
        public string JP { get; set; } = string.Empty;
        public int ENId { get; set; }
        public string EN { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public string KeywordsAsString { get; set; } = string.Empty;
    }
}
