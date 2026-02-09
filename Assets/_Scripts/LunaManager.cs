using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LunaManager : MonoBehaviour
{
    public static LunaManager ins;
    public int countDrop=0;
    public int countDropFinal;
    [LunaPlaygroundField("Time")] public int timeEndCreative=30;
    [LunaPlaygroundField("Time Bot Thinking")] public float timeBotThinking=2f;
    public bool isCretivePause;
    private void Awake()
    {
        ins = this;
    }
    public Button[] lstBtnInstall;
    public GameObject EndCard,EndCardEmpty;
    public GameObject WinCard;
    


    // Start is called before the first frame update
    void Start()
    {
       
        Luna.Unity.LifeCycle.OnPause += PauseGameplay;
        Luna.Unity.LifeCycle.OnResume += ResumeGameplay;
        foreach (var VARIABLE in lstBtnInstall)
        {
            VARIABLE.onClick.AddListener(OnClickEndCard);
        }
        EndCard.SetActive(false);
        EndCardEmpty.SetActive(false);
        WinCard.SetActive(false);
      
        Invoke(nameof(ShowEndCard),timeEndCreative);
    }
  
    public void CheckClickShowEndCard()
    {
        countDrop++;
        if (countDrop>=countDropFinal && isCretivePause==false)
        {
            ShowEndCardEmpty();
        }
    }
    // Update is called once per frame
    public void PauseGameplay()
    {
        Debug.Log("Pause game");
        Time.timeScale = 0;
    }

    public void ResumeGameplay()
    {
        Debug.Log("Load game");
        Time.timeScale = 1;
    }

    public void ShowEndCard()
    {
        if (isCretivePause) return;
        isCretivePause = true;
        AudioManager.ins.PlayMusicLose();
        EndCard.SetActive(true);
        Debug.Log("Show end card");
        Luna.Unity.LifeCycle.GameEnded();
    }
    public void ShowEndCardEmpty()
    {
        if (isCretivePause) return;
        isCretivePause = true;
        //AudioManager.ins.PlayMusicLose();
        EndCardEmpty.SetActive(true);
        Debug.Log("ShowEndCardEmpty");
        Luna.Unity.LifeCycle.GameEnded();
    }
    public void ShowWinCard()
    {
        if (isCretivePause) return;
        isCretivePause = true;
        AudioManager.ins.PlayMusicWin();
        WinCard.SetActive(true);
        Debug.Log("Show win card");
        Luna.Unity.LifeCycle.GameEnded();
    }
    public void OnClickEndCard()
    {
        Debug.Log("Click end card");
        Luna.Unity.Playable.InstallFullGame();
    }

}
