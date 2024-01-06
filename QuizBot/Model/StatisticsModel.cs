namespace QuizBot.Model
{
    public class StatisticsModel
    {
        public int Id { get; set; }

        public long UserId { get; set; }

        public int CorrectCount {  get; set; }
        
        public int TotalCount { get; set; }
    }
}
