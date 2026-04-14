👨‍💻 Cyberpunk Developer Portfolio
Bu proje, modern web teknolojileri ve siberpunk tasarım dili kullanılarak sıfırdan geliştirilmiş, yüksek performanslı ve tam dinamik bir kişisel portfolyo / CV web uygulamasıdır. .NET 10 gücüyle çalışan arka planı ve cam efekti (glassmorphism) ile zenginleştirilmiş ön yüzü sayesinde ziyaretçilere "premium" bir deneyim sunmayı hedefler.

✨ Öne Çıkan Özellikler
🎨 Siberpunk Neon Tasarım: Özel renk paleti (Koyu arduvaz, camgöbeği ve fuşya), neon parlamalar ve akıcı mikro-etkileşimler (hover efektleri) ile alışılmışın dışında bir görsel deneyim.

🚀 One-Page (Tek Sayfa) Mimari: Ziyaretçinin odak akışını bozmayan, AJAX tabanlı form gönderimi ve pürüzsüz kaydırma dinamiklerine sahip yapı.

⚙️ Statik JSON Veri Yönetimi: Eğitim, deneyim, yetenekler ve ilgi alanları gibi bilgiler veritabanı yükü olmadan wwwroot/resume.json dosyası üzerinden anında yüklenir. Geliştirici, tek bir dosyayı düzenleyerek tüm CV'sini güncelleyebilir.

🐙 Dinamik GitHub Entegrasyonu: Arka planda çalışan profesyonel HttpClient mimarisi ile GitHub API'ye bağlanarak en güncel/popüler depoları otomatik çeker ve sergiler.

🛡️ Spam Korumalı İletişim Formu: Kötü niyetli botlara karşı görünmez Honeypot tuzağı, Rate Limiting (istek sınırlayıcı) ve Anti-Forgery (CSRF) koruması ile tam güvenlik.

📧 Dahili Mail Altyapısı: MailKit kütüphanesi kullanılarak, ekstra 3. parti API maliyetlerine gerek kalmadan doğrudan Gmail SMTP üzerinden güvenli e-posta iletimi sağlanır.

🛠️ Teknoloji Yığını (Tech Stack)
Backend:

.NET 8 (ASP.NET Core Razor Pages)

C# 12

MailKit & MimeKit (SMTP E-posta İşlemleri)

System.Text.Json (Yüksek performanslı JSON işlemleri)

IHttpClientFactory (Dış API iletişimleri)

Frontend:

HTML5 & CSS3

Tailwind CSS (Utility-first stil ve karanlık/neon tema yönetimi)

Vanilla JavaScript (ES6+ - AJAX form yönetimi ve DOM manipülasyonu)

AOS (Animate On Scroll animasyon kütüphanesi)

FontAwesome (Vektörel ikonlar)

🚀 Kurulum ve Çalıştırma
Projeyi kendi bilgisayarınızda veya sunucunuzda ayağa kaldırmak için aşağıdaki adımları izleyebilirsiniz:

1. Depoyu Klonlayın:

Bash
git clone https://github.com/erdncbyrk/cyberpunk-portfolio.git
cd cyberpunk-portfolio
2. Gerekli Ayarları Yapılandırın:
Projenin API ve Mail özelliklerinin çalışması için ortam değişkenlerine veya appsettings.json dosyasına (geliştirme aşamasındaysanız User Secrets aracı ile) aşağıdaki anahtarları ekleyin:

JSON
{
  "GitHubToken": "ghp_sizin_github_tokeniniz", // Sadece Private repoları çekmek istiyorsanız gereklidir
  "SmtpSettings": {
    "Email": "gercek_mail_adresiniz@gmail.com",
    "Password": "google_uygulama_sifreniz" // 2FA destekli Google App Password
  }
}
3. Özgeçmişinizi Kişiselleştirin:

wwwroot/resume.json dosyasını kendi yetenekleriniz, deneyimleriniz ve eğitim bilgileriniz ile güncelleyin.

wwwroot/img/profile.jpg dizinine kendi yüksek çözünürlüklü fotoğrafınızı ekleyin.

wwwroot/CV.pdf dosyasını kendi özgeçmiş dosyanızla değiştirin.

4. Derleyin ve Başlatın:

Bash
dotnet build
dotnet run
Proje varsayılan olarak https://localhost:xxxx adresinde yayına girecektir.

📂 Mimari Yapı
/Models: Veri transfer nesneleri ve tip güvenliği sağlayan C# modelleri (ResumeData, GitHubRepo, ContactViewModel).

/Services: Bağımlılık enjeksiyonu (DI) ile kullanılan dış servis iletişimleri (GitHubService, EmailSender).

/Pages: Görüntü ve arka plan mantığının birleştiği Razor Pages bileşenleri (Index.cshtml ve Model sınıfları).

/wwwroot: Statik varlıklar (JSON veritabanı, resimler, PDF dokümanları).

Geliştiriciler tarafından, geliştiriciler için kodlanmıştır. 🚀
