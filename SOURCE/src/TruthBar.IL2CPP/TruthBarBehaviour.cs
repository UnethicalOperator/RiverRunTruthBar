using System.Globalization;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace TruthBar;

public sealed class TruthBarBehaviour : MonoBehaviour
{
    private static readonly string[] MenuPages = { "Main", "Players", "Self", "Rank", "Intel" };
    private readonly List<PlayerStats> _players = new();
    private readonly List<PlayerView> _playerViews = new();
    private readonly List<int> _cardsOnTable = new();
    private Rect _menuRect = new(Screen.width - 410f, 20f, 390f, 620f);
    private Rect _playerCardsRect = new(10f, 300f, 1050f, 250f);
    private Rect _tableCardsRect = new(10f, 200f, 340f, 80f);
    private GUI.WindowFunction? _menuWindow;
    private GUI.WindowFunction? _playerCardsWindow;
    private GUI.WindowFunction? _tableCardsWindow;
    private string _bulletInput = "0";
    private string _status = "Ready. F1 opens the menu.";
    private string _diceRoundSummary = string.Empty;
    private float _nextRefresh;
    private float _nextErrorLog;
    private bool _menuOpen = true;
    private bool _showPlayerOverlay = true;
    private bool _showTableOverlay = true;
    private bool _revealHandsAndDice = true;
    private bool _runtimeUpdateMarked;
    private bool _runtimeGuiMarked;
    private bool _headCaptured;
    private int _menuPage;
    private int _pendingRank = -1;
    private int _selectedPlayerId = -1;
    private int _roundCard = int.MinValue;
    private int _reportedCardsOnTable;
    private int _headArenaId = -1;
    private float _originalYaw;
    private float _originalPitch;
    private float _originalMinYaw;
    private float _originalMaxYaw;
    private float _originalMinPitch;
    private float _originalMaxPitch;

    public TruthBarBehaviour(IntPtr pointer)
        : base(pointer)
    {
    }

    public void Awake()
    {
        _menuWindow = (Action<int>)DrawMenuWindow;
        _playerCardsWindow = (Action<int>)DrawPlayerCardsWindow;
        _tableCardsWindow = (Action<int>)DrawTableCardsWindow;
        Plugin.PluginLog.LogInfo("TruthBar Unity component is alive.");
        Plugin.WriteRuntimeMarker("ComponentAwake", "Unity created the TruthBar behaviour");
    }

    public void Update()
    {
        try
        {
            if (!_runtimeUpdateMarked)
            {
                _runtimeUpdateMarked = true;
                Plugin.WriteRuntimeMarker("UpdateAlive", "Unity invoked the plugin update loop");
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                _menuOpen = !_menuOpen;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ResetUi(closeMenu: true);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                RefreshGameState(forceStatus: true);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                ResetUi(closeMenu: false);
                RefreshGameState(forceStatus: true);
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.25f;
                RefreshGameState(forceStatus: false);
            }
        }
        catch (Exception ex)
        {
            ReportFailure("Update", ex);
        }
    }

    public void OnDestroy()
    {
        RestoreHeadState(silent: true);
    }

