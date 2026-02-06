using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityEngine.EventSystems;

public class ChooseItem : MonoBehaviour, IPointerDownHandler

{
    public int idItem;
    public int playerIndex;           // Chủ sở hữu của board (1 hoặc 2)
    public Image bgTile;
    public GameObject selected;
    public GameObject blackBg;
    public Image icon;

    public bool unlock = false;
    public bool isChosen = false;

    [SerializeField] private ParticleSystem explode;
    public Image frameBlue;
    public Image frameRed;

    private GameClassicPanel _game;
    private bool _isBomb;
    private bool _revealed;

    private Sprite _bombSprite;
    private Sprite _chipSprite;

    public bool IsBomb => _isBomb;
    public bool IsRevealed => _revealed;
    public System.Action<ChooseItem> OnClicked;
    private IPointerDownHandler pointerDownHandlerImplementation;

    public void Init(GameClassicPanel game, int ownerPlayerIndex, int tileIndex, Sprite bombSprite, Sprite chipSprite)
    {
        _game = game;
        playerIndex = ownerPlayerIndex;
        idItem = tileIndex;

        _bombSprite = bombSprite;
        _chipSprite = chipSprite;

        ResetVisual();
        unlock = true;
    }

    public void SetBomb(bool isBomb)
    {
        _isBomb = isBomb;
    }

    public void ResetVisual()
    {
        _revealed = false;
        isChosen = false;

        if (selected) selected.SetActive(false);
        if (blackBg) blackBg.SetActive(true);

        if (icon)
        {
            icon.gameObject.SetActive(false);
            icon.sprite = null;
        }

        //if (frameBlue) frameBlue.gameObject.SetActive(false);
        //if (frameRed) frameRed.gameObject.SetActive(false);
    }

    public void Reveal()
    {
        if (_revealed) return;

        _revealed = true;
        isChosen = true;

        if (selected) selected.SetActive(true);
        if (blackBg) blackBg.SetActive(false);

        if (icon)
        {
            icon.gameObject.SetActive(true);
            if (_isBomb && _bombSprite) icon.sprite = _bombSprite;
            if (!_isBomb && _chipSprite) icon.sprite = _chipSprite;
        }

        //if (frameBlue) frameBlue.gameObject.SetActive(!_isBomb);
        //if (frameRed) frameRed.gameObject.SetActive(_isBomb);

        if (_isBomb && explode) explode.Play();

        // hiệu ứng nhỏ cho cảm giác "lật"
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.12f, 0.22f, 8, 0.7f);
    }

    public void Lock()
    {
        unlock = false;
    }
    public void SetSetupSelected(bool on, Sprite bombSprite = null)
    {
        // tuỳ prefab bạn có gì thì bật/tắt ở đây
        if (selected) selected.SetActive(on);

        if (icon)
        {
            icon.gameObject.SetActive(on);
            if (on && bombSprite != null) icon.sprite = bombSprite;
        }

        // tránh reveal kiểu ingame
        isChosen = on;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("OnPointerClick");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!unlock) return;

        // Nếu bạn có event setup bomb:
        if (OnClicked != null)
        {
            AudioManager.ins.PlaySoundClick();
            OnClicked.Invoke(this);
            return;
        }
        Debug.Log("OnPointerDown");
        // Ingame:
        AudioManager.ins.PlaySoundClick();
        _game?.OnTileClicked(this);
    }

}
