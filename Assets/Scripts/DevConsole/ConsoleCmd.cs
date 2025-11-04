using System;

/// <summary>
/// Attach this attribute to the functions that you want to register to the developer console
/// You can give it a description as well that will show up in the help list in the console panel
/// 
/// Example: [ConsoleCmd("Example description of function")]
/// 
/// Ensure that function is public
/// </summary>

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ConsoleCmd : Attribute
{
    public string Description;

    public ConsoleCmd(string description)
    {
        Description = description;
    }

    public ConsoleCmd()
    {
    }
}