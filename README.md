<div dir="rtl" align="right">

<h1 align="center">🌙 Silver — طلسم</h1>
<p align="center"><b>نسخه‌ی گلوله (Bullet Edition)</b></p>
<p align="center">بازی کارتی آنلاین و چندنفره‌ی بلادرنگ، ۲ تا ۴ نفره، با قابلیت‌های ویژه‌ی هر کارت</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/ASP.NET_Core-SignalR-5C2D91" alt="SignalR" />
  <img src="https://img.shields.io/badge/Next.js-16-000000?logo=nextdotjs" alt="Next.js" />
  <img src="https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black" alt="React" />
  <img src="https://img.shields.io/badge/Tailwind-4-06B6D4?logo=tailwindcss&logoColor=white" alt="Tailwind" />
  <img src="https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white" alt="TypeScript" />
</p>

---

## 📖 درباره‌ی پروژه

**Silver (طلسم)** یک بازی کارتی آنلاین است. هر بازیکن یک «روستا» (Village) شامل ۵ کارت پشت‌رو دارد و هدف این است که در پایان ۴ راند، **کم‌ترین مجموع امتیاز** را داشته باشی. هر کارت یک قابلیت ویژه دارد، کارت‌ها می‌توانند رو یا پشت باشند، و در «نسخه‌ی گلوله» کارت ویژه‌ی **Bullet** به برنده‌ی راند قبل می‌رسد.

منطق بازی کاملاً **سمت سرور** اجرا می‌شود؛ کلاینت فقط اکشن می‌فرستد و سرور آن را اعتبارسنجی می‌کند. هر بازیکن فقط اطلاعاتی را می‌بیند که حق دیدنش را دارد (مقدار کارت‌های پشت‌رو دیگران هرگز به مرورگر ارسال نمی‌شود).

## ✨ ویژگی‌ها

- 🎮 بازی بلادرنگ ۲ تا ۴ نفره با **SignalR (WebSocket)**
- 🚪 ساخت اتاق با کد ۵ حرفی و پیوستن با کد
- 🔒 حالت‌نمایش امن: کارت‌های مخفی فقط برای صاحبشان (یا با قابلیت‌ها) قابل مشاهده‌اند
- 🃏 ۱۴ نوع کارت با قابلیت‌های منحصربه‌فرد + کارت ویژه‌ی Bullet
- 🔄 **اتصال مجدد خودکار**: با شناسه‌ی پایدار بازیکن، بعد از رفرش به همان اتاق و بازی برمی‌گردی
- ⏱️ **Auto-timeout**: اگر بازیکنِ نوبت‌دار ۶۰ ثانیه قطع باشد، نوبتش خودکار رد می‌شود
- 👁️ پنجره‌ی ۱۰ ثانیه‌ای «نگاه اولیه» به ۲ کارت از کارت‌های خودت در ابتدای هر راند
- 🌐 رابط کاربری فارسی (RTL) با تم تیره

## 🃏 قوانین بازی

### هدف
پس از **۴ راند**، بازیکنی که مجموع امتیاز کمتری دارد برنده است.

### شروع راند
- ۵۲ کارت شافل می‌شود؛ هر بازیکن ۵ کارت پشت‌رو دریافت می‌کند.
- یک کارت رو به دسته‌ی دورریختنی (سوخته‌ها) می‌رود.
- برنده‌ی **یکتای** راند قبل، کارت **Bullet** را می‌گیرد.

### نوبت
در هر نوبت یکی از این کارها را انجام می‌دهی:

| اکشن | توضیح |
|------|-------|
| کشیدن از دسته‌ی اصلی | کارت را می‌بینی و یا در روستایت می‌گذاری، یا می‌سوزانی |
| برداشتن از دورریختنی | کارت رو را برمی‌داری |
| **Call** | فقط با ۴ کارت یا کمتر؛ راند به پایان می‌رسد (بعد از یک دور نوبت) |
| **دور آخر** | اعلام می‌کنی؛ راند وقتی نوبت به خودت برگردد تمام می‌شود |

**سوزاندن گروهی:** می‌توانی چند کارت **هم‌عدد** از روستایت را با هم بسوزانی. هر **Lycan** رو، به اندازه‌ی ۱ واحد به ارزش یک کارت اضافه می‌کند تا بتوانی کارت‌های نابرابر را هم‌عدد کنی. تلاش ناموفق لغو می‌شود و ممکن است جریمه داشته باشد.

### امتیازدهی
- امتیاز روستا = مجموع ارزش کارت‌ها (کمتر بهتر است).
- **Call موفق** (کمترین امتیاز): امتیاز کالر = ۰ و **آمیولت** را می‌گیرد.
- **Call ناموفق**: ۱۰ امتیاز جریمه.
- **Bullet:** یک‌بار در هر راند می‌توانی امتیاز یکی از کارت‌های روستایت را صفر کنی.

