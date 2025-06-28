namespace JP_Dictionary.Models
{
    public class StudyWord
    {
        public long Id { get; set; }
        public int Week { get; set; }
        public int Day { get; set; }
        public string Japanese { get; set; }
        public string Pronounciation { get; set; }
        public string Definitions { get; set; }
        public DateTime LastStudied { get; set; } = DateTime.MinValue;
        public int CorrectStreak { get; set; }
    }
}
