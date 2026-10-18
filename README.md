# TMN Core Framework (UPM Package)

Unity mobil ve PC projeleri için paylaşımlı temel mimari, yönetici sınıfları ve yardımcı araçlar kütüphanesidir.

## 📦 Kurulum (Unity Package Manager)

### Git URL ile Kurulum (Önerilen)
1. Unity Editöründe **Window -> Package Manager** penceresini açın.
2. Sol üstteki **`+`** butonuna tıklayın ve **"Add package from git URL..."** seçeneğini seçin.
3. Aşağıdaki URL'yi yapıştırın:
   ```text
   https://github.com/Zart-Zurt/TMNCore.git
   ```
4. Belirli bir sürümü sabitlemek isterseniz sürüm etiketini ekleyebilirsiniz:
   ```text
   https://github.com/Zart-Zurt/TMNCore.git#v1.1.0
   ```

### `Packages/manifest.json` Üzerinden Kurulum
Projenizin `Packages/manifest.json` dosyasına doğrudan bağımlılık olarak ekleyebilirsiniz:
```json
{
  "dependencies": {
    "com.tmn.core": "https://github.com/Zart-Zurt/TMNCore.git",
    ...
  }
}
```

---

## 🧩 Dahili Modüller

* **Core.Events (`EventBus`):** Tip güvenli, gevşek bağlı (loosely coupled) publish-subscribe olay yönetim sistemi.
* **Core.SaveSystem (`SaveManager`):** Versiyonlama, çoklu slot ve migrasyon destekli kalıcı kayıt altyapısı.
* **Core.Localization (`LocalizationManager`):** Çoklu dil desteği (JSON tabanlı), çalışma anında dil değiştirme.
* **Core.Managers:**
  * `AudioManager`: Ses efektleri ve müzik yönetimi (`AudioCueSO`).
  * `InputManager`: Unity Yeni Input Sistemi entegrasyonu ve tuş atama.
  * `SettingsManager`: Grafik, ses ve oynanış ayarlarının kaydedilmesi ve yönetimi.
  * `SrDebuggerManager`: SRDebugger entegrasyonu.
* **Core.Pooling (`PoolManager`):** `IPoolable` arayüzü ile sıfır GC çöpü hedefleyen nesne havuzlama.
* **Core.UI.Settings:** Hazır ayarlar paneli ve tuş yeniden atama UI kontrolcüsü.
* **TMNLibrary (Singletons & EasyMethods):** Generic thread-safe `Singleton` & `MonoSingleton` kalıpları ve matematik/vektör kolaylıkları.
* **Core.Utils:** `OutOfBoundsTrigger`, `RayTest` ve `SortingLayer` yardımcıları.

---

## 🔄 Güncelleme İşlemi

Core reposunda bir geliştirme veya hata düzeltmesi yapıldığında:
1. Bu repoda değişiklik yapılıp commit atılır ve yeni bir tag oluşturulur (örn: `v1.0.1`).
2. Oyun projelerinde **Package Manager** üzerinden **"Update"** butonuna basılır veya `manifest.json` içindeki tag güncellenir.
