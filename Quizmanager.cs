using System;
using System.Collections.Generic;

namespace CypherBotWPF
{
    // ═════════════════════════════════════════════════════════════════
    //  QUIZQUESTION — a single question in the cybersecurity quiz.
    //  Supports both multiple-choice and true/false formats.
    // ═════════════════════════════════════════════════════════════════
    public class QuizQuestion
    {
        public string QuestionText { get; set; }
        public bool IsTrueFalse { get; set; }
        public List<string> Options { get; set; }      // null/empty for True/False questions
        public string CorrectAnswer { get; set; }       // "A"/"B"/"C"/"D" for MCQ, "True"/"False" for T/F
        public string Explanation { get; set; }

        public QuizQuestion(string questionText, List<string> options, string correctAnswer, string explanation)
        {
            QuestionText = questionText;
            Options = options;
            CorrectAnswer = correctAnswer;
            Explanation = explanation;
            IsTrueFalse = options == null || options.Count == 0;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  QUIZMANAGER  (Task 2: Cybersecurity Mini-Game)
    //
    //  Holds the question bank, tracks the current question and score,
    //  and formats questions/feedback for display in the chat window.
    // ═════════════════════════════════════════════════════════════════
    public class QuizManager
    {
        private readonly List<QuizQuestion> questions;
        private int currentIndex;
        private int score;

        public bool IsActive { get; private set; }
        public int Score => score;
        public int TotalQuestions => questions.Count;
        public int QuestionsAnswered => currentIndex;

        public QuizManager()
        {
            questions = BuildQuestionBank();
        }

        // ── Build the question bank (12 questions: mixed MCQ / True-False) ──
        private List<QuizQuestion> BuildQuestionBank()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion(
                    "What should you do if you receive an email asking for your password?",
                    new List<string> { "A) Reply with your password", "B) Delete the email", "C) Report the email as phishing", "D) Ignore it" },
                    "C",
                    "Correct! Reporting phishing emails helps prevent scams and protects others too."),

                new QuizQuestion(
                    "It's safe to reuse the same strong password across multiple accounts, as long as it's complex.",
                    null,
                    "False",
                    "Even a strong password should never be reused — one breach exposes every account sharing it."),

                new QuizQuestion(
                    "Which of these is the best sign that a website is using a secure connection?",
                    new List<string> { "A) The site has a colourful logo", "B) The URL starts with HTTPS and shows a padlock", "C) The site loads quickly", "D) The site asks for your phone number" },
                    "B",
                    "Correct! HTTPS and the padlock icon mean your connection to the site is encrypted."),

                new QuizQuestion(
                    "Pretexting is a social engineering technique where an attacker invents a false scenario to trick you into giving up information.",
                    null,
                    "True",
                    "Correct! Pretexting relies on a believable but fake story to manipulate the victim."),

                new QuizQuestion(
                    "What is the safest way to manage many different passwords?",
                    new List<string> { "A) Write them all in a notebook", "B) Use the same password everywhere", "C) Use a reputable password manager", "D) Save them in a text file on your desktop" },
                    "C",
                    "Correct! A password manager generates and stores strong, unique passwords securely."),

                new QuizQuestion(
                    "Using public Wi-Fi for online banking is just as safe as using your home network.",
                    null,
                    "False",
                    "False! Public Wi-Fi can be intercepted by attackers — use a VPN or avoid sensitive transactions on it."),

                new QuizQuestion(
                    "Which of the following is a common red flag of a phishing email?",
                    new List<string> { "A) It addresses you by name", "B) It creates urgency, e.g. 'Act now or your account will be closed!'", "C) It comes from a known contact", "D) It has no attachments" },
                    "B",
                    "Correct! Urgency and fear tactics are classic phishing techniques designed to rush your judgement."),

                new QuizQuestion(
                    "Enabling two-factor authentication (2FA) significantly reduces the risk of account takeover.",
                    null,
                    "True",
                    "Correct! 2FA means an attacker needs both your password AND your second factor (e.g. phone) to log in."),

                new QuizQuestion(
                    "You receive a link from an unknown sender claiming you've won a prize. What's the safest action?",
                    new List<string> { "A) Click the link to claim your prize", "B) Forward it to friends", "C) Avoid clicking and verify independently or delete it", "D) Reply asking for more details" },
                    "C",
                    "Correct! Unsolicited prize links are almost always scams — never click, just delete or report."),

                new QuizQuestion(
                    "Antivirus software only needs to be installed once and never needs updating.",
                    null,
                    "False",
                    "False! Antivirus definitions must be updated regularly to detect newly discovered threats."),

                new QuizQuestion(
                    "A caller claims to be from your bank's IT department and asks you to confirm your PIN over the phone. What should you do?",
                    new List<string> { "A) Give them your PIN to verify your identity", "B) Hang up and call your bank directly using the official number", "C) Ask them to call back later", "D) Provide a fake PIN" },
                    "B",
                    "Correct! This is a classic social engineering scam — banks never ask for your PIN over the phone."),

                new QuizQuestion(
                    "A strong password should ideally include uppercase, lowercase, numbers, and symbols.",
                    null,
                    "True",
                    "Correct! Mixing character types — and length — makes passwords far harder to crack.")
            };
        }

