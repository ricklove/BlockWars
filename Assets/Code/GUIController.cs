using UnityEngine;
using System.Collections.Generic;

public class GUIController : MonoBehaviour
{
    public GUISkin mainSkin;

    private Texture2D titleImage;
    private Texture2D titleBackgroundImage;
    private GUIStyle titleBackgroundStyle;

    private GUIState guiState;
    private GUIResult guiResult;
    private MainMenuResult mainMenuResult;

    private float titleScreenStartTime;
    private float titleScreenDisplayTimeSpan;

    private bool mainMenuIsShowingOptions;
    private int mainMenuOptionHumanCount;
    private int mainMenuOptionComputerCount;

    private EndOfGameInfo endOfGameInfo;
    private float endOfGameStartTime;
    private float endOfGameDisplayTimeSpan;

    void Start()
    {
        mainMenuOptionHumanCount = 1;
        mainMenuOptionComputerCount = 1;

        if (titleImage == null)
        {
            titleImage = (Texture2D)Resources.Load("Texture/BashnBlocksTitle");
        }
        if (titleBackgroundImage == null)
        {
            titleBackgroundImage = (Texture2D)Resources.Load("Texture/TitleBackground");
        }

        if (titleBackgroundStyle == null)
        {
            titleBackgroundStyle = new GUIStyle();
        }

    }

    void OnGUI()
    {
        GUI.skin = mainSkin;
        // TODO: Use this method to make all GUI calls

        var padding = 10.0f;

        var sw = Screen.width;
        var sh = Screen.height;

        var w = (float)Mathf.Min(sw, 400);
        var h = (float)Mathf.Min(sh, 300);

        w = sw * 0.8f;
        h = sh * 0.8f;

        var l = (sw - w) / 2.0f;
        var t = (sh - h) / 2.0f;

        var fontSize = h / 10.0f;
        GUI.skin.button.fontSize = (int)fontSize;
        GUI.skin.label.fontSize = (int)fontSize;
        GUI.skin.box.fontSize = (int)fontSize;

        // Show Title screen
        if (guiState == GUIState.TitleScreen)
        {
            if (Time.time - titleScreenStartTime < titleScreenDisplayTimeSpan)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), titleBackgroundImage);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), titleImage, ScaleMode.ScaleToFit);
                guiResult = GUIResult.Active;
            }
            else
            {
                guiResult = GUIResult.Finished;
            }
        }

        if (guiState == GUIState.MainMenu)
        {
            // Show New Game button with options
            // TODO: Only show Quit button for Desktops (Web/iPhone ignore this)

            if (guiResult == GUIResult.NotStarted)
            {
                guiResult = GUIResult.Active;
            }

            if (guiResult == GUIResult.Active)
            {


                var buttonCount = 3;
                buttonCount = ShouldShowQuitButton() ? buttonCount + 1 : buttonCount;

                // Button height (taking out the padding for each and the extra padding on top)
                var buttonHeight = ((h - padding) / buttonCount) - padding;

                GUILayout.BeginArea(new Rect(l, t, w, h));
                GUILayout.Space(padding);

                if (!mainMenuIsShowingOptions)
                {
                    if (GUILayout.Button("Quick Game", GUILayout.Height(buttonHeight)))
                    {
                        guiResult = GUIResult.Finished;
                        mainMenuResult = new MainMenuResult(guiResult, MainMenuAction.NewGame, new NewGameOptions(1, 1, false));
                    }
                    GUILayout.Space(padding);

                    if (GUILayout.Button("New Game", GUILayout.Height(buttonHeight)))
                    {
                        mainMenuIsShowingOptions = true;
                    }
                    GUILayout.Space(padding);

                    if (ShouldShowQuitButton())
                    {
                        if (GUILayout.Button("Quit", GUILayout.Height(buttonHeight)))
                        {
                            guiResult = GUIResult.Finished;
                            mainMenuResult = new MainMenuResult(guiResult, MainMenuAction.Close, null);
                        }
                        GUILayout.Space(padding);
                    }

                }
                else
                {
                    GUILayout.BeginHorizontal();
                    mainMenuOptionHumanCount = GUILayout.SelectionGrid(mainMenuOptionHumanCount, new string[]
          {
            "No Humans",
            "1 Human",
            "2 Humans",
            "3 Humans",
            "4 Humans"
          }, 1);
                    mainMenuOptionComputerCount = GUILayout.SelectionGrid(mainMenuOptionComputerCount, new string[]
          {
            "No Computers",
            "1 Computer",
            "2 Computers",
            "3 Computers",
            "4 Computers"
          }, 1);
                    GUILayout.EndHorizontal();

                    if (mainMenuOptionHumanCount + mainMenuOptionComputerCount > 1)
                    {
                        if (GUILayout.Button("Play", GUILayout.Height(buttonHeight)))
                        {
                            guiResult = GUIResult.Finished;
                            mainMenuResult = new MainMenuResult(guiResult, MainMenuAction.NewGame, new NewGameOptions(mainMenuOptionHumanCount, mainMenuOptionComputerCount, true));
                        }
                    }
                }

                GUILayout.EndArea();
            }
        }

        if (guiState == GUIState.EndOfGameReport)
        {
            if (Time.time - endOfGameStartTime < endOfGameDisplayTimeSpan)
            {

                GUI.Box(new Rect(l - padding, t - padding, w + 2 * padding, h + 2 * padding), "");
                GUILayout.BeginArea(new Rect(l, t, w, h));
                GUILayout.Space(padding);

                GUILayout.Box(endOfGameInfo.winnerName + " wins!");
                GUILayout.Space(padding);

                foreach (var loserName in endOfGameInfo.loserHumanNames)
                {
                    GUILayout.Box(loserName + " loses!");
                    GUILayout.Space(padding);
                }

                GUILayout.EndArea();

                guiResult = GUIResult.Active;
            }
            else
            {
                guiResult = GUIResult.Finished;
            }
        }
    }

    private bool ShouldShowQuitButton()
    {
        switch (Application.platform)
        {
            //OSXEditor	
            //OSXPlayer	
            //WindowsPlayer	
            //OSXWebPlayer	
            //OSXDashboardPlayer	
            //WindowsWebPlayer	
            //WiiPlayer	
            //WindowsEditor	
            //IPhonePlayer	
            //XBOX360	
            //PS3	
            //Android	
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXWebPlayer:
            case RuntimePlatform.OSXDashboardPlayer:
            case RuntimePlatform.WindowsWebPlayer:
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
                return false;
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.XBOX360:
            case RuntimePlatform.PS3:
            default:
                return true;
        }
    }

    public GUIResult ShowTitleScreen(float displayTimeSpan)
    {
        if (guiState != GUIState.TitleScreen)
        {
            titleScreenStartTime = Time.time;
            titleScreenDisplayTimeSpan = displayTimeSpan;

            guiState = GUIState.TitleScreen;
            guiResult = GUIResult.NotStarted;
        }

        return guiResult;
    }

    public MainMenuResult ShowMainMenu()
    {
        if (guiState != GUIState.MainMenu)
        {
            mainMenuIsShowingOptions = false;

            // Leave same as last play
            //mainMenuOptionHumanCount = 1; 
            //mainMenuOptionComputerCount = 1;

            guiState = GUIState.MainMenu;
            guiResult = GUIResult.NotStarted;
            mainMenuResult = new MainMenuResult(guiResult, MainMenuAction.None, null);
        }

        return mainMenuResult;
    }

    public GUIResult ShowEndOfGameReport(EndOfGameInfo info, float displayTimeSpan)
    {
        if (guiState != GUIState.EndOfGameReport)
        {
            endOfGameInfo = info;
            endOfGameStartTime = Time.time;
            endOfGameDisplayTimeSpan = displayTimeSpan;

            guiState = GUIState.EndOfGameReport;
            guiResult = GUIResult.NotStarted;
        }

        return guiResult;
    }

    public void ShowNone()
    {
        guiState = GUIState.None;
    }
}

