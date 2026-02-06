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

// để SelectionTrapPanel gọi start ingame sau khi override xong
    public void StartGame()
    {
        // gọi vào hàm setup/spawn game của bạn
        SetupGame(); // nếu SetupGame đang private thì đổi nó thành internal/public
    }

    private void SetupGame()
    {
        _gameOver = false;

        totalTile = boardSize * boardSize;

        _health1 = healthPlayer1 != null ? healthPlayer1.Count : 0;
        _health2 = healthPlayer2 != null ? healthPlayer2.Count : 0;

        _chip1 = 0;
        _chip2 = 0;

        SetupProgressUI();
        SetHealthPLayer1Change(_health1);
        SetHealthPLayer2Change(_health2);
        SetChipPLayer1Change(_chip1);
        SetChipPLayer2Change(_chip2);

        SpawnTile();

        _currentPlayerTurn = randomFirstTurn ? Random.Range(1, 3) : 1;

        if (rollDice) rollDice.SetActive(false); // nếu bạn có flow roll riêng thì chỉnh lại
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
        if (_gameOver) return;
        if (clicked == null) return;
        if (clicked.IsRevealed) return;

        // Chỉ được chọn ô ở board đối phương
        if (clicked.playerIndex == _currentPlayerTurn) return;

        clicked.Reveal();

        // Nếu bomb => người đang mở mất heart
        if (clicked.IsBomb)
        {
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

        if (confetti) confetti.SetActive(true);

        if (winRollDice)
        {
            winRollDice.gameObject.SetActive(true);
            if (winnerPlayer == 0) winRollDice.text = "Draw!";
            else winRollDice.text = $"Player {winnerPlayer} wins!";
        }
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