### کارت‌ها

<details>
<summary><b>جدول کامل کارت‌ها و قابلیت‌ها</b></summary>

| ارزش | کارت | تعداد | قابلیت |
|:---:|------|:---:|--------|
| 0 | **Hunter** (شکارچی) | ۲ | *(رو)* در پایان بازی، یک کارت از شمارش حذف می‌شود |
| 1 | **Lycan** (لیکن) | ۴ | *(رو)* هنگام سوزاندن گروهی، ۱ واحد به ارزش یک کارت اضافه می‌کند |
| 2 | **Priest** (پدر روحانی) | ۴ | *(رو)* در نوبتت یک کارت از خودت را رو می‌کنی (به تعداد Priestهای رو) |
| 3 | **GothGirl** (دختر گاث) | ۴ | *(رو)* کارت‌های سوخته به‌جای دورریختنی، زیر دسته‌ی اصلی می‌روند |
| 4 | **Mortician** (قبرکن) | ۴ | *(رو)* قابلیت کارت‌های ۵ تا ۱۲ هنگام سوزاندن در همه‌ی حالت‌ها فعال می‌شود |
| 5 | **Cow** (گاو) | ۴ | دسته‌ی اصلی را برمی‌گرداند |
| 6 | **Instigator** (تخس) | ۴ | هر کارتی را رو/پشت می‌کند |
| 7 | **Insomnia** (بی‌خوابی) | ۴ | ۵ ثانیه تمام کارت‌های مخفی خودت را می‌بینی |
| 8 | **Thing** (موجود) | ۴ | کارت‌های پشت‌رو یک روستا را بُر می‌زند |
| 9 | **Marksman** (تیرانداز) | ۴ | از قابلیت یک کارت رو (۹ تا ۱۲ و مشابه) استفاده می‌کند |
| 10 | **TheCount** (کنت) | ۴ | ۱۰ کارت از دسته‌ی اصلی را می‌سوزاند |
| 11 | **Troublemaker** (دردسرآفرین) | ۴ | یک کارت از یک روستا را با کارتی از روستای دیگر عوض می‌کند |
| 12 | **Gremlin** (گرملین) | ۴ | کارت را بدون سوزاندن چیزی، به روستای یک بازیکن اضافه می‌کند |
| 13 | **Copycat** (تقلیدکار) | ۲ | ارزش‌ش در شمارش برابر کوچک‌ترین کارت دیگر است |
| 0 | **Bullet** (گلوله) | ۱ | امتیاز یک کارت خودت را در آن راند صفر می‌کند |

> قابلیت‌های فعال (Cow تا Gremlin) وقتی اجرا می‌شوند که کارت **مستقیم از دسته‌ی اصلی** کشیده و بلافاصله سوزانده شود (یا Mortician رو باشد).

</details>

## 🏗️ معماری

```
┌─────────────────────┐   SignalR (WebSocket)   ┌──────────────────────────┐
│  Frontend           │ ◄─────────────────────► │  Silver.Api              │
│  Next.js + React    │   SendGameAction(...)   │  GameHub / RoomService   │
│  Tailwind           │   GameStateUpdated      │  GameSessionService      │
└─────────────────────┘   PrivateCardsRevealed  │  AutoTimeoutBackground   │
                                                └────────────┬─────────────┘
                                                             │ ApplyAction
                                                ┌────────────▼─────────────┐
                                                │  Silver.Engine           │
                                                │  قوانین خالص بازی        │
                                                └──────────────────────────┘
```

- **Silver.Engine** — کتابخانه‌ی مستقل و بدون وابستگی به وب؛ شامل state، اکشن‌ها و تمام قوانین (قابل تست واحد).
- **Silver.Api** — هاب SignalR، مدیریت اتاق‌ها، ذخیره‌ی state (`IGameStateStore`) و ساخت نمای اختصاصی هر بازیکن (`BuildPlayerFacingState`).
- **Frontend** — رابط کاربری و هوک `useGameConnection` برای ارتباط با هاب.

### ساختار پوشه‌ها

```
silver-game/
├── backend/
│   ├── Silver.Api/
│   │   ├── Hubs/GameHub.cs                      # نقطه‌ی ورود SignalR
│   │   ├── Services/
│   │   │   ├── RoomService.cs                   # اتاق‌ها و بازیکن‌ها
│   │   │   ├── GameSessionService.cs            # اجرای اکشن + نمای امن برای هر بازیکن
│   │   │   ├── InMemoryGameStateStore.cs        # ذخیره‌ی state در حافظه
│   │   │   └── AutoTimeoutBackgroundService.cs  # رد نوبتِ بازیکن قطع‌شده
│   │   ├── Models/                              # Room, Player
│   │   └── Program.cs
│   └── Silver.Engine/
│       ├── Cards/                               # CardType, CardDefinitions, SilverCard
│       ├── SilverGameEngine.cs                  # قوانین بازی
│       ├── SilverGameState.cs
│       ├── SilverAction.cs
│       └── SilverPlayerVillage.cs
└── frontend/
    ├── public/cards/                            # تصاویر کارت‌ها
    └── src/
        ├── app/page.tsx                         # صفحه‌ی اصلی: لابی و میز بازی
        ├── components/                          # PeekableCard, DrawPileStack, ...
        └── lib/                                 # signalr.ts, types.ts
```

