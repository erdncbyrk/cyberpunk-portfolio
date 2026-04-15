👨‍💻 Cyberpunk Developer Portfolio
This project is a high-performance, fully dynamic personal portfolio / CV web application built from scratch using modern web technologies and a cyberpunk design language. Powered by a .NET 10 backend and a glassmorphism-enriched frontend, it aims to provide visitors with a "premium" experience.

✨ Key Features
🎨 Cyberpunk Neon Design: An unconventional visual experience with a custom color palette (Dark slate, cyan, and fuchsia), neon glows, and fluid micro-interactions (hover effects).

🚀 One-Page Architecture: A seamless structure featuring AJAX-based form submission and smooth scrolling dynamics that never disrupt the visitor's focus flow.

⚙️ Static JSON Data Management: Information such as education, experience, skills, and interests is loaded instantly via the wwwroot/resume.json file without any database overhead. The developer can update their entire CV by editing a single file.

🐙 Dynamic GitHub Integration: Automatically fetches and displays the latest/most popular repositories by connecting to the GitHub API via a professional HttpClient architecture running in the background.

🛡️ Spam-Protected Contact Form: Complete security against malicious bots with an invisible Honeypot trap, Rate Limiting, and Anti-Forgery (CSRF) protection.

📧 Built-in Mail Infrastructure: Secure email transmission directly via Gmail SMTP using the MailKit library, eliminating the need for extra 3rd-party API costs.

🛠️ Tech Stack
Backend:

.NET 10 (ASP.NET Core Razor Pages)

C# 12

MailKit & MimeKit (SMTP Email Operations)

System.Text.Json (High-performance JSON operations)

IHttpClientFactory (External API communications)

Frontend:

HTML5 & CSS3

Tailwind CSS (Utility-first styling and dark/neon theme management)

Vanilla JavaScript (ES6+ - AJAX form management and DOM manipulation)

AOS (Animate On Scroll animation library)

FontAwesome (Vector icons)

🚀 Installation & Setup
You can follow the steps below to run the project on your local machine or server:

1. Clone the Repository:

Bash
git clone https://github.com/erdncbyrk/cyberpunk-portfolio.git
cd cyberpunk-portfolio
2. Configure Required Settings:
For the project's API and Mail features to work, add the following keys to your environment variables or appsettings.json file (using the User Secrets tool if you are in the development environment):

JSON
{
  "GitHubToken": "ghp_your_github_token_here", // Only required if you want to fetch Private repos
  "SmtpSettings": {
    "Email": "your_real_email_address@gmail.com",
    "Password": "your_google_app_password" // 2FA-supported Google App Password
  }
}
3. Personalize Your Resume:

Update the wwwroot/resume.json file with your own skills, experiences, and education information.

Add your own high-resolution photo to the wwwroot/img/profile.jpg directory.

Replace the wwwroot/CV.pdf file with your own resume file.

4. Build and Run:

Bash
dotnet build
dotnet run
The project will be live at https://localhost:xxxx by default.

📂 Architectural Structure
/Models: Data transfer objects and C# models providing type safety (ResumeData, GitHubRepo, ContactViewModel).

/Services: External service communications used with Dependency Injection (DI) (GitHubService, EmailSender).

/Pages: Razor Pages components where view and backend logic merge (Index.cshtml and Model classes).

/wwwroot: Static assets (JSON database, images, PDF documents).

Coded by developers, for developers. 🚀
