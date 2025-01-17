﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using F23.StringSimilarity;
using FullQuizbowlTrainer.Models;
using FullQuizbowlTrainer.Services.Checking;
using FullQuizbowlTrainer.Services.Database;
using FullQuizbowlTrainer.Services.Reading;
using FullQuizbowlTrainer.Services.Selector;
using FullQuizbowlTrainer.Services.Web;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FullQuizbowlTrainer.ViewModels
{
    public class QuizzingPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Questions question;
        public Questions Question
        {
            get { return question; }
            set
            {
                question = value;
                OnPropertyChanged("Question");
            }
        }

        private Answers answer;
        public Answers Answer
        {
            get { return answer; }
            set
            {
                answer = value;
                OnPropertyChanged("Answer");
            }
        }

        private string questionText;
        public string QuestionText
        {
            get { return questionText; }
            set
            {
                questionText = value;
                OnPropertyChanged("QuestionText");
            }
        }

        private string buttonState;
        public string ButtonState
        {
            get { return buttonState; }
            set
            {
                buttonState = value;
                OnPropertyChanged("ButtonState");
            }
        }

        private bool isReading;
        public bool IsReading
        {
            get { return isReading; }
            set
            {
                isReading = value;
                OnPropertyChanged("IsReading");
                if (IsReading) ButtonState = "Buzz";
                else
                {
                    if (string.IsNullOrEmpty(AnsweredText))
                    {
                        if (!IsCompleted)
                        {
                            ButtonState = "Withdraw";
                        }
                        else { ButtonState = "Start Buzzzing"; }
                    }
                    else ButtonState = "Submit";
                }
            }
        }

        private string answeredText;
        public string AnsweredText
        {
            get { return answeredText; }
            set
            {
                answeredText = value;
                OnPropertyChanged("AnsweredText");
                if (string.IsNullOrEmpty(AnsweredText))
                {
                    if(!IsCompleted) ButtonState = "Withdraw";
                    else { ButtonState = "Start Buzzzing"; }
                }
                else
                {
                    ButtonState = "Submit";
                }
            }
        }

        private string finalAnswer;
        public string FinalAnswer
        {
            get { return finalAnswer; }
            set
            {
                finalAnswer = value;
                OnPropertyChanged("FinalAnswer");
            }
        }

        private string prompts;
        public string Prompts
        {
            get { return prompts; }
            set
            {
                prompts = value;
                OnPropertyChanged("Prompts");
            }
        }

        private string alternate;
        public string Alternate
        {
            get { return alternate; }
            set
            {
                alternate = value;
                OnPropertyChanged("Alternate");
            }
        }

        private bool isCompleted;
        public bool IsCompleted
        {
            get { return isCompleted; }
            set
            {
                isCompleted = value;
                IsNotCompleted = !isCompleted;
                OnPropertyChanged("IsCompleted");
            }
        }

        private bool isNotCompleted;
        public bool IsNotCompleted
        {
            get { return isNotCompleted; }
            set
            {
                isNotCompleted = value;
                OnPropertyChanged("IsNotComplted");
            }
        }

        public double k { get; set; }

        public ReadQuestions Reader { get; set; }

        public bool isStarted { get; set; }

        public UserProfile UserProfile { get; set; }

        string UserBuzzedText { get; set; }

        public string CurrentClue { get; set; }
        
        public QuizzingPageViewModel(UserProfile userProfile)
        {
            UserProfile = userProfile;
            IsCompleted = true;
            IsNotCompleted = false;
            NextQuestion();
            QuestionText = "This is where the question will start reading. Press Withdraw to begin";
            ButtonState = "Start Reading";
        }

        public void Read()
        {
            QuestionText = "";
            Reader.ReadAQuestion(this);
        }

        public async void CheckAnswer()
        {
            UserBuzzedText = AnsweredText + "";
            
            QuestionText = Question.Question;
            FinalAnswer = Question.Answer;
            Alternate = Question.Alternate;
            Prompts = Question.Prompt;
            IsCompleted = true;
            Question.Answered += 1;

            await Check();
        }

        public async Task Check()
        {
            string userText = Stopwords.RemoveStopwords(AnsweredText);
            string ansText = Stopwords.RemoveStopwords(Question.Answer);

            var l = new NormalizedLevenshtein();
            var similarity = 1 - l.Distance(userText, ansText);

            if (similarity >= 0.8)
            {
                UpdateBuzzCorrect();
            }
            else
            {
                UpdateBuzzIncorrect();
            }

            await UpdateAnswered();
        }

        public async Task UpdateAnswered()
        {
            Answered ans = new Answered();
            ans.Answer = UserBuzzedText;
            ans.Correct = Question.Answer;
            ans.AnswerID = Question.AnswerID;
            ans.QuestionID = Question.ID;
            ans.Category = Question.Category;
            ans.Clue = CurrentClue;

            var Ans = UserProfile.Answers.FirstOrDefault(x => x.ID == Answer.ID);
            ans.Difficulty = Ans.Difficulty;
            ans.Negs = Ans.Negs;
            ans.Rating = Ans.Rating;
            ans.Score = Ans.Score;

            AnsweredRest ansRest = new AnsweredRest();
            ansRest.answerid = ans.AnswerID;
            ansRest.buzzed = ans.Answer;
            ansRest.clue = CurrentClue;
            ansRest.questionid = ans.QuestionID;
            ansRest.rating = ans.Rating;
            ansRest.score = ans.Score;
            ansRest.userid = Xamarin.Essentials.Preferences.Get("userid", 243);

            var current = Connectivity.NetworkAccess;
            if(current == NetworkAccess.Internet)
            {
                RestService r = new RestService();
                await r.Get("/wake");
                await r.PostAnswer("/postdata", ansRest);
            }

            DatabaseManager dbM = new DatabaseManager();
            await dbM.InsertAnsweredRead(ans);
        }

        public async Task UpdateScoreAndRating()
        {
            var ans = UserProfile.Answers.FirstOrDefault(x => x.ID == Answer.ID);
            var cat = UserProfile.Categories.FirstOrDefault(x => x.Id == Answer.Category);

            DatabaseManager dbM = new DatabaseManager();
            await dbM.UpdateAnswer(ans);
            await dbM.UpdateQuestion(Question);
            await dbM.UpdateUserCategory(cat);

        }

        public async void UpdateBuzzCorrect()
        {
            var ans = UserProfile.Answers.FirstOrDefault(x => x.ID == Answer.ID);
            var cat = UserProfile.Categories.FirstOrDefault(x => x.Id == Answer.Category);
            double prob = ProbabilityofCorr(ans.Rating, cat.User);
            
            cat.User += k * (1 - prob);
            ans.Rating += k * (0 - (1 - prob));

            ans.Corrects += 1;

            double d = (ans.Difficulty * 10);
            ans.Score -= d * ((ans.Corrects - ans.Negs) * Math.Log10(Math.Pow(d,2) + 1));

            AnsweredText = "Correct";

            await UpdateScoreAndRating();
        }

        public async void UpdateBuzzIncorrect()
        {
            var ans = UserProfile.Answers.FirstOrDefault(x => x.ID == Answer.ID);
            var cat = UserProfile.Categories.FirstOrDefault(x => x.Id == Answer.Category);
            double prob = ProbabilityofCorr(ans.Rating, cat.User);

            cat.User += k * (0 - prob);
            ans.Rating += k * (1 - (1 - prob));

            ans.Negs += 1;

            double d = (ans.Difficulty * 10);
            ans.Score -= d * ((ans.Corrects * Math.Log10(Math.Pow(d, 2) + 1)) - ans.Negs);

            AnsweredText = "Incorrect";

            await UpdateScoreAndRating();
        }

        public double ProbabilityofCorr(double answerRating, double userRating)
        {
            double probability;
            probability = 1 / (1 + Math.Pow(10, (answerRating - userRating)/400));
            return probability;
        }

        public void NextQuestion()
        {
            NewAnswerLine answerLine = new NewAnswerLine(UserProfile);
            Answer = answerLine.SelectedAnswer;
            Task.Run(() => answerLine.GetNewQuestion(Answer)).Wait();
            Question = answerLine.SelectedQuestion;
            AnsweredText = "";
            isStarted = true;
            Prompts = "";
            Alternate = "";
            IsCompleted = false;
            FinalAnswer = "";
            Reader = new ReadQuestions(Question, this);
            ButtonState = "Start Reading";
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
