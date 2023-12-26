using QuizBot.Data;
using QuizBot.Helper;
using QuizBot.Model;
using System.Net.NetworkInformation;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var accessToken = "6938670931:AAHT0T72c0_dzlCx6Gk-ZzVjl9SfPMGuRYI";

var botClient = new TelegramBotClient(accessToken);

string question = "";
string answerA = "";
string answerB = "";
string answerC = "";
string correctAnswer = "";
int questionNumber = 0;
var ticket = new List<QuestionModel>();

botClient.StartReceiving(
               updateHandler: (client, update, token) => GetUpdate(update),
               pollingErrorHandler: (client, exception, token) => Task.CompletedTask);


Console.ReadLine();

//Update
async Task GetUpdate(Update update)
{
    if (update.Type is UpdateType.Message)
    {
        if (update.Message is not { } message) return;

        if (message.Text is not { } messageText) return;

        var chatId = message.Chat.Id;

        var inline = MenuInlineButtons();

        if (messageText == "/start" || messageText == "◀️back")
        {
            questionNumber = 0;
            Database.AssignedAnswers.Clear();
            Database.Answers.Clear();
            await botClient.SendTextMessageAsync(chatId, "back", replyMarkup: new ReplyKeyboardRemove());
            await botClient.SendTextMessageAsync(chatId, @"MENU", replyMarkup: MenuInlineButtons());
            return;
        }

        if (UserAction.MenuStep == MenuStep.insert)
        {
            switch (UserAction.UserStep)
            {
                case UserStep.question:
                    {
                        question = messageText;
                        await botClient.SendTextMessageAsync(chatId, "enter answer A!", replyMarkup: BackButton());
                        UserAction.UserStep = UserStep.answerA;
                    }
                    break;
                case UserStep.answerA:
                    {
                        answerA = messageText;
                        await botClient.SendTextMessageAsync(chatId, "enter answer B!", replyMarkup: BackButton());
                        UserAction.UserStep = UserStep.answerB;
                    }
                    break;
                case UserStep.answerB:
                    {
                        answerB = messageText;
                        await botClient.SendTextMessageAsync(chatId, "enter answer C!", replyMarkup: BackButton());
                        UserAction.UserStep = UserStep.answerC;
                    }
                    break;
                case UserStep.answerC:
                    {
                        answerC = messageText;
                        await botClient.SendTextMessageAsync(chatId, "which one of these is  correct answer?", replyMarkup: CorrectAnswerInlineButtons());
                        UserAction.UserStep = UserStep.correctAnswer;
                    }
                    break;
                case UserStep.correctAnswer:
                    {

                    }
                    break;
            }
        }
    }

    if (update.Type is UpdateType.CallbackQuery)
    {
        if (update.CallbackQuery is not { } callBack) return;

        if (callBack.Data is not { } callBackData) return;

        if (callBack.Message is not { } message) return;

        var chatId = message.Chat.Id;

        if (callBackData == "Profile")
        {
            var me = callBack.From;
            var userInfo = me.Username is not null ? $@"@{me.Username}-{me.FirstName} {me.LastName}" : $@"{me.FirstName} {me.LastName}";
            await botClient.SendTextMessageAsync(chatId, @$"Your Profile: {userInfo}", replyMarkup: BackButton());
            return;
        }

        if (callBackData == "Test")
        {

            UserAction.MenuStep = MenuStep.test;

            questionNumber = 0;
            Database.AssignedAnswers.Clear();
            Database.Answers.Clear();   

            var count = Database.Questions.Count;//11

            var randomNumber = new Random().Next(1, count);//3

            if (randomNumber + 5 < count)
            {
                ticket = Database.Questions.Skip(randomNumber - 1).Take(5).ToList();
            }
            else if (randomNumber + 5 >= count)
            {
                ticket = Database.Questions.Skip(count - 6).Take(5).ToList();
            }

            var question = ticket[questionNumber];
            await botClient.SendTextMessageAsync(chatId, @$"
question: {question.Question}",
replyMarkup: TestAnswerInlineButtons(question.Id, question.AnswerA, question.AnswerB, question.AnswerC, isBack: false, isNext: true));
            return;
        }

        if (callBackData == "Questions")
        {
            var questions = Database.Questions;

            foreach (var question in questions)
            {

                await botClient.SendTextMessageAsync(chatId, @$"
id: {question.Id},
question: {question.Question},
A) {question.AnswerA},
B) {question.AnswerB},
C) {question.AnswerC},

correct: {question.CorrectAnswer},");

            }
            return;
        }

        if (callBackData == "Insert Test")
        {
            await botClient.SendTextMessageAsync(chatId, @$"enter your question!", replyMarkup: BackButton());
            UserAction.MenuStep = MenuStep.insert;
            UserAction.UserStep = UserStep.question;
            return;
        }

        if (UserAction.MenuStep == MenuStep.insert)
        {
            if (callBackData == "A" || callBackData == "B" || callBackData == "C")
            {
                correctAnswer = callBackData;

                var model = new QuestionModel()
                {
                    Id = new Random().Next(1, int.MaxValue),
                    Question = question,
                    AnswerA = answerA,
                    AnswerB = answerB,
                    AnswerC = answerC,
                    CorrectAnswer = correctAnswer
                };

                Database.Questions.Add(model);

                await botClient.SendTextMessageAsync(chatId, "inserted successfully!", replyMarkup: MenuInlineButtons());
                UserAction.MenuStep = MenuStep.start;
                UserAction.UserStep = UserStep.question;
            }
            return;
        }

        if (UserAction.MenuStep == MenuStep.test)
        {
            if (callBackData == "Result")
            {
                if (Database.AssignedAnswers.Count != 5)
                {
                    await botClient.DeleteMessageAsync(chatId, message.MessageId);

                    await botClient.SendTextMessageAsync(chatId, "There are a few questions not to be done");

                    var nextQuestion = ticket[questionNumber];

                    Database.AssignedAnswers.TryGetValue(nextQuestion.Id, out var assignedAnswer);// A, B, C---->B

                    if (assignedAnswer == null)
                    {
                        if (questionNumber == ticket.Count - 1)
                        {
                            await botClient.SendTextMessageAsync(chatId, @$"
question: {nextQuestion.Question}",
            replyMarkup: TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, nextQuestion.AnswerC, isBack: true, isNext: false, isResult: true));


                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, @$"
question: {nextQuestion.Question}",
            replyMarkup: TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, nextQuestion.AnswerC));
                        }

                        return;
                    }

                    else
                    {
                        if (nextQuestion.CorrectAnswer == assignedAnswer)
                        {
                            if (questionNumber == 0)
                            {
                                switch (assignedAnswer)
                                {
                                    case "A":

                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, "✅", nextQuestion.AnswerB, nextQuestion.AnswerC, false, true));
                                        break;

                                    case "B":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "✅", nextQuestion.AnswerC, false, true));
                                        break;

                                    case "C":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "✅", false, true));
                                        break;
                                }
                            }
                            else if (questionNumber == ticket.Count - 1)
                            {
                                switch (assignedAnswer)
                                {
                                    case "A":

                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, "✅", nextQuestion.AnswerB, nextQuestion.AnswerC, true, false, true));
                                        break;

                                    case "B":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "✅", nextQuestion.AnswerC, true, false, true));
                                        break;

                                    case "C":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "✅", true, false, true));
                                        break;
                                }
                            }
                            else
                            {
                                switch (assignedAnswer)
                                {
                                    case "A":

                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, "✅", nextQuestion.AnswerB, nextQuestion.AnswerC, true, true));
                                        break;

                                    case "B":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "✅", nextQuestion.AnswerC, true, true));
                                        break;

                                    case "C":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "✅", true, true));
                                        break;
                                }
                            }
                        }

                        else
                        {
                            if (questionNumber == 0)
                            {
                                switch (assignedAnswer)
                                {
                                    case "A":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, "❌", nextQuestion.AnswerB, nextQuestion.AnswerC, false, true));
                                        break;

                                    case "B":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "❌", nextQuestion.AnswerC, false, true));
                                        break;

                                    case "C":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "❌", false, true));
                                        break;
                                }
                            }

                            else if (questionNumber == ticket.Count - 1)
                            {
                                switch (assignedAnswer)
                                {
                                    case "A":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, "❌", nextQuestion.AnswerB, nextQuestion.AnswerC, true, false, true));
                                        break;

                                    case "B":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "❌", nextQuestion.AnswerC, true, false, true));
                                        break;

                                    case "C":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "❌", true, false, true));
                                        break;
                                }
                            }

                            else
                            {
                                switch (assignedAnswer)
                                {
                                    case "A":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, "❌", nextQuestion.AnswerB, nextQuestion.AnswerC, true, true));
                                        break;

                                    case "B":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "❌", nextQuestion.AnswerC, true, true));
                                        break;

                                    case "C":
                                        await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                    TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "❌", true, true));
                                        break;
                                }
                            }
                        }

                        return;
                    }
                }

                if(Database.AssignedAnswers.Count == 5)
                {
                    await botClient.DeleteMessageAsync(chatId, message.MessageId);

                    int correctAnswer = Database.Answers.Where(x => x.Value == true).ToList().Count;

                    await botClient.SendTextMessageAsync(chatId, $"Result:{correctAnswer}/5");
                 
                    Database.AssignedAnswers.Clear();
                    Database.Answers.Clear();
                    return;
                }
            }
            
            if (callBackData == "next➡️")
            {

                await botClient.DeleteMessageAsync(chatId, message.MessageId);

                var nextQuestion = ticket[++questionNumber];

                Database.AssignedAnswers.TryGetValue(nextQuestion.Id, out var assignedAnswer);// A, B, C---->B

                if (assignedAnswer == null)
                {
                    if (questionNumber == ticket.Count - 1)
                    {
                        await botClient.SendTextMessageAsync(chatId, @$"
question: {nextQuestion.Question}",
        replyMarkup: TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, nextQuestion.AnswerC, isBack: true, isNext: false, isResult: true));


                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, @$"
question: {nextQuestion.Question}",
        replyMarkup: TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, nextQuestion.AnswerC));
                    }

                    return;
                }

                else
                {
                    if (nextQuestion.CorrectAnswer == assignedAnswer)
                    {
                        if (questionNumber == 0)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":

                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                            TestAnswerInlineButtons(nextQuestion.Id, "✅", nextQuestion.AnswerB, nextQuestion.AnswerC, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "✅", nextQuestion.AnswerC, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "✅", false, true));
                                    break;
                            }
                        }
                        else if (questionNumber == ticket.Count - 1)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":

                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                            TestAnswerInlineButtons(nextQuestion.Id, "✅", nextQuestion.AnswerB, nextQuestion.AnswerC, true, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "✅", nextQuestion.AnswerC, true, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "✅", true, false, true));
                                    break;
                            }
                        }
                        else
                        {
                            switch (assignedAnswer)
                            {
                                case "A":

                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                            TestAnswerInlineButtons(nextQuestion.Id, "✅", nextQuestion.AnswerB, nextQuestion.AnswerC, true, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "✅", nextQuestion.AnswerC, true, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "✅", true, true));
                                    break;
                            }
                        }
                    }

                    else
                    {
                        if (questionNumber == 0)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, "❌", nextQuestion.AnswerB, nextQuestion.AnswerC, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "❌", nextQuestion.AnswerC, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "❌", false, true));
                                    break;
                            }
                        }

                        else if (questionNumber == ticket.Count - 1)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, "❌", nextQuestion.AnswerB, nextQuestion.AnswerC, true, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "❌", nextQuestion.AnswerC, true, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "❌", true, false, true));
                                    break;
                            }
                        }

                        else
                        {
                            switch (assignedAnswer)
                            {
                                case "A":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, "❌", nextQuestion.AnswerB, nextQuestion.AnswerC, true, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, "❌", nextQuestion.AnswerC, true, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, nextQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(nextQuestion.Id, nextQuestion.AnswerA, nextQuestion.AnswerB, "❌", true, true));
                                    break;
                            }
                        }
                    }

                    return;
                }

            }

            if (callBackData == "⬅️back")
            {
                await botClient.DeleteMessageAsync(chatId, message.MessageId);
                var backQuestion = ticket[--questionNumber];

                Database.AssignedAnswers.TryGetValue(backQuestion.Id, out var assignedAnswer);// A, B, C---->B

                if (assignedAnswer == null)
                {
                    if (questionNumber == 0)
                    {
                        await botClient.SendTextMessageAsync(chatId, @$"
question: {backQuestion.Question}",
        replyMarkup: TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, backQuestion.AnswerC, isBack: false, isNext: true));
                        return;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, @$"
question: {backQuestion.Question}",
        replyMarkup: TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, backQuestion.AnswerC));
                    }

                    return;
                }

                else
                {
                    if (backQuestion.CorrectAnswer == assignedAnswer)
                    {
                        if (questionNumber == 0)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":

                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                            TestAnswerInlineButtons(backQuestion.Id, "✅", backQuestion.AnswerB, backQuestion.AnswerC, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, "✅", backQuestion.AnswerC, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, "✅", false, true));
                                    break;
                            }
                        }
                        else if (questionNumber == ticket.Count - 1)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":

                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                            TestAnswerInlineButtons(backQuestion.Id, "✅", backQuestion.AnswerB, backQuestion.AnswerC, true, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, "✅", backQuestion.AnswerC, true, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, "✅", true, false, true));
                                    break;
                            }
                        }
                        else
                        {
                            switch (assignedAnswer)
                            {
                                case "A":

                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                            TestAnswerInlineButtons(backQuestion.Id, "✅", backQuestion.AnswerB, backQuestion.AnswerC, true, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, "✅", backQuestion.AnswerC, true, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, "✅", true, true));
                                    break;
                            }
                        }
                    }

                    else
                    {
                        if (questionNumber == 0)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, "❌", backQuestion.AnswerB, backQuestion.AnswerC, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, "❌", backQuestion.AnswerC, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, "❌", false, true));
                                    break;
                            }
                        }

                        else if (questionNumber == ticket.Count - 1)
                        {
                            switch (assignedAnswer)
                            {
                                case "A":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, "❌", backQuestion.AnswerB, backQuestion.AnswerC, true, false, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, "❌", backQuestion.AnswerC, true, false, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, "❌", true, false, true));
                                    break;
                            }
                        }

                        else
                        {
                            switch (assignedAnswer)
                            {
                                case "A":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, "❌", backQuestion.AnswerB, backQuestion.AnswerC, true, true));
                                    break;

                                case "B":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, "❌", backQuestion.AnswerC, true, true));
                                    break;

                                case "C":
                                    await botClient.SendTextMessageAsync(chatId, backQuestion.Question, replyMarkup:
                                TestAnswerInlineButtons(backQuestion.Id, backQuestion.AnswerA, backQuestion.AnswerB, "❌", true, true));
                                    break;
                            }
                        }
                    }

                    return;
                }
            }

            var callBackMassive = callBackData.Split(',');//"11432,A"

            var questionId = int.Parse(callBackMassive[0]);

            var answer = callBackMassive[1];

            var messageId = message.MessageId;

            var question = Database.Questions.FirstOrDefault(x => x.Id == questionId);

            if (Database.AssignedAnswers.Count > 0)
            {
                var assignAnswer = Database.AssignedAnswers.FirstOrDefault(x => x.Key == questionId);

                if (assignAnswer.Value == null)
                    Database.AssignedAnswers.Add(questionId, answer);

                else
                    Database.AssignedAnswers[questionId] = answer;

            }

            else
            {
                Database.AssignedAnswers[questionId] = answer;
            }

            if (question == null)
            {
                return;
            }

            if (question.CorrectAnswer == answer)
            {


                if (questionNumber == 0)
                {
                    switch (answer)
                    {
                        case "A":

                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                    TestAnswerInlineButtons(question.Id, "✅", question.AnswerB, question.AnswerC, false, true));

                            break;

                        case "B":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, "✅", question.AnswerC, false, true));

                            break;

                        case "C":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, question.AnswerB, "✅", false, true));
                            break;
                    }
                }

                else if (questionNumber == ticket.Count - 1)
                {
                    switch (answer)
                    {
                        case "A":

                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                    TestAnswerInlineButtons(question.Id, "✅", question.AnswerB, question.AnswerC, true, false,true));
                            break;

                        case "B":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, "✅", question.AnswerC, true, false,true));
                            break;

                        case "C":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, question.AnswerB, "✅", true, false,true));
                            break;
                    }
                }
                
                else
                {
                    switch (answer)
                    {
                        case "A":

                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                    TestAnswerInlineButtons(question.Id, "✅", question.AnswerB, question.AnswerC, true, true));
                            break;

                        case "B":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, "✅", question.AnswerC, true, true));
                            break;

                        case "C":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, question.AnswerB, "✅", true, true));
                            break;
                    }
                }

                var _question = Database.Answers.FirstOrDefault(x => x.Key == questionId).Value;

                if (_question == null)
                {
                    Database.Answers.Add(questionId, true);
                }
                else
                {
                    Database.Answers[questionId] = true;
                }

            }

            else
            {
                if (questionNumber == 0)
                {
                    switch (answer)
                    {
                        case "A":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, "❌", question.AnswerB, question.AnswerC, false, true));
                            break;

                        case "B":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, "❌", question.AnswerC, false, true));
                            break;

                        case "C":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, question.AnswerB, "❌", false, true));
                            break;
                    }
                }

                else if (questionNumber == ticket.Count - 1)
                {
                    switch (answer)
                    {
                        case "A":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, "❌", question.AnswerB, question.AnswerC, true, false,true));
                            break;

                        case "B":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, "❌", question.AnswerC, true, false,true));
                            break;

                        case "C":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, question.AnswerB, "❌", true, false,true));
                            break;
                    }
                }

                else
                {
                    switch (answer)
                    {
                        case "A":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, "❌", question.AnswerB, question.AnswerC, true, true));
                            break;

                        case "B":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, "❌", question.AnswerC, true, true));
                            break;

                        case "C":
                            await botClient.EditMessageTextAsync(chatId, messageId, question.Question, replyMarkup:
                        TestAnswerInlineButtons(question.Id, question.AnswerA, question.AnswerB, "❌", true, true));
                            break;
                    }
                }
                
                var _question = Database.Answers.FirstOrDefault(x => x.Key == questionId).Value;

                if(_question == null)
                {
                    Database.Answers.Add(questionId, false);
                }
                else
                {
                    Database.Answers[questionId] = false;
                }
            }

            return;
        }
    }
}

