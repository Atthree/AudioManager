# Audio Manager

**Audio manager for Unity 6**: sound assets with variations, pooled sources with voice limiting,
one-line 2D/3D playback, music crossfades, volume channels that are remembered, pause handling,
and components that play sounds without code.

[English](#english) · [Türkçe](#türkçe)

---

<a name="english"></a>
# English

- **Sound assets:** one asset per sound with clip variations, random volume/pitch, 2D/3D range,
  cooldown, max instances and priority. Tweak sounds without touching code.
- **One-liners:** `AudioManager.Play(sound)`, `PlayAt(sound, position)`, `PlayAttached(sound, transform)`.
- **Pooled sources:** no `AudioSource` is created or destroyed per sound. When the limit is reached,
  the least important sound is cut instead of everything clipping.
- **Music** with crossfades; asking for the track that is already playing does nothing.
- **Volume channels:** Master, Music, Sfx, Ui, Voice, Ambience. Saved automatically.
- **Pause:** effects pause and resume where they stopped; music and UI keep playing.
- **Components:** `PlaySound` (on start, enable, disable, collision with impact-based volume, trigger,
  or a button) and `PlayMusic` (per scene or per area).
- **12 sample sounds** made for this package (no license issues) and a demo scene.

**Requirements:** Unity 6 (6000.0+). No other packages.

## Step-by-step setup

### 1. Add the package

**Window > Package Manager** → **+** → **Install package from git URL…** →

```
https://github.com/Atthree/AudioManager.git
```

### 2. Try the demo

1. **Tools > Audio Manager > Create Demo Scene**, then **Play**.
2. Every button plays a UI click. Try **Altın** (coin, two variations), **Zıpla** (jump) and
   **Patlama** (an explosion somewhere on the left or right: listen to the direction).
3. **Kutuları düşür** drops the crates: each one makes an impact sound as loud as the hit.
4. The capsule driving in a circle plays an engine loop: louder when close, panned left/right, pitch
   follows its speed.
5. **Sakin / Hareketli** switch the music with a 2-second crossfade.
6. **Spam: 30 altın**: 30 calls in one frame, but you hear only a few (max instances + cooldown).
7. **Duraklat** pauses effects; music and UI clicks continue.
8. Move the sliders, stop and Play again: the volumes are remembered.

### 3. Create a sound

1. Import your audio files (see [Using your own audio files](#using-your-own-audio-files)).
2. Select one or more clips in the Project window → right click → **Create > Audio Manager >
   Sound From Selected Clips**. Clips whose names only differ by a trailing number
   (`Footstep_01`, `Footstep_02`…) become **one** sound with variations.
3. Select the new sound: press **▶ Dinle** (Listen) in the Inspector to hear it without Play mode;
   every press picks a new variation. Set the channel, volume, pitch variation, cooldown…

You can also create an empty one: **Create > Audio Manager > Sound**, then drag clips into **Clips**.

### 4. Play it from code

```csharp
using Atthree.Audio;

public SoundDefinition jumpSound, explosionSound, engineLoop, forestMusic;

AudioManager.Play(jumpSound);                               // 2D: UI, the player's own sounds
AudioManager.PlayAt(explosionSound, transform.position);    // 3D at a point
SoundHandle engine = AudioManager.PlayAttached(engineLoop, transform); // 3D, follows the object
engine.SetPitch(0.8f + speed / maxSpeed);                   // change while playing
engine.Stop(fadeSeconds: 0.5f);

AudioManager.PlayMusic(forestMusic, fadeSeconds: 2f);       // crossfade
AudioManager.StopMusic(fadeSeconds: 1f);
```

A `SoundHandle` is safe to keep: when the sound has ended its methods do nothing and `IsPlaying` is
false.

### 5. …or without code

- **Add Component > Audio Manager > Play Sound**, pick the sound and **Play On**:

  | Play On | Use for |
  |---|---|
  | Start / Enable | Ambient loops, spawn sounds |
  | Disable | Pick-ups and things that disappear (plays where the object was) |
  | Collision | Crates, cars, ragdolls: louder the harder the hit (**Min Impact Speed**, **Full Volume Speed**) |
  | TriggerEnter | Doors, zones, collectibles |
  | Manual | Call `Play()` from a UI Button's **On Click** |

- **Add Component > Audio Manager > Play Music**: plays its music when the scene starts, or (with
  **Play On Start** off) when the player enters its trigger.

### 6. Settings menu and pause menu

```csharp
musicSlider.value = AudioManager.GetVolume(AudioChannel.Music);
musicSlider.onValueChanged.AddListener(v => AudioManager.SetVolume(AudioChannel.Music, v)); // saved

void OpenPauseMenu()  { AudioManager.SetPaused(true);  Time.timeScale = 0f; }
void ClosePauseMenu() { AudioManager.SetPaused(false); Time.timeScale = 1f; }
```

## Settings

Optional. **Tools > Audio Manager > Create Settings Asset** creates
`Assets/Resources/AudioManagerSettings.asset`, which is loaded automatically.

| Setting | Default | What it does |
|---|---|---|
| Initial Sources | 16 | AudioSources prepared at start |
| Max Voices | 48 | Sounds that can play at once. Real devices mix ~32–64 voices well |
| Save Volumes | on | Remember channel volumes (PlayerPrefs) |
| Default … | 1 / 0.7 music / 0.8 ambience | Volumes on first launch |
| Music / UI Ignores Pause | on | Keep music and UI playing while paused |
| … Group | empty | Route a channel into your own AudioMixer group (reverb, compression, ducking) |

Volumes are applied in code, so **no AudioMixer is required**. Assign mixer groups only if you want
mixer effects.

## Using your own audio files

Sounds from the Asset Store, freesound.org, Sonniss GDC bundles, sfxr/jsfxr or your own recordings work
the same way. These are the things that decide whether they sound right.

### Before importing

| # | Check | Why |
|---|---|---|
| 1 | **License**: the sound may be used in a (commercial) game; note required credits | Many free sounds are CC-BY (credit required) or non-commercial |
| 2 | **Format**: WAV (or OGG) for sources. Avoid MP3 for loops | MP3 adds silence at the start, which makes loops click |
| 3 | **Trim silence** at the start | A delay between the action and the sound feels laggy |
| 4 | **Loops must loop cleanly**: the end flows into the start | Otherwise you hear a click every loop |
| 5 | **Similar loudness** across your sounds (normalize) | Then the `volume` fields mean the same thing for every sound |
| 6 | **3D sounds should be mono** | A stereo clip played in 3D loses its position |

### Import settings (select the clip in the Project window)

| Clip type | Load Type | Compression | Other |
|---|---|---|---|
| Short effects (< 1 s): clicks, hits, steps | Decompress On Load | ADPCM or Vorbis | — |
| Medium (1–10 s): explosions, voice lines | Compressed In Memory | Vorbis, Quality ~70 | — |
| Music and long ambience | **Streaming** | Vorbis, Quality ~60 | Loads little memory |
| Anything played with `PlayAt` / `PlayAttached` | — | — | **Force To Mono** on |

Click **Apply** after changing them. Select several clips to change them together.

### Making good sounds

- **Variations:** name files `Hit_01`, `Hit_02`, `Hit_03` and create the sound from all of them at
  once. With 2–4 variations plus 5–10% **Pitch Variation**, repeated sounds stop sounding robotic.
- **Spam protection:** for sounds that can trigger many times per second (coins, bullets, impacts)
  set **Max Instances** (3–6) and a small **Cooldown** (0.03–0.1 s).
- **Priority:** important sounds (player damage, dialogue) low numbers like 0–64; background details
  high like 180–256, so they are the ones that get cut.
- **3D distances:** **Min Distance** = how close the sound is at full volume (small for footsteps,
  large for explosions); **Max Distance** = where it fades out.
- **Music loops:** set **Loop** on the sound; the import setting "Loop" isn't needed.

### Troubleshooting

| Problem | Likely cause |
|---|---|
| Nothing plays | The sound has no clips, the channel or Master volume is 0, or there's no AudioListener (it's on the Main Camera by default) |
| Console: *"… içinde klip yok"* (no clip) | The sound's Clips list is empty or has a missing clip |
| A 3D sound is heard everywhere at the same volume | **Spatial Blend** is 0, or you used `Play` instead of `PlayAt`/`PlayAttached` |
| A 3D sound has no direction | The clip is stereo: enable **Force To Mono** |
| Loop clicks | The audio file doesn't loop cleanly, or it's an MP3 |
| A sound is sometimes missing | Max Instances / Cooldown blocked it, or the voice limit cut it for a more important sound |
| Music restarts when changing scenes | It doesn't: `PlayMusic` with the same track keeps playing. Make sure both scenes use the same sound asset |
| Volumes reset every launch | **Save Volumes** is off in the settings asset |

## Good to know

Things that commonly go wrong with audio in Unity games, and how this package handles them.

- **One AudioListener per scene.** It sits on the Main Camera by default. Two listeners cause a
  warning and wrong 3D positions; zero listeners means silence. In split-screen games keep one.
- **3D sounds are heard from the listener.** In a third-person game the listener is on the camera,
  a few meters behind the player. If footsteps sound too far, raise their **Min Distance** a bit.
- **Pausing:** `Time.timeScale = 0` does **not** pause audio. Call `AudioManager.SetPaused(true)` too.
  The package uses unscaled time, so fades and music keep working while the game is paused.
- **Don't call `Play` every frame for a loop.** Start a loop once, keep its `SoundHandle`, and change
  its volume/pitch. (`PlayMusic` is the exception: calling it again with the same track does nothing.)
- **Objects that are destroyed:** `PlayAttached` stops the sound when its target is destroyed. For a
  sound that should outlive the object (a dying enemy's scream, a picked-up coin), use `PlayAt` with
  the object's position, or `PlaySound` with **Play On = Disable**.
- **Your own object pools:** if you reuse objects instead of destroying them, stop their attached
  loops when you return them to the pool (`handle.Stop()`), otherwise the sound keeps following the
  hidden object.
- **Scene changes:** the `[Audio Manager]` object survives scene loads, so music continues. Effects
  attached to objects of the old scene stop because their objects are destroyed.
- **Web builds (WebGL):** browsers block audio until the player clicks or presses a key. Start the
  music after the first input (e.g. on the "Play" button of your main menu).
- **Mobile:** on iOS the silent switch mutes the game by default; on Android audio pauses when the
  app goes to the background. Both are normal platform behavior.
- **Memory:** every clip with Load Type *Decompress On Load* sits uncompressed in memory. Long
  clips should be *Streaming* or *Compressed In Memory* (see the import table above).
- **AudioMixer:** if you route channels into mixer groups and also change the mixer's exposed
  volume parameters yourself, both apply (they multiply). Use one or the other for volume.

## How it works

`AudioManager` creates an invisible `[Audio Manager]` object the first time you play a sound. It
survives scene loads and owns a pool of AudioSources:

1. **Play:** checks cooldown and max instances, takes a free source (or creates one up to Max Voices,
   or cuts the least important / oldest sound), sets clip, volume, pitch and 3D settings, plays.
2. **Every frame:** moves attached sounds to their target, applies fades and channel volumes, and
   returns finished sounds to the pool.
3. **Music:** the new track fades in while the old one fades out, then the old one returns to the pool.

Final volume = sound volume × random variation × fade × channel volume × master volume.

| File | Job |
|---|---|
| `Runtime/AudioManager.cs` | The API |
| `Runtime/AudioRuntime.cs` | Pool, voice limit, fades, music, volumes |
| `Runtime/SoundDefinition.cs` | The sound asset |
| `Runtime/SoundHandle.cs` | Remote control for a playing sound |
| `Runtime/AudioManagerSettings.cs` | Optional settings asset |
| `Runtime/PlaySound.cs`, `Runtime/PlayMusic.cs` | Components |
| `Runtime/Demo/` | Demo panel and orbiting engine |
| `Editor/` | Sound inspector with preview, menus |
| `Sounds/` | Sample clips and sound assets |
| `Art~/make_sounds.py` | The script that synthesized the sample clips |
| `Tests/Runtime/AudioManagerTests.cs` | 13 tests |

## Tests

13 PlayMode tests: 2D and volume math, live channel changes, 3D position and following a target,
target destroyed, variations without repeats, pitch range, cooldown, max instances, voice limit with
priorities, finished sounds returning to the pool, fade-out, music crossfade (and not restarting the
same track), pause behavior, volume persistence, and `PlaySound` on collision.

```json
"testables": [ "com.atthree.audio-manager" ]
```

Add this to `Packages/manifest.json`, then **Window > General > Test Runner > PlayMode**.

## License

Code and sample sounds: [MIT](LICENSE.md).

---

<a name="türkçe"></a>
# Türkçe

- **Ses asset'leri:** her ses için bir asset: klip varyasyonları, rastgele ses/pitch, 2D/3D mesafe,
  bekleme süresi, en fazla kopya sayısı ve öncelik. Sesleri kodu değiştirmeden ayarlarsın.
- **Tek satır:** `AudioManager.Play(sound)`, `PlayAt(sound, konum)`, `PlayAttached(sound, transform)`.
- **Kaynak havuzu:** her ses için `AudioSource` yaratılıp silinmez. Sınıra gelince her şey birbirine
  girmesin diye en önemsiz ses susturulur.
- **Müzik** yumuşak geçişli; zaten çalan parçayı tekrar istemek bir şey yapmaz.
- **Ses kanalları:** Master, Music, Sfx, Ui, Voice, Ambience. Otomatik kaydedilir.
- **Duraklatma:** efektler durur ve kaldığı yerden devam eder; müzik ve UI çalmaya devam eder.
- **Bileşenler:** `PlaySound` (başlangıçta, açılınca, kapanınca, darbe şiddetine göre çarpışmada,
  trigger'da ya da butonla) ve `PlayMusic` (sahneye ya da alana göre).
- Bu paket için üretilmiş **12 örnek ses** (lisans sorunu yok) ve bir demo sahnesi.

**Gereksinimler:** Unity 6 (6000.0+). Başka paket gerekmez.

## Adım adım kurulum

### 1. Paketi ekle

**Window > Package Manager** → **+** → **Install package from git URL…** →

```
https://github.com/Atthree/AudioManager.git
```

### 2. Demoyu dene

1. **Tools > Audio Manager > Create Demo Scene**, sonra **Play**.
2. Her düğme bir UI tık sesi çalar. **Altın** (iki varyasyon), **Zıpla** ve **Patlama**'yı dene
   (patlama solda ya da sağda bir yerde olur: yönü dinle).
3. **Kutuları düşür**: her kutu yere çarpınca darbe şiddetiyle orantılı ses çıkarır.
4. Çember çizen kapsül motor sesi çalar: yaklaşınca yükselir, sağdan/soldan duyulur, pitch hızına uyar.
5. **Sakin / Hareketli** müziği 2 saniyelik geçişle değiştirir.
6. **Spam: 30 altın**: tek karede 30 çağrı, ama sadece birkaçını duyarsın (en fazla kopya + bekleme).
7. **Duraklat** efektleri durdurur; müzik ve UI tıkları devam eder.
8. Kaydırıcıları oynat, Play'den çıkıp tekrar gir: seviyeler hatırlanır.

### 3. Ses oluştur

1. Ses dosyalarını içe aktar ([Kendi ses dosyaların](#kendi-ses-dosyaların) bölümüne bak).
2. Project penceresinde bir ya da daha çok klip seç → sağ tık → **Create > Audio Manager > Sound From
   Selected Clips**. Adı sadece sonundaki numarayla ayrılan klipler (`Footstep_01`, `Footstep_02`…)
   varyasyonlu **tek** bir ses olur.
3. Yeni sesi seç: Inspector'daki **▶ Dinle** ile Play'e basmadan dinle; her basışta yeni bir varyasyon
   çalar. Kanalı, sesi, pitch farkını, bekleme süresini... ayarla.

Boş da oluşturabilirsin: **Create > Audio Manager > Sound**, sonra klipleri **Clips** listesine sürükle.

### 4. Koddan çal

```csharp
using Atthree.Audio;

public SoundDefinition jumpSound, explosionSound, engineLoop, forestMusic;

AudioManager.Play(jumpSound);                               // 2D: arayüz, oyuncunun kendi sesleri
AudioManager.PlayAt(explosionSound, transform.position);    // 3D, bir noktada
SoundHandle engine = AudioManager.PlayAttached(engineLoop, transform); // 3D, objeyi takip eder
engine.SetPitch(0.8f + speed / maxSpeed);                   // çalarken değiştir
engine.Stop(fadeSeconds: 0.5f);

AudioManager.PlayMusic(forestMusic, fadeSeconds: 2f);       // yumuşak geçiş
AudioManager.StopMusic(fadeSeconds: 1f);
```

`SoundHandle`'ı saklamak güvenlidir: ses bittiğinde metotları bir şey yapmaz, `IsPlaying` false olur.

### 5. …ya da kodsuz

- **Add Component > Audio Manager > Play Sound**, sesi ve **Play On**'u seç:

  | Play On | Ne için |
  |---|---|
  | Start / Enable | Ortam döngüleri, doğma sesleri |
  | Disable | Toplanan ve kaybolan şeyler (objenin olduğu yerde çalar) |
  | Collision | Kutular, arabalar, ragdoll'lar: darbe ne kadar sertse o kadar yüksek (**Min Impact Speed**, **Full Volume Speed**) |
  | TriggerEnter | Kapılar, alanlar, toplanabilir eşyalar |
  | Manual | Bir UI Button'ın **On Click**'inden `Play()` çağır |

- **Add Component > Audio Manager > Play Music**: sahne başlayınca müziğini çalar, ya da
  (**Play On Start** kapalıyken) oyuncu trigger'ına girince.

### 6. Ayarlar menüsü ve duraklatma menüsü

```csharp
musicSlider.value = AudioManager.GetVolume(AudioChannel.Music);
musicSlider.onValueChanged.AddListener(v => AudioManager.SetVolume(AudioChannel.Music, v)); // kaydedilir

void OpenPauseMenu()  { AudioManager.SetPaused(true);  Time.timeScale = 0f; }
void ClosePauseMenu() { AudioManager.SetPaused(false); Time.timeScale = 1f; }
```

## Ayarlar

İsteğe bağlı. **Tools > Audio Manager > Create Settings Asset**,
`Assets/Resources/AudioManagerSettings.asset` oluşturur; otomatik okunur.

| Ayar | Varsayılan | Ne yapar |
|---|---|---|
| Initial Sources | 16 | Başta hazırlanan AudioSource sayısı |
| Max Voices | 48 | Aynı anda çalabilecek ses. Gerçek cihazlar ~32–64 sesi iyi karıştırır |
| Save Volumes | açık | Kanal seviyelerini hatırla (PlayerPrefs) |
| Default … | 1 / müzik 0.7 / ortam 0.8 | İlk açılıştaki seviyeler |
| Music / UI Ignores Pause | açık | Duraklatılınca müzik ve UI çalmaya devam etsin |
| … Group | boş | Bir kanalı kendi AudioMixer grubuna yönlendir (yankı, sıkıştırma, ducking) |

Seviyeler kodla uygulanır; **AudioMixer gerekmez**. Mixer gruplarını sadece mixer efektleri
istiyorsan ata.

## Kendi ses dosyaların

Asset Store, freesound.org, Sonniss GDC paketleri, sfxr/jsfxr ya da kendi kayıtların aynı şekilde
çalışır. Doğru duyulup duyulmayacaklarını şunlar belirler.

### İçe aktarmadan önce

| # | Kontrol | Neden |
|---|---|---|
| 1 | **Lisans**: ses (ticari) bir oyunda kullanılabilir mi; gereken atıfları not et | Ücretsiz seslerin çoğu CC-BY (atıf şart) ya da ticari olmayan kullanım içindir |
| 2 | **Format**: kaynak olarak WAV (ya da OGG). Döngülerde MP3 kullanma | MP3 başa sessizlik ekler, döngüde tık sesi yapar |
| 3 | Baştaki **sessizliği kırp** | Eylem ile ses arasındaki gecikme gecikmeli (laggy) hissettirir |
| 4 | **Döngüler temiz dönmeli**: son, başa akmalı | Yoksa her turda bir tık duyarsın |
| 5 | Seslerin **benzer yükseklikte** olsun (normalize) | Böylece `volume` alanları her seste aynı anlama gelir |
| 6 | **3D sesler mono olmalı** | Stereo klip 3D çalınınca konumunu kaybeder |

### İçe aktarma ayarları (klibi Project penceresinde seç)

| Klip tipi | Load Type | Compression | Diğer |
|---|---|---|---|
| Kısa efektler (< 1 sn): tık, darbe, adım | Decompress On Load | ADPCM ya da Vorbis | — |
| Orta (1–10 sn): patlama, konuşma | Compressed In Memory | Vorbis, Quality ~70 | — |
| Müzik ve uzun ortam sesleri | **Streaming** | Vorbis, Quality ~60 | Az bellek kullanır |
| `PlayAt` / `PlayAttached` ile çalınan her şey | — | — | **Force To Mono** açık |

Değiştirdikten sonra **Apply**'a bas. Birden çok klibi seçip birlikte değiştirebilirsin.

### İyi ses için ipuçları

- **Varyasyon:** dosyaları `Hit_01`, `Hit_02`, `Hit_03` diye adlandır ve sesi hepsinden birden oluştur.
  2–4 varyasyon + %5–10 **Pitch Variation** ile tekrar eden sesler robotik duyulmaz.
- **Spam koruması:** saniyede çok kez tetiklenebilen sesler (altın, mermi, darbe) için **Max
  Instances** (3–6) ve küçük bir **Cooldown** (0.03–0.1 sn) ver.
- **Öncelik:** önemli sesler (oyuncunun hasar alması, diyalog) 0–64 gibi düşük; arka plan detayları
  180–256 gibi yüksek değer alsın, susturulan onlar olsun.
- **3D mesafeler:** **Min Distance** = sesin tam yüksek duyulduğu yakınlık (adım için küçük, patlama
  için büyük); **Max Distance** = kaybolduğu uzaklık.
- **Müzik döngüsü:** sesteki **Loop**'u aç; içe aktarma ayarındaki "Loop" gerekmez.

### Sorun giderme

| Sorun | Muhtemel sebep |
|---|---|
| Hiç ses yok | Seste klip yok, kanal ya da Master seviyesi 0, ya da AudioListener yok (varsayılan olarak Main Camera'da) |
| Console: *"… içinde klip yok"* | Sesin Clips listesi boş ya da eksik klip var |
| 3D ses her yerde aynı yükseklikte | **Spatial Blend** 0, ya da `PlayAt`/`PlayAttached` yerine `Play` kullanılmış |
| 3D sesin yönü yok | Klip stereo: **Force To Mono**'yu aç |
| Döngü tıklıyor | Ses dosyası temiz dönmüyor ya da MP3 |
| Bir ses bazen çalmıyor | Max Instances / Cooldown engelledi, ya da ses sınırı daha önemli bir ses için onu kesti |
| Sahne değişince müzik baştan başlıyor | Başlamaz: aynı parçayla `PlayMusic` çalmaya devam eder. İki sahnenin de aynı ses asset'ini kullandığından emin ol |
| Seviyeler her açılışta sıfırlanıyor | Ayar asset'inde **Save Volumes** kapalı |

## Dikkat edilecekler

Unity oyunlarında seste sık yaşanan sorunlar ve bu paketin onları nasıl ele aldığı.

- **Sahnede tek AudioListener olmalı.** Varsayılan olarak Main Camera'dadır. İki tane olursa uyarı
  çıkar ve 3D konumlar yanlış olur; hiç yoksa ses duyulmaz. Bölünmüş ekranlı oyunlarda bile bir tane tut.
- **3D sesler dinleyicinin (listener) yerinden duyulur.** 3. şahıs oyunda listener kameradadır, yani
  oyuncunun birkaç metre arkasında. Ayak sesleri fazla uzak geliyorsa **Min Distance**'ı biraz artır.
- **Duraklatma:** `Time.timeScale = 0` sesi **durdurmaz**. Ayrıca `AudioManager.SetPaused(true)` çağır.
  Paket ölçeklenmemiş zaman kullanır; oyun durmuşken de fade'ler ve müzik çalışır.
- **Bir döngü sesi için her kare `Play` çağırma.** Döngüyü bir kez başlat, `SoundHandle`'ını sakla,
  sesini/pitch'ini onunla değiştir. (`PlayMusic` istisnadır: aynı parçayla tekrar çağırmak bir şey yapmaz.)
- **Yok edilen objeler:** `PlayAttached`, takip ettiği obje yok olunca sesi durdurur. Objeden uzun
  yaşaması gereken sesler için (ölen düşmanın çığlığı, toplanan altın) objenin konumuyla `PlayAt` ya da
  **Play On = Disable** ayarlı `PlaySound` kullan.
- **Kendi obje havuzların:** objeleri yok etmek yerine tekrar kullanıyorsan, havuza geri koyarken
  takip eden döngü seslerini durdur (`handle.Stop()`); yoksa ses gizlenmiş objeyi takip etmeye devam eder.
- **Sahne değişimi:** `[Audio Manager]` objesi sahne değişince yok olmaz, müzik devam eder. Eski
  sahnedeki objelere bağlı efektler, objeleri yok olduğu için durur.
- **Web (WebGL):** tarayıcılar, oyuncu tıklayana ya da tuşa basana kadar sesi engeller. Müziği ilk
  girdiden sonra başlat (ör. ana menüdeki "Oyna" düğmesinde).
- **Mobil:** iOS'ta sessiz düğmesi oyunu varsayılan olarak susturur; Android'de uygulama arka plana
  atılınca ses durur. İkisi de platformun normal davranışı.
- **Bellek:** Load Type'ı *Decompress On Load* olan her klip bellekte sıkıştırılmamış durur. Uzun
  klipler *Streaming* ya da *Compressed In Memory* olmalı (yukarıdaki içe aktarma tablosuna bak).
- **AudioMixer:** kanalları mixer gruplarına yönlendirip bir de mixer'ın dışa açık (exposed) ses
  parametrelerini kendin değiştirirsen ikisi de uygulanır (çarpılır). Ses seviyesi için birini seç.

## Nasıl çalışıyor?

`AudioManager` ilk ses çalındığında görünmez bir `[Audio Manager]` objesi oluşturur. Bu obje sahne
değişince yok olmaz ve bir AudioSource havuzunu yönetir:

1. **Çal:** bekleme süresi ve en fazla kopya sayısını kontrol eder, boş bir kaynak alır (yoksa Max
   Voices'a kadar yenisini yaratır ya da en önemsiz / en eski sesi keser), klibi, sesi, pitch'i ve 3D
   ayarlarını verir, çalar.
2. **Her kare:** takip eden sesleri hedefe taşır, fade'leri ve kanal seviyelerini uygular, biten
   sesleri havuza geri verir.
3. **Müzik:** yeni parça açılırken eskisi kısılır, sonra eskisi havuza döner.

Son ses seviyesi = sesin seviyesi × rastgele fark × fade × kanal seviyesi × master seviyesi.

Dosya listesi ve testler için İngilizce bölümdeki [How it works](#how-it-works) ve [Tests](#tests)
kısımlarına bak.

## Lisans

Kod ve örnek sesler: [MIT](LICENSE.md).
