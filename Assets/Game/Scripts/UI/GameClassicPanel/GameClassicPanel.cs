using System.Collections;
using DG.Tweening;
using Spine.Unity;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameClassicPanel : MonoBehaviour
{
    [Header("Mode/Prefabs")]
    [SerializeField] private int idModePlay;
    [SerializeField] private GameObject tile;

    [Header("Boards")]
    [SerializeField] private GameObject mainboard1;   // Board của Player 1 (để Player 2 chọn)
    [SerializeField] private GameObject mainboard2;   // Board của Player 2 (để Player 1 chọn)

    [Header("Turn Notice (hiện Player(x)'s turn)")]
    [SerializeField] private GameObject notice1;
    [SerializeField] private GameObject notice2;

    [Header("Highlight UI")]
    [SerializeField] private GameObject highlight1;
    [SerializeField] private GameObject highlight2;
    [SerializeField] private GameObject highlightBoard1;
    [SerializeField] private GameObject highlightBoard2;

    [Header("Health UI (Hearts)")]
    [SerializeField] private List<GameObject> healthPlayer1;
    [SerializeField] private List<GameObject> healthPlayer2;

    [Header("Progress UI (Chip count)")]
    [SerializeField] private Slider progress1;
    [SerializeField] private Slider progress2;

    [Header("Optional UI")]
    [SerializeField] private Button homeBtn;
    [SerializeField] private GameObject rollDice;
    [SerializeField] private TMP_Text winRollDice;
    [SerializeField] private SkeletonGraphic coin;
    [SerializeField] private GameObject confetti;

    [Header("Sprites")]
    [SerializeField] private Sprite bombSprite;
    [SerializeField] private Sprite chipSprite;

    [Header("Gameplay Config")]
    [SerializeField] private int boardSize = 3;              // 3x3
    [SerializeField] private int bombsPerBoard = 3;          // số bomb trên MỖI board
    [SerializeField] private bool randomFirstTurn = true;    // random người đi trước
    [SerializeField] private float noticeShowSeconds = 1.0f; // thời gian hiện notice

    [SerializeField] private int totalTile;
    [SerializeField] private List<ChooseItem> player1Tile;
    [SerializeField] private List<ChooseItem> player2Tile;

    private int _currentPlayerTurn = 1; // 1 hoặc 2
    private int _health1;
    private int _health2;
    private int _chip1;
    private int _chip2;
    private bool _gameOver;

    private int ChipsTarget => Mathf.Max(1, totalTile - bombsPerBoard);
    [Header("Manual Bomb Setup (Can be overridden by SelectionTrapPanel)")]
    [SerializeField] private bool useManualBombSetup = true;

    [SerializeField] private int bombCountBoard1 = 3;
    [SerializeField] private List<int> bombIndicesBoard1 = new List<int>();

    [SerializeField] private int bombCountBoard2 = 3;
    [SerializeField] private List<int> bombIndicesBoard2 = new List<int>();
    
    [Header("Bomb FX (GameObject Particle)")]
    [SerializeField] private GameObject bombFxGO;          // GameObject chứa ParticleSystem
    [SerializeField] private ParticleSystem bombFxPS;      // (optional) nếu không kéo, sẽ tự GetComponentInChildren
    [SerializeField] private Camera fxCamera;              // camera để ScreenToWorld (world particle). Nếu null sẽ dùng Camera.main
    [SerializeField] private float fxDepth = 10f;          // độ sâu khi ScreenToWorld (thường = 10 nếu camera ở z=-10 và particle ở z=0)
    
    [Header("BOT (Player 2)")]
    [SerializeField] private bool player2IsBot = true;
    [SerializeField] private float botThinkDelay = 0.6f;
    private void SetBoardUnlock(List<ChooseItem> tiles, bool on)
    {
        if (tiles == null) return;
        foreach (var t in tiles)
            if (t != null) t.unlock = on;
    }

    private void UpdateInputForTurn()
    {
        // mặc định: tắt hết input người chơi
        SetBoardUnlock(player1Tile, false);
        SetBoardUnlock(player2Tile, false);

        if (_gameOver) return;

        // Player1 (human) chỉ được click board2
        if (_currentPlayerTurn == 1)
        {
            SetBoardUnlock(player2Tile, true);
            return;
        }

        // Player2
        if (_currentPlayerTurn == 2)
        {
            if (!player2IsBot)
            {
                // nếu Player2 là người chơi thật thì được click board1
                SetBoardUnlock(player1Tile, true);
            }
            // nếu bot thì giữ false (không cho người bấm trong lượt bot)
        }
    }
    private void StartBotTurnIfNeeded()
    {
        if (!player2IsBot) return;
        if (_gameOver) return;
        if (_currentPlayerTurn != 2) return;

        if (_botCo != null) StopCoroutine(_botCo);
        _botCo = StartCoroutine(CoBotTakeTurn());
    }

    private IEnumerator CoBotTakeTurn()
    {
        yield return new WaitForSeconds(botThinkDelay);

        if (_gameOver) yield break;
        if (_currentPlayerTurn != 2) yield break;

        // chọn ô chưa mở trên board1 (vì bot là Player2 -> mở board đối phương = Player1 board)
        var candidates = player1Tile.Where(t => t != null && !t.IsRevealed).ToList();
        if (candidates.Count == 0) yield break;

        var pick = candidates[Random.Range(0, candidates.Count)];

        _botActing = true;
        OnTileClicked(pick);
        _botActing = false;
    }

    private Coroutine _botCo;
    private bool _botActing;

    private void PlayBombFxAt(ChooseItem tile)
    {
        if (bombFxGO == null || tile == null) return;

        if (bombFxPS == null)
            bombFxPS = bombFxGO.GetComponentInChildren<ParticleSystem>(true);

        // Lấy camera của Canvas chứa tile (nếu canvas ScreenSpaceCamera/WorldSpace)
        var tileCanvas = tile.GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (tileCanvas != null && tileCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = tileCanvas.worldCamera;

        // Vị trí trung tâm tile (nếu UI)
        var tileRect = tile.GetComponent<RectTransform>();
        Vector3 tileWorldCenter = (tileRect != null)
            ? tileRect.TransformPoint(tileRect.rect.center)
            : tile.transform.position;

        // 1) Nếu particle cũng là UI (RectTransform) -> đặt theo world position trực tiếp là ok
        var fxRect = bombFxGO.GetComponent<RectTransform>();
        if (fxRect != null && tileRect != null)
        {
            fxRect.position = tileWorldCenter;
            bombFxGO.SetActive(true);

            if (bombFxPS != null)
            {
                bombFxPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                bombFxPS.Play(true);
            }
            return;
        }

        // 2) Particle là world object: convert tile UI -> screen -> world
        Camera cam = fxCamera != null ? fxCamera : Camera.main;
        if (cam == null)
        {
            // fallback: đặt thẳng theo tile transform (có thể lệch nếu UI)
            bombFxGO.transform.position = tileWorldCenter;
        }
        else
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, tileWorldCenter);
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, fxDepth));
            bombFxGO.transform.position = worldPos;
        }

        bombFxGO.SetActive(true);

        if (bombFxPS != null)
        {
            bombFxPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            bombFxPS.Play(true);
        }
    }

    [Header("Roll Dice Flow")]
    [SerializeField] private float rollDiceMaxWait = 2f; // timeout nếu vì lý do nào đó rollDice không tắt

    private bool _waitingRollDice;
    private Coroutine _rollCo;
    public void BeginGameWithRollDice()
    {
        // reset & setup board nhưng CHƯA cho click
        SetupGame(lockTiles: true);

        // show roll dice
        if (rollDice) rollDice.SetActive(true);
        if (coin) coin.gameObject.SetActive(true);

        // chọn người đi trước (coin result)
        _currentPlayerTurn = randomFirstTurn ? Random.Range(1, 3) : 1;

        // chờ rollDice tự tắt rồi mới start
        if (_rollCo != null) StopCoroutine(_rollCo);
        _rollCo = StartCoroutine(CoWaitRollDiceThenStart());
    }
    private IEnumerator CoWaitRollDiceThenStart()
    {
        _waitingRollDice = true;

        float t = 0f;
        while (rollDice != null && rollDice.activeSelf && t < rollDiceMaxWait)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // đảm bảo không bị kẹt overlay nếu timeout
        if (rollDice != null) rollDice.SetActive(false);

        _waitingRollDice = false;

        // mở khóa tile và bắt đầu game thật
        SetTilesInteractable(true);
        UpdateTurnUI();
    }

    public void OverrideManualBombSetupFromSelection(
        List<int> board1BombIndices,
        List<int> board2BombIndices,
        bool overrideBoard1,
        bool overrideBoard2)
    {
        useManualBombSetup = true;

        if (overrideBoard1 && board1BombIndices != null)
        {
            bombIndicesBoard1 = new List<int>(board1BombIndices);
            bombCountBoard1 = bombIndicesBoard1.Count;
        }

        if (overrideBoard2 && board2BombIndices != null)
        {
            bombIndicesBoard2 = new List<int>(board2BombIndices);
            bombCountBoard2 = bombIndicesBoard2.Count;
        }
    }
    private void SetTilesInteractable(bool on)
    {
        if (player1Tile != null)
            foreach (var t in player1Tile) if (t != null) t.unlock = on;

        if (player2Tile != null)
            foreach (var t in player2Tile) if (t != null) t.unlock = on;
    }

