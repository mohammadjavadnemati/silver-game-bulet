namespace Silver.Engine;

using Silver.Engine.Cards;

public class SilverGameEngine
{
    private readonly Random _random;

    public SilverGameEngine(Random? random = null)
    {
        _random = random ?? new Random();
    }

    // ------------------ شروع بازی و راند ------------------

    public SilverGameState StartGame(string gameId, List<string> playerIdsInTurnOrder)
    {
        if (playerIdsInTurnOrder.Count < 2 || playerIdsInTurnOrder.Count > 4)
            throw new InvalidOperationException("بازی سیلور بین ۲ تا ۴ بازیکن پشتیبانی می‌شود.");

        var state = new SilverGameState
        {
            GameId = gameId,
            PlayerIdsInTurnOrder = new List<string>(playerIdsInTurnOrder),
        };

        foreach (var playerId in playerIdsInTurnOrder)
        {
            state.Villages[playerId] = new SilverPlayerVillage { PlayerId = playerId };
            state.CumulativeScores[playerId] = 0;
        }

        StartNewRound(state, firstRound: true);
        return state;
    }

    private void StartNewRound(SilverGameState state, bool firstRound)
    {
        var deck = CardDefinitions.BuildFullDeck();
        Shuffle(deck);

        state.DrawPile.Clear();
        state.DrawPile.AddRange(deck);

        state.DiscardPile.Clear();
        state.InitialPeeksUsedByPlayer.Clear();
        foreach (var playerId in state.PlayerIdsInTurnOrder)
            state.InitialPeeksUsedByPlayer[playerId] = 0;

        // قبل از پخش کارت‌های جدید، کارت آمیولت رو (اگه دست کسی بود) از همه‌ی دهکده‌ها جدا می‌کنیم
        foreach (var v in state.Villages.Values)
        {
            v.Cards.RemoveAll(c => c.CardId == state.BulletCard.CardId);
        }

        foreach (var playerId in state.PlayerIdsInTurnOrder)
        {
            var village = state.Villages[playerId];
            for (int i = 0; i < 5; i++)
                village.Cards.Add(DrawTopOfDeck(state));
        }

        // برنده‌ی دورِ قبل (فقط اگه یک برنده‌ی یکتا داشته) کارت آمیولت رو می‌گیره
        if (state.LastRoundScores.Count > 0)
        {
            var minScore = state.LastRoundScores.Values.Min();
            var winners = state.LastRoundScores.Where(kv => kv.Value == minScore).Select(kv => kv.Key).ToList();

            if (winners.Count == 1)
            {
                state.Villages[winners[0]].Cards.Add(state.BulletCard);
            }
        }

        var firstDiscard = DrawTopOfDeck(state);
        firstDiscard.IsPubliclyRevealed = true;
        state.DiscardPile.Add(firstDiscard);

        if (firstRound)
        {
            var starterIndex = _random.Next(state.PlayerIdsInTurnOrder.Count);
            state.CurrentPlayerId = state.PlayerIdsInTurnOrder[starterIndex];
            state.AmuletHolderPlayerId = state.CurrentPlayerId;
        }
        else
        {
            state.CurrentPlayerId = state.AmuletHolderPlayerId ?? state.PlayerIdsInTurnOrder[0];
        }

        state.HasBeenCalled = false;
        state.CallerPlayerId = null;
        state.PendingDrawnCard = null;
        state.DrawnCardSource = PendingDrawnCardSource.None;
        state.SideActionUsedThisTurn = false;
        state.IsFinalRoundDeclared = false;
        state.FinalRoundDeclarerPlayerId = null;
        state.BulletTargetCardId = null;
        state.PendingHunterDecisionPlayerIds = null;
        ClearPendingAbility(state);

        state.Phase = GamePhase.RoundInProgress;

        state.InitialPeekDeadlineUtc =
            DateTime.UtcNow.AddSeconds(SilverGameState.InitialPeekDurationSeconds);

        state.UpdatedAt = DateTime.UtcNow;
    }

    private static SilverCard DrawTopOfDeck(SilverGameState state)
    {
        if (state.DrawPile.Count == 0)
            throw new InvalidOperationException("دسته‌ی اصلی خالی است.");

        var card = state.DrawPile[^1];
        state.DrawPile.RemoveAt(state.DrawPile.Count - 1);
        return card;
    }

