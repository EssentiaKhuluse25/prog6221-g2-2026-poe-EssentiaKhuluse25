using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CybersecurityChatbotWPF
{
    public partial class MainWindow : Window
    {
        // Memory variables
        private string userName = "";
        private string userInterest = "";
        private string lastTopic = "";

        // Sentiment detection keywords
        private Dictionary<string, string> sentimentKeywords;

        // Response collections - using Lists for random responses
        private Dictionary<string, List<string>> randomResponses;
        private Dictionary<string, string> keywordResponses;

        // Delegate for sentiment-based responses
        public delegate string SentimentResponseDelegate(string userMessage, string topic);

        public MainWindow()
        {
            InitializeComponent();
            InitializeChatbot();
            PlayVoiceGreeting();
        }

        private void InitializeChatbot()
        {
            // Sentiment keywords
            sentimentKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "worried", "concerned" },
                { "scared", "concerned" },
                { "nervous", "concerned" },
                { "anxious", "concerned" },
                { "happy", "positive" },
                { "excited", "positive" },
                { "great", "positive" },
                { "frustrated", "negative" },
                { "angry", "negative" },
                { "confused", "negative" },
                { "curious", "curious" },
                { "interested", "curious" }
            };

            // Random responses using Lists
            randomResponses = new Dictionary<string, List<string>>();

            randomResponses["phishing"] = new List<string>
            {
                "Be cautious of emails asking for personal information. Scammers disguise themselves as trusted organisations.",
                "Always check the sender's email address carefully - scammers use addresses that look similar.",
                "Never click on links in suspicious emails. Hover over them first to see the actual URL.",
                "Legitimate companies never ask for your password via email. This is a major red flag!"
            };

            randomResponses["password"] = new List<string>
            {
                "Use passwords with at least 12 characters combining uppercase, lowercase, numbers, and symbols.",
                "Never reuse passwords across different accounts. Each account needs its own unique password.",
                "Use a password manager like Bitwarden or LastPass to generate and store strong passwords.",
                "Enable Two-Factor Authentication (2FA) whenever possible for an extra layer of security."
            };

            randomResponses["scam"] = new List<string>
            {
                "If an offer seems too good to be true, it probably is a scam.",
                "Never send money or gift cards to someone you've only met online.",
                "Scammers create urgency - always take time to verify before acting.",
                "The 'CEO scam' asks employees to transfer money urgently - always verify through another channel."
            };

            randomResponses["privacy"] = new List<string>
            {
                "Review your social media privacy settings regularly to control who sees your information.",
                "Be careful what you share online - once posted, it's difficult to remove completely.",
                "Use different email addresses for different purposes (personal, shopping, work).",
                "Check if your data has been breached using websites like HaveIBeenPwned.com."
            };

            randomResponses["browsing"] = new List<string>
            {
                "Look for 'https://' and the padlock icon in your browser's address bar.",
                "Avoid using public Wi-Fi for banking or shopping without a VPN.",
                "Keep your browser and extensions updated to the latest versions.",
                "Use ad blockers to reduce exposure to malicious advertisements."
            };

            // Static keyword responses
            keywordResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "how are you", "I'm functioning well and ready to help you stay safe online!" },
                { "what is your purpose", "My purpose is to educate South African citizens about cybersecurity threats like phishing, malware, and social engineering." },
                { "what can i ask", "You can ask me about passwords, phishing, scams, privacy, safe browsing, or share how you're feeling about online security." }
            };
        }

        private void PlayVoiceGreeting()
        {
            try
            {
                string audioPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "greeting.wav");
                if (System.IO.File.Exists(audioPath))
                {
                    SoundPlayer player = new SoundPlayer(audioPath);
                    player.Play();
                }
            }
            catch (Exception) { }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                MessageBox.Show("Please enter your name.", "Name Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            userName = txtUserName.Text.Trim();
            txtUserName.IsEnabled = false;
            btnStart.IsEnabled = false;
            txtUserInput.IsEnabled = true;
            btnSend.IsEnabled = true;

            AddToChat($"Hello, {userName}! Welcome to the Cybersecurity Awareness Bot for South Africa.", "Bot");
            AddToChat("I'm here to help you stay safe online. How are you feeling about cybersecurity today?", "Bot");
        }

        private void BtnPlayVoice_Click(object sender, RoutedEventArgs e)
        {
            PlayVoiceGreeting();
            AddToChat("Voice greeting played!", "System");
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            ProcessUserInput();
        }

        private void TxtUserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessUserInput();
            }
        }

        private void ProcessUserInput()
        {
            string userMessage = txtUserInput.Text.Trim();

            // Input validation for empty entries
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                AddToChat("I didn't quite understand that. Could you rephrase?", "Bot");
                txtUserInput.Clear();
                return;
            }

            AddToChat(userMessage, userName);
            txtUserInput.Clear();

            // Exit command
            if (userMessage.ToLower() == "exit" || userMessage.ToLower() == "quit")
            {
                AddToChat($"Goodbye, {userName}! Stay safe online!", "Bot");
                txtUserInput.IsEnabled = false;
                btnSend.IsEnabled = false;
                return;
            }

            string response = ProcessWithSentiment(userMessage);
            AddToChat(response, "Bot");
        }

        private string ProcessWithSentiment(string userMessage)
        {
            string lowerMsg = userMessage.ToLower();
            string sentiment = DetectSentiment(lowerMsg);

            // Conversation flow - handle follow-up requests
            if (IsFollowUpRequest(lowerMsg))
            {
                if (!string.IsNullOrEmpty(lastTopic))
                {
                    return GetResponseForTopic(lastTopic, sentiment);
                }
                return "What topic would you like me to explain more about? Try asking about passwords, phishing, scams, privacy, or safe browsing.";
            }

            // Check keyword responses first
            foreach (var keyword in keywordResponses.Keys)
            {
                if (lowerMsg.Contains(keyword))
                {
                    return keywordResponses[keyword];
                }
            }

            // Check for cybersecurity topics
            if (lowerMsg.Contains("password") || lowerMsg.Contains("passphrase"))
            {
                lastTopic = "password";
                return GetResponseForTopic("password", sentiment);
            }
            else if (lowerMsg.Contains("phish"))
            {
                lastTopic = "phishing";
                return GetResponseForTopic("phishing", sentiment);
            }
            else if (lowerMsg.Contains("scam") || lowerMsg.Contains("fraud"))
            {
                lastTopic = "scam";
                return GetResponseForTopic("scam", sentiment);
            }
            else if (lowerMsg.Contains("privacy") || lowerMsg.Contains("data"))
            {
                lastTopic = "privacy";
                // Memory - store user interest
                if (string.IsNullOrEmpty(userInterest))
                {
                    userInterest = "privacy";
                    return $"Great! I'll remember that you're interested in privacy. " + GetResponseForTopic("privacy", sentiment);
                }
                return GetResponseForTopic("privacy", sentiment);
            }
            else if (lowerMsg.Contains("brows") || lowerMsg.Contains("web") || lowerMsg.Contains("internet"))
            {
                lastTopic = "browsing";
                return GetResponseForTopic("browsing", sentiment);
            }
            else if (lowerMsg.Contains("how") && lowerMsg.Contains("feel"))
            {
                return HandleSentimentQuestion(userMessage);
            }

            // Default response for unrecognized input (error handling)
            return "I'm not sure I understand. Can you try rephrasing? You can ask me about passwords, phishing, scams, privacy, or safe browsing.";
        }

        private string DetectSentiment(string message)
        {
            foreach (var keyword in sentimentKeywords)
            {
                if (message.Contains(keyword.Key))
                {
                    return keyword.Value;
                }
            }
            return "neutral";
        }

        private string HandleSentimentQuestion(string userMessage)
        {
            if (userMessage.ToLower().Contains("worried") || userMessage.ToLower().Contains("scared"))
            {
                return "It's completely understandable to feel worried about online threats. Scammers can be very convincing. Let me share something helpful. " + GetRandomResponse("phishing");
            }
            else if (userMessage.ToLower().Contains("confused"))
            {
                return "No problem at all! Let me explain clearly. " + GetRandomResponse(lastTopic != "" ? lastTopic : "password");
            }
            else if (userMessage.ToLower().Contains("frustrated"))
            {
                return "I understand it can be frustrating. Let's take it step by step. " + GetRandomResponse(lastTopic != "" ? lastTopic : "password");
            }
            else if (userMessage.ToLower().Contains("curious") || userMessage.ToLower().Contains("interested"))
            {
                return "That's great to hear! Learning about cybersecurity is important. " + GetRandomResponse(lastTopic != "" ? lastTopic : "password");
            }
            else
            {
                return "How you're feeling matters! If you're worried about online security, just let me know and I'll help you stay safe.";
            }
        }

        private string GetResponseForTopic(string topic, string sentiment)
        {
            string baseResponse = GetRandomResponse(topic);

            // Adjust response based on sentiment (empathetic responses)
            if (sentiment == "concerned")
            {
                return "I understand your concern. " + baseResponse;
            }
            else if (sentiment == "positive")
            {
                return "That's great to hear! " + baseResponse + " Keep up the good security habits!";
            }
            else if (sentiment == "curious")
            {
                return "Great question! " + baseResponse + " Would you like to know more about this topic?";
            }

            // Memory - recall user interest
            if (!string.IsNullOrEmpty(userInterest) && userInterest == topic)
            {
                return $"As someone interested in {topic}, here's a helpful tip: " + baseResponse;
            }

            return baseResponse;
        }

        private string GetRandomResponse(string topic)
        {
            if (randomResponses.ContainsKey(topic) && randomResponses[topic].Count > 0)
            {
                Random rand = new Random();
                int index = rand.Next(randomResponses[topic].Count);
                return randomResponses[topic][index];
            }

            // Fallback responses
            if (topic == "password")
                return "Create strong passwords with at least 12 characters, mixing letters, numbers, and symbols. Never share them with anyone!";
            if (topic == "phishing")
                return "Always verify the sender's email address before clicking any links or downloading attachments.";
            if (topic == "scam")
                return "If someone pressures you for immediate action or payment, it's likely a scam. Take time to verify.";
            if (topic == "privacy")
                return "Regularly check your account security settings and remove apps you no longer use.";
            if (topic == "browsing")
                return "Use HTTPS websites and avoid entering personal info on unsecured connections.";

            return "Stay vigilant online! Always think before you click.";
        }

        private bool IsFollowUpRequest(string message)
        {
            string lowerMsg = message.ToLower();
            return lowerMsg.Contains("another tip") ||
                   lowerMsg.Contains("tell me more") ||
                   lowerMsg.Contains("explain more") ||
                   lowerMsg.Contains("more about") ||
                   lowerMsg.Contains("say that again") ||
                   lowerMsg.Contains("what else");
        }

        private void QuickTopic_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(userName))
            {
                MessageBox.Show("Please enter your name and click Start Chat first.", "Not Started", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Button btn = sender as Button;
            string topic = btn.Tag.ToString();

            AddToChat($"Tell me about {topic}", userName);
            lastTopic = topic;

            // Memory - store interest
            if (string.IsNullOrEmpty(userInterest))
            {
                userInterest = topic;
                AddToChat($"Great! I'll remember that you're interested in {topic}. " + GetResponseForTopic(topic, "curious"), "Bot");
            }
            else
            {
                AddToChat(GetResponseForTopic(topic, "curious"), "Bot");
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            lstChat.Items.Clear();
            AddToChat("Chat history cleared. How can I help you today?", "Bot");
        }

        private void AddToChat(string message, string senderName)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Dispatcher.Invoke(() =>
            {
                lstChat.Items.Add($"[{timestamp}] {senderName}: {message}");
                lstChat.ScrollIntoView(lstChat.Items[lstChat.Items.Count - 1]);
            });
        }
    }
}