<h1 align="center">🌙 Silver</h1>
<p align="center"><b>Bullet Edition</b></p>
<p align="center">A real-time online multiplayer card game for 2–4 players, with unique abilities on every card</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/ASP.NET_Core-SignalR-5C2D91" alt="SignalR" />
  <img src="https://img.shields.io/badge/Next.js-16-000000?logo=nextdotjs" alt="Next.js" />
  <img src="https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black" alt="React" />
  <img src="https://img.shields.io/badge/Tailwind-4-06B6D4?logo=tailwindcss&logoColor=white" alt="Tailwind" />
  <img src="https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white" alt="TypeScript" />
</p>

---

## 📖 About

**Silver** is an online card game. Each player has a **Village** of 5 face-down cards, and the goal is to finish 4 rounds with the **lowest total score**. Every card has a special ability, cards can be face-up or face-down, and in the **Bullet Edition** a special **Bullet** card goes to the winner of the previous round.

All game logic runs **server-side**: clients only send actions, and the server validates them. Each player only receives what they're allowed to see; face-down card values of other players are never sent to the browser.

## ✨ Features

- 🎮 Real-time 2–4 player games over **SignalR (WebSocket)**
- 🚪 Room creation with a 5-character code; join by code
- 🔒 Secure state views: hidden cards are visible only to their owner (or via abilities)
- 🃏 14 card types with unique abilities, plus the special Bullet card
- 🔄 **Automatic reconnection**: a persistent player ID brings you back to the same room and game after a refresh
- ⏱️ **Auto-timeout**: if the current player stays disconnected for 60 seconds, their turn is skipped automatically
- 👁️ A 10-second **initial peek** window at 2 of your own cards at the start of each round
- 🌐 Persian (RTL) UI with a dark theme

## 🃏 Game Rules

### Goal
After **4 rounds**, the player with the lowest total score wins.

### Round Setup
- The 52-card deck is shuffled; each player receives 5 face-down cards.
- One card is placed face-up on the discard pile.
- The **sole** winner of the previous round receives the **Bullet** card.

### Turn
On your turn, you do one of the following:

| Action | Description |
|--------|-------------|
| Draw from the deck | Look at the card, then either place it in your village or burn it |
| Take from the discard pile | Take the face-up top card |
| **Call** | Only with 4 or fewer cards; the round ends after one more turn for everyone else |
| **Final round** | Declare it; the round ends when the turn returns to you |

**Group burning:** you may burn several cards of **equal value** from your village together. Each face-up **Lycan** adds +1 to a card's value, letting you equalize unequal cards. A failed attempt is cancelled and may carry a penalty.

### Scoring
- Village score = sum of card values (lower is better).
- **Successful Call** (lowest score): caller's score becomes 0 and they take the **Amulet**.
- **Failed Call**: +10 penalty points.
- **Bullet:** once per round, you may set the score of one of your cards to zero.

### Cards

<details>
<summary><b>Full card table and abilities</b></summary>

| Value | Card | Count | Ability |
|:---:|------|:---:|---------|
| 0 | **Hunter** | 2 | *(face-up)* At game end, one card is removed from scoring |
| 1 | **Lycan** | 4 | *(face-up)* Adds +1 to a card's value during group burning |
| 2 | **Priest** | 4 | *(face-up)* On your turn, reveal one of your own cards (one per face-up Priest) |
| 3 | **GothGirl** | 4 | *(face-up)* Burned cards go to the bottom of the draw pile instead of the discard pile |
| 4 | **Mortician** | 4 | *(face-up)* Abilities of cards 5–12 trigger when burned in any way |
| 5 | **Cow** | 4 | Flips the draw pile |
| 6 | **Instigator** | 4 | Flips any card face-up/face-down |
| 7 | **Insomnia** | 4 | View all your hidden cards for 5 seconds |
| 8 | **Thing** | 4 | Shuffles the face-down cards of one village |
| 9 | **Marksman** | 4 | Uses the ability of a face-up card (9–12 and similar) |
| 10 | **TheCount** | 4 | Burns 10 cards from the draw pile |
| 11 | **Troublemaker** | 4 | Swaps a card from one village with a card from another |
| 12 | **Gremlin** | 4 | Adds the card to a player's village without burning anything |
| 13 | **Copycat** | 2 | Counts as the value of the lowest other card when scoring |
| 0 | **Bullet** | 1 | Sets the score of one of your own cards to zero for that round |

> Active abilities (Cow through Gremlin) trigger only when the card is drawn **directly from the draw pile** and burned immediately (or when a Mortician is face-up).

</details>

