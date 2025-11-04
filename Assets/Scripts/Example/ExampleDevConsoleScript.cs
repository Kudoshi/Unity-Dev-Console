
using UnityEngine;

public class ExampleDevConsoleScript : MonoBehaviour
{
    private void OnEnable()
    {
        DeveloperConsole.Instance.RegisterToConsole(this);
    }

    private void OnDisable()
    {
        DeveloperConsole.Instance.UnregisterToConsole(this);
    }

    [ConsoleCmd]
    public void TestString(string str)
    {
        Debug.Log(str);
    }

    [ConsoleCmd]
    public void TestTwoString(string str1, string str2)
    {
        Debug.Log(str1 + "|" + str2);
    }

    [ConsoleCmd]
    public void TestBool(bool b)
    { Debug.Log(b); }

    [ConsoleCmd]
    public void TestFunction(string str, int number)
    {
        Debug.Log(str + number);
    }

    [ConsoleCmd("Calculates 4 number")]
    public void TestCalculate(int n1, int n2, int n3, int n4)
    {
        int sum = n1+ n2 + n3 + n4;

        Debug.Log(sum);
    }

    [ConsoleCmd]
    public void TestDivide(float n1, float n2)
    {
        Debug.Log(n1 / n2);
    }

    [ConsoleCmd("Debug log enums")]
    public void TestEnum(ExampleEnum enm)
    {
        Debug.Log(enm);
    }

    [ConsoleCmd("Test Vec3")]
    public void TestVector3(Vector3 vec)
    {
        Debug.Log(vec);
    }
}