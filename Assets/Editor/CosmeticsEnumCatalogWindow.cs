using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CosmeticsEnumCatalogWindow : EditorWindow
{
    private const string VfxEnumPath = "Assets/Scripts/Enumes/VfxType.cs";
    private const string SfxEnumPath = "Assets/Scripts/Enumes/SfxType.cs";

    private string newVfxTypeName = string.Empty;
    private string newSfxTypeName = string.Empty;
    private Vector2 scrollPosition;
    private string statusMessage = string.Empty;
    private MessageType statusMessageType = MessageType.Info;

    [MenuItem("Tools/Game Data/Cosmetics Enum Catalog")]
    public static void Open()
    {
        GetWindow<CosmeticsEnumCatalogWindow>("Cosmetics Enum Catalog");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Cosmetics Enum Catalog", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "מוסיף ערכים חדשים ל-VfxType ול-SfxType מתוך ה-Editor, בלי לעבור ידנית על קבצי enum.",
            MessageType.Info);

        DrawEnumAdder(
            "VFX Types",
            ref newVfxTypeName,
            nameof(VfxType),
            Enum.GetNames(typeof(VfxType)),
            () => AppendEnumValue(VfxEnumPath, nameof(VfxType), newVfxTypeName, ref newVfxTypeName));

        EditorGUILayout.Space(12f);

        DrawEnumAdder(
            "SFX Types",
            ref newSfxTypeName,
            nameof(SfxType),
            Enum.GetNames(typeof(SfxType)),
            () => AppendEnumValue(SfxEnumPath, nameof(SfxType), newSfxTypeName, ref newSfxTypeName));

        EditorGUILayout.Space(12f);

        if (!string.IsNullOrWhiteSpace(statusMessage))
            EditorGUILayout.HelpBox(statusMessage, statusMessageType);
    }

    private void DrawEnumAdder(
        string title,
        ref string inputValue,
        string enumName,
        string[] currentValues,
        Action addAction)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        inputValue = EditorGUILayout.TextField("New Value", inputValue);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button($"Add To {enumName}", GUILayout.Width(150f)))
                addAction();
        }

        EditorGUILayout.LabelField("Current Values", EditorStyles.miniBoldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(90f));
        EditorGUILayout.TextArea(string.Join(Environment.NewLine, currentValues), GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void AppendEnumValue(
        string assetRelativePath,
        string enumName,
        string rawValue,
        ref string inputValue)
    {
        string sanitizedValue = SanitizeEnumIdentifier(rawValue);
        if (string.IsNullOrWhiteSpace(sanitizedValue))
        {
            SetStatus("צריך להזין שם תקין לערך החדש.", MessageType.Warning);
            return;
        }

        string assetFullPath = Path.GetFullPath(assetRelativePath);
        if (!File.Exists(assetFullPath))
        {
            SetStatus($"לא נמצא קובץ enum בנתיב: {assetRelativePath}", MessageType.Error);
            return;
        }

        string[] lines = File.ReadAllLines(assetFullPath, Encoding.UTF8);
        int enumDeclarationIndex = Array.FindIndex(lines, line => line.Contains($"enum {enumName}"));
        if (enumDeclarationIndex < 0)
        {
            SetStatus($"לא נמצאה ההגדרה של {enumName}.", MessageType.Error);
            return;
        }

        int startBraceIndex = FindLineIndex(lines, enumDeclarationIndex, "{");
        int endBraceIndex = FindLineIndex(lines, startBraceIndex, "}");
        if (startBraceIndex < 0 || endBraceIndex < 0 || endBraceIndex <= startBraceIndex)
        {
            SetStatus($"לא הצלחתי לנתח את המבנה של {enumName}.", MessageType.Error);
            return;
        }

        bool alreadyExists = Enumerable.Range(startBraceIndex + 1, endBraceIndex - startBraceIndex - 1)
            .Select(index => lines[index].Trim().TrimEnd(','))
            .Any(value => string.Equals(value, sanitizedValue, StringComparison.Ordinal));
        if (alreadyExists)
        {
            SetStatus($"{sanitizedValue} כבר קיים ב-{enumName}.", MessageType.Warning);
            return;
        }

        int lastValueIndex = FindLastEnumValueLine(lines, startBraceIndex, endBraceIndex);
        if (lastValueIndex < 0)
        {
            SetStatus($"לא נמצא מקום תקין להוסיף ערך ל-{enumName}.", MessageType.Error);
            return;
        }

        if (!lines[lastValueIndex].TrimEnd().EndsWith(","))
            lines[lastValueIndex] = $"{lines[lastValueIndex].TrimEnd()},";

        string indent = GetIndentation(lines[lastValueIndex]);
        string newLine = $"{indent}{sanitizedValue}";

        string[] updatedLines = new string[lines.Length + 1];
        Array.Copy(lines, 0, updatedLines, 0, endBraceIndex);
        updatedLines[endBraceIndex] = newLine;
        Array.Copy(lines, endBraceIndex, updatedLines, endBraceIndex + 1, lines.Length - endBraceIndex);

        File.WriteAllLines(assetFullPath, updatedLines, Encoding.UTF8);
        AssetDatabase.Refresh();

        inputValue = string.Empty;
        SetStatus($"{sanitizedValue} נוסף בהצלחה ל-{enumName}.", MessageType.Info);
    }

    private static int FindLineIndex(string[] lines, int startIndex, string content)
    {
        for (int index = startIndex; index < lines.Length; index++)
        {
            if (lines[index].Contains(content))
                return index;
        }

        return -1;
    }

    private static int FindLastEnumValueLine(string[] lines, int startBraceIndex, int endBraceIndex)
    {
        for (int index = endBraceIndex - 1; index > startBraceIndex; index--)
        {
            string trimmed = lines[index].Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            return index;
        }

        return -1;
    }

    private static string GetIndentation(string line)
    {
        int index = 0;
        while (index < line.Length && char.IsWhiteSpace(line[index]))
            index++;

        return line.Substring(0, index);
    }

    private static string SanitizeEnumIdentifier(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        StringBuilder builder = new StringBuilder(rawValue.Length);
        for (int index = 0; index < rawValue.Length; index++)
        {
            char character = rawValue[index];
            if (char.IsLetterOrDigit(character) || character == '_')
                builder.Append(character);
        }

        string sanitized = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
            return string.Empty;

        if (char.IsDigit(sanitized[0]))
            sanitized = $"_{sanitized}";

        return sanitized;
    }

    private void SetStatus(string message, MessageType type)
    {
        statusMessage = message;
        statusMessageType = type;
        Repaint();
    }
}
