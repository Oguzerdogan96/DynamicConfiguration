# Dynamic Configuration Management System

## 🎯 Amaç
Bu proje, .NET 8 kullanılarak dinamik konfigürasyon yönetimi sağlayan bir yapı sunmaktadır. Amaç; konfigürasyon verilerine uygulama yeniden başlatılmadan erişebilmek, güncelleyebilmek ve her servisin yalnızca kendine ait verileri görebilmesini sağlamaktır.

## 🏗️ Katmanlar

### 🔹 DynamicConfiguration (DLL)
- `ConfigurationReader` sınıfı:
  - `ApplicationName`, `ConnectionString` ve `RefreshInterval` parametreleriyle başlatılır.
  - Veritabanındaki `ConfigurationSettings` tablosundan yalnızca `IsActive = 1` ve ilgili `ApplicationName` olan kayıtları çeker.
  - Veriler `MemoryCache` ile tutulur, storage erişilemese bile sistem çalışmaya devam eder.
  - `GetValue<T>()`, `InsertValue()`, `UpdateValue()`, `Delete()` ve `GetAll()` gibi metotlar içerir.
  - Belirli aralıklarla storage’ı kontrol etmek için `Timer` kullanılır.

### 🔹 ConfigDemo.Api (ASP.NET Core API)
- `ServiceAController` üzerinden tüm CRUD işlemleri yapılabilir.
- `ConfigurationReader` sınıfı DI ile kullanılır.
- Swagger arayüzü ile test edilebilir.
- `SERVICE-A` ve `SERVICE-B` gibi farklı uygulamaların sadece kendi konfigürasyonlarını görebilmesi sağlanır.

### 🔹 ServiceA (HTML + JavaScript Arayüz)
- `index.html`: Tüm konfigürasyonları listeler, silme ve güncelleme butonları içerir.
- `ekle.html`: Yeni konfigürasyon eklemek için form içerir.
- `guncelle.html`: Mevcut konfigürasyonu düzenlemeye yarar.
- Arayüz doğrudan API ile haberleşir (pure JS).

## 🛠️ Teknolojiler
- .NET 8
- ASP.NET Core Web API
- JavaScript, HTML
- MemoryCache
- SQL Server

## 🗃️ Veritabanı Yapısı

```sql
CREATE TABLE ConfigurationSettings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100),
    Type NVARCHAR(50),
    Value NVARCHAR(MAX),
    IsActive BIT,
    ApplicationName NVARCHAR(100)
);