        // ── Start / restart the quiz ──────────────────────────────────────
        public void StartQuiz()
        {
            currentIndex = 0;
            score = 0;
            IsActive = true;
        }

        public void EndQuiz()
        {
            IsActive = false;
        }

        public bool HasMoreQuestions => currentIndex < questions.Count;

        // ── Format the current question for display ──────────────────────
        public string GetCurrentQuestionFormatted()
        {
            if (!HasMoreQuestions) return null;

            QuizQuestion q = questions[currentIndex];
            string header = $"Question {currentIndex + 1} of {questions.Count}:\n{q.QuestionText}";

            if (q.IsTrueFalse)
            {
                return $"{header}\n(Answer True or False)";
            }
            else
            {
                return $"{header}\n{string.Join("\n", q.Options)}";
            }
        }

        // ── Submit the user's answer for the current question ─────────────
        // Returns the feedback text (correct/incorrect + explanation).
        public string SubmitAnswer(string userAnswer, out bool wasCorrect)
        {
            wasCorrect = false;
            if (!HasMoreQuestions) return "The quiz has already finished.";

            QuizQuestion q = questions[currentIndex];
            string cleaned = userAnswer.Trim().ToUpper();

            // Normalise the user's answer for comparison (accepts "A", "A)", "a", "True", "T", etc.)
            string normalisedAnswer = NormaliseAnswer(cleaned, q.IsTrueFalse);
            string normalisedCorrect = q.CorrectAnswer.Trim().ToUpper();

            wasCorrect = normalisedAnswer == normalisedCorrect;

            string feedback;
            if (wasCorrect)
            {
                score++;
                feedback = $"Correct! {q.Explanation}";
            }
            else
            {
                feedback = $"Not quite — the correct answer was {q.CorrectAnswer}. {q.Explanation}";
            }

            currentIndex++;
            return feedback;
        }

        private string NormaliseAnswer(string raw, bool isTrueFalse)
        {
            raw = raw.Trim();
            if (isTrueFalse)
            {
                if (raw.StartsWith("T")) return "TRUE";
                if (raw.StartsWith("F")) return "FALSE";
                return raw;
            }
            else
            {
                // Accept "A", "A)", "(A)", "A." etc. — but only as an isolated
                // leading letter, so words like "DONT KNOW" don't falsely match "D".
                if (raw.Length == 0) return raw;

                char first = raw[0] == '(' && raw.Length > 1 ? raw[1] : raw[0];
                if (first >= 'A' && first <= 'D')
                {
                    int posAfterLetter = (raw[0] == '(') ? 2 : 1;
                    bool isIsolated = raw.Length == posAfterLetter ||
                                       raw[posAfterLetter] == ')' || raw[posAfterLetter] == '.' ||
                                       raw[posAfterLetter] == ' ' || raw[posAfterLetter] == ':';
                    if (isIsolated) return first.ToString();
                }
                return raw;
            }
        }

        // ── Final score summary with motivational feedback ────────────────
        public string GetFinalScoreMessage()
        {
            double percentage = questions.Count == 0 ? 0 : (double)score / questions.Count * 100;
            string motivational;

            if (percentage >= 80)
                motivational = "Great job! You're a cybersecurity pro!";
            else if (percentage >= 50)
                motivational = "Good effort! Keep learning to stay even safer online.";
            else
                motivational = "Keep learning to stay safe online — every bit of knowledge helps!";

            return $"Quiz complete! You scored {score} out of {questions.Count} ({percentage:F0}%). {motivational}";
        }
    }
}