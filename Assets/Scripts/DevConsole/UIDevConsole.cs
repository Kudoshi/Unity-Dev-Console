using System;
using System.Collections.Generic;
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
        PrintHelp();
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
        Debug.Log("------- HOW TO USE DEV CONSOLE -------");
        Debug.Log("Format: 'function parameter1 parameter2'");
        Debug.Log("   E.g. calculate 1 1 1");
        Debug.Log("   E.g. testlog \"hello world\"");
        Debug.Log("   E.g. testvector3 3,3,3");
        Debug.Log("   E.g. clear");

        PrintHelp();
    }

    [ConsoleCmd("Show list of commands")]
    public void Commands()
    {
        PrintHelp();
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (_logList.Count >= _maxLogCount)
        {
            Destroy(_logList[0].gameObject);
            _logList.RemoveAt(0);
        }

        _logList.Add(CreateLog(condition, type));
    }

    private TextMeshProUGUI CreateLog(string message, LogType type)
    {
        TextMeshProUGUI text = Instantiate(_logMsgPrefab, _logParent);
        text.color = type == LogType.Exception ? _errorLogColor : type == LogType.Warning ? _warningLogColor : _normalLogColor;
        text.text = ($"[{System.DateTime.Now.ToString("HH:mm:ss")}]  {message}"); 
        text.gameObject.SetActive(true);

        return text;
    }

    private void InputEntered()
    {
        string cmd = _cmdInput.text;
        Debug.Log(cmd);
        DeveloperConsole.Instance.ParseCommand(cmd);
        _cmdInput.text = "";
        SelectInputField();
        _viewCurrentHistoryIndex = -1;

        Util.WaitNextFrame(this, Util_ScrollDown);

    }

    private void PrintHelp()
    {
        Dictionary<string, CommandData> commandList = DeveloperConsole.Instance.GetListOfCommands();

        Debug.Log("------- HELP COMMAND LIST -------");

        foreach (KeyValuePair<string, CommandData> command in commandList)
        {
            Debug.Log($"  {command.Key} - {command.Value.Description}");
        }
        Debug.Log("-------------------");

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
