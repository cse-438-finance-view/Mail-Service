# Mail Service Tests

Bu proje, Mail Service uygulaması için test süitini içerir. Hem unit testleri hem de integration testleri bulunmaktadır.

## Test Yapısı

- **Unit Tests**: Uygulama bileşenlerinin izole edilmiş testleri
- **Integration Tests**: Sistemin çeşitli bileşenlerinin birlikte çalışma testleri

## Proje Yapısı

```
MailService.Tests/
├── Unit/                      # Unit testleri
│   ├── EmailServiceTests.cs   # EmailService için unit testler
│   ├── RabbitMQServiceTests.cs # RabbitMQService için unit testler
│   └── EmailCommandTests.cs   # EmailCommand sınıfı için unit testler
├── Integration/                # Integration testleri
│   ├── EmailServiceIntegrationTests.cs  # E-posta gönderimi için integration testler
│   ├── RabbitMQServiceIntegrationTests.cs # RabbitMQ servisi için integration testler
│   └── EmailCommandIntegrationTests.cs  # Email Command API için testler
├── appsettings.test.json      # Test konfigürasyonu
└── MailService.Tests.csproj   # Test proje dosyası
```

## Testleri Çalıştırma

Testleri çalıştırmak için aşağıdaki komutları kullanabilirsiniz:

```bash
# Tüm testleri çalıştırma
dotnet test

# Unit testleri çalıştırma
dotnet test --filter "Category=Unit"

# Integration testleri çalıştırma (Not: Bu testler manuel olarak çalıştırılmalıdır)
dotnet test --filter "Category=Integration"
```

## Test Konfigürasyonu

Integration testleri çalıştırmadan önce `appsettings.test.json` dosyasını düzenleyin:

1. `TestEmail` değerini geçerli bir e-posta adresine güncelleyin
2. `MailService` bölümündeki SMTP ayarlarını düzenleyin
3. `RabbitMQ` bölümündeki bağlantı bilgilerini güncelleyin

## Integration Testleri Hakkında Not

Integration testleri gerçek e-posta gönderimine ve RabbitMQ bağlantısına ihtiyaç duyar. Bu testler varsayılan olarak `Skip` ile işaretlenmiştir ve manuel olarak çalıştırılmalıdır.

RabbitMQ testlerini çalıştırmak için:
1. RabbitMQ sunucusunun çalıştığından emin olun
2. `RabbitMQServiceIntegrationTests.cs` dosyasındaki `Skip` parametrelerini kaldırın
3. Testleri çalıştırın

E-posta testlerini çalıştırmak için:
1. Geçerli SMTP ayarlarını yapılandırın
2. `EmailServiceIntegrationTests.cs` dosyasındaki `Skip` parametrelerini kaldırın
3. Testleri çalıştırın 