    public void OnGUI()
    {
        try
        {
            if (!_runtimeGuiMarked)
            {
                _runtimeGuiMarked = true;
                Plugin.WriteRuntimeMarker("OnGUIAlive", "Unity invoked the plugin UI loop");
            }

            var hintStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 14
            };
            GUI.Label(new Rect(10f, 10f, 620f, 26f), "<b>RiverRunTruthBar 2.2</b> by PlsDntChase - F1 menu | L refresh | P reset | F2 close", hintStyle);

            if (_showTableOverlay && _cardsOnTable.Count > 0 && _tableCardsWindow is not null)
            {
                _tableCardsRect = GUI.Window(7202, _tableCardsRect, _tableCardsWindow, "Cards on the table");
            }

            if (_showPlayerOverlay && _playerViews.Count > 0 && _playerCardsWindow is not null)
            {
                _playerCardsRect.width = Math.Min(1050f, Math.Max(500f, Screen.width - 20f));
                _playerCardsRect.height = Math.Max(100f, 45f + (_playerViews.Count * 52f));
                _playerCardsRect = GUI.Window(7201, _playerCardsRect, _playerCardsWindow, "Live player hands, dice, health and chambers");
            }

            if (_menuOpen && _menuWindow is not null)
            {
                _menuRect.x = Mathf.Clamp(_menuRect.x, 0f, Math.Max(0f, Screen.width - 80f));
                _menuRect.y = Mathf.Clamp(_menuRect.y, 0f, Math.Max(0f, Screen.height - 40f));
                _menuRect = GUI.Window(7269, _menuRect, _menuWindow, "RiverRunTruthBar - by PlsDntChase");
            }
        }
        catch (Exception ex)
        {
            ReportFailure("OnGUI", ex);
        }
    }

    private void RefreshGameState(bool forceStatus)
    {
        _players.Clear();
        _playerViews.Clear();
        _cardsOnTable.Clear();
        _roundCard = int.MinValue;
        _reportedCardsOnTable = 0;
        _diceRoundSummary = string.Empty;

        GameObject[] taggedPlayers;
        try
        {
            taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        }
        catch (Exception)
        {
            taggedPlayers = Array.Empty<GameObject>();
        }

        foreach (var playerObject in taggedPlayers)
        {
            if (playerObject is null)
            {
                continue;
            }

            var stats = playerObject.GetComponent<PlayerStats>();
            if (stats is null || _players.Any(existing => existing.GetInstanceID() == stats.GetInstanceID()))
            {
                continue;
            }

            _players.Add(stats);
        }

        _players.Sort((left, right) => left.NetworkSlot.CompareTo(right.NetworkSlot));

        foreach (var player in _players)
        {
            var cards = new List<int>();
            var otherCards = new List<string>();
            var diceValues = new List<int>();
            var handSource = string.Empty;
            var diceSource = string.Empty;
            var currentChamber = 0;
            var lethalChamber = 0;
            var gameplay = player.GetComponent<BlorfGamePlay>();
            if (gameplay is not null)
            {
                currentChamber = gameplay.Networkcurrentrevoler;
                lethalChamber = gameplay.Networkrevolverbulllet;
            }

            if (TryReadCardObjects(player.GetComponent<ChaosDeckGameplay>()?.Cards, cards))
            {
                handSource = "Chaos Deck";
            }
            else if (TryReadCardObjects(player.GetComponent<ChaosGamePlay>()?.Cards, cards))
            {
                handSource = "Chaos";
            }
            else if (TryReadCardObjects(player.GetComponent<PokerGamePlay>()?.Cards, cards))
            {
                handSource = "Poker";
            }
            else if (TryReadCardObjects(player.GetComponent<DeckGameplay>()?.Cards, cards))
            {
                handSource = "Deck";
            }
            else if (TryReadCardObjects(gameplay?.Cards, cards))
            {
                handSource = "Liar's Deck";
            }
            else if (TryReadTexasCards(player.GetComponent<TexasGamePlay>(), otherCards))
            {
                handSource = "Texas";
            }

            ReadDiceState(player.GetComponent<DiceGamePlay>(), diceValues, out diceSource);

            var name = string.IsNullOrWhiteSpace(player.NetworkPlayerName)
                ? player.PlayerName
                : player.NetworkPlayerName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Player {player.NetworkSlot + 1}";
            }

            _playerViews.Add(new PlayerView(
                player.GetInstanceID(),
                name,
                player.NetworkSlot,
                player.isLocalPlayer,
                player.NetworkHealth,
                player.NetworkDead,
                player.NetworkHaveTurn,
                cards,
                otherCards,
                diceValues,
                handSource,
                diceSource,
                currentChamber,
                lethalChamber));
        }

        var manager = UnityEngine.Object.FindObjectOfType<BlorfGamePlayManager>();
        if (manager is not null)
        {
            _roundCard = manager.NetworkRoundCard;
            _reportedCardsOnTable = manager.NetworkCardsOnTable;
            var lastRound = manager.LastRound;
            if (lastRound is not null)
            {
                for (var index = 0; index < lastRound.Count; index++)
                {
                    _cardsOnTable.Add(lastRound[index]);
                }
            }
        }

        var diceManager = UnityEngine.Object.FindObjectOfType<DiceGamePlayManager>();
        if (diceManager is not null)
        {
            var bidder = string.IsNullOrWhiteSpace(diceManager.NetworkLastDiceName)
                ? "waiting"
                : diceManager.NetworkLastDiceName;
            _diceRoundSummary = $"Dice bid: {diceManager.NetworkLastCount} x face {diceManager.NetworkLastDice} | Total dice: {diceManager.NetworkTotalCount} | Bidder: {bidder}";
        }

        if (_selectedPlayerId != -1 && _players.All(player => player.GetInstanceID() != _selectedPlayerId))
        {
            _selectedPlayerId = -1;
        }

        if (forceStatus)
        {
            _status = _players.Count == 0
                ? "Refreshed: waiting for a lobby or match."
                : $"Refreshed: {_players.Count.ToString(CultureInfo.InvariantCulture)} player(s) found.";
        }
    }

    private static bool TryReadCardObjects(
        Il2CppSystem.Collections.Generic.List<GameObject>? gameCards,
        ICollection<int> destination)
    {
        if (gameCards is null || gameCards.Count == 0)
        {
            return false;
        }

        var startingCount = destination.Count;
        for (var index = 0; index < gameCards.Count; index++)
        {
            var card = gameCards[index]?.GetComponent<Card>();
            if (card is not null)
            {
                destination.Add(card.cardtype);
            }
        }

        return destination.Count > startingCount;
    }

    private static bool TryReadTexasCards(TexasGamePlay? gameplay, ICollection<string> destination)
    {
        var cards = gameplay?.Cards;
        if (cards is null || cards.Count == 0)
        {
            return false;
        }

        var startingCount = destination.Count;
        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            if (card is not null)
            {
                destination.Add(TruthBarLogic.TexasCardLabel(card.CardNumber, card.CardValue, card.CardType.ToString()));
            }
        }

        return destination.Count > startingCount;
    }

    private static void ReadDiceState(DiceGamePlay? gameplay, ICollection<int> destination, out string source)
    {
        source = string.Empty;
        var values = gameplay?.DiceValues;
        if (values is not null && values.Count > 0)
        {
            for (var index = 0; index < values.Count; index++)
            {
                destination.Add(values[index]);
            }

            source = "values";
            return;
        }

        var renderers = gameplay?.dicerenders;
        if (renderers is null || renderers.Count == 0)
        {
            return;
        }

        for (var index = 0; index < renderers.Count; index++)
        {
            var die = renderers[index];
            if (die is not null)
            {
                destination.Add(die.Face);
            }
        }

        if (destination.Count > 0)
        {
            source = "render face index";
        }
    }

    public void DrawMenuWindow(int windowId)
    {
        try
        {
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };

            DrawPageTabs(buttonStyle);
            switch (_menuPage)
            {
                case 0:
                    DrawMainPage(labelStyle, buttonStyle);
                    break;
                case 1:
                    DrawPlayersPage(labelStyle, buttonStyle);
                    break;
                case 2:
                    DrawSelfPage(labelStyle, buttonStyle);
                    break;
                case 3:
                    DrawRankPage(labelStyle, buttonStyle);
                    break;
                default:
                    DrawIntelPage(labelStyle);
                    break;
            }

            GUI.Label(new Rect(12f, 526f, 366f, 48f), _status, labelStyle);
            if (GUI.Button(new Rect(90f, 577f, 210f, 25f), "Reset overlay positions", buttonStyle))
            {
                ResetUi(closeMenu: false);
            }
        }
        catch (Exception ex)
        {
            ReportFailure("Draw menu", ex);
        }
    }

    private void DrawPageTabs(GUIStyle buttonStyle)
    {
        for (var index = 0; index < MenuPages.Length; index++)
        {
            var title = index == _menuPage ? $"[{MenuPages[index]}]" : MenuPages[index];
            if (GUI.Button(new Rect(8f + (index * 75f), 24f, 72f, 27f), title, buttonStyle))
            {
                _menuPage = index;
            }
        }
    }

    private void DrawMainPage(GUIStyle labelStyle, GUIStyle buttonStyle)
    {
        GUI.Label(new Rect(12f, 58f, 366f, 24f), "Local revolver control", labelStyle);
        _bulletInput = GUI.TextField(new Rect(55f, 86f, 90f, 25f), _bulletInput);
        if (GUI.Button(new Rect(152f, 86f, 180f, 25f), "Move lethal chamber", buttonStyle))
        {
            if (!TruthBarLogic.TryNormalizeBullet(_bulletInput, out var value, out var normalized))
            {
                _bulletInput = normalized;
                _status = "Enter at least one digit (0-999).";
            }
            else
            {
                _bulletInput = normalized;
                ModifyLocalBullet(value);
            }
        }

        if (GUI.Button(new Rect(28f, 120f, 160f, 28f), "Lethal next chamber", buttonStyle))
        {
            SetLocalBulletToNext();
        }
        if (GUI.Button(new Rect(200f, 120f, 160f, 28f), "Lethal on chamber 6", buttonStyle))
        {
            ModifyLocalBullet(5);
        }

        GUI.Label(new Rect(12f, 164f, 366f, 24f), "Overlay visibility", labelStyle);
        var playerText = _showPlayerOverlay ? "Hide player overlay" : "Show player overlay";
        if (GUI.Button(new Rect(28f, 192f, 160f, 28f), playerText, buttonStyle))
        {
            _showPlayerOverlay = !_showPlayerOverlay;
        }
        var tableText = _showTableOverlay ? "Hide table overlay" : "Show table overlay";
        if (GUI.Button(new Rect(200f, 192f, 160f, 28f), tableText, buttonStyle))
        {
            _showTableOverlay = !_showTableOverlay;
        }

        var revealText = _revealHandsAndDice
            ? "Always reveal hands and dice: ON"
            : "Always reveal hands and dice: OFF";
        if (GUI.Button(new Rect(55f, 228f, 280f, 30f), revealText, buttonStyle))
        {
            _revealHandsAndDice = !_revealHandsAndDice;
            _status = _revealHandsAndDice
                ? "Live hand and dice reveal enabled."
                : "Live hand and dice reveal hidden by user choice.";
        }

        GUI.Label(new Rect(20f, 272f, 350f, 74f), GetRoundSummary(), labelStyle);
        GUI.Label(
            new Rect(20f, 356f, 350f, 92f),
            "2.2 keeps every 2.1 feature and continuously reads real standard, Chaos, Poker, Texas and Dice hand objects for the player overlay.",
            labelStyle);
    }

    private void DrawPlayersPage(GUIStyle labelStyle, GUIStyle buttonStyle)
    {
        GUI.Label(new Rect(12f, 58f, 366f, 24f), "Select a player to inspect or edit cards", labelStyle);
        if (_players.Count == 0)
        {
            GUI.Label(new Rect(12f, 86f, 366f, 38f), "Join a lobby or match to populate this page.", labelStyle);
        }

        for (var index = 0; index < _players.Count && index < 8; index++)
        {
            var player = _players[index];
            var id = player.GetInstanceID();
            var view = _playerViews[index];
            var state = view.Dead ? " [DEAD]" : view.HaveTurn ? " [TURN]" : string.Empty;
            var x = 12f + ((index % 2) * 184f);
            var y = 86f + ((index / 2) * 31f);
            if (GUI.Button(new Rect(x, y, 178f, 27f), $"{view.Name}{state}", buttonStyle))
            {
                _selectedPlayerId = id;
            }
        }

        var selectedPlayer = _players.FirstOrDefault(player => player.GetInstanceID() == _selectedPlayerId);
        PlayerView? selectedView = null;
        foreach (var playerView in _playerViews)
        {
            if (playerView.InstanceId == _selectedPlayerId)
            {
                selectedView = playerView;
                break;
            }
        }
        if (selectedPlayer is null || selectedView is null)
        {
            GUI.Label(new Rect(12f, 224f, 366f, 52f), "Select a player above. Health, death state, turn, hands and dice remain visible without sending a player action.", labelStyle);
            return;
        }

        GUI.Label(
            new Rect(12f, 218f, 366f, 52f),
            $"Selected: {selectedView.Name} | Health {selectedView.Health} | {(selectedView.Dead ? "Dead" : "Alive")} | {(selectedView.HaveTurn ? "Their turn" : "Waiting")}",
            labelStyle);
        GUI.Label(
            new Rect(12f, 266f, 366f, 28f),
            $"Live hand: {FormatHand(selectedView)} | Dice: {FormatDice(selectedView)}",
            labelStyle);
        if (GUI.Button(new Rect(20f, 302f, 170f, 28f), "All <color=red>Kings</color>", buttonStyle))
        {
            EditCards(selectedPlayer, 1);
        }
        if (GUI.Button(new Rect(200f, 302f, 170f, 28f), "All <color=purple>Queens</color>", buttonStyle))
        {
            EditCards(selectedPlayer, 2);
        }
        if (GUI.Button(new Rect(20f, 337f, 170f, 28f), "All <color=orange>Aces</color>", buttonStyle))
        {
            EditCards(selectedPlayer, 3);
        }
        if (GUI.Button(new Rect(200f, 337f, 170f, 28f), "All Jokers", buttonStyle))
        {
            EditCards(selectedPlayer, 4);
        }

        GUI.Label(
            new Rect(20f, 385f, 350f, 80f),
            "Remote force-death is intentionally unavailable: this build has no dependable private-host-only gate. Player selection and complete state inspection still work here.",
            labelStyle);
    }

    private void DrawSelfPage(GUIStyle labelStyle, GUIStyle buttonStyle)
    {
        GUI.Label(new Rect(12f, 58f, 366f, 24f), "Your head look controls", labelStyle);
        GUI.Label(new Rect(30f, 85f, 330f, 50f), GetHeadSummary(), labelStyle);
        if (GUI.Button(new Rect(130f, 142f, 130f, 28f), "Look up", buttonStyle))
        {
            AdjustLocalHead(0f, 5f);
        }
        if (GUI.Button(new Rect(28f, 177f, 130f, 28f), "Look left", buttonStyle))
        {
            AdjustLocalHead(-10f, 0f);
        }
        if (GUI.Button(new Rect(232f, 177f, 130f, 28f), "Look right", buttonStyle))
        {
            AdjustLocalHead(10f, 0f);
        }
        if (GUI.Button(new Rect(130f, 212f, 130f, 28f), "Look down", buttonStyle))
        {
            AdjustLocalHead(0f, -5f);
        }
        if (GUI.Button(new Rect(28f, 258f, 160f, 30f), "Enable wide head range", buttonStyle))
        {
            EnableWideHeadRange();
        }
        if (GUI.Button(new Rect(202f, 258f, 160f, 30f), "Reset head safely", buttonStyle))
        {
            RestoreHeadState(silent: false);
        }

        GUI.Label(
            new Rect(24f, 310f, 342f, 92f),
            "These controls bind only to your local ArenaGameplay component. Original angles and limits are captured before the first change and restored on Reset or plugin shutdown.",
            labelStyle);
    }

    private void DrawRankPage(GUIStyle labelStyle, GUIStyle buttonStyle)
    {
        var currentRank = GetCurrentRankIndex();
        if (_pendingRank < 0 && currentRank >= 0)
        {
            _pendingRank = currentRank;
        }

        GUI.Label(new Rect(12f, 58f, 366f, 24f), $"Current selected rank: {GetCurrentRankName()}", labelStyle);
        GUI.Label(
            new Rect(12f, 84f, 366f, 28f),
            $"Pending choice: {TruthBarLogic.RankName(_pendingRank)}",
            labelStyle);

        for (var rank = 0; rank < TruthBarLogic.RankNames.Length; rank++)
        {
            var title = rank == _pendingRank
                ? $"[Selected] {TruthBarLogic.RankNames[rank]}"
                : TruthBarLogic.RankNames[rank];
            if (GUI.Button(new Rect(65f, 118f + (rank * 39f), 260f, 31f), title, buttonStyle))
            {
                _pendingRank = rank;
                _status = $"Pending rank: {TruthBarLogic.RankNames[rank]}. Press Apply to commit it.";
            }
        }

        if (GUI.Button(new Rect(65f, 400f, 260f, 29f), "Choose Truth Teller (reset)", buttonStyle))
        {
            _pendingRank = 0;
            _status = "Pending rank reset: Truth Teller. Press Apply to commit it.";
        }
        if (GUI.Button(new Rect(65f, 437f, 260f, 34f), "Apply selected rank", buttonStyle))
        {
            if (_pendingRank < 0)
            {
                _status = "Choose a rank before pressing Apply.";
            }
            else
            {
                ChangeRank(_pendingRank);
            }
        }

        GUI.Label(
            new Rect(24f, 477f, 342f, 42f),
            "Applies the game's selected profile rank. Server XP and leaderboard progression remain server-authoritative.",
            labelStyle);
    }

    private void DrawIntelPage(GUIStyle labelStyle)
    {
        GUI.Label(new Rect(12f, 58f, 366f, 60f), GetRoundSummary(), labelStyle);
        GUI.Label(new Rect(12f, 124f, 366f, 24f), "Live player state", labelStyle);
        if (_playerViews.Count == 0)
        {
            GUI.Label(new Rect(12f, 152f, 366f, 38f), "Waiting for a lobby or match.", labelStyle);
            return;
        }

        for (var index = 0; index < _playerViews.Count && index < 8; index++)
        {
            var player = _playerViews[index];
            var local = player.IsLocal ? " [YOU]" : string.Empty;
            var state = player.Dead ? "DEAD" : player.HaveTurn ? "TURN" : "WAIT";
            var handCount = (player.Cards.Count + player.OtherCards.Count).ToString(CultureInfo.InvariantCulture);
            GUI.Label(
                new Rect(16f, 152f + (index * 48f), 358f, 46f),
                $"Seat {player.Slot + 1}: {player.Name}{local} | HP {player.Health} | {state}\nHand {handCount} | Dice {FormatDice(player)} | {TruthBarLogic.DeathTimer(player.CurrentChamber, player.LethalChamber)}",
                labelStyle);
        }
    }

    public void DrawPlayerCardsWindow(int windowId)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = 14
        };

        for (var index = 0; index < _playerViews.Count; index++)
        {
            var player = _playerViews[index];
            var local = player.IsLocal ? " <color=#70d6ff>[YOU]</color>" : string.Empty;
            var state = player.Dead ? " <color=red>[DEAD]</color>" : player.HaveTurn ? " <color=yellow>[TURN]</color>" : string.Empty;
            var y = 27f + (index * 52f);
            var header = $"{player.Name}{local}{state} >> HP {player.Health} | {TruthBarLogic.DeathTimer(player.CurrentChamber, player.LethalChamber)}";
            var reveal = _revealHandsAndDice
                ? $"Hand ({(string.IsNullOrWhiteSpace(player.HandSource) ? "current mode" : player.HandSource)}): {FormatHand(player)} | Dice: {FormatDice(player)}"
                : "Hand and dice reveal is OFF.";
            GUI.Label(new Rect(10f, y, _playerCardsRect.width - 20f, 24f), header, style);
            GUI.Label(new Rect(10f, y + 23f, _playerCardsRect.width - 20f, 25f), reveal, style);
        }
    }

    public void DrawTableCardsWindow(int windowId)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = 15
        };
        GUI.Label(
            new Rect(10f, 28f, _tableCardsRect.width - 20f, 35f),
            string.Join(", ", _cardsOnTable.Select(TruthBarLogic.ColoredCardLabel)),
            style);
    }

    private string GetRoundSummary()
    {
        var deckSummary = _roundCard == int.MinValue
            ? string.Empty
            : $"Declared round card: {TruthBarLogic.ColoredCardLabel(_roundCard)}\nCards on table: {_reportedCardsOnTable} | Last revealed group: {_cardsOnTable.Count}";
        if (string.IsNullOrEmpty(deckSummary) && string.IsNullOrEmpty(_diceRoundSummary))
        {
            return "Round intel: waiting for the table manager.";
        }

        return string.IsNullOrEmpty(deckSummary)
            ? _diceRoundSummary
            : string.IsNullOrEmpty(_diceRoundSummary)
                ? deckSummary
                : $"{deckSummary}\n{_diceRoundSummary}";
    }

    [HideFromIl2Cpp]
    private string FormatHand(PlayerView player)
    {
        if (!_revealHandsAndDice)
        {
            return "hidden";
        }

        var values = new List<string>(player.Cards.Count + player.OtherCards.Count);
        foreach (var card in player.Cards)
        {
            values.Add(TruthBarLogic.ColoredCardLabel(card));
        }

        values.AddRange(player.OtherCards);
        return values.Count == 0 ? "not exposed yet" : string.Join(" | ", values);
    }

    [HideFromIl2Cpp]
    private string FormatDice(PlayerView player)
    {
        if (!_revealHandsAndDice)
        {
            return "hidden";
        }

        if (player.DiceValues.Count == 0)
        {
            return "not exposed yet";
        }

        return string.Equals(player.DiceSource, "render face index", StringComparison.Ordinal)
            ? string.Join(" | ", player.DiceValues.Select(value => $"face-index {value}"))
            : string.Join(" | ", player.DiceValues.Select(TruthBarLogic.DiceLabel));
    }

    private string GetHeadSummary()
    {
        var arena = FindLocalArena();
        return arena is null
            ? "Join a lobby or match to find your local head controller."
            : $"Yaw {arena._yaw:0.0} | Pitch {arena._pitch:0.0} | Range X {arena.MinX:0}/{arena.MaxX:0}, Y {arena.MinY:0}/{arena.MaxY:0}";
    }

    private string GetCurrentRankName()
    {
        try
        {
            var database = DatabaseManager.instance;
            return database is null ? "waiting for login" : TruthBarLogic.RankName(database.selectedRank);
        }
        catch (Exception)
        {
            return "unavailable";
        }
    }

    private int GetCurrentRankIndex()
    {
        try
        {
            var database = DatabaseManager.instance;
            return database is null ? -1 : database.selectedRank;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    private void EditCards(PlayerStats player, int cardType)
    {
        try
        {
            var gameplay = player.GetComponent<BlorfGamePlay>();
            var cards = gameplay?.Cards;
            if (cards is null || cards.Count == 0)
            {
                _status = "That player has no editable cards right now.";
                return;
            }

            var edited = 0;
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index]?.GetComponent<Card>();
                if (card is null)
                {
                    continue;
                }

                card.cardtype = cardType;
                card.SetCard();
                edited++;
            }

            _status = $"Updated {edited.ToString(CultureInfo.InvariantCulture)} card(s) for {player.PlayerName}.";
            RefreshGameState(forceStatus: false);
        }
        catch (Exception ex)
        {
            ReportFailure("Edit cards", ex);
        }
    }

    private void SetLocalBulletToNext()
    {
        try
        {
            var localPlayer = _players.FirstOrDefault(player => player.isLocalPlayer);
            var gameplay = localPlayer?.GetComponent<BlorfGamePlay>();
            if (gameplay is null)
            {
                _status = "The local revolver state is not available yet.";
                return;
            }

            ModifyLocalBullet(gameplay.Networkcurrentrevoler);
        }
        catch (Exception ex)
        {
            ReportFailure("Set next chamber", ex);
        }
    }

    private void ModifyLocalBullet(int value)
    {
        try
        {
            var localPlayer = _players.FirstOrDefault(player => player.isLocalPlayer);
            if (localPlayer is null)
            {
                _status = "Local player not found. Join a lobby or match first.";
                return;
            }

            var gameplay = localPlayer.GetComponent<BlorfGamePlay>();
            if (gameplay is null)
            {
                _status = "The local revolver state is not available yet.";
                return;
            }

            gameplay.Networkrevolverbulllet = value;
            _status = $"Local lethal chamber moved to {value.ToString(CultureInfo.InvariantCulture)}.";
            RefreshGameState(forceStatus: false);
        }
        catch (Exception ex)
        {
            ReportFailure("Move bullet", ex);
        }
    }

    private ArenaGameplay? FindLocalArena()
    {
        var localPlayer = _players.FirstOrDefault(player => player.isLocalPlayer);
        if (localPlayer is null)
        {
            return null;
        }

        var direct = localPlayer.GetComponent<ArenaGameplay>();
        if (direct is not null)
        {
            return direct;
        }

        var arenas = UnityEngine.Object.FindObjectsOfType<ArenaGameplay>();
        foreach (var arena in arenas)
        {
            if (arena?.PlayerStats is not null && arena.PlayerStats.GetInstanceID() == localPlayer.GetInstanceID())
            {
                return arena;
            }
        }

        return null;
    }

    private bool CaptureHeadState(ArenaGameplay arena)
    {
        var arenaId = arena.GetInstanceID();
        if (_headCaptured && _headArenaId == arenaId)
        {
            return true;
        }

        if (_headCaptured)
        {
            RestoreHeadState(silent: true);
        }

        _headArenaId = arenaId;
        _originalYaw = arena._yaw;
        _originalPitch = arena._pitch;
        _originalMinYaw = arena.MinY;
        _originalMaxYaw = arena.MaxY;
        _originalMinPitch = arena.MinX;
        _originalMaxPitch = arena.MaxX;
        _headCaptured = true;
        return true;
    }

    private void AdjustLocalHead(float yawDelta, float pitchDelta)
    {
        try
        {
            var arena = FindLocalArena();
            if (arena is null)
            {
                _status = "Local head controller not found. Join a lobby or match first.";
                return;
            }

            CaptureHeadState(arena);
            arena._yaw = ClampWhenValid(arena._yaw + yawDelta, arena.MinY, arena.MaxY);
            arena._pitch = ClampWhenValid(arena._pitch + pitchDelta, arena.MinX, arena.MaxX);
            arena.RotateHead();
            _status = $"Local head moved: yaw {arena._yaw:0.0}, pitch {arena._pitch:0.0}.";
        }
        catch (Exception ex)
        {
            ReportFailure("Move local head", ex);
        }
    }

    private void EnableWideHeadRange()
    {
        try
        {
            var arena = FindLocalArena();
            if (arena is null)
            {
                _status = "Local head controller not found. Join a lobby or match first.";
                return;
            }

            CaptureHeadState(arena);
            arena.MinY = -180f;
            arena.MaxY = 180f;
            arena.MinX = -89f;
            arena.MaxX = 89f;
            _status = "Wide local head range enabled. Reset restores the exact original limits.";
        }
        catch (Exception ex)
        {
            ReportFailure("Enable wide head range", ex);
        }
    }

    private void RestoreHeadState(bool silent)
    {
        if (!_headCaptured)
        {
            if (!silent)
            {
                _status = "Head controls were already at the game defaults.";
            }

            return;
        }

        try
        {
            var arena = FindLocalArena();
            if (arena is not null && arena.GetInstanceID() == _headArenaId)
            {
                arena.MinY = _originalMinYaw;
                arena.MaxY = _originalMaxYaw;
                arena.MinX = _originalMinPitch;
                arena.MaxX = _originalMaxPitch;
                arena._yaw = _originalYaw;
                arena._pitch = _originalPitch;
                arena.RotateHead();
            }

            if (!silent)
            {
                _status = "Local head angles and limits restored.";
            }
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                ReportFailure("Restore local head", ex);
            }
        }
        finally
        {
            _headCaptured = false;
            _headArenaId = -1;
        }
    }

    private static float ClampWhenValid(float value, float minimum, float maximum)
    {
        return minimum < maximum ? Mathf.Clamp(value, minimum, maximum) : value;
    }

    private void ChangeRank(int rank)
    {
        try
        {
            var database = DatabaseManager.instance;
            if (database is null)
            {
                _status = "Rank data is not ready. Wait for login to finish.";
                return;
            }

            if (rank < 0 || rank >= TruthBarLogic.RankNames.Length)
            {
                _status = "That rank is outside the supported range.";
                return;
            }

            database.ChangeSelectedRank(rank);
            database.calculate();
            _pendingRank = rank;
            _status = $"Applied selected rank: {TruthBarLogic.RankNames[rank]}. Server progression may still validate it.";
            Plugin.PluginLog.LogInfo(_status);
        }
        catch (Exception ex)
        {
            ReportFailure("Change rank", ex);
        }
    }

    private void ResetUi(bool closeMenu)
    {
        _selectedPlayerId = -1;
        _menuPage = 0;
        if (closeMenu)
        {
            _menuOpen = false;
        }

        _playerCardsRect = new Rect(10f, 300f, 1050f, 250f);
        _tableCardsRect = new Rect(10f, 200f, 340f, 80f);
        _menuRect = new Rect(Screen.width - 410f, 20f, 390f, 620f);
        _status = closeMenu ? "Menu closed and reset." : "Overlay positions and menu page reset.";
    }

    [HideFromIl2Cpp]
    private void ReportFailure(string operation, Exception ex)
    {
        _status = $"{operation} failed safely: {ex.Message}";
        if (Time.unscaledTime >= _nextErrorLog)
        {
            _nextErrorLog = Time.unscaledTime + 5f;
            Plugin.PluginLog.LogError($"{operation}: {ex}");
            Plugin.WriteRuntimeMarker("SafeFailure", $"{operation}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private sealed record PlayerView(
        int InstanceId,
        string Name,
        int Slot,
        bool IsLocal,
        int Health,
        bool Dead,
        bool HaveTurn,
        List<int> Cards,
        List<string> OtherCards,
        List<int> DiceValues,
        string HandSource,
        string DiceSource,
        int CurrentChamber,
        int LethalChamber);
}