    private void Shuffle(List<SilverCard> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
    // ------------------ GothGirl: مسیر سوزوندن ------------------

    private void BurnCard(SilverGameState state, string ownerPlayerId, SilverCard card)
    {
        var village = state.Villages[ownerPlayerId];
        bool hasRevealedGothGirl = village.Cards.Any(c => c.Type == CardType.GothGirl && c.IsPubliclyRevealed);

        card.IsPubliclyRevealed = true;

        if (hasRevealedGothGirl)
            state.DrawPile.Insert(0, card); // پایین دسته‌ی اصلی
        else
            state.DiscardPile.Add(card);
    }

    // ------------------ اعتبارسنجی عمومی ------------------

    private SilverActionResult? ValidateCommonTurnPreconditions(SilverGameState state, SilverAction action, bool requiresCurrentPlayerTurn = true)
    {
        if (state.Phase != GamePhase.RoundInProgress && state.Phase != GamePhase.FinalTurnsAfterCall)
            return SilverActionResult.Fail("در حال حاضر راندی در جریان نیست.");

        if (requiresCurrentPlayerTurn && action.PlayerId != state.CurrentPlayerId)
            return SilverActionResult.Fail("الان نوبت این بازیکن نیست.");

        if (!state.Villages.ContainsKey(action.PlayerId))
            return SilverActionResult.Fail("این بازیکن در بازی نیست.");

        return null;
    }

    // ------------------ مسیریابی اکشن‌ها ------------------

    public SilverActionResult ApplyAction(SilverGameState state, SilverAction action)
    {
        if (action is StartNextRoundAction startNextRound)
            return HandleStartNextRound(state, startNextRound);
        if (action is InitialCardPeekAction initialPeek)
            return HandleInitialCardPeek(state, initialPeek);
        if (action is HunterRemoveCardAction hunterRemove)
            return HandleHunterRemoveCard(state, hunterRemove);
        if (action is HunterSkipRemovalAction hunterSkip)
            return HandleHunterSkipRemoval(state, hunterSkip);
        if (action is BulletShootAction bulletShoot)
            return HandleBulletShoot(state, bulletShoot);

        var precheck = ValidateCommonTurnPreconditions(state, action);
        if (precheck != null) return precheck;

        // اگر منتظر resolve شدن یه قابلیت کارتی هستیم، فقط Skip قابل قبوله
        // (کارت‌های جدید که تعریف بشن، case های مخصوص خودشون اینجا اضافه می‌شن)
        if (state.PendingAbilityPlayerId != null)
        {
            return action switch
            {
                GremlinPenalizeAction a => HandleGremlinPenalize(state, a),
                TroublemakerSwapAction a => HandleTroublemakerSwap(state, a),
                TheCountBurnTenAction a => HandleTheCountBurnTen(state, a),
                MarksmanActivateAction a => HandleMarksmanActivate(state, a),
                CowFlipDeckAction a => HandleCowFlipDeck(state, a),
                InstigatorFlipCardAction a => HandleInstigatorFlip(state, a),
                InsomniaViewAllAction a => HandleInsomniaViewAll(state, a),
                ThingShuffleVillageAction a => HandleThingShuffle(state, a),
                SkipCardAbilityAction => HandleSkipAbility(state, action),
                _ => SilverActionResult.Fail("یک قابلیت کارت در انتظار تصمیم توست؛ اول اون رو انجام بده یا Skip کن.")
            };
        }

        return action switch
        {
            DeclareFinalRoundAction => HandleDeclareFinalRound(state, action),
            DrawFromDeckAction => HandleDrawFromDeck(state, action),
            TakeFromDiscardAction => HandleTakeFromDiscard(state, action),
            CallAction => HandleCall(state, action),
            DiscardDrawnCardAction a => HandleDiscardDrawn(state, a),
            SwapDrawnCardWithOwnAction a => HandleSwapDrawn(state, a),
            SwapDiscardCardWithOwnAction a => HandleSwapDiscard(state, a),
            PriestRevealAction a => HandlePriestReveal(state, a),
            _ => SilverActionResult.Fail("این اکشن هنوز پشتیبانی نمی‌شود.")
        };
    }

    // ------------------ کشیدن از دسته اصلی ------------------

    private SilverActionResult HandleDrawFromDeck(SilverGameState state, SilverAction action)
    {
        if (state.DrawPile.Count == 0)
        {
            EndRound(state, RoundEndReason.CardsExhausted);
            return SilverActionResult.Ok(state);
        }

        var card = DrawTopOfDeck(state);
        state.PendingDrawnCard = card;
        state.DrawnCardSource = PendingDrawnCardSource.Deck;

        var privateInfo = new Dictionary<string, CardType> { [card.CardId] = card.Type };
        return SilverActionResult.Ok(state, privateInfo);
    }

    private SilverActionResult HandleTakeFromDiscard(SilverGameState state, SilverAction action)
    {
        if (state.DiscardPile.Count == 0)
            return SilverActionResult.Fail("دسته‌ی دورریختنی خالی است.");

        var topCard = state.DiscardPile[^1];
        state.DiscardPile.RemoveAt(state.DiscardPile.Count - 1);
        state.PendingDrawnCard = topCard;
        state.DrawnCardSource = PendingDrawnCardSource.Discard;

        return SilverActionResult.Ok(state); // از قبل عمومیه، نیازی به privateInfo نیست
    }

    // ------------------ تصمیم بعد از Draw/TakeFromDiscard ------------------

    private static readonly HashSet<CardType> DiscardTriggeredAbilityTypes = new()
    {
        CardType.Marksman, CardType.TheCount, CardType.Troublemaker, CardType.Gremlin,
        CardType.Cow, CardType.Instigator, CardType.Insomnia, CardType.Thing
    };

    private SilverActionResult HandleDiscardDrawn(SilverGameState state, DiscardDrawnCardAction action)
    {
        if (state.PendingDrawnCard == null || state.PendingDrawnCard.CardId != action.DrawnCardId)
            return SilverActionResult.Fail("کارت کشیده‌شده‌ی معتبری برای دور انداختن پیدا نشد.");

        var pending = state.PendingDrawnCard;
        var source = state.DrawnCardSource;

        BurnCard(state, action.PlayerId, pending);
        state.PendingDrawnCard = null;
        state.DrawnCardSource = PendingDrawnCardSource.None;

        // فقط کارتی که مستقیم از دسته‌ی اصلی کشیده و بلافاصله سوزونده شده، ability فعال می‌کنه
        bool abilityEligible = source == PendingDrawnCardSource.Deck || IsMorticianActive(state, action.PlayerId);

        if (abilityEligible && DiscardTriggeredAbilityTypes.Contains(pending.Type))
        {
            state.PendingAbilityPlayerId = action.PlayerId;
            state.PendingAbilityCardType = pending.Type;
            state.PendingAbilityCardId = pending.CardId;
            return SilverActionResult.Ok(state);
        }

        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    private SilverActionResult HandleSwapDrawn(SilverGameState state, SwapDrawnCardWithOwnAction action)
    {
        if (state.PendingDrawnCard == null || state.PendingDrawnCard.CardId != action.DrawnCardId)
            return SilverActionResult.Fail("کارت کشیده‌شده‌ی معتبری برای تعویض پیدا نشد.");

        var village = state.Villages[action.PlayerId];
        var drawnCard = state.PendingDrawnCard;
        var source = state.DrawnCardSource;
        var swapResult = TrySwapMultiple(village, action.OwnCardIdsToReplace, drawnCard, state, source);

        if (!swapResult.Success)
        {
            village.Cards.Add(drawnCard);

            state.PendingDrawnCard = null;
            state.DrawnCardSource = PendingDrawnCardSource.None;

            AdvanceTurn(state);
            return SilverActionResult.Fail(swapResult.ErrorMessage!, state);
        }

        state.PendingDrawnCard = null;
        state.DrawnCardSource = PendingDrawnCardSource.None;

        if (state.PendingAbilityPlayerId != null)
            return SilverActionResult.Ok(state);

        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    private SilverActionResult HandleSwapDiscard(SilverGameState state, SwapDiscardCardWithOwnAction action)
    {
        if (state.PendingDrawnCard == null || state.PendingDrawnCard.CardId != action.DiscardCardId)
            return SilverActionResult.Fail("کارت دورریختنیِ معتبری برای تعویض پیدا نشد.");

        var village = state.Villages[action.PlayerId];
        var pendingCard = state.PendingDrawnCard;
        var source = state.DrawnCardSource;
        var swapResult = TrySwapMultiple(village, action.OwnCardIdsToReplace, pendingCard, state, source);

        if (!swapResult.Success)
        {
            state.DiscardPile.Add(pendingCard);
            return swapResult;
        }

        state.PendingDrawnCard = null;
        state.DrawnCardSource = PendingDrawnCardSource.None;

        if (state.PendingAbilityPlayerId != null)
            return SilverActionResult.Ok(state);

        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    private SilverActionResult TrySwapMultiple(
    SilverPlayerVillage village,
    List<string> ownCardIdsToReplace,
    SilverCard newCard,
    SilverGameState state,
    PendingDrawnCardSource newCardSource)
    {
        if (ownCardIdsToReplace.Count == 0)
            return SilverActionResult.Fail("حداقل باید یک کارت برای تعویض انتخاب شود.");

        var selectedCards = new List<SilverCard>();
        foreach (var cardId in ownCardIdsToReplace)
        {
            var found = village.Cards.FirstOrDefault(c => c.CardId == cardId);
            if (found == null)
                return SilverActionResult.Fail($"کارت {cardId} در روستای این بازیکن پیدا نشد.");
            if (found.CardId == state.BulletCard.CardId)
                return SilverActionResult.Fail("کارت Bullet را نمی‌توان سوزاند یا جابه‌جا کرد.");
            selectedCards.Add(found);
        }

        bool morticianActive = IsMorticianActive(state, village.PlayerId);

        // مسیر ۱ (قدیمی): فقط کارت‌های خودت باید با کمک لایکن با هم برابر باشن؛
        // کارت تازه‌گرفته‌شده مستقل از ارزشش وارد دهکده می‌شه
        if (IsValidDiscardGroup(selectedCards))
        {
            foreach (var card in selectedCards)
            {
                village.Cards.Remove(card);
                BurnCard(state, village.PlayerId, card);
            }

            village.Cards.Add(newCard);

            if (morticianActive)
            {
                var triggerCard = selectedCards.FirstOrDefault(c => DiscardTriggeredAbilityTypes.Contains(c.Type));
                if (triggerCard != null)
                {
                    state.PendingAbilityPlayerId = village.PlayerId;
                    state.PendingAbilityCardType = triggerCard.Type;
                    state.PendingAbilityCardId = triggerCard.CardId;
                }
            }

            return SilverActionResult.Ok(state);
        }

        // مسیر ۲ (جدید): کارت تازه‌گرفته‌شده هم جزو گروه محسوب می‌شه؛ اگه کل گروه
        // (کارت تازه + کارت‌های خودت) با کمک لایکن برابر بشن، همه با هم می‌سوزن
        // و هیچی وارد دهکده نمی‌شه
        var fullGroup = new List<SilverCard>(selectedCards) { newCard };
        if (IsValidDiscardGroup(fullGroup))
        {
            foreach (var card in selectedCards)
            {
                village.Cards.Remove(card);
                BurnCard(state, village.PlayerId, card);
            }
            BurnCard(state, village.PlayerId, newCard);

            bool newCardEligible = newCardSource == PendingDrawnCardSource.Deck || morticianActive;

            SilverCard? triggerCard = null;
            if (newCardEligible && DiscardTriggeredAbilityTypes.Contains(newCard.Type))
                triggerCard = newCard;
            else if (morticianActive)
                triggerCard = selectedCards.FirstOrDefault(c => DiscardTriggeredAbilityTypes.Contains(c.Type));

            if (triggerCard != null)
            {
                state.PendingAbilityPlayerId = village.PlayerId;
                state.PendingAbilityCardType = triggerCard.Type;
                state.PendingAbilityCardId = triggerCard.CardId;
            }

            return SilverActionResult.Ok(state);
        }

        // هیچ‌کدوم از دو مسیر معتبر نبود
        if (selectedCards.Count >= 3 && state.DrawPile.Count > 0)
        {
            var penaltyCard = state.DrawPile[^1];
            state.DrawPile.RemoveAt(state.DrawPile.Count - 1);
            village.Cards.Add(penaltyCard);
        }
        return SilverActionResult.Fail("کارت‌های انتخاب‌شده هم‌عدد نبودند؛ تعویض لغو شد.");
    }
    // ------------------ Lycan: بررسی معتبر بودن گروه برای دور انداختن دسته‌جمعی ------------------

    private bool IsValidDiscardGroup(List<SilverCard> selected)
    {
        var lycanBoosters = selected.Where(c => c.Type == CardType.Lycan && c.IsPubliclyRevealed).ToList();
        var others = selected.Except(lycanBoosters).ToList();

        if (others.Count == 0)
        {
            // فقط کارت‌های لایکن انتخاب شدن؛ چون همه یه ارزش دارن، حالت عادیه
            return selected.Select(c => c.Value).Distinct().Count() <= 1;
        }

        var target = others.Max(c => c.Value);
        var totalDeficit = others.Sum(c => target - c.Value);

        return totalDeficit == lycanBoosters.Count;
    }

    // ------------------ Skip قابلیت در انتظار ------------------

    private SilverActionResult HandleSkipAbility(SilverGameState state, SilverAction action)
    {
        if (state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("قابلیتی برای این بازیکن در انتظار نیست.");

        ClearPendingAbility(state);
        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    private void ClearPendingAbility(SilverGameState state)
    {
        state.PendingAbilityPlayerId = null;
        state.PendingAbilityCardType = null;
        state.PendingAbilityCardId = null;
        state.AbilityStepUsedThisResolution = false;
    }
    private bool IsMorticianActive(SilverGameState state, string playerId)
    {
        if (!state.Villages.TryGetValue(playerId, out var village)) return false;
        return village.Cards.Any(c => c.Type == CardType.Mortician && c.IsPubliclyRevealed);
    }
    // ------------------ Gremlin ------------------

    private SilverActionResult HandleGremlinPenalize(SilverGameState state, GremlinPenalizeAction action)
    {
        if (state.PendingAbilityCardType != CardType.Gremlin || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت Gremlin نیستیم.");

        if (!state.Villages.TryGetValue(action.TargetPlayerId, out var targetVillage))
            return SilverActionResult.Fail("بازیکن هدف پیدا نشد.");

        var cardInDiscard = state.DiscardPile.FirstOrDefault(c => c.CardId == state.PendingAbilityCardId);
        if (cardInDiscard == null)
            return SilverActionResult.Fail("کارت مورد نظر در دسته‌ی سوخته‌ها پیدا نشد.");

        state.DiscardPile.Remove(cardInDiscard);
        targetVillage.Cards.Add(cardInDiscard); // از قبل IsPubliclyRevealed = true هست

        ClearPendingAbility(state);
        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    // ------------------ Troublemaker ------------------

    private SilverActionResult HandleTroublemakerSwap(SilverGameState state, TroublemakerSwapAction action)
    {
        if (state.PendingAbilityCardType != CardType.Troublemaker || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت Troublemaker نیستیم.");

        if (action.FirstPlayerId == action.SecondPlayerId)
            return SilverActionResult.Fail("دو کارت باید از دو دهکده‌ی متفاوت باشند.");

        if (!state.Villages.TryGetValue(action.FirstPlayerId, out var firstVillage))
            return SilverActionResult.Fail("بازیکن اول پیدا نشد.");
        if (!state.Villages.TryGetValue(action.SecondPlayerId, out var secondVillage))
            return SilverActionResult.Fail("بازیکن دوم پیدا نشد.");

        var firstCard = firstVillage.Cards.FirstOrDefault(c => c.CardId == action.FirstCardId);
        if (firstCard == null)
            return SilverActionResult.Fail("کارت اول پیدا نشد.");
        var secondCard = secondVillage.Cards.FirstOrDefault(c => c.CardId == action.SecondCardId);
        if (secondCard == null)
            return SilverActionResult.Fail("کارت دوم پیدا نشد.");

        firstVillage.Cards.Remove(firstCard);
        secondVillage.Cards.Remove(secondCard);
        firstVillage.Cards.Add(secondCard);
        secondVillage.Cards.Add(firstCard);

        ClearPendingAbility(state);
        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    // ------------------ TheCount ------------------

    private SilverActionResult HandleTheCountBurnTen(SilverGameState state, TheCountBurnTenAction action)
    {
        if (state.PendingAbilityCardType != CardType.TheCount || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت TheCount نیستیم.");

        var burnCount = Math.Min(10, state.DrawPile.Count);
        for (int i = 0; i < burnCount; i++)
        {
            var card = state.DrawPile[^1];
            state.DrawPile.RemoveAt(state.DrawPile.Count - 1);
            card.IsPubliclyRevealed = true;
            state.DiscardPile.Add(card);
        }

        ClearPendingAbility(state);

        if (state.DrawPile.Count == 0)
        {
            EndRound(state, RoundEndReason.CardsExhausted);
            return SilverActionResult.Ok(state);
        }

        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    // ------------------ Marksman ------------------

    private static readonly HashSet<CardType> MarksmanBorrowableTypes = new()
    {
        CardType.Marksman, CardType.TheCount, CardType.Troublemaker, CardType.Gremlin,
        CardType.Cow, CardType.Instigator, CardType.Insomnia, CardType.Thing
    };

    private SilverActionResult HandleMarksmanActivate(SilverGameState state, MarksmanActivateAction action)
    {
        if (state.PendingAbilityCardType != CardType.Marksman || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت Marksman نیستیم.");

        SilverCard? targetCard = null;
        foreach (var village in state.Villages.Values)
        {
            targetCard = village.Cards.FirstOrDefault(c => c.CardId == action.TargetCardId);
            if (targetCard != null) break;
        }

        if (targetCard == null)
            return SilverActionResult.Fail("کارت هدف پیدا نشد.");
        if (!targetCard.IsPubliclyRevealed)
            return SilverActionResult.Fail("این کارت باید رو باشد.");
        if (!MarksmanBorrowableTypes.Contains(targetCard.Type))
            return SilverActionResult.Fail("از قابلیت این کارت نمی‌توان با Marksman استفاده کرد.");

        // فقط نوع قابلیتی که در حال resolve شدنه عوض می‌شه؛ PendingAbilityCardId
        // (اشاره به کارتی که روی دسته‌ی سوخته‌هاست و در حالت Gremlin قابل ریدایرکته) دست‌نخورده می‌مونه
        state.PendingAbilityCardType = targetCard.Type;

        return SilverActionResult.Ok(state);
    }
    // ------------------ Cow ------------------

    private SilverActionResult HandleCowFlipDeck(SilverGameState state, CowFlipDeckAction action)
    {
        if (state.PendingAbilityCardType != CardType.Cow || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت Cow نیستیم.");

        state.DrawPile.Reverse();
        foreach (var card in state.DrawPile)
            card.IsPubliclyRevealed = !card.IsPubliclyRevealed;

        ClearPendingAbility(state);
        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    // ------------------ Instigator ------------------

    private SilverActionResult HandleInstigatorFlip(SilverGameState state, InstigatorFlipCardAction action)
    {
        if (state.PendingAbilityCardType != CardType.Instigator || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت Instigator نیستیم.");

        SilverCard? targetCard = null;
        foreach (var village in state.Villages.Values)
        {
            targetCard = village.Cards.FirstOrDefault(c => c.CardId == action.TargetCardId);
            if (targetCard != null) break;
        }

        if (targetCard == null)
            return SilverActionResult.Fail("کارت هدف پیدا نشد.");

        targetCard.IsPubliclyRevealed = !targetCard.IsPubliclyRevealed;

        ClearPendingAbility(state);
        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    // ------------------ Insomnia ------------------

    private SilverActionResult HandleInsomniaViewAll(SilverGameState state, InsomniaViewAllAction action)
    {
        if (state.PendingAbilityCardType != CardType.Insomnia || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت Insomnia نیستیم.");

        var village = state.Villages[action.PlayerId];
        var privateInfo = village.Cards
            .Where(c => !c.IsPubliclyRevealed)
            .ToDictionary(c => c.CardId, c => c.Type);

        // عمداً نه ClearPendingAbility صدا زده می‌شه نه AdvanceTurn؛
        // فرانت بعد از ۵ ثانیه نمایش، خودش SkipAbility می‌فرسته تا نوبت رد بشه
        return SilverActionResult.Ok(state, privateInfo);
    }
    // ------------------ Thing ------------------

    private SilverActionResult HandleThingShuffle(SilverGameState state, ThingShuffleVillageAction action)
    {
        if (state.PendingAbilityCardType != CardType.Thing || state.PendingAbilityPlayerId != action.PlayerId)
            return SilverActionResult.Fail("در حال حاضر منتظر قابلیت Thing نیستیم.");

        if (!state.Villages.TryGetValue(action.TargetPlayerId, out var targetVillage))
            return SilverActionResult.Fail("بازیکن هدف پیدا نشد.");

        var hiddenIndices = new List<int>();
        for (int i = 0; i < targetVillage.Cards.Count; i++)
        {
            if (!targetVillage.Cards[i].IsPubliclyRevealed)
                hiddenIndices.Add(i);
        }

        var hiddenCards = hiddenIndices.Select(i => targetVillage.Cards[i]).ToList();
        Shuffle(hiddenCards);

        for (int i = 0; i < hiddenIndices.Count; i++)
            targetVillage.Cards[hiddenIndices[i]] = hiddenCards[i];

        ClearPendingAbility(state);
        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }
    // ------------------ Priest ------------------

    private SilverActionResult HandlePriestReveal(SilverGameState state, PriestRevealAction action)
    {
        if (state.SideActionUsedThisTurn)
            return SilverActionResult.Fail("در این نوبت قبلاً از یک اکشن جانبی استفاده کرده‌ای.");

        var village = state.Villages[action.PlayerId];
        var revealedPriestCount = village.Cards.Count(c => c.IsPubliclyRevealed && c.Type == CardType.Priest);

        if (revealedPriestCount == 0)
            return SilverActionResult.Fail("Priest رو‌شده‌ای در روستای تو پیدا نشد.");

        if (action.OwnCardIdsToReveal.Count == 0)
            return SilverActionResult.Fail("حداقل باید یک کارت انتخاب کنی.");

        if (action.OwnCardIdsToReveal.Count > revealedPriestCount)
            return SilverActionResult.Fail($"با {revealedPriestCount} کارت Priest رو‌شده، حداکثر {revealedPriestCount} کارت می‌تونی رو کنی.");

        if (action.OwnCardIdsToReveal.Distinct().Count() != action.OwnCardIdsToReveal.Count)
            return SilverActionResult.Fail("کارت‌های انتخاب‌شده باید متفاوت باشند.");

        var targets = new List<SilverCard>();
        foreach (var cardId in action.OwnCardIdsToReveal)
        {
            var card = village.Cards.FirstOrDefault(c => c.CardId == cardId);
            if (card == null)
                return SilverActionResult.Fail("یکی از کارت‌های هدف در روستای تو پیدا نشد.");
            if (card.IsPubliclyRevealed)
                return SilverActionResult.Fail("این کارت از قبل رو شده است.");
            targets.Add(card);
        }

        foreach (var card in targets)
            card.IsPubliclyRevealed = true;

        state.SideActionUsedThisTurn = true;
        return SilverActionResult.Ok(state);
    }

    // ------------------ Bullet ------------------

    private SilverActionResult HandleBulletShoot(SilverGameState state, BulletShootAction action)
    {
        var precheck = ValidateCommonTurnPreconditions(state, action);
        if (precheck != null) return precheck;

        if (state.BulletTargetCardId != null)
            return SilverActionResult.Fail("Bullet قبلاً برای این راند استفاده شده است.");

        var village = state.Villages[action.PlayerId];
        bool hasBullet = village.Cards.Any(c => c.CardId == state.BulletCard.CardId);
        if (!hasBullet)
            return SilverActionResult.Fail("کارت Bullet در روستای تو نیست.");

        if (action.TargetCardId == state.BulletCard.CardId)
            return SilverActionResult.Fail("Bullet نمی‌تواند خودش را هدف بگیرد.");

        var target = village.Cards.FirstOrDefault(c => c.CardId == action.TargetCardId);
        if (target == null)
            return SilverActionResult.Fail("کارت هدف در روستای تو پیدا نشد.");

        state.BulletTargetCardId = target.CardId;

        // این یک اکشن جانبیِ آزاده؛ نه نوبت رو تموم می‌کنه، نه به SideActionUsedThisTurn وابسته‌ست.
        return SilverActionResult.Ok(state);
    }

    // ------------------ Hunter: تصمیم پایان بازی ------------------

    private SilverActionResult HandleHunterRemoveCard(SilverGameState state, HunterRemoveCardAction action)
    {
        if (state.Phase != GamePhase.AwaitingFinalHunterDecisions || state.PendingHunterDecisionPlayerIds == null)
            return SilverActionResult.Fail("در حال حاضر منتظر تصمیم Hunter نیستیم.");

        if (!state.PendingHunterDecisionPlayerIds.Contains(action.PlayerId))
            return SilverActionResult.Fail("نوبت تصمیم‌گیری Hunter برای این بازیکن نیست.");

        var village = state.Villages[action.PlayerId];
        var card = village.Cards.FirstOrDefault(c => c.CardId == action.CardId);
        if (card == null)
            return SilverActionResult.Fail("کارت هدف در روستای تو پیدا نشد.");
        if (card.CardId == state.BulletCard.CardId)
            return SilverActionResult.Fail("کارت Bullet را نمی‌توان حذف کرد.");

        village.Cards.Remove(card);

        state.PendingHunterDecisionPlayerIds.Remove(action.PlayerId);
        return FinalizeHunterDecisionsIfDone(state);
    }

    private SilverActionResult HandleHunterSkipRemoval(SilverGameState state, HunterSkipRemovalAction action)
    {
        if (state.Phase != GamePhase.AwaitingFinalHunterDecisions || state.PendingHunterDecisionPlayerIds == null)
            return SilverActionResult.Fail("در حال حاضر منتظر تصمیم Hunter نیستیم.");

        if (!state.PendingHunterDecisionPlayerIds.Contains(action.PlayerId))
            return SilverActionResult.Fail("نوبت تصمیم‌گیری Hunter برای این بازیکن نیست.");

        state.PendingHunterDecisionPlayerIds.Remove(action.PlayerId);
        return FinalizeHunterDecisionsIfDone(state);
    }

    private SilverActionResult FinalizeHunterDecisionsIfDone(SilverGameState state)
    {
        if (state.PendingHunterDecisionPlayerIds!.Count > 0)
            return SilverActionResult.Ok(state);

        state.PendingHunterDecisionPlayerIds = null;
        state.Phase = GamePhase.RoundScoring;
        ScoreRound(state);
        state.Phase = GamePhase.GameFinished;
        state.WinnerPlayerId = state.CumulativeScores.OrderBy(kv => kv.Value).First().Key;

        return SilverActionResult.Ok(state);
    }

    // ------------------ Call ------------------

    private SilverActionResult HandleCall(SilverGameState state, SilverAction action)
    {
        var village = state.Villages[action.PlayerId];
        if (village.Cards.Count > 4)
            return SilverActionResult.Fail("برای Call باید ۴ کارت یا کمتر داشته باشی.");

        state.HasBeenCalled = true;
        state.CallerPlayerId = action.PlayerId;
        state.Phase = GamePhase.FinalTurnsAfterCall;

        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    private SilverActionResult HandleInitialCardPeek(SilverGameState state, InitialCardPeekAction action)
    {
        if (state.Phase != GamePhase.RoundInProgress)
            return SilverActionResult.Fail("در حال حاضر امکان دیدن کارت‌های اولیه وجود ندارد.");

        if (!state.Villages.TryGetValue(action.PlayerId, out var village))
            return SilverActionResult.Fail("این بازیکن در بازی نیست.");

        var usedSoFar = state.InitialPeeksUsedByPlayer.GetValueOrDefault(action.PlayerId, 0);

        if (usedSoFar >= SilverGameState.MaxInitialPeeksPerRound)
            return SilverActionResult.Fail("سهمیه‌ی دیدن کارت‌های اولیه‌ت تموم شده.");

        var card = village.Cards.FirstOrDefault(c => c.CardId == action.OwnCardId);

        if (card == null)
            return SilverActionResult.Fail("کارت هدف در روستای تو پیدا نشد.");

        if (card.IsPubliclyRevealed)
            return SilverActionResult.Fail("این کارت از قبل رو شده است.");

        state.InitialPeeksUsedByPlayer[action.PlayerId] = usedSoFar + 1;

        var privateInfo = new Dictionary<string, CardType> { [card.CardId] = card.Type };

        state.UpdatedAt = DateTime.UtcNow;

        return SilverActionResult.Ok(state, privateInfo);
    }

    // ------------------ گردش نوبت ------------------

    private void AdvanceTurn(SilverGameState state)
    {
        var currentIndex = state.PlayerIdsInTurnOrder.IndexOf(state.CurrentPlayerId);
        var nextIndex = (currentIndex + 1) % state.PlayerIdsInTurnOrder.Count;
        var nextPlayerId = state.PlayerIdsInTurnOrder[nextIndex];

        if (state.Phase == GamePhase.FinalTurnsAfterCall && nextPlayerId == state.CallerPlayerId)
        {
            EndRound(state, RoundEndReason.Call);
            return;
        }

        if (state.IsFinalRoundDeclared && nextPlayerId == state.FinalRoundDeclarerPlayerId)
        {
            EndRound(state, RoundEndReason.FinalRoundDeclared);
            return;
        }

        state.CurrentPlayerId = nextPlayerId;
        state.SideActionUsedThisTurn = false;
        state.UpdatedAt = DateTime.UtcNow;
    }

    // ------------------ پایان راند و امتیازدهی ------------------

    internal void EndRound(SilverGameState state, RoundEndReason reason)
    {
        state.RoundEndReason = reason;

        bool isFinalRound = state.RoundNumber >= SilverGameState.TotalRounds;

        if (isFinalRound)
        {
            var eligibleForHunterDecision = state.Villages
                .Where(kv => kv.Value.Cards.Any(c => c.Type == CardType.Hunter && c.IsPubliclyRevealed))
                .Select(kv => kv.Key)
                .ToList();

            if (eligibleForHunterDecision.Count > 0)
            {
                state.Phase = GamePhase.AwaitingFinalHunterDecisions;
                state.PendingHunterDecisionPlayerIds = eligibleForHunterDecision;
                return;
            }

            state.Phase = GamePhase.RoundScoring;
            ScoreRound(state);
            state.Phase = GamePhase.GameFinished;
            state.WinnerPlayerId = state.CumulativeScores.OrderBy(kv => kv.Value).First().Key;
            return;
        }

        state.Phase = GamePhase.RoundScoring;
        ScoreRound(state);
    }

    internal void ScoreRound(SilverGameState state)
    {
        var rawScores = state.Villages.ToDictionary(kv => kv.Key, kv => kv.Value.TotalScore(state.BulletTargetCardId));

        if (state.HasBeenCalled && state.CallerPlayerId != null)
        {
            var minScore = rawScores.Values.Min();
            var callerScore = rawScores[state.CallerPlayerId];

            if (callerScore == minScore)
            {
                rawScores[state.CallerPlayerId] = 0;
                state.AmuletHolderPlayerId = state.CallerPlayerId;
            }
            else
            {
                rawScores[state.CallerPlayerId] += 10;
            }
        }

        state.LastRoundScores = new Dictionary<string, int>(rawScores);

        foreach (var (playerId, score) in rawScores)
        {
            state.CumulativeScores[playerId] += score;
        }
    }

    private SilverActionResult HandleDeclareFinalRound(SilverGameState state, SilverAction action)
    {
        if (state.HasBeenCalled || state.IsFinalRoundDeclared)
            return SilverActionResult.Fail("دور آخر قبلاً اعلام شده است.");

        if (state.PendingDrawnCard != null)
            return SilverActionResult.Fail("دیگه دیر شده؛ باید قبل از کشیدن کارت، دور آخر رو اعلام می‌کردی.");

        state.IsFinalRoundDeclared = true;
        state.FinalRoundDeclarerPlayerId = action.PlayerId;

        AdvanceTurn(state);
        return SilverActionResult.Ok(state);
    }

    private SilverActionResult HandleStartNextRound(SilverGameState state, StartNextRoundAction action)
    {
        if (state.Phase != GamePhase.RoundScoring)
            return SilverActionResult.Fail("در حال حاضر امکان شروع دور جدید نیست.");

        if (!state.Villages.ContainsKey(action.PlayerId))
            return SilverActionResult.Fail("این بازیکن در بازی نیست.");

        state.RoundNumber++;
        StartNewRound(state, firstRound: false);

        return SilverActionResult.Ok(state);
    }

    // فقط برای تست
    internal void SetPendingDrawnCardForTest(SilverGameState state, SilverCard card)
    {
        state.PendingDrawnCard = card;
    }
}