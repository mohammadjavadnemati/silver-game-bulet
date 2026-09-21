export type PlayerInfo = {
  playerId: string;
  name: string;
  isHost: boolean;
  isConnected: boolean;
};

export type RoomInfo = {
  roomCode: string;
  status: "WaitingForPlayers" | "InGame" | "Finished";
  players: PlayerInfo[];
};

export type CardType =
  | "Hunter" | "Lycan" | "Priest" | "GothGirl" | "Mortician"
  | "Cow" | "Instigator" | "Insomnia" | "Thing" | "Marksman"
  | "TheCount" | "Troublemaker" | "Gremlin" | "Copycat"|"Bullet";

export type CardView = {
  cardId: string;
  type: CardType | null;   // null یعنی پشت‌ورو و برای تو قابل دیدن نیست
  value: number | null;
  isPubliclyRevealed: boolean;
};

export type VillageView = {
  playerId: string;
 
  cards: CardView[];
};

export type GamePhase =
  | "WaitingToStart" | "RoundInProgress" | "FinalTurnsAfterCall"
  | "AwaitingFinalHunterDecisions" | "RoundScoring" | "GameFinished";

export type GameStateView = {
  gameId: string;
  phase: GamePhase;
  roundNumber: number;
  initialPeekDeadlineUtc: string;
  currentPlayerId: string;
  playerIdsInTurnOrder: string[];
  cumulativeScores: Record<string, number>;
  hasBeenCalled: boolean;
  callerPlayerId: string | null;

      drawPileCount: number;
  drawPile: CardView[];
  bulletTargetCardId: string | null;
  pendingHunterDecisionPlayerIds: string[] | null;
  discardPileTop: CardView | null;
discardPile: CardView[];   // ← این خط رو اضافه کن
  discardPileCount: number;
  pendingAbilityPlayerId: string | null;
  pendingAbilityCardType: CardType | null;
  pendingDrawnCard: CardView | null;
  
sideActionUsedThisTurn: boolean;
  villages: Record<string, VillageView>;
  winnerPlayerId: string | null;
  InitialPeekDeadlineUtc: string | null;   // ← جدید
  myInitialPeeksRemaining: number; 
  drawnCardSource: "None" | "Deck" | "Discard";

  abilityUsedThisTurn: boolean;
  roundEndReason: string;
lastRoundScores: Record<string, number>;
isFinalRoundDeclared: boolean;
finalRoundDeclarerPlayerId: string | null;
pendingGremlinCard: CardView | null;


};

export const CARD_NAMES_FA: Record<CardType, string> = {
  Hunter: "شکارچی",
  Lycan: "لیکن",
  Priest: "پدر روحانی",
  GothGirl: "دختر گاث",
  Mortician: "قبرکن",
  Cow: "گاو",
  Instigator: "تخس",
  Insomnia: "بی‌خوابی",
  Thing: "موجود",
  Marksman: "تیرانداز",
  TheCount: "کنت",
  Troublemaker: "دردسرآفرین",
  Gremlin: "گرملین",
  Copycat: "تقلیدکار",
  Bullet: "گلوله"
};
export const CARD_IMAGES: Record<CardType, string> = {
  Hunter: "/cards/Hunter.jpg",
  Lycan: "/cards/Lycan.jpg",
  Priest: "/cards/Priest.jpg",
  GothGirl: "/cards/GothGirl.jpg",
  Mortician: "/cards/Mortician.jpg",
  Cow: "/cards/Cow.jpg",
  Instigator: "/cards/Instigator.jpg",
  Insomnia: "/cards/Insomnia.jpg",
  Thing: "/cards/Thing.jpg",
  Marksman: "/cards/Marksman.jpg",
  TheCount: "/cards/TheCount.jpg",
  Troublemaker: "/cards/Troublemaker.jpg",
  Gremlin: "/cards/Gremlin.jpg",
  Copycat: "/cards/Copycat.jpg",
  Bullet:"/cards/Bullet.jpg"
};

export const CARD_BACK_IMAGE = "/cards/back.png";

// توضیح توانایی هر کارت — متن دلخواه خودتو جای این‌ها بذار
export const CARD_DESCRIPTIONS_FA: Record<CardType, string> = { 
  Hunter: "اگر روباشد:در پایان راند، اگر رو‌شده باشد، ۱ کارت حذف میشود.", 
  Lycan: "اگر روباشد: برای سوزاندن کارت به ارزش یکی از کارت ها یکی اضافه میکند", 
  Priest: "اگرروباشد: درنوبت خودت یکی از کارت هات رو روکن", 
  GothGirl: "اگرروباشد: کارت هارو به جای سوزاندان باید در پایین دسته کارت ها بگذاری.", 
  Mortician: "اگر رو باشد: موقع دورانداختن کارت‌های‌۵تا۱۲‌میتونی‌ازویژگیشون‌استفاده‌کنی.", 
  Cow: "دسته‌کارت‌ها‌رو‌برمی‌گردونه", 
  Instigator: "هرکارتی‌روکه‌خواستی‌برگردون.", 
  Insomnia: "تمام‌کارت‌های‌دستت‌رو‌ببین.",
  Thing: "میتونی‌تمام‌کارت‌های‌به‌پشت‌یک‌دهکده‌را‌بربزنی.", 
  Marksman: "از قابلیت یک کارت ۵تا۱۲ که داخل هردهکده‌ای روهستند استفاده کن.",
  TheCount: "ده کارت از دسته کارت ها بردار و بسوزان.", 
  Troublemaker: "یک کارت از یک بازیکن را با یک کارت از بازیکن دیگری جابه‌جا می‌کند.", 
  Gremlin: "یک کارت به روستای یک بازیکن اضافه می‌کند، بدون اینکه کارت دیگری را دور بیندازد.", 
  Copycat: "درشمارش برابر باکوچکترین کارت است.",
Bullet:"میتونی یکی از کارت های دستت رو امتیازش رو صفر کنی." 
};
  