## 🏗️ Architecture

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
                                                │  Pure game rules         │
                                                └──────────────────────────┘
```

- **Silver.Engine**: standalone library with no web dependencies; contains state, actions, and all rules (unit-testable).
- **Silver.Api**: SignalR hub, room management, state storage (`IGameStateStore`), and per-player view building (`BuildPlayerFacingState`).
- **Frontend**: UI and the `useGameConnection` hook for talking to the hub.

### Project Structure

```
silver-game/
├── backend/
│   ├── Silver.Api/
│   │   ├── Hubs/GameHub.cs                      # SignalR entry point
│   │   ├── Services/
│   │   │   ├── RoomService.cs                   # Rooms and players
│   │   │   ├── GameSessionService.cs            # Action execution + safe per-player view
│   │   │   ├── InMemoryGameStateStore.cs        # In-memory state storage
│   │   │   └── AutoTimeoutBackgroundService.cs  # Skips turns of disconnected players
│   │   ├── Models/                              # Room, Player
│   │   └── Program.cs
│   └── Silver.Engine/
│       ├── Cards/                               # CardType, CardDefinitions, SilverCard
│       ├── SilverGameEngine.cs                  # Game rules
│       ├── SilverGameState.cs
│       ├── SilverAction.cs
│       └── SilverPlayerVillage.cs
└── frontend/
    ├── public/cards/                            # Card images
    └── src/
        ├── app/page.tsx                         # Main page: lobby and game table
        ├── components/                          # PeekableCard, DrawPileStack, ...
        └── lib/                                 # signalr.ts, types.ts
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.9 or newer

### 1. Clone

```bash
git clone https://github.com/mohammadjavadnemati/silver-game.git
cd silver-game
```

### 2. Run the backend

```bash
cd backend/Silver.Api
dotnet run
```

The server starts at `http://localhost:5000`:

| Path | Description |
|------|-------------|
| `/hubs/game` | SignalR hub |
| `/health` | Health check |

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:3000`. To test multiplayer, open several tabs/browsers (or incognito windows) and join with the room code.

> The hub URL is defined in `frontend/src/lib/signalr.ts` (`HUB_URL`). For LAN testing, replace `localhost` with your machine's IP.

## 🔌 Hub API (SignalR)

**Client → Server methods**

| Method | Description |
|--------|-------------|
| `CreateRoom(playerId, playerName)` | Create a new room |
| `JoinRoom(roomCode, playerId, playerName)` | Join or reconnect |
| `StartGame(roomCode)` | Start the game (minimum 2 players) |
| `SendGameAction(roomCode, actionType, payload)` | Single entry point for all game actions |

**Server → Client events**

| Event | Description |
|-------|-------------|
| `RoomUpdated` | Lobby/player changes |
| `GameStateUpdated` | Filtered game state for that specific player |
| `PrivateCardsRevealed` | Private info (initial peek, drawn card, Insomnia) |

<details>
<summary><b>Action types</b></summary>

`DrawFromDeck` · `TakeFromDiscard` · `DiscardDrawn` · `SwapDrawn` · `SwapDiscard` · `Call` · `DeclareFinalRound` · `InitialPeek` · `PriestReveal` · `SkipAbility` · `GremlinPenalize` · `TroublemakerSwap` · `TheCountBurnTen` · `MarksmanActivate` · `CowFlipDeck` · `InstigatorFlip` · `InsomniaViewAll` · `ThingShuffleVillage` · `BulletShoot` · `HunterRemoveCard` · `HunterSkipRemoval` · `StartNextRound`

</details>

## 🧰 Tech Stack

| Area | Tools |
|------|-------|
| Backend | C# · ASP.NET Core (.NET 10) · SignalR |
| Frontend | Next.js 16 · React 19 · TypeScript · Tailwind CSS 4 |
| Transport | `@microsoft/signalr` |
| Fonts | Fraunces · Inter · JetBrains Mono |

## ⚠️ Current Limitations

- Game state and rooms are stored **in memory** and are lost on server restart (a replacement `IGameStateStore`, e.g. Redis, is easy to add).
- CORS is **wide open** for development; restrict it before deploying to production.
- The hub URL is hardcoded in the frontend.

## 🗺️ Roadmap

- [ ] Persistent state storage (Redis / DB)
- [ ] Unit tests for the game engine
- [ ] Card animations and sound effects
- [ ] In-room chat
- [ ] Deployment (Docker / CI)

## 🤝 Contributing

Pull requests and issues are welcome. Please open an issue before making large changes.

## 📄 License

Released under the **MIT** license. *(Add a `LICENSE` file to the project root.)*