//Inline button
InlineKeyboardMarkup MenuInlineButtons()
{

    //button for row 1
    InlineKeyboardButton button1 = InlineKeyboardButton.WithCallbackData(text: "Profile", callbackData: "Profile");
    InlineKeyboardButton button2 = InlineKeyboardButton.WithCallbackData(text: "Test", callbackData: "Test");
    InlineKeyboardButton button3 = InlineKeyboardButton.WithCallbackData(text: "Questions", callbackData: "Questions");
    InlineKeyboardButton button4 = InlineKeyboardButton.WithCallbackData(text: "Statistics", callbackData: "Statistics");
    InlineKeyboardButton button5 = InlineKeyboardButton.WithCallbackData(text: "Insert Test", callbackData: "Insert Test");


    //1 qatorni yasadik
    List<InlineKeyboardButton> buttonRow1 = new List<InlineKeyboardButton>();

    buttonRow1.Add(button1);

    List<InlineKeyboardButton> buttonRow2 = new List<InlineKeyboardButton>();
    buttonRow2.Add(button2);

    List<InlineKeyboardButton> buttonRow3 = new List<InlineKeyboardButton>();
    buttonRow3.Add(button3);

    List<InlineKeyboardButton> buttonRow4 = new List<InlineKeyboardButton>();
    buttonRow4.Add(button4);

    List<InlineKeyboardButton> buttonRow5 = new List<InlineKeyboardButton>();
    buttonRow5.Add(button5);

    List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();

    rows.Add(buttonRow1);
    rows.Add(buttonRow2);
    rows.Add(buttonRow3);
    rows.Add(buttonRow4);
    rows.Add(buttonRow5);

    return new InlineKeyboardMarkup(rows);
}