## 🚀 اجرای پروژه

### پیش‌نیازها

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) نسخه‌ی ۲۰.۹ یا بالاتر

### ۱. کلون

```bash
git clone https://github.com/mohammadjavadnemati/silver-game.git
cd silver-game
```

### ۲. اجرای بک‌اند

```bash
cd backend/Silver.Api
dotnet run
```

سرور روی `http://localhost:5000` بالا می‌آید:

| مسیر | توضیح |
|------|-------|
| `/hubs/game` | هاب SignalR |
| `/health` | بررسی سلامت سرویس |

### ۳. اجرای فرانت‌اند

```bash
cd frontend
npm install
npm run dev
```

مرورگر را روی `http://localhost:3000` باز کن. برای تست چندنفره، چند تب/مرورگر (یا حالت ناشناس) باز کن و با کد اتاق وارد شو.

> آدرس هاب در `frontend/src/lib/signalr.ts` (`HUB_URL`) تعریف شده است. برای تست روی شبکه‌ی محلی، `localhost` را با IP سیستم عوض کن.

## 🔌 API هاب (SignalR)

**متدهای کلاینت → سرور**

| متد | توضیح |
|-----|-------|
| `CreateRoom(playerId, playerName)` | ساخت اتاق جدید |
| `JoinRoom(roomCode, playerId, playerName)` | پیوستن یا اتصال مجدد |
| `StartGame(roomCode)` | شروع بازی (حداقل ۲ بازیکن) |
| `SendGameAction(roomCode, actionType, payload)` | نقطه‌ی ورود واحد برای همه‌ی اکشن‌های بازی |

**رویدادهای سرور → کلاینت**

| رویداد | توضیح |
|--------|-------|
| `RoomUpdated` | تغییر در لابی/بازیکنان |
| `GameStateUpdated` | state فیلترشده‌ی بازی برای همان بازیکن |
| `PrivateCardsRevealed` | اطلاعات خصوصی (نگاه اولیه، کارت کشیده‌شده، Insomnia) |

<details>
<summary><b>لیست actionTypeها</b></summary>

`DrawFromDeck` · `TakeFromDiscard` · `DiscardDrawn` · `SwapDrawn` · `SwapDiscard` · `Call` · `DeclareFinalRound` · `InitialPeek` · `PriestReveal` · `SkipAbility` · `GremlinPenalize` · `TroublemakerSwap` · `TheCountBurnTen` · `MarksmanActivate` · `CowFlipDeck` · `InstigatorFlip` · `InsomniaViewAll` · `ThingShuffleVillage` · `BulletShoot` · `HunterRemoveCard` · `HunterSkipRemoval` · `StartNextRound`

</details>

## 🧰 تکنولوژی‌ها

| بخش | ابزار |
|-----|-------|
| Backend | C# · ASP.NET Core (.NET 10) · SignalR |
| Frontend | Next.js 16 · React 19 · TypeScript · Tailwind CSS 4 |
| ارتباط | `@microsoft/signalr` |
| فونت‌ها | Fraunces · Inter · JetBrains Mono |

## ⚠️ محدودیت‌های فعلی

- state بازی و اتاق‌ها **در حافظه** نگه‌داری می‌شوند؛ با ری‌استارت سرور از بین می‌روند (پیاده‌سازی جایگزین `IGameStateStore`، مثلاً Redis، ساده است).
- تنظیم CORS برای توسعه **باز** است؛ قبل از استقرار در production محدودش کن.
- آدرس هاب در فرانت‌اند هاردکد است.

## 🗺️ نقشه‌ی راه

- [ ] ذخیره‌ی پایدار state (Redis / DB)
- [ ] تست‌های واحد موتور بازی
- [ ] انیمیشن کارت‌ها و افکت صوتی
- [ ] چت داخل اتاق
- [ ] استقرار (Docker / CI)

## 🤝 مشارکت

Pull Request و Issue خوش‌آمد است. لطفاً قبل از تغییرات بزرگ، یک Issue باز کن.

## 📄 مجوز

این پروژه تحت مجوز **MIT** منتشر می‌شود. *(فایل `LICENSE` را به ریشه‌ی پروژه اضافه کن.)*

</div>
