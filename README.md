# Crypt-See
Crypt-See adalah game 2D bergenre stealth di mana pemain berperan sebagai seorang pemula yang tengah menjalani pelatihan di dalam sebuah museum. Tujuan utama pemain adalah mengumpulkan semua objek yang tersebar di dalam area museum dan mencapai pintu keluar. Namun, pemain harus berhati-hati dan menghindari deteksi serta kejaran robot penjaga yang terus berpatroli di setiap sudut ruangan.

---

## 🕹️ Mekanik Game
| Kontrol              | Aksi                        |
|----------------------|-----------------------------|
| `W` `A` `S` `D`       | Bergerak (atas, kiri, bawah, kanan) |
| Klik Kiri Mouse      | Menembak                   |
| Gerakan Mouse        | Menggerakkan kamera         |
| Scroll Mouse         | Mengubah kecepatan gerak    |

## 🎯 Tujuan Permainan
- Mengumpulkan seluruh objektif yang ada
- Menghindar dari deteksi musuh
- Mencari jalan keluar setelah mengumpulkan seluruh objektif

## 🧠 Fitur Utama
- Lingkungan yang Interaktif: Map didesain dengan menggunakan pencahayaan yang dapat dihancurkan atau dimatikan dalam rentang waktu tertentu
- Enemy berbasi AI: Musuh dalam map dilatih menggunakan machine learning yang membuat pola patroli lebih acak dan susah ditebak

## 🛠️ Teknologi yang digunakan
- Unity
- Ml-agents

## 🤖 Penjelasan Peran AI
AI berperan sebagai sistem navigasi dan penjaga utama dalam game ini. Setiap musuh dikendalikan oleh AI yang secara mandiri memilih jalur patroli di dalam museum, tanpa perlu rute yang ditentukan secara manual.

AI akan terus berpatroli hingga mendeteksi keberadaan pemain melalui raycast (penglihatan) atau audio source (suara langkah). Ketika pemain terdeteksi, tension meter akan mulai terisi. Begitu meter ini penuh, AI akan masuk ke mode agresif dan mulai mengejar pemain secara aktif.

Jika pemain berhasil melarikan diri dan keluar dari jangkauan deteksi, AI akan kembali ke berpatroli dan melanjutkan pengawasan di sekitar area.

## Downloadable Build
🔗 [Download Build (Windows) ](https://github.com/adharjmn3/Crypt-See/releases/tag/0.0.1)

# Credit
<a href="https://github.com/adharjmn3/Crypt-See/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=adharjmn3/Crypt-See" />
</a>