InlineKeyboardMarkup CorrectAnswerInlineButtons()
{

    //button for row 1
    InlineKeyboardButton button1 = InlineKeyboardButton.WithCallbackData(text: "A", callbackData: "A");
    InlineKeyboardButton button2 = InlineKeyboardButton.WithCallbackData(text: "B", callbackData: "B");
    InlineKeyboardButton button3 = InlineKeyboardButton.WithCallbackData(text: "C", callbackData: "C");
    InlineKeyboardButton button4 = InlineKeyboardButton.WithCallbackData(text: "◀️back", callbackData: "◀️back");


    //1 qatorni yasadik
    List<InlineKeyboardButton> buttonRow1 = new List<InlineKeyboardButton>();

    buttonRow1.Add(button1);
    buttonRow1.Add(button2);
    buttonRow1.Add(button3);

    List<InlineKeyboardButton> buttonRow2 = new List<InlineKeyboardButton>();
    buttonRow2.Add(button4);

    List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();

    rows.Add(buttonRow1);
    rows.Add(buttonRow2);

    return new InlineKeyboardMarkup(rows);
}

InlineKeyboardMarkup TestAnswerInlineButtons(int questionId, string answerA, string answerB, string answerC, bool isBack = true, bool isNext = true, bool isResult = false)
{
    if (isBack == true && isResult == true)
    {
        //button for row 1
        InlineKeyboardButton button1 = InlineKeyboardButton.WithCallbackData(text: $"A){answerA}", callbackData: $"{questionId},A");
        InlineKeyboardButton button2 = InlineKeyboardButton.WithCallbackData(text: $"B){answerB}", callbackData: $"{questionId},B");
        InlineKeyboardButton button3 = InlineKeyboardButton.WithCallbackData(text: $"C){answerC}", callbackData: $"{questionId},C");
        InlineKeyboardButton button4 = InlineKeyboardButton.WithCallbackData(text: "⬅️back", callbackData: "⬅️back");
        InlineKeyboardButton button5 = InlineKeyboardButton.WithCallbackData(text: "Result", callbackData: "Result");



        //1 qatorni yasadik
        List<InlineKeyboardButton> buttonRow1 = new List<InlineKeyboardButton>();

        buttonRow1.Add(button1);
        buttonRow1.Add(button2);
        buttonRow1.Add(button3);

        List<InlineKeyboardButton> buttonRow2 = new List<InlineKeyboardButton>();
        buttonRow2.Add(button4);
        buttonRow2.Add(button5);

        List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();

        rows.Add(buttonRow1);
        rows.Add(buttonRow2);

        return new InlineKeyboardMarkup(rows);
    }
    if (isBack == true && isNext == true)
    {
        //button for row 1
        InlineKeyboardButton button1 = InlineKeyboardButton.WithCallbackData(text: $"A){answerA}", callbackData: $"{questionId},A");
        InlineKeyboardButton button2 = InlineKeyboardButton.WithCallbackData(text: $"B){answerB}", callbackData: $"{questionId},B");
        InlineKeyboardButton button3 = InlineKeyboardButton.WithCallbackData(text: $"C){answerC}", callbackData: $"{questionId},C");
        InlineKeyboardButton button4 = InlineKeyboardButton.WithCallbackData(text: "⬅️back", callbackData: "⬅️back");
        InlineKeyboardButton button5 = InlineKeyboardButton.WithCallbackData(text: "next➡️", callbackData: "next➡️");



        //1 qatorni yasadik
        List<InlineKeyboardButton> buttonRow1 = new List<InlineKeyboardButton>();

        buttonRow1.Add(button1);
        buttonRow1.Add(button2);
        buttonRow1.Add(button3);

        List<InlineKeyboardButton> buttonRow2 = new List<InlineKeyboardButton>();
        buttonRow2.Add(button4);
        buttonRow2.Add(button5);

        List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();

        rows.Add(buttonRow1);
        rows.Add(buttonRow2);

        return new InlineKeyboardMarkup(rows);
    }
    else if (isBack == true && isNext == false)
    {
        //button for row 1
        InlineKeyboardButton button1 = InlineKeyboardButton.WithCallbackData(text: $"A){answerA}", callbackData: $"{questionId},A");
        InlineKeyboardButton button2 = InlineKeyboardButton.WithCallbackData(text: $"B){answerB}", callbackData: $"{questionId},B");
        InlineKeyboardButton button3 = InlineKeyboardButton.WithCallbackData(text: $"C){answerC}", callbackData: $"{questionId},C");
        InlineKeyboardButton button4 = InlineKeyboardButton.WithCallbackData(text: "⬅️back", callbackData: "⬅️back");
        //InlineKeyboardButton button5 = InlineKeyboardButton.WithCallbackData(text: "next➡️", callbackData: "next➡️");



        //1 qatorni yasadik
        List<InlineKeyboardButton> buttonRow1 = new List<InlineKeyboardButton>();

        buttonRow1.Add(button1);
        buttonRow1.Add(button2);
        buttonRow1.Add(button3);

        List<InlineKeyboardButton> buttonRow2 = new List<InlineKeyboardButton>();
        buttonRow2.Add(button4);
        //buttonRow2.Add(button5);

        List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();

        rows.Add(buttonRow1);
        rows.Add(buttonRow2);

        return new InlineKeyboardMarkup(rows);
    }
    else if (isBack == false && isNext == true)
    {
        //button for row 1
        InlineKeyboardButton button1 = InlineKeyboardButton.WithCallbackData(text: $"A){answerA}", callbackData: $"{questionId},A");
        InlineKeyboardButton button2 = InlineKeyboardButton.WithCallbackData(text: $"B){answerB}", callbackData: $"{questionId},B");
        InlineKeyboardButton button3 = InlineKeyboardButton.WithCallbackData(text: $"C){answerC}", callbackData: $"{questionId},C");
        //InlineKeyboardButton button4 = InlineKeyboardButton.WithCallbackData(text: "⬅️back", callbackData: "⬅️back");
        InlineKeyboardButton button5 = InlineKeyboardButton.WithCallbackData(text: "next➡️", callbackData: "next➡️");



        //1 qatorni yasadik
        List<InlineKeyboardButton> buttonRow1 = new List<InlineKeyboardButton>();

        buttonRow1.Add(button1);
        buttonRow1.Add(button2);
        buttonRow1.Add(button3);

        List<InlineKeyboardButton> buttonRow2 = new List<InlineKeyboardButton>();
        //buttonRow2.Add(button4);
        buttonRow2.Add(button5);

        List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();

        rows.Add(buttonRow1);
        rows.Add(buttonRow2);

        return new InlineKeyboardMarkup(rows);
    }
    else
    {
        //button for row 1
        InlineKeyboardButton button1 = InlineKeyboardButton.WithCallbackData(text: $"A){answerA}", callbackData: $"{questionId},A");
        InlineKeyboardButton button2 = InlineKeyboardButton.WithCallbackData(text: $"B){answerB}", callbackData: $"{questionId},B");
        InlineKeyboardButton button3 = InlineKeyboardButton.WithCallbackData(text: $"C){answerC}", callbackData: $"{questionId},C");
        //InlineKeyboardButton button4 = InlineKeyboardButton.WithCallbackData(text: "⬅️back", callbackData: "⬅️back");
        //InlineKeyboardButton button5 = InlineKeyboardButton.WithCallbackData(text: "next➡️", callbackData: "next➡️");



        //1 qatorni yasadik
        List<InlineKeyboardButton> buttonRow1 = new List<InlineKeyboardButton>();

        buttonRow1.Add(button1);
        buttonRow1.Add(button2);
        buttonRow1.Add(button3);

        List<InlineKeyboardButton> buttonRow2 = new List<InlineKeyboardButton>();
        //buttonRow2.Add(button4);
        //buttonRow2.Add(button5);

        List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();

        rows.Add(buttonRow1);
        rows.Add(buttonRow2);

        return new InlineKeyboardMarkup(rows);
    }
}

//Text button
ReplyKeyboardMarkup BackButton()
{
    KeyboardButton keyboardButton = new KeyboardButton("◀️back");


    List<KeyboardButton> row = new List<KeyboardButton>();

    row.Add(keyboardButton);

    List<List<KeyboardButton>> keyboardButtons = new List<List<KeyboardButton>>();

    keyboardButtons.Add(row);

    return new ReplyKeyboardMarkup(keyboardButton)
    {
        ResizeKeyboard = true
    };
}

