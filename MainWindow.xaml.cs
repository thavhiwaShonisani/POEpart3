using System.Speech.Synthesis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CypherBotWPF
{
    public partial class MainWindow : Window
    {
        // ── Memory (based on reference code) ─────────────────────────────
        Dictionary<string, string> memory = new Dictionary<string, string>();

        // ── Speech ────────────────────────────────────────────────────────
        SpeechSynthesizer speech = new SpeechSynthesizer();

        // ── Random number generator for varied responses ──────────────────
        Random rng = new Random();

        // ── Last topic discussed (for follow-up / conversation flow) ──────
        string lastTopic = "";

        // ── Task 2: Quiz mini-game ──────────────────────────────────────────
        QuizManager quiz = new QuizManager();

        // ── Task 1: Task Assistant — in-memory task/reminder storage ───────
        private TaskDatabaseManager taskManager = new TaskDatabaseManager();
        int pendingTaskId = -1;                 // Id of the task awaiting a yes/no reminder prompt
        string? pendingTaskTitle = null;        // Title of that task, used in chat messages
        bool awaitingReminderTimeframe = false; // true once the user said "yes" and we're waiting for a timeframe

        // Friendly, pre-written descriptions for common cybersecurity tasks.
        // If the user's task title matches one of these keywords, the bot
        // expands it into a fuller description (matching the brief's example
        // of "Review privacy settings" → "Review account privacy settings
        // to ensure your data is protected.").
        Dictionary<string, string> taskDescriptionTemplates = new Dictionary<string, string>()
        {
            { "privacy",        "Review account privacy settings to ensure your data is protected." },
            { "2fa",             "Enable two-factor authentication to add an extra layer of security to your account." },
            { "two-factor",      "Enable two-factor authentication to add an extra layer of security to your account." },
            { "two factor",      "Enable two-factor authentication to add an extra layer of security to your account." },
            { "password",       "Update your password to a strong, unique passphrase that isn't reused elsewhere." },
            { "backup",          "Back up your important files and verify the backup can be restored." },
            { "update",          "Install the latest software and security updates on your devices." },
            { "antivirus",       "Run a full antivirus scan and confirm your virus definitions are up to date." },
            { "firewall",        "Check that your firewall is enabled and correctly configured." },
            { "wifi",            "Review your home Wi-Fi settings and ensure WPA3/WPA2 encryption is enabled." },
            { "phishing",        "Review recent emails for phishing red flags and report any suspicious messages." },
            { "vpn",             "Set up a VPN for secure browsing, especially on public Wi-Fi." },
            { "breach",          "Check haveibeenpwned.com to see if any of your accounts were exposed in a breach." },
        };

        // ── Task 4: Activity log ────────────────────────────────────────────
        ActivityLogManager activityLog = new ActivityLogManager();

        // ═════════════════════════════════════════════════════════════════
        //  KEYWORD → LIST OF RESPONSES  (varied, randomly selected)
        // ═════════════════════════════════════════════════════════════════
        Dictionary<string, List<string>> responses = new Dictionary<string, List<string>>()
        {
            {
                "Hello", new List<string>
                {
                "Hello! I'm CypherBot, your friendly cybersecurity assistant. Ask me anything about staying safe online!",
                "Hi there! I'm CypherBot, here to help you with cybersecurity tips and advice. What would you like to know?",
                "Greetings! I'm CypherBot, your go-to source for cybersecurity information. How can I assist you today?"
                }
            },

            {
                "I need help", new List<string>
                {
                    "Of course! I'm here to help. What cybersecurity topic are you interested in?",
                    "Sure! What specific area of cybersecurity do you need assistance with?",
                    "I'm happy to assist you. Please let me know what cybersecurity topic you'd like to learn about."
                }
            },

            {
                "password", new List<string>
                {
                    "Use a passphrase — four random words are both memorable and very strong.",
                    "Never reuse passwords. A single breach can expose every account sharing that password.",
                    "Let a password manager generate 20+ character random passwords for every site.",
                    "Change passwords immediately if you suspect a breach, and check haveibeenpwned.com regularly.",
                    "Use uppercase, lowercase, numbers, and symbols. Never share your password with anyone."
                }
            },
            {
                "phishing", new List<string>
                {
                    "Phishing emails mimic trusted brands to steal your credentials. Always verify the sender's domain.",
                    "Be cautious of emails creating urgency — 'Your account will be closed!' is a classic phishing tactic.",
                    "Hover over links before clicking. Phishers disguise malicious URLs to look legitimate.",
                    "When in doubt, go directly to the official website instead of clicking any emailed link.",
                    "Legitimate companies will never ask for your password via email."
                }
            },
            {
                "scam", new List<string>
                {
                    "Never click suspicious links or provide personal information via email.",
                    "Scammers often impersonate banks or government agencies. Call them directly to verify.",
                    "If an offer sounds too good to be true, it almost certainly is.",
                    "Gift card payment requests are almost always scams — no legitimate organisation uses them.",
                    "Romance scams are increasing — be wary of anyone online who quickly asks for money."
                }
            },
            {
                "malware", new List<string>
                {
                    "Malware often arrives via email attachments or fake software downloads. Never open unexpected attachments.",
                    "Keep your OS and software patched — most attacks exploit known, unpatched vulnerabilities.",
                    "Run a reputable antivirus with real-time protection and schedule weekly full-system scans.",
                    "Ransomware encrypts your files and demands payment. Regular offline backups are your best protection.",
                    "Only download software from official, verified sources to avoid malware infections."
                }
            },
            {
                "privacy", new List<string>
                {
                    "Never share your full name, address, or phone number in public online spaces.",
                    "Use unique email aliases for sign-ups to limit data exposure from breaches.",
                    "Regularly audit the privacy settings on all your social media and online accounts.",
                    "Consider using a privacy-focused browser like Firefox or Brave and a search engine like DuckDuckGo.",
                    "Review what data apps collect about you and revoke permissions you don't need."
                }
            },
            {
                "antivirus", new List<string>
                {
                    "Use reputable antivirus software and keep it updated — new threats emerge daily.",
                    "Enable real-time protection so threats are caught before they can cause damage.",
                    "Schedule regular full-system scans, not just quick scans.",
                    "A firewall combined with antivirus gives you a much stronger defence.",
                    "Free antivirus can be effective, but paid solutions often offer better real-time protection."
                }
            },
            {
                "2fa", new List<string>
                {
                    "Enable two-factor authentication on every account that supports it.",
                    "Authenticator apps like Google Authenticator are safer than SMS-based 2FA.",
                    "Hardware keys like YubiKey offer the strongest form of 2FA — phishing-resistant.",
                    "Avoid SMS-based 2FA where possible — SIM-swapping attacks can intercept SMS codes.",
                    "2FA means attackers need your password AND your device. It stops most account takeovers."
                }
            },
            {
                "vpn", new List<string>
                {
                    "A VPN encrypts your internet connection and hides your IP address.",
                    "Always use a VPN on public Wi-Fi to prevent eavesdropping.",
                    "Choose a VPN with a strict no-logs policy so your activity isn't recorded.",
                    "A VPN protects your data in transit but doesn't protect against malware on your device.",
                    "Free VPNs often sell your data — invest in a reputable paid service."
                }
            },
            {
                "ransomware", new List<string>
                {
                    "Regular offline backups are your best defence against ransomware.",
                    "Never pay the ransom — it funds criminals and doesn't guarantee you'll get your files back.",
                    "Ransomware often spreads through phishing emails — train yourself to spot them.",
                    "Keep your OS patched; many ransomware attacks exploit outdated systems.",
                    "Network segmentation can limit how far ransomware spreads across an organisation."
                }
            },
            {
                "backup", new List<string>
                {
                    "Follow the 3-2-1 rule: 3 copies, on 2 different media, with 1 stored offsite.",
                    "Automate your backups so you never forget — and test restores regularly.",
                    "Cloud backups are convenient but also back up locally for resilience.",
                    "Ransomware can encrypt cloud-synced files too — keep a disconnected backup.",
                    "Verify your backups actually work by restoring a test file periodically."
                }
            },
            {
                "update", new List<string>
                {
                    "Install software and OS updates promptly — most patches fix critical vulnerabilities.",
                    "Enable automatic updates so you never fall behind on security patches.",
                    "Outdated software is one of the most common entry points for attackers.",
                    "Don't just update your OS — update browsers, plugins, and apps too.",
                    "End-of-life software no longer receives patches, making it a serious risk."
                }
            },
            {
                "wifi", new List<string>
                {
                    "Avoid sensitive transactions on public Wi-Fi — use a VPN if you must connect.",
                    "Use WPA3 encryption on your home router for the strongest wireless security.",
                    "Change your router's default admin password immediately after setup.",
                    "A guest network for IoT devices keeps them isolated from your main devices.",
                    "Turn off Wi-Fi when you're not using it to reduce your attack surface."
                }
            },
            {
                "yes", new List<string>
                {
                    "Great! Happy to help.",
                    "Glad you're interested.",
                    "That's wonderful!"
                }
            },
            {
                "no", new List<string>
                {
                    "No problem! Let me know if you need anything else.",
                    "That's okay, feel free to ask something else.",
                    "No worries, I'm here to help with other topics."
                }
            },
            {
                "help", new List<string>
                {
                    "I'm here to help! Ask me about passwords, phishing, malware, privacy, encryption, or any cybersecurity topic.",
                    "I can assist you with cybersecurity awareness. What would you like to know?",
                    "Sure! What cybersecurity topic would you like help with?"
                }
            },
            {
                "thank you", new List<string>
                {
                    "You're welcome! Stay safe online.",
                    "Happy to help! Feel free to ask me anything else.",
                    "My pleasure! Keep learning about cybersecurity."
                }
            },
            {
                "thanks", new List<string>
                {
                    "You're welcome! Stay safe online.",
                    "Happy to help! Feel free to ask me anything else.",
                    "My pleasure! Keep learning about cybersecurity."
                }
            },
            {
                "i dont understand", new List<string>
                {
                    "I apologize for being unclear. Could you rephrase your question or ask about a specific cybersecurity topic?",
                    "Let me try to explain better. Which part didn't make sense?",
                    "Sorry about that! Ask me about a specific security topic like passwords or phishing."
                }
            },
            {
                "another tip", new List<string>
                {
                    "Sure! Here's another valuable tip for staying secure online.",
                    "I'd be happy to share more insights with you.",
                    "Let me give you another security recommendation."
                }
            },
            {
                "explain more", new List<string>
                {
                    "I'll give you more details to help you understand better.",
                    "Let me expand on that with more information.",
                    "Here's a deeper explanation for you."
                }
            },
            {
                "tell me more", new List<string>
                {
                    "Absolutely! I have more information for you.",
                    "I'm happy to share more details.",
                    "Here's additional information on this topic."
                }
            },
            {
                "encryption", new List<string>
                {
                    "Encryption scrambles your data using complex mathematics so only authorized people can read it.",
                    "End-to-end encryption ensures your messages are protected from the moment you send them until received.",
                    "Always use encrypted connections (HTTPS) when entering sensitive information online.",
                    "Encryption is the backbone of digital security — it protects your data at rest and in transit."
                }
            },
            {
                "social engineering", new List<string>
                {
                    "Social engineering manipulates people into divulging confidential information through psychological tactics.",
                    "Attackers use pretexting, baiting, and tailgating to trick employees into bypassing security.",
                    "Never share sensitive information with anyone, even if they claim to be from your company.",
                    "Security awareness training is your best defence against social engineering attacks."
                }
            },
            {
                "credential stuffing", new List<string>
                {
                    "Credential stuffing is when attackers use stolen username-password pairs from one breach to access other accounts.",
                    "Never reuse passwords across sites — if one account is breached, all others are vulnerable.",
                    "Use a unique, strong password for every online account to prevent credential stuffing attacks.",
                    "Password managers can help you maintain unique passwords for every service."
                }
            },
            {
                "brute force", new List<string>
                {
                    "Brute force attacks involve trying many password combinations until finding the correct one.",
                    "Strong, long passwords make brute force attacks impractical — they'd take millions of years to crack.",
                    "Account lockout policies after failed login attempts help defend against brute force attacks.",
                    "Longer passwords exponentially increase the time needed for a brute force attack to succeed."
                }
            },
            {
                "ddos", new List<string>
                {
                    "A DDoS (Distributed Denial of Service) attack floods a server with traffic to make it unavailable.",
                    "Botnets, which are networks of compromised devices, are commonly used to launch DDoS attacks.",
                    "DDoS attacks can target websites, servers, and entire infrastructure to disrupt services.",
                    "Organizations use DDoS mitigation services to detect and block malicious traffic."
                }
            },
            {
                "sql injection", new List<string>
                {
                    "SQL injection exploits vulnerabilities in web applications by inserting malicious database commands.",
                    "Never trust user input — always validate and sanitize data before using it in database queries.",
                    "Developers should use parameterized queries to prevent SQL injection attacks.",
                    "SQL injection can expose sensitive databases containing customer information."
                }
            },
            {
                "cross-site scripting", new List<string>
                {
                    "Cross-site scripting (XSS) allows attackers to inject malicious scripts into web pages you visit.",
                    "XSS attacks steal cookies, session tokens, and other sensitive information from your browser.",
                    "Keep your browser and plugins updated to protect against XSS exploits.",
                    "Web developers should encode user input to prevent XSS vulnerabilities."
                }
            },
            {
                "man in the middle", new List<string>
                {
                    "A Man-in-the-Middle attack intercepts communication between you and a website or service.",
                    "Always look for the HTTPS padlock icon to ensure your connection is encrypted.",
                    "Avoid using public Wi-Fi for sensitive transactions — attackers can intercept your data.",
                    "VPNs protect you from man-in-the-middle attacks on untrusted networks."
                }
            },
            {
                "zero day", new List<string>
                {
                    "A zero-day is a vulnerability unknown to the software vendor until attackers exploit it.",
                    "Zero-day exploits are highly valuable and dangerous because there's no patch available yet.",
                    "Keep software updated with the latest security patches to minimize zero-day exposure.",
                    "Bug bounty programs help discover zero-days before attackers can exploit them."
                }
            },
            {
                "firewall", new List<string>
                {
                    "A firewall is a barrier between your network and untrusted networks, controlling incoming and outgoing traffic.",
                    "Enable your operating system's built-in firewall for an essential layer of protection.",
                    "Both software firewalls on your device and hardware firewalls on your network provide defence.",
                    "Firewalls block unauthorized access while allowing legitimate traffic through."
                }
            },
            {
                "password manager", new List<string>
                {
                    "A password manager securely stores and generates complex passwords for all your accounts.",
                    "Popular password managers like Bitwarden, 1Password, and LastPass encrypt your passwords.",
                    "Using a password manager makes it easy to maintain unique, strong passwords everywhere.",
                    "Master password protection ensures only you can access passwords stored in the manager."
                }
            },
            {
                "data breach", new List<string>
                {
                    "A data breach occurs when sensitive information is accessed or stolen by unauthorized parties.",
                    "Check haveibeenpwned.com to see if your email appears in known data breaches.",
                    "After a breach, change your passwords immediately, especially on important accounts.",
                    "Data breaches can compromise personal information, financial details, and identity."
                }
            },
            {
                "biometric", new List<string>
                {
                    "Biometric authentication uses fingerprints, facial recognition, or iris scans to verify your identity.",
                    "Biometrics are more secure than passwords because they're difficult to replicate or steal.",
                    "Multi-factor authentication combining biometrics with passwords provides maximum security.",
                    "Biometric data is personal and sensitive — ensure services handling it have strong protections."
                }
            },
            {
                "authentication", new List<string>
                {
                    "Authentication verifies you are who you claim to be through credentials like passwords or biometrics.",
                    "Strong authentication methods combine something you know (password), have (phone), or are (biometric).",
                    "Never share your authentication credentials with anyone, even support personnel.",
                    "Multi-factor authentication significantly improves security by requiring multiple verification methods."
                }
            },
            {
                "authorization", new List<string>
                {
                    "Authorization determines what actions an authenticated user is allowed to perform.",
                    "Principle of least privilege means users should only have access to what they absolutely need.",
                    "Regular access reviews ensure people don't retain permissions for roles they no longer hold.",
                    "Authorization controls prevent unauthorized users from accessing sensitive data or functions."
                }
            },
            {
                "access control", new List<string>
                {
                    "Access control limits who can access resources based on authentication and authorization rules.",
                    "Role-based access control (RBAC) grants permissions based on job responsibilities.",
                    "Regular access audits help identify and remove unnecessary permissions.",
                    "Principle of least privilege ensures users have minimal necessary access."
                }
            },
            {
                "vulnerability", new List<string>
                {
                    "A vulnerability is a weakness in software or systems that attackers can exploit.",
                    "Vulnerabilities range from configuration errors to code defects that create security gaps.",
                    "Regular security assessments and penetration testing identify vulnerabilities before attackers do.",
                    "Patching vulnerabilities promptly is critical to maintaining security."
                }
            },
            {
                "exploit", new List<string>
                {
                    "An exploit is a technique that takes advantage of a vulnerability to compromise a system.",
                    "Exploits can range from simple to highly sophisticated depending on the vulnerability.",
                    "Using publicly available exploits makes attacks easier for less skilled attackers.",
                    "Security tools and patches help defend against known exploits."
                }
            },
            {
                "patch management", new List<string>
                {
                    "Patch management involves regularly updating software to fix vulnerabilities and bugs.",
                    "Enable automatic updates so you receive security patches without delay.",
                    "Test patches in a controlled environment before deploying to critical systems.",
                    "Unpatched systems are a major security risk — make updates a priority."
                }
            },
            {
                "security awareness", new List<string>
                {
                    "Security awareness training teaches employees to recognize and prevent security threats.",
                    "Informed users are your strongest defence against phishing, social engineering, and attacks.",
                    "Regular security training helps people understand threats and follow best practices.",
                    "Creating a security-conscious culture reduces human error and improves overall security."
                }
            },
            {
                "incident response", new List<string>
                {
                    "Incident response is a coordinated process to detect, investigate, and recover from security breaches.",
                    "Having an incident response plan reduces damage and recovery time after an attack.",
                    "Immediate containment stops attackers from spreading further after detection.",
                    "Post-incident analysis helps prevent similar attacks in the future."
                }
            },
            {
                "penetration testing", new List<string>
                {
                    "Penetration testing simulates real attacks to identify vulnerabilities before criminals do.",
                    "Ethical hackers conduct authorized penetration tests to strengthen security defences.",
                    "Regular penetration testing helps organizations understand their real security posture.",
                    "Test results guide prioritization of security improvements and investments."
                }
            },
            {
                "network security", new List<string>
                {
                    "Network security protects data flowing across your organization's network from unauthorized access.",
                    "Firewalls, intrusion detection systems, and VPNs are key network security tools.",
                    "Network segmentation isolates critical systems and limits lateral movement by attackers.",
                    "Monitoring network traffic helps detect suspicious activity and potential breaches."
                }
            },
            {
                "cloud security", new List<string>
                {
                    "Cloud security protects data and applications stored in cloud environments from threats.",
                    "Choose cloud providers with strong security certifications and compliance records.",
                    "Enable encryption, access controls, and monitoring for all cloud resources.",
                    "Understand shared security responsibility — cloud providers secure infrastructure, you secure your data."
                }
            },
            {
                "mobile security", new List<string>
                {
                    "Mobile security protects smartphones and tablets from malware, data theft, and unauthorized access.",
                    "Use strong passwords or biometric locks on your mobile devices.",
                    "Install apps only from official app stores and keep them updated.",
                    "Enable remote wipe capabilities in case your mobile device is lost or stolen."
                }
            },
            {
                "endpoint security", new List<string>
                {
                    "Endpoint security protects devices like computers, phones, and tablets from cyber threats.",
                    "Endpoint Detection and Response (EDR) tools monitor devices for suspicious activity.",
                    "Antivirus software, firewalls, and encryption work together for endpoint protection.",
                    "Regular patching and updates keep endpoints protected against known vulnerabilities."
                }
            },
            {
                "cyber hygiene", new List<string>
                {
                    "Cyber hygiene refers to basic security practices and habits that protect your systems.",
                    "Regular password changes, timely updates, and backups are essential cyber hygiene practices.",
                    "Maintaining good cyber hygiene prevents most common attacks.",
                    "Think of cyber hygiene like personal hygiene — daily habits keep you healthy and secure."
                }
            },
            {
                "strong password", new List<string>
                {
                    "A strong password is at least 12-16 characters with uppercase, lowercase, numbers, and symbols.",
                    "Avoid common words, birthdays, and sequential numbers in your passwords.",
                    "Passphrases combining random words are both strong and memorable.",
                    "Strong passwords are your first line of defence against account takeover."
                }
            },
            {
                "default password", new List<string>
                {
                    "Change default passwords on all devices and services immediately after setup.",
                    "Attackers commonly target devices still using factory default credentials.",
                    "Default passwords are publicly known, making them extremely vulnerable.",
                    "Many breaches occur simply because administrators never changed default access credentials."
                }
            },
            {
                "api security", new List<string>
                {
                    "API security protects application programming interfaces from exploitation and data exposure.",
                    "Use API keys and tokens to authenticate and authorize API requests securely.",
                    "Rate limiting prevents abuse of APIs by attackers attempting DoS attacks.",
                    "Validate and sanitize all API inputs to prevent injection and other attacks."
                }
            },
        };

        // ═════════════════════════════════════════════════════════════════
        //  SENTIMENT KEYWORDS → empathetic prefix responses
        // ═════════════════════════════════════════════════════════════════
        Dictionary<string, string> sentimentPrefixes = new Dictionary<string, string>()
        {
            { "worried",    "It's completely understandable to feel that way. You're taking the right steps by learning. " },
            { "scared",     "It's completely understandable to feel that way. You're taking the right steps by learning. " },
            { "anxious",    "It's completely understandable to feel that way. You're taking the right steps by learning. " },
            { "confused",   "I hear you — this can be confusing. Let me try to explain it simply. " },
            { "frustrated", "I hear you — this can be confusing. Let me try to explain it simply. " },
            { "curious",    "Great question! Curiosity is your best security tool. " },
            { "interested", "Great question! Curiosity is your best security tool. " },
        };

        // ═════════════════════════════════════════════════════════════════
        //  FOLLOW-UP RESPONSES per topic (conversation flow)
        // ═════════════════════════════════════════════════════════════════
        Dictionary<string, List<string>> followUps = new Dictionary<string, List<string>>()
        {
            {
                "password", new List<string>
                {
                    "Also: never write passwords on sticky notes or store them in plain text files.",
                    "Longer is stronger — a 20-character passphrase beats a short complex password every time.",
                    "HaveIBeenPwned.com lets you check if your email has appeared in a known breach."
                }
            },
            {
                "phishing", new List<string>
                {
                    "Also: verify the sender's full email address, not just the display name — attackers spoof names easily.",
                    "Another tip: real organisations never ask you to confirm your password by clicking a link.",
                    "You can report phishing emails to your provider and to the Anti-Phishing Working Group."
                }
            },
            {
                "privacy", new List<string>
                {
                    "As someone interested in privacy, you might want to review the security settings on your accounts.",
                    "Browser fingerprinting tracks you even in incognito mode — Brave browser blocks most of it.",
                    "Regularly search your own name online to see what personal data is publicly visible.",
                    "Privacy settings on social media often expose more than you realize — audit them regularly."
                }
            },
            {
                "malware", new List<string>
                {
                    "Sandboxing unknown software before running it on your main system is an excellent precaution.",
                    "Zero-day malware can bypass traditional antivirus — behaviour-based detection catches it better.",
                    "Disconnect from the internet immediately if you suspect an active malware infection."
                }
            },
            {
                "ransomware", new List<string>
                {
                    "After a ransomware infection, isolate the device immediately to stop it spreading.",
                    "Report ransomware attacks to law enforcement — they may have decryption tools available.",
                    "Cyber insurance can help cover ransomware recovery costs for businesses."
                }
            },
            {
                "2fa", new List<string>
                {
                    "Start with your email account — it's the master key to all your other accounts.",
                    "If a site doesn't support 2FA, consider whether it's worth having an account there.",
                    "Backup codes are important — store them somewhere safe offline in case you lose your device."
                }
            },
            {
                "encryption", new List<string>
                {
                    "End-to-end encryption is critical for protecting your sensitive communications.",
                    "Always verify that websites use HTTPS before entering personal or financial information.",
                    "Encryption key management is essential — never store keys with encrypted data."
                }
            },
            {
                "data breach", new List<string>
                {
                    "After a data breach notification, prioritize changing passwords for affected accounts.",
                    "Monitor your credit reports for signs of identity theft following a breach.",
                    "Consider a credit freeze to prevent unauthorized account openings."
                }
            },
            {
                "social engineering", new List<string>
                {
                    "Always verify requests through official channels before sharing sensitive information.",
                    "Be suspicious of unexpected calls or emails asking for personal or financial details.",
                    "Trust your instincts — if something feels off, it probably is."
                }
            },
            {
                "strong password", new List<string>
                {
                    "Update your passwords regularly, especially for critical accounts like email and banking.",
                    "Never share your passwords with anyone, including IT support or friends.",
                    "Consider using a password manager to securely store and generate passwords."
                }
            },
            {
                "backup", new List<string>
                {
                    "Test your backups regularly to ensure they're working properly.",
                    "Keep at least one backup disconnected from your main device to protect against ransomware.",
                    "Verify backup encryption settings to ensure your backed-up data is protected."
                }
            },
            {
                "update", new List<string>
                {
                    "Don't delay updates — they often contain critical security patches.",
                    "Set devices to auto-update when possible to avoid missing important security fixes.",
                    "Even minor updates can contain patches for severe vulnerabilities."
                }
            },
        };

        // ═════════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═════════════════════════════════════════════════════════════════
        public MainWindow()
        {
            InitializeComponent();
            speech.Volume = 100;
            speech.Rate = 0;

            Loaded += (s, e) =>
            {
                AppendColoredText(txtChat, "CypherBot", "Hello! I'm CYPHERBOT, your cybersecurity awareness assistant. Please enter your name to get started.", Brushes.Green);
                Speak("Hello! I'm CypherBot, your cybersecurity awareness assistant. Please tell me your name.");
                RefreshTasksDataGrid();  // Initialize the DataGrid with existing tasks
            };
        }

        // ═════════════════════════════════════════════════════════════════
        //  BUTTON CLICK — main logic
        // ═════════════════════════════════════════════════════════════════
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string userText = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(userText)) return;

            string message = userText.ToLower();

            string userName = memory.ContainsKey("name") ? memory["name"] : "You";
            AppendColoredText(txtChat, userName, userText, Brushes.DodgerBlue);
            txtMessage.Clear();

            // ── Exit ─────────────────────────────────────────────────────
            if (message == "exit")
            {
                string bye = "Goodbye! Stay safe online!";
                AppendColoredText(txtChat, "CypherBot", bye, Brushes.Green);
                Speak(bye);
                Thread.Sleep(800);
                Application.Current.Shutdown();
                return;
            }

            // ── Store name (from reference code) ─────────────────────────
            // If name hasn't been set yet, treat first input as the name
            if (!memory.ContainsKey("name"))
            {
                if (string.IsNullOrWhiteSpace(userText))
                {
                    string errorReply = "Error: You must enter a valid name to proceed. Please enter your name.";
                    AppendColoredText(txtChat, "CypherBot", errorReply, Brushes.Red);
                    Speak(errorReply);
                    return;
                }
                memory["name"] = userText;
                string reply = "Nice to meet you, " + userText + "! I'll remember your name. You can ask me about passwords, phishing, malware, privacy, and more.";
                AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                Speak(reply);

                // Display the menu of available options
                string menu = GenerateOptionsMenu();
                AppendColoredText(txtChat, "CypherBot", menu, Brushes.Green);
                return;
            }

            // Allow "my name is" to update the name later
            if (message.Contains("my name is"))
            {
                string name = userText.Substring(
                    userText.IndexOf("my name is", StringComparison.OrdinalIgnoreCase) + 10).Trim();
                memory["name"] = name;
                string reply = "Nice to meet you, " + name + "! I'll remember your name. You can ask me about passwords, phishing, malware, privacy, and more.";
                AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                Speak(reply);
                return;
            }

            // ═════════════════════════════════════════════════════════════
            //  TASK 2: QUIZ — if a quiz is in progress, route the user's
            //  input to the quiz first, before anything else is checked.
            // ═════════════════════════════════════════════════════════════
            if (quiz.IsActive)
            {
                // Allow the user to bail out of the quiz early.
                if (message == "quit quiz" || message == "stop quiz" || message == "exit quiz" || message == "cancel quiz")
                {
                    quiz.EndQuiz();
                    string cancelMsg = "Quiz cancelled. Let me know if you'd like to try again later!";
                    AppendColoredText(txtChat, "CypherBot", cancelMsg, Brushes.Green);
                    Speak(cancelMsg);
                    activityLog.AddEntry("Quiz cancelled early by user.");
                    return;
                }

                string feedback = quiz.SubmitAnswer(userText, out bool wasCorrect);
                AppendColoredText(txtChat, "CypherBot", feedback, wasCorrect ? Brushes.Green : Brushes.OrangeRed);
                Speak(feedback);

                if (quiz.HasMoreQuestions)
                {
                    string nextQuestion = quiz.GetCurrentQuestionFormatted();
                    AppendColoredText(txtChat, "CypherBot", nextQuestion, Brushes.Green);
                }
                else
                {
                    string finalMsg = quiz.GetFinalScoreMessage();
                    AppendColoredText(txtChat, "CypherBot", finalMsg, Brushes.Green);
                    Speak(finalMsg);
                    activityLog.AddEntry($"Quiz completed - scored {quiz.Score}/{quiz.TotalQuestions}.");
                    quiz.EndQuiz();
                }
                return;
            }

            // ═════════════════════════════════════════════════════════════
            //  TASK 1: TASK ASSISTANT — handle a pending "would you like a
            //  reminder?" follow-up for a task that was just added.
            //  Handles both:
            //    "Yes"                       → ask for a timeframe next
            //    "Yes, remind me in 3 days"  → set the reminder immediately
            // ═════════════════════════════════════════════════════════════
            if (pendingTaskId != -1 && !awaitingReminderTimeframe)
            {
                if (ContainsWord(message, "no"))
                {
                    string okReply = "No problem, I won't set a reminder for that task.";
                    AppendColoredText(txtChat, "CypherBot", okReply, Brushes.Green);
                    Speak(okReply);
                    pendingTaskId = -1;
                    pendingTaskTitle = null;
                    return;
                }

                if (ContainsWord(message, "yes") || ContainsWord(message, "yeah") || ContainsWord(message, "sure") || ContainsWord(message, "please"))
                {
                    string? timeframe = ExtractReminderTimeframe(message);
                    if (timeframe != null)
                    {
                        // Combined reply, e.g. "Yes, remind me in 3 days."
                        taskManager.SetReminder(pendingTaskId, timeframe);
                        string confirm = $"Got it! I'll remind you in {timeframe}.";
                        AppendColoredText(txtChat, "CypherBot", confirm, Brushes.Green);
                        Speak(confirm);
                        activityLog.AddEntry($"Reminder set for '{pendingTaskTitle}' ({timeframe}).");
                        pendingTaskId = -1;
                        pendingTaskTitle = null;
                        return;
                    }
                    else
                    {
                        awaitingReminderTimeframe = true;
                        string askDate = "Sure! When would you like to be reminded (e.g. 'in 3 days', 'tomorrow', 'next Monday')?";
                        AppendColoredText(txtChat, "CypherBot", askDate, Brushes.Green);
                        Speak(askDate);
                        return;
                    }
                }
                // Anything else falls through and the prompt is simply forgotten on the next match.
            }

            if (awaitingReminderTimeframe && pendingTaskId != -1)
            {
                string timeframe = ExtractReminderTimeframe(message) ?? userText;
                taskManager.SetReminder(pendingTaskId, timeframe);
                string confirmReminder = $"Got it! I'll remind you in {timeframe}.";
                AppendColoredText(txtChat, "CypherBot", confirmReminder, Brushes.Green);
                Speak(confirmReminder);
                activityLog.AddEntry($"Reminder set for '{pendingTaskTitle}' ({timeframe}).");
                pendingTaskId = -1;
                pendingTaskTitle = null;
                awaitingReminderTimeframe = false;
                return;
            }

            // ═════════════════════════════════════════════════════════════
            //  TASK 4: ACTIVITY LOG — let the user ask what the bot has
            //  done for them so far.
            // ═════════════════════════════════════════════════════════════
            if (message.Contains("show full log") || message.Contains("show more") || message.Contains("full history"))
            {
                string fullLog = activityLog.GetFullHistoryFormatted();
                AppendColoredText(txtChat, "CypherBot", fullLog, Brushes.Green);
                return;
            }

            if (message.Contains("show activity log") || message.Contains("activity log") ||
                message.Contains("what have you done for me") || message.Contains("what have you done"))
            {
                string recentLog = activityLog.GetRecentEntriesFormatted();
                AppendColoredText(txtChat, "CypherBot", recentLog, Brushes.Green);
                return;
            }

            // ═════════════════════════════════════════════════════════════
            //  TASK 2: QUIZ — start the quiz when the user asks for it.
            // ═════════════════════════════════════════════════════════════
            if (message.Contains("quiz") || message.Contains("play a game") || message.Contains("test my knowledge"))
            {
                quiz.StartQuiz();
                string intro = "Let's test your cybersecurity knowledge! I'll ask you a series of questions — answer with the letter (A-D) or True/False. (You can exit anytime by typing 'exit quiz', 'quit quiz', 'stop quiz', or 'cancel quiz')";
                AppendColoredText(txtChat, "CypherBot", intro, Brushes.Green);
                Speak(intro);
                AppendColoredText(txtChat, "CypherBot", quiz.GetCurrentQuestionFormatted(), Brushes.Green);
                activityLog.AddEntry("Quiz started.");
                return;
            }

            // ═════════════════════════════════════════════════════════════
            //  TASK 1: TASK ASSISTANT — view, complete, or delete tasks.
            //  Checked before "add task" so that, e.g., "delete task" isn't
            //  misread as a request to add a task called "delete".
            // ═════════════════════════════════════════════════════════════
            if (message.Contains("show tasks") || message.Contains("view tasks") || message.Contains("my tasks") || message == "tasks")
            {
                var allTasks = taskManager.GetAllTasks();
                if (allTasks.Count == 0)
                {
                    string noTasks = "You don't have any tasks yet. Try saying \"Add task - Enable two-factor authentication\".";
                    AppendColoredText(txtChat, "CypherBot", noTasks, Brushes.Green);
                    return;
                }

                var sb = new StringBuilder("Here are your tasks:\n");
                for (int i = 0; i < allTasks.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {allTasks[i]}");
                }
                AppendColoredText(txtChat, "CypherBot", sb.ToString().TrimEnd(), Brushes.Green);
                return;
            }

            if (message.Contains("delete task") || message.Contains("remove task"))
            {
                string titleFragment = ExtractAfter(userText, new[] { "delete task", "remove task" });
                if (string.IsNullOrWhiteSpace(titleFragment))
                {
                    AppendColoredText(txtChat, "CypherBot", "Which task would you like to delete? Please include its name.", Brushes.Green);
                    return;
                }

                var match = taskManager.FindTaskByTitleFragment(titleFragment);
                if (match == null)
                {
                    AppendColoredText(txtChat, "CypherBot", $"I couldn't find a task matching '{titleFragment}'.", Brushes.Green);
                    return;
                }

                taskManager.DeleteTask(match.Id);
                string deleteReply = $"Deleted task: '{match.Title}'.";
                AppendColoredText(txtChat, "CypherBot", deleteReply, Brushes.Green);
                Speak(deleteReply);
                activityLog.AddEntry($"Task deleted: '{match.Title}'.");
                RefreshTasksDataGrid();  // Refresh to remove the deleted task
                return;
            }

            if (message.Contains("complete task") || message.Contains("mark task") || message.Contains("finished task") || message.Contains("done with task"))
            {
                string titleFragment = ExtractAfter(userText, new[] { "complete task", "mark task", "finished task", "done with task" });
                titleFragment = titleFragment.Replace("as completed", "").Replace("as done", "").Replace("complete", "").Trim();

                if (string.IsNullOrWhiteSpace(titleFragment))
                {
                    AppendColoredText(txtChat, "CypherBot", "Which task have you completed? Please include its name.", Brushes.Green);
                    return;
                }

                var match = taskManager.FindTaskByTitleFragment(titleFragment);
                if (match == null)
                {
                    AppendColoredText(txtChat, "CypherBot", $"I couldn't find a task matching '{titleFragment}'.", Brushes.Green);
                    return;
                }

                taskManager.MarkCompleted(match.Id);
                string completeReply = $"Great work! Marked '{match.Title}' as completed.";
                AppendColoredText(txtChat, "CypherBot", completeReply, Brushes.Green);
                Speak(completeReply);
                activityLog.AddEntry($"Task completed: '{match.Title}'.");
                RefreshTasksDataGrid();  // Refresh to show the updated task status
                return;
            }

            // ═════════════════════════════════════════════════════════════
            //  TASK 1 / TASK 3: NLP — recognise requests to add a task or
            //  set a reminder, even when phrased differently, using simple
            //  keyword/string-matching (string.Contains) rather than an
            //  exact match. Covers variations such as:
            //    "Add task - Review privacy settings"   (dash format)
            //    "Add a task to enable 2FA"
            //    "Remind me to update my password tomorrow"
            //    "Add a reminder to check my privacy settings"
            // ═════════════════════════════════════════════════════════════
            bool mentionsTask = message.Contains("task");
            bool mentionsReminder = message.Contains("remind");
            bool mentionsAdd = message.Contains("add") || message.Contains("set") || message.Contains("create");

            if (mentionsReminder && (mentionsAdd || message.Contains("remind me")))
            {
                // e.g. "Remind me to update my password tomorrow."
                string description = ExtractAfter(userText, new[] { "remind me to", "reminder to", "remind me", "set a reminder to", "add a reminder to" });
                string? timeframe = ExtractReminderTimeframe(message);

                if (timeframe != null)
                {
                    description = StripTimeframePhrase(description, timeframe);
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    string clarify = "Sure — what would you like me to remind you about?";
                    AppendColoredText(txtChat, "CypherBot", clarify, Brushes.Green);
                    Speak(clarify);
                    return;
                }

                string fullDescription = BuildTaskDescription(description);
                int newId = taskManager.AddTask(description, fullDescription, timeframe);

                string reply = timeframe != null
                    ? $"Task added: '{description}'. Reminder set for {timeframe}."
                    : $"Got it — I'll remind you to '{description}'. When would you like the reminder (e.g. 'in 3 days')?";

                AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                Speak(reply);

                if (timeframe != null)
                {
                    activityLog.AddEntry($"Task added with reminder: '{description}' ({timeframe}).");
                }
                else
                {
                    pendingTaskId = newId;
                    pendingTaskTitle = description;
                    awaitingReminderTimeframe = true; // next message is treated directly as the timeframe
                    activityLog.AddEntry($"Task added: '{description}'.");
                }
                return;
            }

            if (mentionsTask && (mentionsAdd || message.Contains("task -") || message.Contains("task:")))
            {
                // Supports "Add task - Review privacy settings", "Add a task to enable 2FA",
                // "Add task: enable 2FA", "Create task ..." etc.
                string description = ExtractAfter(userText, new[]
                {
                    "add task -", "add task:", "add a task -", "add a task:",
                    "add a task to", "add task to", "add a task", "add task",
                    "create a task to", "create task to", "create a task", "create task",
                    "set a task to", "set task"
                });

                if (string.IsNullOrWhiteSpace(description))
                {
                    string clarify = "Sure — what should the task be?";
                    AppendColoredText(txtChat, "CypherBot", clarify, Brushes.Green);
                    Speak(clarify);
                    return;
                }

                string fullDescription = BuildTaskDescription(description);
                int newId = taskManager.AddTask(description, fullDescription, null);

                string reply = $"Task added with the description \"{fullDescription}\" Would you like a reminder?";
                AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                Speak(reply);

                pendingTaskId = newId;
                pendingTaskTitle = description;
                awaitingReminderTimeframe = false;
                activityLog.AddEntry($"Task added: '{description}'.");
                RefreshTasksDataGrid();  // Refresh to show the new task
                return;
            }

            // ── Store favourite topic (from reference code) ───────────────
            if (message.Contains("i'm interested in") || message.Contains("im interested in") || message.Contains("i am interested in") || message.Contains("my favourite topic is"))
            {
                string topic = ExtractAfter(message, new[] { "i'm interested in", "i am interested in", "my favourite topic is" });
                if (!string.IsNullOrEmpty(topic))
                {
                    memory["topic"] = topic;
                    string reply = "Great! I'll remember that you're interested in " + topic + ". It's a crucial part of staying safe online.";
                    AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                    Speak(reply);
                    lastTopic = topic;
                    return;
                }
            }

            // ── Recall memory for advice (from reference code) ────────────
            if (message.Contains("advice") || message.Contains("remind me") || message.Contains("what do you know about me"))
            {
                if (memory.ContainsKey("name") && memory.ContainsKey("topic"))
                {
                    string reply = memory["name"] + ", since you're interested in " + memory["topic"] +
                                   ", remember to review your account settings regularly and stay updated on the latest threats.";
                    AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                    Speak(reply);
                }
                else if (memory.ContainsKey("name"))
                {
                    string reply = "I know your name is " + memory["name"] + ", but you haven't told me your favourite cybersecurity topic yet!";
                    AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                    Speak(reply);
                }
                else
                {
                    AppendColoredText(txtChat, "CypherBot", "I don't know your name yet!", Brushes.Green);
                }
                return;
            }

            // ── Follow-up / conversation flow ─────────────────────────────
            if ((message.Contains("tell me more") || message.Contains("more") || message.Contains("another tip") ||
                 message.Contains("explain more") || message.Contains("continue")) && lastTopic != "")
            {
                if (followUps.ContainsKey(lastTopic))
                {
                    string tip = followUps[lastTopic][rng.Next(followUps[lastTopic].Count)];
                    AppendColoredText(txtChat, "CypherBot", tip, Brushes.Green);
                    Speak(tip);
                }
                else
                {
                    string reply = "Keep exploring " + lastTopic + " — staying informed is your strongest defence!";
                    AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                }
                return;
            }

            // ── Help / Menu Command ───────────────────────────────────────
            if (message == "help" || message == "menu" || message.Contains("show me options") || 
                message.Contains("what can you do") || message.Contains("what options"))
            {
                string menu = GenerateOptionsMenu();
                AppendColoredText(txtChat, "CypherBot", menu, Brushes.Green);
                return;
            }

            // ── Detect sentiment prefix ───────────────────────────────────
            string prefix = "";
            foreach (var kvp in sentimentPrefixes)
            {
                if (message.Contains(kvp.Key))
                {
                    prefix = kvp.Value;
                    break;
                }
            }

            // ── Match keyword and pick a random response ──────────────────
            foreach (var kvp in responses)
            {
                if (message.Contains(kvp.Key))
                {
                    lastTopic = kvp.Key;
                    string reply = prefix + kvp.Value[rng.Next(kvp.Value.Count)];
                    AppendColoredText(txtChat, "CypherBot", reply, Brushes.Green);
                    Speak(reply);
                    return;
                }
            }

            // ── Proactive memory recall ───────────────────────────────────
            if (memory.ContainsKey("topic") && rng.Next(3) == 0)
            {
                string name = memory.ContainsKey("name") ? memory["name"] + ", I" : "I";
                string recall = name + " remember you're interested in " + memory["topic"] + ". Could you rephrase your question?";
                AppendColoredText(txtChat, "CypherBot", recall, Brushes.Green);
                Speak(recall);
                return;
            }

            // ── Default fallback ──────────────────────────────────────────
            string[] defaults =
            {
                "I'm not sure I understand. Could you try rephrasing that?",
                "Hmm, I didn't quite catch that. Try asking about passwords, phishing, or malware!",
                "I'm still learning! I'm best with cybersecurity topics — try asking about privacy or 2FA.",
            };
            string fallback = defaults[rng.Next(defaults.Length)];
            AppendColoredText(txtChat, "CypherBot", fallback, Brushes.Green);
            Speak(fallback);
        }

        // ═════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════════

        // ── Checks whether `word` appears in `message` as a whole word,
        //    avoiding false positives like "know" matching "no" or
        //    "yesterday" matching "yes".
        private bool ContainsWord(string message, string word)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                message, $@"\b{System.Text.RegularExpressions.Regex.Escape(word)}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Extract the text that comes after a trigger phrase
        private string ExtractAfter(string input, string[] triggers)
        {
            foreach (var t in triggers)
            {
                int idx = input.IndexOf(t, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    return input.Substring(idx + t.Length).Trim().TrimEnd('.', '!', '?');
            }
            return "";
        }

        // ── Task 1: extract a reminder timeframe phrase from a message,
        //    e.g. "remind me in 3 days" → "3 days", "tomorrow" → "tomorrow".
        //    Returns null if no recognisable timeframe phrase is found.
        private string? ExtractReminderTimeframe(string message)
        {
            string lower = message.ToLower();

            // "in X day(s)/week(s)/hour(s)/month(s)"
            var match = System.Text.RegularExpressions.Regex.Match(lower, @"in\s+(\d+|a|an)\s+(day|days|week|weeks|hour|hours|month|months)");
            if (match.Success)
            {
                string amount = match.Groups[1].Value == "a" || match.Groups[1].Value == "an" ? "1" : match.Groups[1].Value;
                return $"{amount} {match.Groups[2].Value}";
            }

            string[] simplePhrases = { "tomorrow", "today", "next week", "next monday", "next tuesday",
                                        "next wednesday", "next thursday", "next friday", "next saturday", "next sunday" };
            foreach (var phrase in simplePhrases)
            {
                if (lower.Contains(phrase)) return phrase;
            }

            return null;
        }

        // Remove a timeframe phrase from a task description once it's been extracted separately.
        private string StripTimeframePhrase(string description, string timeframe)
        {
            if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(timeframe)) return description;
            string cleaned = System.Text.RegularExpressions.Regex.Replace(description, $@"in\s+{System.Text.RegularExpressions.Regex.Escape(timeframe)}", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            cleaned = cleaned.Replace(timeframe, "", StringComparison.OrdinalIgnoreCase);
            return cleaned.Trim();
        }

        // ── Task 1: expand a short task title into a friendlier description
        //    using the keyword templates, falling back to a generic phrasing.
        private string BuildTaskDescription(string title)
        {
            string lowerTitle = title.ToLower();
            foreach (var kvp in taskDescriptionTemplates)
            {
                if (lowerTitle.Contains(kvp.Key))
                    return kvp.Value;
            }
            return $"Complete the task: {title}.";
        }

        // Generate a formatted menu of all available chatbot options
        private string GenerateOptionsMenu()
        {
            return @"Here's what I can help you with:

TASK ASSISTANT:
  • 'Add task - [task name]' — Create a new cybersecurity task
  • 'Show tasks' — View all your tasks
  • 'Delete task - [task name]' — Remove a task
  • 'Complete task - [task name]' — Mark a task as done

QUIZ:
  • 'Quiz' or 'Play a game' — Test your cybersecurity knowledge (12 questions)
  • 'Exit quiz' or 'Stop quiz' — Exit the quiz anytime if you want to stop

ACTIVITY LOG:
  • 'Activity log' — See recent actions
  • 'Show full log' — View complete history

GENERAL HELP:
  • Ask me about: passwords, phishing, malware, encryption, 2FA, VPN, backup, firewall, data breach, and more!
  • 'Help' — Show this menu again

Type any command above or ask a question about cybersecurity!";
        }

        // Add colored text to RichTextBox
        private void AppendColoredText(RichTextBox rtb, string name, string text, Brush color)
        {
            Paragraph para = new Paragraph();
            Run nameRun = new Run(name + ": ");
            nameRun.Foreground = color;
            nameRun.FontWeight = FontWeights.Bold;
            para.Inlines.Add(nameRun);

            Run messageRun = new Run(text + "\n");
            messageRun.Foreground = Brushes.Black;
            para.Inlines.Add(messageRun);

            rtb.Document.Blocks.Add(para);
            rtb.ScrollToEnd();
        }

        // Run TTS on a background thread so the UI never freezes
        private void Speak(string text)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { speech.Speak(text); }
                catch { }
            });
        }

        // Exit button click handler
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            string bye = "Goodbye! Stay safe online!";
            AppendColoredText(txtChat, "CypherBot", bye, Brushes.Green);
            Speak(bye);
            Thread.Sleep(800);
            Application.Current.Shutdown();
        }

        // Refresh the DataGrid with the latest tasks from the database
        private void RefreshTasksDataGrid()
        {
            try
            {
                var allTasks = taskManager.GetAllTasks();
                tasksDataGrid.ItemsSource = null;  // Clear existing binding
                tasksDataGrid.ItemsSource = allTasks;
            }
            catch (Exception ex)
            {
                // Silently fail if database is unavailable
            }
        }
    }
}
