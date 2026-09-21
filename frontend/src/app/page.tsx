"use client";

import { useEffect, useState } from "react";
import { useGameConnection } from "@/lib/signalr";
import { InitialPeekTimer } from "@/components/InitialPeekTimer";
import { PeekableCard } from "@/components/PeekableCard";
import { DrawPileStack } from "@/components/DrawPileStack";
import { DrawnCardDecisionModal } from "@/components/DrawnCardDecisionModal";
import type { CardType } from "@/lib/types";

const MARKSMAN_BORROWABLE_TYPES: CardType[] = [
  "Marksman", "TheCount", "Troublemaker", "Gremlin",
  "Cow", "Instigator", "Insomnia", "Thing",
];

function RoundSummaryOverlay({
  gameState,
  showDetails,
  onToggleDetails,
  onStartNextRound,
  getPlayerName,
}: {
  gameState: any;
  showDetails: boolean;
  onToggleDetails: () => void;
  onStartNextRound: () => void;
  getPlayerName: (playerId: string | null | undefined) => string;
}) {
  const isGameFinished = gameState.phase === "GameFinished";

  const lastRoundScores: Record<string, number> = gameState.lastRoundScores ?? {};
  const cumulativeScores: Record<string, number> = gameState.cumulativeScores ?? {};

  const rankedByLastRound = Object.entries(lastRoundScores).sort(([, a], [, b]) => a - b);
  const rankedByTotal = Object.entries(cumulativeScores).sort(([, a], [, b]) => a - b);

  const roundWinnerName = rankedByLastRound[0]?.[0];
  const gameWinnerName = gameState.winnerPlayerId;

  return (
    <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-6">
      <div className="bg-panel rounded-lg p-6 max-w-md w-full space-y-4 text-center">
        <h2 className="font-display text-2xl text-silver">
          {isGameFinished ? "پایان بازی" : `پایان دور ${gameState.roundNumber}`}
        </h2>

        <div className="text-ember">
          {isGameFinished
            ? `برنده‌ی نهایی: ${getPlayerName(gameWinnerName)}`
            : `برنده‌ی این دور: ${getPlayerName(roundWinnerName)}`}
        </div>

        <div className="space-y-2">
          {rankedByLastRound.map(([playerId], index) => (
            <div
              key={getPlayerName(playerId)}
              className="flex items-center justify-between bg-panel-light rounded px-3 py-2"
            >
              <span className="text-sm">
                {index + 1}. {getPlayerName(playerId)}
              </span>
              {showDetails && (
                <span className="font-mono text-xs text-silver/70">
                  این دور: {lastRoundScores[playerId]} — کل: {cumulativeScores[playerId]}
                </span>
              )}
            </div>
          ))}
        </div>

        {isGameFinished && showDetails && (
          <div className="pt-2 border-t border-silver/10 space-y-1">
            <div className="text-xs text-silver/50 mb-1">رتبه‌بندی نهایی بر اساس مجموع امتیاز</div>
            {rankedByTotal.map(([playerId, score], index) => (
              <div key={getPlayerName(playerId)} className="flex items-center justify-between text-sm">
                <span>{index + 1}. {getPlayerName(playerId)}</span>
                <span className="font-mono text-ember">{score}</span>
              </div>
            ))}
          </div>
        )}

        <div className="flex justify-center gap-2 pt-2">
          {!isGameFinished && (
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium hover:brightness-110 transition"
              onClick={onStartNextRound}
            >
              شروع دور جدید
            </button>
          )}
          <button
            className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
            onClick={onToggleDetails}
          >
            {showDetails ? "بستن جمع‌بندی" : "جمع‌بندی"}
          </button>
        </div>
      </div>
    </div>
  );
}

