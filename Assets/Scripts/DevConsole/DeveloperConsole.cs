using Kudoshi.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Windows;


/// <summary>
/// Author: Kudoshi 4/11/2025
/// 
/// Developer console system that allows you to input command in runtime on a developer console panel and trigger functions
/// Helpful for testing various functions when ingame
/// 
/// Can only support parameter of bool, float, int and string
/// </summary>
public class DeveloperConsole : Singleton<DeveloperConsole> 
{
    public static int MAX_HISTORY_COMMAND_STORE = 6;


    private Dictionary<string, CommandData> _commandList = new Dictionary<string, CommandData>();
    private Dictionary<string, string[]> _instanceMethodsReference = new Dictionary<string, string[]>(); // Stores reference to what instance has what CommandData

    private List<string> _commandHistory = new List<string>();

    public List<string> CommandHistory { get => _commandHistory; }

    private void Awake()
    {
        if (DeveloperConsole.Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

    }

    /// <summary>
    /// Call this function in the class you want to hook into the dev console
    /// 
    /// It will register the public functions that has [ConsoleCmd] attribute
    /// Pass in the script component via 'this'
    /// </summary>
    /// <param name="instance">The script component you called from</param>
    /// <param name="type"></param>
    public void RegisterToConsole(object instance)
    {
        ExtractCommandFromClass(instance, instance.GetType());
    }

    /// <summary>
    /// Call this function in the class you want to unhook from the dev console
    /// Should be called when the object gets destroyed
    /// 
    /// It will unregister the commands registered from the system
    /// Pass in the script component via 'this'
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="type"></param>
    public void UnregisterToConsole(object instance)
    {
        foreach (string classType in _instanceMethodsReference.Keys)
        {
            _commandList.Remove(classType);
        }

        _instanceMethodsReference.Remove(instance.GetType().ToString());
    }

    /// <summary>
    /// Called by UIDevConsole feeding in string of command to call trigger the command function
    /// 
    /// This function will check the command to see if it exist and if the parameters match
    /// </summary>
    /// <param name="command"></param>
    public void ParseCommand(string command)
    {
        command.ToLower();

        string[] commandSplitted = command.Split(' ', 2);

        if (!_commandList.ContainsKey(commandSplitted[0]))
        {
            Debug.Log("[CONSOLE] Command not found");
            return; 
        }

        try
        {
            CommandData commandData = _commandList[commandSplitted[0]];

            object[] parameters = null;
            bool parametersOkay = true;

            if (commandSplitted.Length > 1)
            {
                parametersOkay = ParseParameters(commandData.Method, commandSplitted[1], out parameters);
            }

            if (parametersOkay)
            {
                commandData.Method.Invoke(commandData.Instance, parameters);
                _commandHistory.Add(command);
            }
            else
            {
                throw new Exception("Invalid Parameters");
            }
        }
        catch (Exception e)
        {
            Debug.Log("[CONSOLE] Invalid command: " + e.Message);
        }
    }



    /// <summary>
    /// Called by UI Dev Console to get a list of commands
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, CommandData> GetListOfCommands()
    {
        return _commandList;
    }

    public string GetNearestCommand(string input, out string[] parameters)
    {
        parameters = null;

        string inputLower = input.ToLower();
        string _nearestCommand = _commandList.Keys.ToList()
            .Select(m => m.ToLower())
            .FirstOrDefault(name => name.StartsWith(inputLower));

        if (_nearestCommand != null)
            parameters = _commandList[_nearestCommand].Method.GetParameters().Select(p => p.Name).ToArray();

        return _nearestCommand;
    }

    #region Parsing of commands
    /// <summary>
    /// Checks the class for [ConsoleCmd] attribute and registers it into the system
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="type"></param>
    private void ExtractCommandFromClass(object instance, Type type)
    {
        if (_instanceMethodsReference.ContainsKey(type.ToString()))
        {
            Debug.LogWarning($"[CONSOLE] Class {type.ToString()} already subscribed to console.");
            return;
        }

        List<string> newCommandList = new List<string>();

        foreach (MethodInfo method in type.GetMethods())
        {
            ConsoleCmd attribute = method.GetCustomAttribute<ConsoleCmd>();

            if (attribute == null)
                continue;

            if (_commandList.ContainsKey(method.Name.ToLower()))
            {
                Debug.LogWarning($"[CONSOLE] Command {method.Name.ToLower()} already exists.");
                continue;
            }

            _commandList.Add(method.Name.ToLower(), new CommandData(instance, method, type, attribute.Description));
            newCommandList.Add(method.Name.ToLower());
        }

        _instanceMethodsReference.Add(type.ToString(), newCommandList.ToArray());
    }

    private bool ParseParameters(MethodInfo method, string parameterString, out object[] outParameters)
    {
        outParameters = null;

        ParameterInfo[] originalMethodParams = method.GetParameters();

        if (originalMethodParams.Length == 0)
        {
            return false;
        }

        List<string> extractedParameters = ExtractParametersFromString(parameterString);

        // Check if number of provided parameters mataches the method's signature
        if (originalMethodParams.Length != extractedParameters.Count)
        {
            return false;
        }

        outParameters = new object[originalMethodParams.Length];

        for (int i = 0; i < originalMethodParams.Length; i++)
        {
            object convertedParam = ConvertParameter(originalMethodParams[i], extractedParameters[i]);

            if (convertedParam == null) return false;

            outParameters[i] = convertedParam;
        }

        return true;
    }

    /// <summary>
    /// Gets a parameter string and separates out all the parameters taking into account quotation marks
    /// </summary>
    /// <param name="parameterString"></param>
    /// <returns></returns>
    private List<string> ExtractParametersFromString(string parameterString)
    {
        List<string> paramValues = new List<string>();
        bool inQuotes = false;
        StringBuilder stringBuilder = new StringBuilder();

        // Go through every character checking for inquotes and separates all the parameters out
        for (int i = 0; i < parameterString.Length; i++)
        {
            char currentChar = parameterString[i];

            // If see quotes symbol, toggle inQuotes
            if (currentChar == '"')
            {
                inQuotes = !inQuotes;

                // If ending of quotation
                if (!inQuotes)
                {
                    paramValues.Add(stringBuilder.ToString());
                    stringBuilder.Clear();
                }
            }
            // If it is not quotation string, then split by empty space as usual
            else if (currentChar == ' ' && !inQuotes)
            {
                if (stringBuilder.Length > 0)
                {
                    paramValues.Add(stringBuilder.ToString());
                    stringBuilder.Clear();
                }
            }
            else
            {
                stringBuilder.Append(currentChar);
            }

        }

        if (stringBuilder.Length > 0)
        {
            paramValues.Add(stringBuilder.ToString());
        }

        return paramValues;
    }

    // Function to parse the string and convert it to the appropriate object type
    private object ConvertParameter(ParameterInfo originalParameter, string givenParameterString)
    {
        Type parameterType = originalParameter.ParameterType;

        try
        {
            if (parameterType == typeof(Vector3))
            {
                string[] vec3 = givenParameterString.Split(',');
                return new Vector3(float.Parse(vec3[0]), float.Parse(vec3[1]), float.Parse(vec3[2]));
            }
            else if (parameterType == typeof(Vector2))
            {
                string[] vec2 = givenParameterString.Split(',');
                return new Vector2(float.Parse(vec2[0]), float.Parse(vec2[1]));
            }
            else if (parameterType.IsEnum)
            {
                return Enum.Parse(parameterType, givenParameterString, true);
            }
            // Blanket parse to cover remaining other types
            else
            {
                return Convert.ChangeType(givenParameterString, parameterType);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error parsing parameter: {givenParameterString} | {ex.Message}");

            return null;
        }
    }

    #endregion

    #region Command History

    /// <summary>
    /// Index 1 
    /// </summary>
    /// <param name="index"></param>
    public string GetHistoryAtIndex(int index)
    {
        int actualIdx = MAX_HISTORY_COMMAND_STORE -(MAX_HISTORY_COMMAND_STORE - _commandHistory.Count) - index - 1;

        return _commandHistory[actualIdx];
    }

    private void AddHistory(string command)
    {
        _commandHistory.Add(command);

        if (_commandHistory.Count > MAX_HISTORY_COMMAND_STORE)
        {
            _commandHistory.RemoveAt(0);
        }
    }

    #endregion
}

public struct CommandData
{
    public object Instance;
    public MethodInfo Method;
    public Type Type;
    public string Description;

    public CommandData(object instance, MethodInfo method, Type type, string description)
    {
        Instance = instance;
        Method = method;
        Type = type;
        Description = description;
    }
}