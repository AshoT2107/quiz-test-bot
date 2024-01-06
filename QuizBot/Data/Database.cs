using QuizBot.Model;

namespace QuizBot.Data
{
    public static class Database
    {
        public static List<QuestionModel> Questions = new List<QuestionModel>()
        {
            new QuestionModel()
            {
                Id = 24234,
                Question = "savol 1",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 2123,
                Question = "savol 2",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 31234,
                Question = "savol 3",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 41231,
                Question = "savol 4",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 83748,
                Question = "savol 5",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 32346,
                Question = "savol 6",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 712344,
                Question = "savol 7",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 81234,
                Question = "savol 8",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 92342,
                Question = "savol 9",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 12450,
                Question = "savol 10",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },

            new QuestionModel()
            {
                Id = 11422,
                Question = "savol 11",
                AnswerA = "A",
                AnswerB = "B",
                AnswerC = "C",
                CorrectAnswer = "B",
            },
        };

        public static List<StatisticsModel> Statistics = new List<StatisticsModel>();

        public static Dictionary<int, string> AssignedAnswers = new Dictionary<int, string>();

        public static Dictionary<int, bool?> Answers = new Dictionary<int, bool?>();
    }
}
