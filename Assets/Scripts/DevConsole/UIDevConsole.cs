using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDevConsole : MonoBehaviour
{
    private static int WORD_COUNT_TO_START_AUTOCOMPLETE = 2;

    [SerializeField] private GameObject _devConsolePanel;
    [SerializeField] private Button _consoleToggleBtn;
    [SerializeField] private Transform _logParent;
    [SerializeField] private TextMeshProUGUI _logMsgPrefab;
    [SerializeField] private TMPro.TMP_InputField _cmdInput;
    [SerializeField] private TextMeshProUGUI _textAutoCompleteGhost;
    [SerializeField] private ScrollRect _logDisplayRect;

    [Header("Settings")]
    [SerializeField] private int _maxLogCount = 75;
    [SerializeField] private int _maxHelpListPerPage = 15;
    [SerializeField] private Color _errorLogColor;
    [SerializeField] private Color _warningLogColor;
    [SerializeField] private Color _normalLogColor;

    private List<TextMeshProUGUI> _logList = new List<TextMeshProUGUI>();

    private int _viewCurrentHistoryIndex = -1;
    private string _nearestCommand;

    private void Start()
    {
        _devConsolePanel.SetActive(false);
        //_cmdInput.onSelect.AddListener(ConsoleInputSelectedHandler);

        _consoleToggleBtn.onClick.AddListener(UI_ConsoleBtn);
        _cmdInput.onValueChanged.AddListener(AutoCompleteCommandCheck);
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        DeveloperConsole.Instance.RegisterToConsole(this);
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        DeveloperConsole.Instance.UnregisterToConsole(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            ShowDevConsole();
        }

        if (_devConsolePanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Return))
                InputEntered();
            else if (Input.GetKeyDown(KeyCode.Tab))
                AutoCompleteText();
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                AutoCompleteTextHistory(true);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                AutoCompleteTextHistory(false);
            }
        }


    }

    public void UI_ConsoleBtn()
    {
        ShowDevConsole();
    }

    public void UI_EnterInputBtn()
    {
        InputEntered();
    }

    public void UI_HelpBtn()
    {
        PrintCommandList();
    }

    public void ShowDevConsole()
    {
        _devConsolePanel.SetActive(!_devConsolePanel.activeInHierarchy);
        _cmdInput.text = "";

        if (_devConsolePanel.activeInHierarchy)
            SelectInputField();

        Util_ScrollDown();
    }

    [ConsoleCmd("Clears console")]
    public void ClearConsole()
    {
        _logList.ForEach(log => Destroy(log.gameObject));
        _logList.Clear();
    }

    [ConsoleCmd("Show how to use dev console")]
    public void Help()
    {
        CreateLog($"========================================");
        CreateLog("------- HOW TO USE DEV CONSOLE -------");
        CreateLog("Format: 'function parameter1 parameter2'");
        CreateLog("   E.g. calculate 1 1 1");
        CreateLog("   E.g. testlog \"hello world\"");
        CreateLog("   E.g. testvector3 3,3,3");
        CreateLog("   E.g. clear");
        CreateLog("   ");
        CreateLog("   Type commands <page index> - to access different pages of the commands");

        PrintCommandList();
    }

    [ConsoleCmd("Show list of commands. Give page index to access different pages of the commands")]
    public void Commands(int pageIndex = 1)
    {
        PrintCommandList(pageIndex);
    }

    [ConsoleCmd("Show list of ALL commands")]
    public void CommandsAll()
    {
        PrintCommandList(1, true);
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        CreateLog(condition, type);
    }

    private void CreateLog(string message, LogType type = LogType.Log)
    {
        if (_logList.Count >= _maxLogCount)
        {
            Destroy(_logList[0].gameObject);
            _logList.RemoveAt(0);
        }

        TextMeshProUGUI text = Instantiate(_logMsgPrefab, _logParent);
        text.color = type == LogType.Exception? _errorLogColor: type == LogType.Error? _errorLogColor :
            type == LogType.Warning ? _warningLogColor : _normalLogColor;
        text.text = ($"[{System.DateTime.Now.ToString("HH:mm:ss")}]  {message}"); 
        text.gameObject.SetActive(true);

        _logList.Add(text);
    }

    private void InputEntered()
    {
        string cmd = _cmdInput.text;
        Debug.Log(cmd);
        _cmdInput.text = "";
        SelectInputField();
        _viewCurrentHistoryIndex = -1;
        Util.WaitNextFrame(this, Util_ScrollDown);

        DeveloperConsole.Instance.ParseCommand(cmd);
    }

    private void PrintCommandList(int pageIndex = 1, bool commandListAll = false)
    {
        int maxHelpListPerPage = !commandListAll? _maxHelpListPerPage : 999;

        Dictionary<string, CommandData> commandList = DeveloperConsole.Instance.GetListOfCommands();
        int maxPageCount = (commandList.Count / (maxHelpListPerPage + 1)) + 1;

        CreateLog($"========================================");
        CreateLog("=======[ HELP COMMAND LIST ]=======");
        CreateLog($"List of commands (Pg {pageIndex} / {maxPageCount})");
        int startingCmdIdx = (pageIndex - 1) * maxHelpListPerPage;

        if (startingCmdIdx >= commandList.Count)
        {
            CreateLog("Help page count exceeded!");
            return;
        }

        List<KeyValuePair<string, CommandData>> commandsArr = commandList.ToList();

        for (int i = startingCmdIdx; i < commandsArr.Count && i < (startingCmdIdx + maxHelpListPerPage); i++)
        {
            CreateLog($"    {commandsArr[i].Key} - {commandsArr[i].Value.Description}");
        }


        CreateLog($"-------[ Pg {pageIndex} / {maxPageCount} ]-------");
        CreateLog($"========================================");

        Util.WaitNextFrame(this, Util_ScrollDown);
    }

    private void AutoCompleteText()
    {
        if (_nearestCommand != null)
        {
            _cmdInput.text = _nearestCommand;
            _cmdInput.caretPosition = _cmdInput.text.Length;
        }
    }

    private void AutoCompleteCommandCheck(string input)
    {
        if (_cmdInput.text.Length > WORD_COUNT_TO_START_AUTOCOMPLETE)
        {
            string[] parameters;
            _nearestCommand = DeveloperConsole.Instance.GetNearestCommand(input, out parameters);

            if (_nearestCommand == null)
                _textAutoCompleteGhost.text = "";
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(_nearestCommand);

                if (parameters != null)
                {
                    foreach (string param in parameters)
                    {
                        stringBuilder.Append($" <{param}>");
                    }
                }
                
                _textAutoCompleteGhost.text = stringBuilder.ToString();
            }
        }
        else if (_textAutoCompleteGhost.text != "")
        { 
            _textAutoCompleteGhost.text = "";
        }
        
        // Resets view current history index
        if (_cmdInput.text.Length == 0)
        {
            _viewCurrentHistoryIndex = -1;
        }
    }

    private void AutoCompleteTextHistory(bool isKeyUp)
    {
        if (DeveloperConsole.Instance.CommandHistory.Count == 0) return;

        if (isKeyUp)
        {
            _viewCurrentHistoryIndex = Mathf.Clamp(_viewCurrentHistoryIndex + 1, 0, DeveloperConsole.Instance.CommandHistory.Count - 1);
        }
        else
        {
            // If at reset point -> Not pressed key up yet, don't do anything
            if (_viewCurrentHistoryIndex == -1 || _viewCurrentHistoryIndex == 0) return;
            _viewCurrentHistoryIndex = Mathf.Clamp(_viewCurrentHistoryIndex - 1, 0, DeveloperConsole.Instance.CommandHistory.Count - 1);
        }

        if (_viewCurrentHistoryIndex == DeveloperConsole.Instance.CommandHistory.Count) return;

        string command = DeveloperConsole.Instance.GetHistoryAtIndex(_viewCurrentHistoryIndex);
        _cmdInput.text = command;
        _cmdInput.caretPosition = _cmdInput.text.Length;
    }

    private void SelectInputField()
    {
        EventSystem.current.SetSelectedGameObject(_cmdInput.gameObject);
        _cmdInput.ActivateInputField(); 
    }

    private void Util_ScrollDown()
    {
        _logDisplayRect.normalizedPosition = new Vector2(0, 0);
    }
}