public enum GUIState
{
    None,
    TitleScreen,
    MainMenu,
    EndOfGameReport
}

public enum GUIResult
{
    NotStarted,
    Active,
    Finished
}

public enum MainMenuAction
{
    None,
    NewGame,
    Close
}

public class MainMenuResult
{
    public GUIResult guiResult;
    public MainMenuAction mainMenuAction;
    public NewGameOptions newGameOptions;

    public bool isFinished { get { return guiResult == GUIResult.Finished; } }

    public MainMenuResult(GUIResult guiResult, MainMenuAction action, NewGameOptions newGameOptions)
    {
        this.guiResult = guiResult;
        this.mainMenuAction = action;
        this.newGameOptions = newGameOptions;
    }
}

public class NewGameOptions
{
    public int humanPlayerCount;
    public int computerPlayerCount;
    public bool shouldShowEditor;

    public NewGameOptions(int humanPlayerCount, int computerPlayerCount, bool shouldShowEditor)
    {
        this.humanPlayerCount = humanPlayerCount;
        this.computerPlayerCount = computerPlayerCount;
        this.shouldShowEditor = shouldShowEditor;
    }
}

public class EndOfGameInfo
{
    public string winnerName;
    public bool isWinnerHuman;

    public string[] loserHumanNames;

    public EndOfGameInfo(string winnerName, bool isWinnerHuman, string[] loserHumanNames)
    {
        this.winnerName = winnerName;
        this.isWinnerHuman = isWinnerHuman;
        this.loserHumanNames = loserHumanNames;
    }
}