export default function Home() {
  const {
    status, room, gameState, joinError, lastActionError, privateReveals,
    createRoom, joinRoom, startGame, sendAction, myPlayerId,
  } = useGameConnection();

  const playerNames: Record<string, string> = Object.fromEntries(
    (room?.players ?? []).map((p: any) => [p.playerId, p.name])
  );
  const displayName = (playerId: string | null | undefined) =>
    playerId ? playerNames[playerId] ?? playerId : "-";

  const [playerName, setPlayerName] = useState("");
  const [roomCodeInput, setRoomCodeInput] = useState("");
  const [selectedCardIds, setSelectedCardIds] = useState<string[]>([]);
  const [drawnCardInVillage, setDrawnCardInVillage] = useState(false);
  const [showRoundSummaryDetails, setShowRoundSummaryDetails] = useState(false);

  // ---- Troublemaker ----
  const [troublemakerFirst, setTroublemakerFirst] = useState<{ playerId: string; cardId: string } | null>(null);
  const [troublemakerSecond, setTroublemakerSecond] = useState<{ playerId: string; cardId: string } | null>(null);

  // ---- Marksman ----
  const [marksmanTargetCardId, setMarksmanTargetCardId] = useState<string | null>(null);
    // ---- Instigator ----
  const [instigatorTargetCardId, setInstigatorTargetCardId] = useState<string | null>(null);

  // ---- Insomnia ----
  const [insomniaRequestPending, setInsomniaRequestPending] = useState(false);
  const [insomniaTargetIds, setInsomniaTargetIds] = useState<string[]>([]);
  const [insomniaViewedCards, setInsomniaViewedCards] = useState<Record<string, { type: string; value: number }>>({});
    // ---- Thing ----
  const [thingTargetPlayerId, setThingTargetPlayerId] = useState<string | null>(null);
    // ---- Priest ----
  const [priestSelectedCardIds, setPriestSelectedCardIds] = useState<string[]>([]);

  // ---- Bullet ----
  const [bulletSelecting, setBulletSelecting] = useState(false);
  const [bulletTargetSelection, setBulletTargetSelection] = useState<string | null>(null);

  // ---- Hunter (پایان بازی) ----
  const [hunterSelectedCardId, setHunterSelectedCardId] = useState<string | null>(null);

  useEffect(() => {
    setShowRoundSummaryDetails(false);
  }, [gameState?.roundNumber]);

  useEffect(() => {
    setSelectedCardIds([]);
  }, [gameState?.currentPlayerId]);

  useEffect(() => {
    setDrawnCardInVillage(false);
  }, [gameState?.pendingDrawnCard?.cardId]);

       useEffect(() => {
    setTroublemakerFirst(null);
    setTroublemakerSecond(null);
    setMarksmanTargetCardId(null);
    setInstigatorTargetCardId(null);
    setThingTargetPlayerId(null);
  }, [gameState?.pendingAbilityPlayerId, gameState?.pendingAbilityCardType]);

  useEffect(() => {
    setPriestSelectedCardIds([]);
    setBulletSelecting(false);
    setBulletTargetSelection(null);
  }, [gameState?.currentPlayerId]);

  useEffect(() => {
    setHunterSelectedCardId(null);
  }, [gameState?.phase]);

  // Insomnia: وقتی privateReveals جدید اومد و منتظرشیم، چک کن همه‌ی کارت‌های هدف رسیدن یا نه
  useEffect(() => {
    if (!insomniaRequestPending) return;
    if (insomniaTargetIds.length === 0) return;

    const revealed = privateReveals as Record<string, { type: string; value: number }> | undefined;
    if (!revealed) return;

    const matched: Record<string, { type: string; value: number }> = {};
    for (const id of insomniaTargetIds) {
      if (revealed[id]) matched[id] = revealed[id];
    }

    if (Object.keys(matched).length === insomniaTargetIds.length) {
      setInsomniaViewedCards(matched);
      setInsomniaRequestPending(false);
    }
  }, [privateReveals, insomniaTargetIds, insomniaRequestPending]);

    // بعد از ۵ ثانیه کارت‌های Insomnia خودکار پاک می‌شن و نوبت رد می‌شه
  useEffect(() => {
    if (Object.keys(insomniaViewedCards).length === 0) return;
    const timeout = setTimeout(() => {
      setInsomniaViewedCards({});
      setInsomniaTargetIds([]);
      if (room) sendAction(room.roomCode, "SkipAbility", {});
    }, 5000);
    return () => clearTimeout(timeout);
  }, [insomniaViewedCards, room]);

  // ---- Initial Peek ----
  const [visiblePeeks, setVisiblePeeks] = useState<Record<string, { type: string; value: number }>>({});

  useEffect(() => {
    if (!gameState?.initialPeekDeadlineUtc) {
      setVisiblePeeks({});
      return;
    }
    const deadline = new Date(gameState.initialPeekDeadlineUtc).getTime();
    if (Date.now() >= deadline) {
      setVisiblePeeks({});
      return;
    }
    setVisiblePeeks((prev) => ({ ...prev, ...privateReveals }));
  }, [privateReveals, gameState?.initialPeekDeadlineUtc]);

  useEffect(() => {
    if (!gameState?.initialPeekDeadlineUtc) return;
    const deadline = new Date(gameState.initialPeekDeadlineUtc).getTime();
    const remainingMs = deadline - Date.now();
    if (remainingMs <= 0) {
      setVisiblePeeks({});
      return;
    }
    const timeoutId = window.setTimeout(() => setVisiblePeeks({}), remainingMs);
    return () => window.clearTimeout(timeoutId);
  }, [gameState?.initialPeekDeadlineUtc]);

  const statusColor = status === "connected" ? "bg-emerald-500" : status === "connecting" ? "bg-ember" : "bg-blood-moon";

  if (gameState) {
    const peekWindowOpen =
      gameState.initialPeekDeadlineUtc
        ? new Date(gameState.initialPeekDeadlineUtc).getTime() > Date.now()
        : false;

    const canPeek = peekWindowOpen && gameState.myInitialPeeksRemaining > 0;

    const handlePeek = (cardId: string) => {
      if (!room) return;
      sendAction(room.roomCode, "InitialPeek", { ownCardId: cardId });
    };

    const isMyTurn = gameState.currentPlayerId === myPlayerId;
    const myPendingDrawnCard = isMyTurn ? gameState.pendingDrawnCard : null;

    const toggleCardSelection = (cardId: string) => {
      setSelectedCardIds((prev) =>
        prev.includes(cardId) ? prev.filter((id) => id !== cardId) : [...prev, cardId]
      );
    };

    const handleDeclareFinalRound = () => {
      if (!room) return;
      sendAction(room.roomCode, "DeclareFinalRound", {});
    };

    const canDeclareFinalRound =
      isMyTurn &&
      !myPendingDrawnCard &&
      !gameState.pendingAbilityPlayerId &&
      !gameState.hasBeenCalled &&
      !gameState.isFinalRoundDeclared;

    const handleSkipAbility = () => {
      if (!room) return;
      sendAction(room.roomCode, "SkipAbility", {});
    };

        const isGremlinAbilityPending =
      gameState.pendingAbilityCardType === "Gremlin" && gameState.pendingAbilityPlayerId === myPlayerId;
    const isTroublemakerAbilityPending =
      gameState.pendingAbilityCardType === "Troublemaker" && gameState.pendingAbilityPlayerId === myPlayerId;
    const isTheCountAbilityPending =
      gameState.pendingAbilityCardType === "TheCount" && gameState.pendingAbilityPlayerId === myPlayerId;
    const isMarksmanAbilityPending =
      gameState.pendingAbilityCardType === "Marksman" && gameState.pendingAbilityPlayerId === myPlayerId;
    const isCowAbilityPending =
      gameState.pendingAbilityCardType === "Cow" && gameState.pendingAbilityPlayerId === myPlayerId;
    const isInstigatorAbilityPending =
      gameState.pendingAbilityCardType === "Instigator" && gameState.pendingAbilityPlayerId === myPlayerId;
    const isInsomniaAbilityPending =
      gameState.pendingAbilityCardType === "Insomnia" && gameState.pendingAbilityPlayerId === myPlayerId;
    const isThingAbilityPending =
      gameState.pendingAbilityCardType === "Thing" && gameState.pendingAbilityPlayerId === myPlayerId;
          // ---- Priest ----
    const myVillageForPriest = gameState.villages[myPlayerId ?? ""];
    const revealedPriestCount = myVillageForPriest
      ? myVillageForPriest.cards.filter((c: any) => c.isPubliclyRevealed && c.type === "Priest").length
      : 0;
    const canUsePriest =
      isMyTurn && !gameState.sideActionUsedThisTurn && !myPendingDrawnCard &&
      !gameState.pendingAbilityPlayerId && revealedPriestCount > 0;

    const handleTogglePriestCard = (cardId: string) => {
      if (!canUsePriest) return;
      setPriestSelectedCardIds((prev) => {
        if (prev.includes(cardId)) return prev.filter((id) => id !== cardId);
        if (prev.length >= revealedPriestCount) return prev;
        return [...prev, cardId];
      });
    };

    const handleConfirmPriestReveal = () => {
      if (!room || priestSelectedCardIds.length === 0) return;
      sendAction(room.roomCode, "PriestReveal", { ownCardIds: priestSelectedCardIds });
      setPriestSelectedCardIds([]);
    };

    // ---- Bullet ----
    const myVillageForBullet = gameState.villages[myPlayerId ?? ""];
    const myHasBullet = !!myVillageForBullet?.cards.some((c: any) => c.type === "Bullet");
    const canUseBullet = isMyTurn && myHasBullet && !gameState.bulletTargetCardId;

    const handleClickBulletCard = () => {
      if (!canUseBullet) return;
      setBulletSelecting((prev) => !prev);
      setBulletTargetSelection(null);
    };

    const handleSelectBulletTarget = (cardId: string) => {
      if (!canUseBullet || !bulletSelecting) return;
      setBulletTargetSelection((prev) => (prev === cardId ? null : cardId));
    };

    const handleConfirmBulletShoot = () => {
      if (!room || !bulletTargetSelection) return;
      sendAction(room.roomCode, "BulletShoot", { targetCardId: bulletTargetSelection });
      setBulletSelecting(false);
      setBulletTargetSelection(null);
    };

    // ---- Hunter (پایان بازی) ----
    const isMyHunterDecisionPending =
      gameState.phase === "AwaitingFinalHunterDecisions" &&
      !!gameState.pendingHunterDecisionPlayerIds?.includes(myPlayerId ?? "");

    const handleConfirmHunterRemove = () => {
      if (!room || !hunterSelectedCardId) return;
      sendAction(room.roomCode, "HunterRemoveCard", { cardId: hunterSelectedCardId });
    };

    const handleSkipHunterRemoval = () => {
      if (!room) return;
      sendAction(room.roomCode, "HunterSkipRemoval", {});
    };

    const isAnyKnownAbilityPendingForMe =
      isGremlinAbilityPending || isTroublemakerAbilityPending || isTheCountAbilityPending ||
      isMarksmanAbilityPending || isCowAbilityPending || isInstigatorAbilityPending ||
      isInsomniaAbilityPending || isThingAbilityPending;
    const isUnknownAbilityPendingForMe =
      !!gameState.pendingAbilityPlayerId &&
      gameState.pendingAbilityPlayerId === myPlayerId &&
      !isAnyKnownAbilityPendingForMe;

    // ---- Gremlin: دابل‌کلیک روی یک دهکده یعنی جریمه بده ----
    const handleGremlinDoubleClickVillage = (targetPlayerId: string) => {
      if (!room || !isGremlinAbilityPending) return;
      sendAction(room.roomCode, "GremlinPenalize", { targetPlayerId });
    };

    // ---- Troublemaker: انتخاب یک کارت از یک دهکده، بعد یک کارت از دهکده‌ی دیگه ----
    const handleTroublemakerSelectCard = (playerId: string, cardId: string) => {
      if (!isTroublemakerAbilityPending) return;

      if (!troublemakerFirst) {
        setTroublemakerFirst({ playerId, cardId });
        return;
      }

      if (playerId === troublemakerFirst.playerId) {
        // انتخاب مجدد کارت اول (از همون دهکده)
        setTroublemakerFirst({ playerId, cardId });
        setTroublemakerSecond(null);
        return;
      }

      setTroublemakerSecond({ playerId, cardId });
    };

    const handleConfirmTroublemakerSwap = () => {
      if (!room || !troublemakerFirst || !troublemakerSecond) return;
      sendAction(room.roomCode, "TroublemakerSwap", {
        firstPlayerId: troublemakerFirst.playerId,
        firstCardId: troublemakerFirst.cardId,
        secondPlayerId: troublemakerSecond.playerId,
        secondCardId: troublemakerSecond.cardId,
      });
    };

    // ---- TheCount ----
    const handleTheCountBurnTen = () => {
      if (!room || !isTheCountAbilityPending) return;
      sendAction(room.roomCode, "TheCountBurnTen", {});
    };

    // ---- Marksman ----
    const handleMarksmanSelectCard = (card: any) => {
      if (!isMarksmanAbilityPending) return;
      if (!card.isPubliclyRevealed || !card.type) return;
      if (!MARKSMAN_BORROWABLE_TYPES.includes(card.type as CardType)) return;
      setMarksmanTargetCardId((prev) => (prev === card.cardId ? null : card.cardId));
    };

    const handleConfirmMarksmanActivate = () => {
      if (!room || !marksmanTargetCardId) return;
      sendAction(room.roomCode, "MarksmanActivate", { targetCardId: marksmanTargetCardId });
    };
        // ---- Cow ----
    const handleCowFlipDeck = () => {
      if (!room || !isCowAbilityPending) return;
      sendAction(room.roomCode, "CowFlipDeck", {});
    };

    // ---- Instigator ----
    const handleInstigatorSelectCard = (cardId: string) => {
      if (!isInstigatorAbilityPending) return;
      setInstigatorTargetCardId((prev) => (prev === cardId ? null : cardId));
    };

    const handleConfirmInstigatorFlip = () => {
      if (!room || !instigatorTargetCardId) return;
      sendAction(room.roomCode, "InstigatorFlip", { targetCardId: instigatorTargetCardId });
    };

    // ---- Insomnia ----
    const handleInsomniaViewAll = () => {
      if (!room || !isInsomniaAbilityPending || insomniaRequestPending) return;
      const myVillageNow = gameState.villages[myPlayerId ?? ""];
      const hiddenIds = (myVillageNow?.cards ?? [])
        .filter((c: any) => !c.isPubliclyRevealed)
        .map((c: any) => c.cardId);

      if (hiddenIds.length === 0) {
        // کارت پشت‌ورویی وجود نداره؛ فقط رد کن
        sendAction(room.roomCode, "SkipAbility", {});
        return;
      }

      setInsomniaTargetIds(hiddenIds);
      setInsomniaRequestPending(true);
      sendAction(room.roomCode, "InsomniaViewAll", {});
    };

    // ---- Thing ----
    const handleThingSelectVillage = (targetPlayerId: string) => {
      if (!isThingAbilityPending) return;
      setThingTargetPlayerId((prev) => (prev === targetPlayerId ? null : targetPlayerId));
    };

    const handleConfirmThingShuffle = () => {
      if (!room || !thingTargetPlayerId) return;
      sendAction(room.roomCode, "ThingShuffleVillage", { targetPlayerId: thingTargetPlayerId });
    };

    const handleDrawFromDeck = () => {
      if (!room || !isMyTurn || myPendingDrawnCard) return;
      sendAction(room.roomCode, "DrawFromDeck", {});
    };

    const handleDiscardPileClick = () => {
      if (!room) return;

      if (isMyTurn && !myPendingDrawnCard) {
        sendAction(room.roomCode, "TakeFromDiscard", {});
        return;
      }

      if (myPendingDrawnCard && !drawnCardInVillage) {
        handleDiscardDrawnDirectly();
        return;
      }

      if (myPendingDrawnCard && drawnCardInVillage) {
        const drawnId = myPendingDrawnCard.cardId;
        const selectedWithoutDrawn = selectedCardIds.filter((id) => id !== drawnId);

        if (selectedCardIds.length === 0 || (selectedCardIds.length === 1 && selectedCardIds[0] === drawnId)) {
          sendAction(room.roomCode, "DiscardDrawn", { drawnCardId: drawnId });
        } else if (selectedWithoutDrawn.length > 0) {
          sendAction(room.roomCode, "SwapDrawn", {
            drawnCardId: drawnId,
            ownCardIds: selectedWithoutDrawn,
          });
        }
        setSelectedCardIds([]);
        setDrawnCardInVillage(false);
      }
    };

    const handleAddDrawnCardToVillage = () => {
      if (myPendingDrawnCard && !drawnCardInVillage) {
        setDrawnCardInVillage(true);
      }
    };

    const handleDiscardDrawnDirectly = () => {
      if (myPendingDrawnCard && !drawnCardInVillage && room) {
        sendAction(room.roomCode, "DiscardDrawn", { drawnCardId: myPendingDrawnCard.cardId });
      }
    };

    const handleStartNextRound = () => {
      if (!room) return;
      sendAction(room.roomCode, "StartNextRound", {});
    };

    const turnOrder =
      gameState.playerIdsInTurnOrder?.length
        ? gameState.playerIdsInTurnOrder
        : Object.keys(gameState.villages);

    const myIndex = turnOrder.indexOf(myPlayerId ?? "");
    const startIndex = myIndex === -1 ? 0 : myIndex;

    const rotatedIds = [...turnOrder.slice(startIndex), ...turnOrder.slice(0, startIndex)];
    const orderedVillages = rotatedIds.map((id) => gameState.villages[id]).filter(Boolean);

    const POSITION_LAYOUTS: Record<number, string[]> = {
      1: ["bottom"],
      2: ["bottom", "top"],
      3: ["bottom", "right", "left"],
      4: ["bottom", "right", "top", "left"],
    };
    const positions = POSITION_LAYOUTS[orderedVillages.length] ?? orderedVillages.map(() => "bottom");
    const POSITION_GRID_CLASSES: Record<string, string> = {
      bottom: "col-start-2 row-start-3",
      top: "col-start-2 row-start-1",
      left: "col-start-1 row-start-2",
      right: "col-start-3 row-start-2",
    };

    return (
      <main className="min-h-screen p-6 space-y-4">
        <InitialPeekTimer endsAt={gameState.initialPeekDeadlineUtc} />

        {(gameState.phase === "RoundScoring" || gameState.phase === "GameFinished") && (
          <RoundSummaryOverlay
            gameState={gameState}
            showDetails={showRoundSummaryDetails}
            onToggleDetails={() => setShowRoundSummaryDetails((v) => !v)}
            onStartNextRound={handleStartNextRound}
            getPlayerName={displayName}
          />
        )}

        {myPendingDrawnCard && !drawnCardInVillage && (
          <DrawnCardDecisionModal
            card={myPendingDrawnCard}
            onAddToVillage={handleAddDrawnCardToVillage}
            onDiscard={handleDiscardDrawnDirectly}
          />
        )}

        <div className="flex items-center justify-between">
          <h1 className="font-display text-2xl text-silver">راند {gameState.roundNumber} از ۴</h1>
          <span className="font-mono text-sm text-ember">{gameState.phase}</span>
        </div>

        <div className="bg-panel rounded-lg p-4 space-y-1">
          {gameState.isFinalRoundDeclared && (
            <div className="text-sm text-ember">
              دور آخر توسط {displayName(gameState.finalRoundDeclarerPlayerId)} اعلام شده — با رسیدن نوبت به او، این دور تمام می‌شود.
            </div>
          )}

          {canDeclareFinalRound && (
            <button
              className="rounded-md bg-panel-light px-3 py-1.5 text-xs font-medium hover:bg-panel transition mt-1"
              onClick={handleDeclareFinalRound}
            >
              دور آخر
            </button>
          )}

          <div className="text-sm">
            نوبت: <span className="text-ember">{displayName(gameState.currentPlayerId)}</span>
          </div>

          {isGremlinAbilityPending && (
            <div className="text-xs text-ember">
              کارت Gremlin رو کردی: برای جریمه دادن، روی دهکده‌ی یک بازیکن دابل‌کلیک کن، یا پایان نوبت رو بزن.
            </div>
          )}
          {isTroublemakerAbilityPending && (
            <div className="text-xs text-ember">
              یک کارت از یک دهکده و یک کارت از دهکده‌ی دیگه انتخاب کن تا جابه‌جا بشن.
            </div>
          )}
          {isMarksmanAbilityPending && (
            <div className="text-xs text-ember">
              می‌تونی یک کارت روِ ۹ تا ۱۲ از هر دهکده‌ای انتخاب کنی تا از قابلیتش استفاده کنی.
            </div>
          )}
        </div>

        <div className="grid grid-cols-3 grid-rows-3 gap-4 items-center justify-items-center min-h-[520px]">
          <div className="col-start-2 row-start-2 flex gap-6 items-center justify-center relative">
                        <DrawPileStack
              count={gameState.drawPileCount}
              cards={gameState.drawPile}
              onClick={handleDrawFromDeck}
              disabled={!isMyTurn || !!myPendingDrawnCard}
            />

            <div className="flex flex-col items-center gap-1">
              <div
                onClick={handleDiscardPileClick}
                className={`transition ${
                  (isMyTurn && !myPendingDrawnCard) || myPendingDrawnCard
                    ? "cursor-pointer hover:brightness-110"
                    : "opacity-50 cursor-not-allowed"
                }`}
              >
                {gameState.discardPileTop ? (
                  <PeekableCard
                    card={gameState.discardPileTop}
                    canPeek={false}
                    peekWindowOpen={false}
                    onPeek={() => {}}
                    peekedValue={null}
                    size="opponent"
                  />
                ) : (
                  <div className="w-20 h-28 sm:w-24 sm:h-32 rounded-md border border-dashed border-silver/20 flex items-center justify-center text-silver/30 text-xs">
                    خالی
                  </div>
                )}
              </div>
              <span className="text-[10px] text-silver/40">
                {myPendingDrawnCard ? "برای سوزوندن بزن" : `دورریختنی (${gameState.discardPileCount})`}
              </span>
            </div>
          </div>

          {orderedVillages.map((village, index) => {
            const isMe = village.playerId === myPlayerId;
            const isCurrentTurn = village.playerId === gameState.currentPlayerId;
            const position = positions[index] ?? "bottom";

            return (
              <div
                key={village.playerId}
                                onDoubleClick={
                  isGremlinAbilityPending ? () => handleGremlinDoubleClickVillage(village.playerId) : undefined
                }
                onClick={
                  isThingAbilityPending ? () => handleThingSelectVillage(village.playerId) : undefined
                }
                style={{
                  borderColor: isCurrentTurn ? "#8B2E3A" : isMe ? "#C9D3DE" : "rgba(201,211,222,0.1)",
                  borderWidth: isCurrentTurn ? "4px" : isMe ? "2px" : "1px",
                  borderStyle: "solid",
                  boxShadow: isCurrentTurn ? "0 0 20px rgba(139,46,58,0.6)" : "none",
                }}
                className={`bg-panel-light rounded-lg transition-all ${POSITION_GRID_CLASSES[position]} ${
                  isMe ? "p-6 w-full max-w-2xl" : "p-3 w-full max-w-xs"
                                } ${isGremlinAbilityPending || isThingAbilityPending ? "cursor-pointer hover:brightness-110" : ""} ${
                  thingTargetPlayerId === village.playerId ? "ring-4 ring-ember" : ""
                }`}
              >
                <div className={`font-display mb-1 flex items-center gap-2 ${isMe ? "text-lg" : "text-sm"}`}>
                  {isMe ? "روستای تو" : village.playerId}
                  {isCurrentTurn && (
                    <span className="text-[10px] font-body text-blood-moon animate-pulse">● نوبتشه</span>
                  )}
                  {(() => {
                    const playerInRoom = room?.players.find((p: any) => p.playerId === village.playerId);
                    if (playerInRoom && !playerInRoom.isConnected) {
                      return (
                        <span className="text-[10px] font-body text-silver/50">
                          ⚠ قطعه {isCurrentTurn ? "(بعد از ۶۰ ثانیه رد می‌شه)" : ""}
                        </span>
                      );
                    }
                    return null;
                  })()}
                  <span className="mr-auto">
                    {" — امتیاز کل: "}
                    <span className="font-mono text-ember">{gameState.cumulativeScores[village.playerId]}</span>
                  </span>
                </div>

                {canPeek && isMe && (
                  <div className="text-xs text-ember mb-2">
                    می‌تونی {gameState.myInitialPeeksRemaining} کارت دیگه رو مخفیانه ببینی (دابل‌کلیک)
                  </div>
                )}

                {myPendingDrawnCard && drawnCardInVillage && isMe && (
                  <div className="text-xs text-ember mb-2">
                    حالا کارت(های)ی که می‌خوای بسوزونی رو انتخاب کن (باید هم‌عدد باشن)، بعد روی دورریختنی کلیک کن.
                  </div>
                )}

                <div className={`flex flex-nowrap justify-center ${isMe ? "gap-3" : "gap-1"}`}>
                  {village.cards.map((card: any) => {
                    const isSelected = selectedCardIds.includes(card.cardId);
                    const canSelectForSwap = isMe && !!myPendingDrawnCard && drawnCardInVillage;

                    const isTroublemakerSelected =
                      troublemakerFirst?.cardId === card.cardId || troublemakerSecond?.cardId === card.cardId;

                    const isMarksmanEligible =
                      isMarksmanAbilityPending && card.isPubliclyRevealed && card.type &&
                      MARKSMAN_BORROWABLE_TYPES.includes(card.type as CardType);
                    const isMarksmanSelected = marksmanTargetCardId === card.cardId;

                                        const isInstigatorSelected = instigatorTargetCardId === card.cardId;
                    const isBulletCardItself = isMe && card.type === "Bullet";
                    const isBulletTargetSelectable =
                      isMe && canUseBullet && bulletSelecting && !isBulletCardItself;
                    const isBulletTargetSelected = bulletTargetSelection === card.cardId;
                    const isPriestSelectable =
                      isMe && canUsePriest && !card.isPubliclyRevealed &&
                      priestSelectedCardIds.length < revealedPriestCount;
                    const isPriestSelected = priestSelectedCardIds.includes(card.cardId);
                    const isHunterSelectable = isMyHunterDecisionPending && isMe;
                    const isHunterSelected = hunterSelectedCardId === card.cardId;

                    const canClick =
                      canSelectForSwap || isTroublemakerAbilityPending || isMarksmanEligible ||
                      isInstigatorAbilityPending || isBulletTargetSelectable || isPriestSelectable ||
                      isHunterSelectable;
                                        const handleClick = () => {
                      if (canSelectForSwap) {
                        toggleCardSelection(card.cardId);
                      } else if (isTroublemakerAbilityPending) {
                        handleTroublemakerSelectCard(village.playerId, card.cardId);
                      } else if (isMarksmanEligible) {
                        handleMarksmanSelectCard(card);
                      } else if (isInstigatorAbilityPending) {
                        handleInstigatorSelectCard(card.cardId);
                      } else if (isBulletCardItself) {
                        handleClickBulletCard();
                      } else if (isBulletTargetSelectable) {
                        handleSelectBulletTarget(card.cardId);
                      } else if (isPriestSelectable) {
                        handleTogglePriestCard(card.cardId);
                      } else if (isHunterSelectable) {
                        setHunterSelectedCardId((prev) => (prev === card.cardId ? null : card.cardId));
                      }
                    };

                    return (
                      <div key={card.cardId}>
                        <div
                          onClick={canClick ? handleClick : undefined}
                                                   className={`transition-transform ${canClick ? "cursor-pointer" : ""} ${
                            isSelected || isTroublemakerSelected || isMarksmanSelected || isInstigatorSelected ||
                            isBulletTargetSelected || isPriestSelected || isHunterSelected ||
                            (isBulletCardItself && bulletSelecting)
                              ? "-translate-y-3 ring-4 ring-ember rounded-md"
                              : ""
                          }`}
                        >
                                                    <PeekableCard
                            card={card}
                            canPeek={isMe && canPeek}
                            peekWindowOpen={peekWindowOpen || (isMe && Object.keys(insomniaViewedCards).length > 0)}
                            onPeek={() => handlePeek(card.cardId)}
                            peekedValue={
                              isMe
                                ? visiblePeeks[card.cardId] ?? insomniaViewedCards[card.cardId] ?? null
                                : null
                            }
                            size={isMe ? "own" : "opponent"}
                          />
                        </div>
                      </div>
                    );
                  })}

                  {isMe && myPendingDrawnCard && drawnCardInVillage && (
                    <div
                      onClick={() => toggleCardSelection(myPendingDrawnCard.cardId)}
                      className={`cursor-pointer transition-transform ${
                        selectedCardIds.includes(myPendingDrawnCard.cardId)
                          ? "-translate-y-3 ring-4 ring-ember rounded-md"
                          : ""
                      }`}
                    >
                      <PeekableCard
                        card={myPendingDrawnCard}
                        canPeek={false}
                        peekWindowOpen={false}
                        onPeek={() => {}}
                        peekedValue={null}
                        size="own"
                        forceReveal={gameState.drawnCardSource === "Discard"}
                      />
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>

        {/* ---- Gremlin ---- */}
        {isGremlinAbilityPending && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت (جریمه نده)
            </button>
          </div>
        )}

        {/* ---- Troublemaker ---- */}
        {isTroublemakerAbilityPending && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
              disabled={!troublemakerFirst || !troublemakerSecond}
              onClick={handleConfirmTroublemakerSwap}
            >
              جابه‌جایی کارت‌ها
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}

        {/* ---- TheCount ---- */}
        {isTheCountAbilityPending && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium hover:brightness-110 transition"
              onClick={handleTheCountBurnTen}
            >
              سوزوندن ۱۰ کارت
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}

        {/* ---- Marksman ---- */}
        {isMarksmanAbilityPending && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
              disabled={!marksmanTargetCardId}
              onClick={handleConfirmMarksmanActivate}
            >
              استفاده از قابلیت
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}
                {/* ---- Cow ---- */}
        {isCowAbilityPending && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium hover:brightness-110 transition"
              onClick={handleCowFlipDeck}
            >
              برگرداندن دسته
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}

        {/* ---- Instigator ---- */}
        {isInstigatorAbilityPending && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
              disabled={!instigatorTargetCardId}
              onClick={handleConfirmInstigatorFlip}
            >
              برگردوندن کارت
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}

               {/* ---- Insomnia ---- */}
        {isInsomniaAbilityPending && Object.keys(insomniaViewedCards).length === 0 && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
              disabled={insomniaRequestPending}
              onClick={handleInsomniaViewAll}
            >
              {insomniaRequestPending ? "در حال دریافت..." : "دیدن تمام کارت‌ها"}
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}

        {/* ---- Thing ---- */}
        {isThingAbilityPending && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
              disabled={!thingTargetPlayerId}
              onClick={handleConfirmThingShuffle}
            >
              برهم زدن دهکده
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}
                {/* ---- Priest ---- */}
        {canUsePriest && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
              disabled={priestSelectedCardIds.length === 0}
              onClick={handleConfirmPriestReveal}
            >
              رو کردن کارت{priestSelectedCardIds.length > 1 ? "‌ها" : ""} ({priestSelectedCardIds.length}/{revealedPriestCount})
            </button>
          </div>
        )}

        {/* ---- Bullet ---- */}
        {canUseBullet && bulletSelecting && (
          <div className="flex justify-center gap-2">
            <button
              className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
              disabled={!bulletTargetSelection}
              onClick={handleConfirmBulletShoot}
            >
              شلیک
            </button>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={() => {
                setBulletSelecting(false);
                setBulletTargetSelection(null);
              }}
            >
              انصراف
            </button>
          </div>
        )}

        

        {/* fallback برای هر ability ناشناخته‌ای که هنوز پیاده نشده */}
        {isUnknownAbilityPendingForMe && (
          <div className="flex justify-center gap-2">
            <span className="text-sm text-silver/60 self-center">
              قابلیت {gameState.pendingAbilityCardType} در انتظار است (هنوز پیاده‌سازی نشده)
            </span>
            <button
              className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
              onClick={handleSkipAbility}
            >
              پایان نوبت
            </button>
          </div>
        )}

        {lastActionError && <div className="text-blood-moon text-sm">{lastActionError}</div>}
                {gameState.phase === "AwaitingFinalHunterDecisions" && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-6">
            <div className="bg-panel rounded-lg p-6 max-w-md w-full space-y-4 text-center">
              <h2 className="font-display text-xl text-silver">پایان بازی — تصمیم Hunter</h2>
              {isMyHunterDecisionPending ? (
                <>
                  <p className="text-sm text-silver/70">
                    Hunter تو رو‌شده؛ می‌تونی یکی از کارت‌هات رو قبل از محاسبه‌ی امتیاز نهایی حذف کنی.
                  </p>
                  <div className="flex justify-center gap-2">
                                        <button
                      className="rounded-md bg-blood-moon px-4 py-2 text-sm font-medium disabled:opacity-50 hover:brightness-110 transition"
                      disabled={!hunterSelectedCardId}
                      onClick={handleConfirmHunterRemove}
                    >
                      حذف از شمارش
                    </button>
                    <button
                      className="rounded-md bg-panel-light px-4 py-2 text-sm font-medium hover:bg-panel transition"
                      onClick={handleSkipHunterRemoval}
                    >
                      رد کردن
                    </button>
                  </div>
                </>
              ) : (
                <p className="text-sm text-silver/60">
                  منتظر تصمیم بقیه‌ی بازیکنانی که Hunter رو‌شده دارن...
                </p>
              )}
            </div>
          </div>
        )}
      </main>
    );
  }

  if (room) {
    return (
      <main className="min-h-screen p-8 flex items-center justify-center">
        <div className="max-w-xl w-full space-y-6">
          <div className="flex items-center gap-2">
            <span className={`w-3 h-3 rounded-full ${statusColor}`} />
            <span className="text-sm text-silver/60">وضعیت اتصال: {status}</span>
          </div>

          <div className="p-4 rounded-lg bg-panel">
            <div className="text-sm text-silver/60">کد اتاق</div>
            <div className="font-display text-3xl tracking-widest text-ember">{room.roomCode}</div>
          </div>

          <div className="space-y-2">
            <div className="text-sm text-silver/60">بازیکنان ({room.players.length}/4)</div>
            {room.players.map((p: any) => (
              <div key={p.playerId} className="flex items-center gap-2 p-2 rounded bg-panel-light">
                <span className={`w-2 h-2 rounded-full ${p.isConnected ? "bg-emerald-500" : "bg-silver/20"}`} />
                <span>{p.name}</span>
                {p.isHost && <span className="text-xs text-ember">(میزبان)</span>}
              </div>
            ))}
          </div>

          {room.players.filter((p: any) => p.isConnected).length >= 2 && (
            <button
              className="w-full rounded-md bg-blood-moon px-4 py-3 font-display text-lg hover:brightness-110 transition"
              onClick={() => startGame(room.roomCode)}
            >
              شروع بازی
            </button>
          )}
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen p-8 flex items-center justify-center">
      <div className="max-w-sm w-full space-y-6">
        <h1 className="font-display text-4xl text-center text-silver">Silver — طلسم</h1>

        <div className="flex items-center gap-2">
          <span className={`w-3 h-3 rounded-full ${statusColor}`} />
          <span className="text-sm text-silver/60">وضعیت اتصال: {status}</span>
        </div>

        <input
          className="w-full rounded-md bg-panel px-3 py-2 text-sm border border-silver/10 focus:outline-none focus:ring-2 focus:ring-silver"
          value={playerName}
          onChange={(e) => setPlayerName(e.target.value)}
          placeholder="نام تو"
        />

        <button
          className="w-full rounded-md bg-panel-light px-4 py-2 text-sm font-medium disabled:opacity-50 hover:bg-panel transition"
          disabled={status !== "connected" || !playerName}
          onClick={() => createRoom(playerName)}
        >
          ساخت اتاق جدید
        </button>

        <div className="flex items-center gap-2 text-silver/40 text-xs">
          <div className="flex-1 h-px bg-silver/10" />
          یا
          <div className="flex-1 h-px bg-silver/10" />
        </div>

        <input
          className="w-full rounded-md bg-panel px-3 py-2 text-sm uppercase tracking-widest border border-silver/10 focus:outline-none focus:ring-2 focus:ring-silver"
          value={roomCodeInput}
          onChange={(e) => setRoomCodeInput(e.target.value)}
          placeholder="کد اتاق"
          maxLength={5}
        />
        <button
          className="w-full rounded-md bg-panel-light px-4 py-2 text-sm font-medium disabled:opacity-50 hover:bg-panel transition"
          disabled={status !== "connected" || !playerName || !roomCodeInput}
          onClick={() => joinRoom(roomCodeInput, playerName)}
        >
          پیوستن به اتاق
        </button>

        {joinError && <div className="text-sm text-blood-moon">{joinError}</div>}
      </div>
    </main>
  );
}