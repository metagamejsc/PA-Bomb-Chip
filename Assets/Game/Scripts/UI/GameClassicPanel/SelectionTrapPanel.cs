using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionTrapPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private int idPanel;
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;

    [SerializeField] public Slider progress1;
    [SerializeField] public Slider progress2;

    [SerializeField] private GameObject tile;          // nếu bạn muốn instantiate (optional)
    [SerializeField] private GameObject mainboard1;     // board cho step Player1 chọn
    [SerializeField] private GameObject mainboard2;     // board cho step Player2 chọn

    [SerializeField] private Image bgTile1;
    [SerializeField] private Image bgTile2;

    [SerializeField] private TMP_Text progressTxt1;
    [SerializeField] private TMP_Text progressTxt2;

    [SerializeField] private TMP_Text timerTxt;         // thêm 1 text để hiện timer (optional)

    [SerializeField] private Button nextStep;

    [Header("Setup Config")]
    [SerializeField] private int bombsToChoose = 3;
    [SerializeField] private float timePerPlayer = 15f; // thời gian mỗi player setup

    [Header("Visual")]
    [SerializeField] private Sprite bombSprite;         // sprite bomb để show khi chọn (optional)

    [Header("Flow To Ingame")]
    [SerializeField] private GameObject inGamePanelRoot;     // panel ingame để bật lên
    [SerializeField] private GameClassicPanel gameClassic;   // script ingame để override bomb

    [SerializeField] private int currentStep = 1;
    [SerializeField] private int totalTile = 9;

    [SerializeField] private List<ChooseItem> player1Chosen = new List<ChooseItem>();
    [SerializeField] private List<ChooseItem> player2Chosen = new List<ChooseItem>();

    // 2 biến bool theo yêu cầu (để ghi đè ManualBombSetup)
    public bool player1DidSetupBomb { get; private set; }
    public bool player2DidSetupBomb { get; private set; }

    private List<ChooseItem> _tilesBoardStep1 = new List<ChooseItem>();
    private List<ChooseItem> _tilesBoardStep2 = new List<ChooseItem>();

    private float _timeLeft;
    private Coroutine _timerCo;

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        StopTimer();
        UnhookClicks();
    }

    private void Init()
    {
        player1DidSetupBomb = false;
        player2DidSetupBomb = false;

        currentStep = Mathf.Clamp(currentStep, 1, 2);

        // Lấy tile sẵn có trong hierarchy (Tile_1..Tile_9)
        _tilesBoardStep1 = mainboard1.GetComponentsInChildren<ChooseItem>(true).ToList();
        _tilesBoardStep2 = mainboard2.GetComponentsInChildren<ChooseItem>(true).ToList();

        // Nếu bạn muốn instantiate tile prefab (tuỳ project), có thể mở đoạn này:
        // if (_tilesBoardStep1.Count == 0) SpawnTiles(mainboard1.transform, _tilesBoardStep1);
        // if (_tilesBoardStep2.Count == 0) SpawnTiles(mainboard2.transform, _tilesBoardStep2);

        totalTile = Mathf.Max(totalTile, Mathf.Max(_tilesBoardStep1.Count, _tilesBoardStep2.Count));

        // reset chọn
        player1Chosen.Clear();
        player2Chosen.Clear();
        ResetBoardVisual(_tilesBoardStep1);
        ResetBoardVisual(_tilesBoardStep2);

        HookClicks();

        // UI slider max
        if (progress1) { progress1.minValue = 0; progress1.maxValue = bombsToChoose; progress1.value = 0; }
        if (progress2) { progress2.minValue = 0; progress2.maxValue = bombsToChoose; progress2.value = 0; }

        GoToStep(currentStep);
    }

    private void HookClicks()
    {
        foreach (var t in _tilesBoardStep1)
        {
            if (t == null) continue;
            t.OnClicked = OnTileClicked;
            t.unlock = true;
        }

        foreach (var t in _tilesBoardStep2)
        {
            if (t == null) continue;
            t.OnClicked = OnTileClicked;
            t.unlock = true;
        }

        if (nextStep)
        {
            nextStep.onClick.RemoveAllListeners();
            nextStep.onClick.AddListener(OnNextStepPressed);
        }
    }

    private void UnhookClicks()
    {
        foreach (var t in _tilesBoardStep1) if (t != null) t.OnClicked = null;
        foreach (var t in _tilesBoardStep2) if (t != null) t.OnClicked = null;
    }

    private void ResetBoardVisual(List<ChooseItem> tiles)
    {
        foreach (var t in tiles)
        {
            if (t == null) continue;
            t.SetBomb(false); // setup panel: bomb thật sẽ set ở ingame, ở đây chỉ mark chọn
            t.isChosen = false;

            // nếu ChooseItem có hàm reset/reveal thì bạn dùng ở đây
            // t.ResetVisual();

            // visual tick off
            t.SetSetupSelected(false, bombSprite);
        }
    }

    private void GoToStep(int step)
    {
        currentStep = step;

        // show/hide board tương ứng
        if (mainboard1) mainboard1.SetActive(step == 1);
        if (mainboard2) mainboard2.SetActive(step == 2);

        // show player header
        if (player1) player1.SetActive(step == 1);
        if (player2) player2.SetActive(step == 2);

        // enable nextStep chỉ khi đủ bomb (hoặc hết giờ)
        UpdateProgressUI();

        StartTimer();
    }

    private void StartTimer()
    {
        StopTimer();
        _timeLeft = timePerPlayer;
        _timerCo = StartCoroutine(TimerTick());
    }

    private void StopTimer()
    {
        if (_timerCo != null)
        {
            StopCoroutine(_timerCo);
            _timerCo = null;
        }
    }

    private IEnumerator TimerTick()
    {
        while (_timeLeft > 0f)
        {
            _timeLeft -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }

        _timeLeft = 0f;
        UpdateTimerUI();

        // hết giờ => auto chốt step
        AutoFillIfNeeded();
        OnNextStepPressed();
    }

    private void UpdateTimerUI()
    {
        if (timerTxt == null) return;
        int sec = Mathf.CeilToInt(_timeLeft);
        timerTxt.text = $"Time: {sec}s";
    }

    private void UpdateProgressUI()
    {
        int chosenCount = (currentStep == 1) ? player1Chosen.Count : player2Chosen.Count;

        if (currentStep == 1)
        {
            if (progress1) progress1.value = chosenCount;
            if (progressTxt1) progressTxt1.text = $"{chosenCount}/{bombsToChoose}";
        }
        else
        {
            if (progress2) progress2.value = chosenCount;
            if (progressTxt2) progressTxt2.text = $"{chosenCount}/{bombsToChoose}";
        }

        if (nextStep)
            nextStep.interactable = (chosenCount >= bombsToChoose); // chỉ cho bấm khi đủ (hết giờ sẽ auto)
    }

    private void OnTileClicked(ChooseItem item)
    {
        if (item == null) return;

        // chỉ cho click board hiện tại
        if (currentStep == 1 && !_tilesBoardStep1.Contains(item)) return;
        if (currentStep == 2 && !_tilesBoardStep2.Contains(item)) return;

        var chosenList = (currentStep == 1) ? player1Chosen : player2Chosen;

        // toggle
        if (chosenList.Contains(item))
        {
            chosenList.Remove(item);
            item.SetSetupSelected(false, bombSprite);
        }
        else
        {
            if (chosenList.Count >= bombsToChoose) return;
            chosenList.Add(item);
            item.SetSetupSelected(true, bombSprite);
        }

        UpdateProgressUI();
    }

    private void AutoFillIfNeeded()
    {
        // nếu player chưa chọn đủ => random bù cho đủ
        var chosenList = (currentStep == 1) ? player1Chosen : player2Chosen;
        var tiles = (currentStep == 1) ? _tilesBoardStep1 : _tilesBoardStep2;

        if (chosenList.Count >= bombsToChoose) return;

        var candidates = tiles.Where(t => t != null && !chosenList.Contains(t)).OrderBy(_ => Random.value);
        foreach (var t in candidates)
        {
            chosenList.Add(t);
            t.SetSetupSelected(true, bombSprite);
            if (chosenList.Count >= bombsToChoose) break;
        }

        UpdateProgressUI();
    }

    private void OnNextStepPressed()
    {
        // nếu chưa đủ bomb mà người dùng bấm => không cho qua (trừ khi auto do hết giờ)
        if (currentStep == 1 && player1Chosen.Count < bombsToChoose) return;
        if (currentStep == 2 && player2Chosen.Count < bombsToChoose) return;

        if (currentStep == 1)
        {
            player1DidSetupBomb = true;
            StopTimer();
            GoToStep(2);
            return;
        }

        if (currentStep == 2)
        {
            player2DidSetupBomb = true;
            StopTimer();
            ApplyToInGameAndStart();
        }
    }

    private List<int> GetChosenIndices(List<ChooseItem> chosen, List<ChooseItem> tilesOfThatBoard)
    {
        // index theo thứ tự tilesOfThatBoard (0..8)
        var idx = new List<int>();
        for (int i = 0; i < tilesOfThatBoard.Count; i++)
        {
            if (chosen.Contains(tilesOfThatBoard[i]))
                idx.Add(i);
        }
        return idx;
    }

    private void ApplyToInGameAndStart()
    {
        // Mapping “trap opponent”:
        // - Step1: Player1 chọn bomb => bomb đặt lên Board2 (ingame)
        // - Step2: Player2 chọn bomb => bomb đặt lên Board1 (ingame)
        var bombsForBoard2 = GetChosenIndices(player1Chosen, _tilesBoardStep1);
        var bombsForBoard1 = GetChosenIndices(player2Chosen, _tilesBoardStep2);

        if (gameClassic != null)
        {
            // ghi đè manual bomb setup
            gameClassic.OverrideManualBombSetupFromSelection(
                board1BombIndices: bombsForBoard1,
                board2BombIndices: bombsForBoard2,
                overrideBoard1: player2DidSetupBomb,
                overrideBoard2: player1DidSetupBomb
            );

            // nếu ingame panel đang bật sẵn, gọi StartGame() để spawn lại theo bomb mới
            gameClassic.StartGame();
        }

        // chuyển panel
        if (inGamePanelRoot) inGamePanelRoot.SetActive(true);
        gameObject.SetActive(false);
    }

    // Optional nếu bạn muốn instantiate tile prefab thay vì tile có sẵn
    // private void SpawnTiles(Transform root, List<ChooseItem> outList)
    // {
    //     for (int i = 0; i < totalTile; i++)
    //     {
    //         var go = Instantiate(tile, root);
    //         var ci = go.GetComponent<ChooseItem>();
    //         outList.Add(ci);
    //     }
    // }
}