// để SelectionTrapPanel gọi start ingame sau khi override xong
    public void StartGame()
    {
        if (rollDice) rollDice.SetActive(false);
        SetupGame(lockTiles: false);
        _currentPlayerTurn = randomFirstTurn ? Random.Range(1, 3) : 1;
        UpdateTurnUI();
    }


    private void SetupGame(bool lockTiles)
    {
        _gameOver = false;

        totalTile = boardSize * boardSize;

        _health1 = healthPlayer1 != null ? healthPlayer1.Count : 0;
        _health2 = healthPlayer2 != null ? healthPlayer2.Count : 0;

        _chip1 = 0;
        _chip2 = 0;
        
        if (_botCo != null) { StopCoroutine(_botCo); _botCo = null; }
        _botActing = false;

        SetupProgressUI();
        SetHealthPLayer1Change(_health1);
        SetHealthPLayer2Change(_health2);
        SetChipPLayer1Change(_chip1);
        SetChipPLayer2Change(_chip2);

        SpawnTile();

        // tắt win text khi vào game mới
        if (winRollDice) winRollDice.gameObject.SetActive(false);
        if (confetti) confetti.SetActive(false);

        // LOCK/UNLOCK tile theo giai đoạn
        SetTilesInteractable(!lockTiles);

        // lưu ý: KHÔNG gọi UpdateTurnUI ở đây nếu lockTiles = true
        // vì phải chờ RollDice xong mới hiện turn + cho click
        if (!lockTiles)
            UpdateTurnUI();
    }

    private void PlaceBombsByIndices(List<ChooseItem> tiles, int bombCount, List<int> manualIndices)
    {
        foreach (var t in tiles) t.SetBomb(false);

        bombCount = Mathf.Clamp(bombCount, 0, tiles.Count);

        HashSet<int> finalIdx = new HashSet<int>();

        if (useManualBombSetup && manualIndices != null && manualIndices.Count > 0)
        {
            foreach (var i in manualIndices)
                if (i >= 0 && i < tiles.Count) finalIdx.Add(i);
        }

        // bù random nếu thiếu
        if (finalIdx.Count < bombCount)
        {
            var extra = Enumerable.Range(0, tiles.Count)
                .Where(i => !finalIdx.Contains(i))
                .OrderBy(_ => Random.value)
                .Take(bombCount - finalIdx.Count);

            foreach (var i in extra) finalIdx.Add(i);
        }

        // cắt bớt nếu dư
        if (finalIdx.Count > bombCount)
            finalIdx = new HashSet<int>(finalIdx.Take(bombCount));

        foreach (var i in finalIdx)
            tiles[i].SetBomb(true);
    }

    private void SetupProgressUI()
    {
        if (progress1)
        {
            progress1.minValue = 0;
            progress1.maxValue = ChipsTarget;
            progress1.value = 0;
        }

        if (progress2)
        {
            progress2.minValue = 0;
            progress2.maxValue = ChipsTarget;
            progress2.value = 0;
        }
    }
    
    private void SpawnTile()
    {
        totalTile = Mathf.Min(player1Tile.Count, player2Tile.Count);

        for (int i = 0; i < player1Tile.Count; i++)
        {
            var item = player1Tile[i];
            if (item == null) continue;

            item.Init(this, ownerPlayerIndex: 1, tileIndex: i, bombSprite, chipSprite);
            item.ResetVisual();
        }

        for (int i = 0; i < player2Tile.Count; i++)
        {
            var item = player2Tile[i];
            if (item == null) continue;

            item.Init(this, ownerPlayerIndex: 2, tileIndex: i, bombSprite, chipSprite);
            item.ResetVisual();
        }

        PlaceBombsByIndices(player1Tile, bombCountBoard1, bombIndicesBoard1);
        PlaceBombsByIndices(player2Tile, bombCountBoard2, bombIndicesBoard2);
    }

    private void PlaceBombs(List<ChooseItem> tiles, int bombCount, List<Vector2Int> manualCells)
    {
        if (tiles == null || tiles.Count == 0) return;

        // Reset về không-bomb trước
        foreach (var t in tiles) t.SetBomb(false);

        bombCount = Mathf.Clamp(bombCount, 0, tiles.Count);

        HashSet<int> finalBombIdx = new HashSet<int>();

        if (useManualBombSetup && manualCells != null && manualCells.Count > 0)
        {
            foreach (var cell in manualCells)
            {
                int row = cell.x;
                int col = cell.y;

                if (row < 0 || row >= boardSize || col < 0 || col >= boardSize)
                    continue;

                int idx = row * boardSize + col;
                if (idx >= 0 && idx < tiles.Count)
                    finalBombIdx.Add(idx);
            }
        }

        // Nếu manual ít hơn bombCount => random bù cho đủ
        if (finalBombIdx.Count < bombCount)
        {
            var candidates = Enumerable.Range(0, tiles.Count)
                .Where(i => !finalBombIdx.Contains(i))
                .OrderBy(_ => Random.value)
                .Take(bombCount - finalBombIdx.Count);

            foreach (var i in candidates) finalBombIdx.Add(i);
        }
        // Nếu manual nhiều hơn bombCount => cắt bớt
        else if (finalBombIdx.Count > bombCount)
        {
            finalBombIdx = new HashSet<int>(finalBombIdx.Take(bombCount));

        }

        foreach (int i in finalBombIdx)
            tiles[i].SetBomb(true);
    }

    private void PlaceBombs(List<ChooseItem> tiles, int bombCount)
    {
        if (tiles == null || tiles.Count == 0) return;

        bombCount = Mathf.Clamp(bombCount, 0, tiles.Count);

        var indices = Enumerable.Range(0, tiles.Count)
            .OrderBy(_ => Random.value)
            .Take(bombCount);

        foreach (var idx in indices)
            tiles[idx].SetBomb(true);
    }

    // === CORE CLICK FLOW ===
    public void OnTileClicked(ChooseItem clicked)
    {
        // nếu đang lượt bot mà không phải bot gọi thì bỏ qua
        if (player2IsBot && _currentPlayerTurn == 2 && !_botActing) return;
        
        if (_waitingRollDice) return;
        if (_gameOver) return;
        if (clicked == null) return;
        if (clicked.IsRevealed) return;

        // Chỉ được chọn ô ở board đối phương
        if (clicked.playerIndex == _currentPlayerTurn) return;

        clicked.Reveal();

        // Nếu bomb => người đang mở mất heart
        // Nếu bomb => người đang mở mất heart
        if (clicked.IsBomb)
        {
            PlayBombFxAt(clicked); // <-- thêm dòng này
            AudioManager.ins.PlaySoundBomb();
            if (_currentPlayerTurn == 1)
            {
                _health1 = Mathf.Max(0, _health1 - 1);
                SetHealthPLayer1Change(_health1);
            }
            else
            {
                _health2 = Mathf.Max(0, _health2 - 1);
                SetHealthPLayer2Change(_health2);
            }
        }

        else
        {
            // Chip => an toàn, cộng chip cho người mở
            AudioManager.ins.PlaySoundChip();
            if (_currentPlayerTurn == 1)
            {
                _chip1++;
                SetChipPLayer1Change(_chip1);
            }
            else
            {
                _chip2++;
                SetChipPLayer2Change(_chip2);
            }
        }

        // Check end game: hết mạng
        if (_health1 <= 0)
        {
            EndGame(winnerPlayer: 2);
            return;
        }
        if (_health2 <= 0)
        {
            EndGame(winnerPlayer: 1);
            return;
        }

        // (Tuỳ chọn) Nếu cả 2 board đã mở hết thì kết thúc theo chip/health
        if (AllRevealed(player1Tile) && AllRevealed(player2Tile))
        {
            int winner = DecideWinnerByChipsThenHealth();
            EndGame(winner);
            return;
        }

        // Luân phiên turn sau MỖI lần mở
        SwitchTurn();
    }

    private bool AllRevealed(List<ChooseItem> tiles)
    {
        return tiles != null && tiles.All(t => t != null && t.IsRevealed);
    }

    private int DecideWinnerByChipsThenHealth()
    {
        if (_chip1 != _chip2) return _chip1 > _chip2 ? 1 : 2;
        if (_health1 != _health2) return _health1 > _health2 ? 1 : 2;
        return 0; // hoà
    }

    private void SwitchTurn()
    {
        _currentPlayerTurn = (_currentPlayerTurn == 1) ? 2 : 1;
        UpdateTurnUI();
    }

    private void UpdateTurnUI()
    {
        // highlight player + highlight board đối phương (board có thể click)
        bool p1Turn = _currentPlayerTurn == 1;

        if (highlight1) highlight1.SetActive(p1Turn);
        if (highlight2) highlight2.SetActive(!p1Turn);

        // Player 1 turn => click board2 => highlightBoard2
        if (highlightBoard1) highlightBoard1.SetActive(!p1Turn);
        if (highlightBoard2) highlightBoard2.SetActive(p1Turn);

        // text cảnh báo "Player(x)'s turn"
        ShowTurnNotice(_currentPlayerTurn);
        // NEW:
        UpdateInputForTurn();
        StartBotTurnIfNeeded();
    }

    private void ShowTurnNotice(int playerIndex)
    {
        GameObject go = (playerIndex == 1) ? notice1 : notice2;
        GameObject other = (playerIndex == 1) ? notice2 : notice1;

        if (other) other.SetActive(false);
        if (go == null) return;

        var txt = go.GetComponentInChildren<TMP_Text>(true);
        if (txt) txt.text = $"Player {playerIndex}'s turn";

        go.SetActive(true);

        // fade in/out bằng CanvasGroup
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        cg.DOKill();
        cg.alpha = 0f;

        cg.DOFade(1f, 0.15f).SetUpdate(true);
        cg.DOFade(0f, 0.2f)
            .SetDelay(noticeShowSeconds)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (go) go.SetActive(false);
            });
    }

    private void EndGame(int winnerPlayer)
    {
        _gameOver = true;

        // khoá click
        if (player1Tile != null) foreach (var t in player1Tile) if (t) t.Lock();
        if (player2Tile != null) foreach (var t in player2Tile) if (t) t.Lock();
        if (_botCo != null) { StopCoroutine(_botCo); _botCo = null; }
        if (confetti) confetti.SetActive(true);

        if (winRollDice)
        {
            winRollDice.gameObject.SetActive(true);
            if (winnerPlayer == 0) winRollDice.text = "Draw!";
            else winRollDice.text = $"Player {winnerPlayer} wins!";
        }
        LunaManager.ins.ShowWinCard();
    }

    // === UI UPDATE ===
    private void SetHealthPLayer1Change(int health)
    {
        if (healthPlayer1 == null) return;
        for (int i = 0; i < healthPlayer1.Count; i++)
        {
            if (healthPlayer1[i]) healthPlayer1[i].SetActive(i < health);
        }
    }

    private void SetHealthPLayer2Change(int health)
    {
        if (healthPlayer2 == null) return;
        for (int i = 0; i < healthPlayer2.Count; i++)
        {
            if (healthPlayer2[i]) healthPlayer2[i].SetActive(i < health);
        }
    }

    private void SetChipPLayer1Change(int chip)
    {
        if (progress1) progress1.value = Mathf.Clamp(chip, 0, (int)progress1.maxValue);
    }

    private void SetChipPLayer2Change(int chip)
    {
        if (progress2) progress2.value = Mathf.Clamp(chip, 0, (int)progress2.maxValue);
    }
}
