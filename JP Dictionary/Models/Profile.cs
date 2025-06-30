namespace JP_Dictionary.Models
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public int LoginStreak { get; set; }
        public byte CurrentWeek { get; set; } = 1;
        public byte CurrentDay { get; set; }
        public bool AutoSpeak { get; set; } = true;
    }
}
