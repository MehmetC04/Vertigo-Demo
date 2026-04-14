# Vertigo Games - Technical Artist Demo

Bu proje, Vertigo Games Technical Artist pozisyonu değerlendirme süreci için Mehmet Çakar tarafından hazırlanmıştır. 

Proje, mobil oyun standartlarına uygun olarak optimize edilmiş iki ana görevden (Task) oluşmaktadır. 

## 📁 Proje Yapısı ve Sahne Konumları

> **Not:** Lütfen projeyi incelerken ana dizindeki klasörleri değil, doğrudan `Demo` klasörünün altındaki yapıyı baz alınız. Tüm geçerli içerikler ve sahneler **`Demo/Assets/`** dizini altında düzenlenmiştir.

### 🎯 Task 1: Attachment Window (Eklenti Arayüzü)
Bu görevde, kullanıcı girdilerine tepki veren, silah eklentilerinin (attachment) değiştirilebildiği ve istatistiklerin görüntülendiği temel bir UI sistemi kurgulanmıştır.
* **Sahne Konumu:**  `Vertigo Demo\Assets\Task 1\Task 1.unity""` 
* **Özellikler:**
  * Kategori seçimi ile eklenti listesinin dinamik olarak güncellenmesi.
  * Seçilen eklentinin 3D silah modeli üzerinde anında güncellenmesi.
  * Pürüzsüz kaydırma (smooth scroll) özelliğine sahip istatistik paneli.

### 🌪️ Task 2: Weapon VFX (Silah Görsel Efektleri)
Bu görevde, mobil platformların performans limitleri göz önünde bulundurularak (maliyet hesaplamalarına dikkat edilerek) Shader Graph ve Particle System ile "Akan Rüzgar" (Flowing Wind) efekti oluşturulmuştur.
* **Sahne Konumu:** `Vertigo Demo\Assets\Task 2\Task 2.unity"` 
* **Özellikler:**
  * Shader Graph kullanılarak oluşturulan ve UI mesh'leri / silah üzerine uygulanan rüzgar efekti.
  * Unity Particle System ile desteklenen ikincil (secondary) efektler.
  * Post-processing (ekran sonrası efekt) kullanılmadan, tamamen shader/materyal bazlı saf görselleştirme.
  * Temiz ve düzenli Shader yapısı.

## 🛠️ Kullanılan Teknolojiler & Versiyon
* **Oyun Motoru:** Unity 6.3f1 
* **Render Pipeline:** Universal Render Pipeline (URP)

## 👤 Geliştirici
**Mehmet Çakar**
* [Portfolyo](https://mehmetcakarcv.blogspot.com/)
* [LinkedIn](https://www.linkedin.com/in/mehmetcakar0404/)
