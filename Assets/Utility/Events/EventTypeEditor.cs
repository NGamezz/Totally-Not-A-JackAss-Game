using NaughtyAttributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
public class EventTypeEditor : MonoBehaviour
{
    [Header("Recompilation of the code base can be slow.")]
    
    [SerializeField] private readonly List<string> elements;

    private string GetPath()
    {
        const string scriptName = nameof(Events.EventType);
        string[] guids = AssetDatabase.FindAssets(scriptName + " t:Script");

        string path = null;
        foreach (string guid in guids)
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(scriptName + ".cs"))
            {
                break;
            }
        }
        if (path == null)
        {
            Debug.Log("Path not found.");
            return null;
        }
        return path;
    }

    private List<string> GetFileContents()
    {
        var path = GetPath();

        if (path == null)
            return null;

        List<string> contents = new();
        using (StreamReader streamReader = new(path, Encoding.UTF8))
        {
            string s = string.Empty;
            while ((s = streamReader.ReadLine()) != null)
            {
                contents.Add(s);
            }
        }

        return contents;
    }

    [Button]//Parse the file contents into the array.
    private Task GenerateElements()
    {
        if (Application.isPlaying)
            return Task.CompletedTask;

        var contents = GetFileContents();

        if (contents == null || contents.Count < 1)
            return Task.CompletedTask;

        elements.Clear();
        bool started = false;
        for (int i = 0; i < contents.Count; ++i)
        {
            var currentLine = contents[i];

            if (currentLine.Length < 1)
                continue;

            if (i - 1 > -1 && contents[i - 1][0] == '{' || currentLine[0] == '}')
            {
                started = !started;
                if (!started)
                    return Task.CompletedTask;
            }

            if (started)
            {
                if (currentLine[0] == '}')
                    break;

                if (currentLine[^1] == ',')
                    currentLine = Utility.Utility.CleanString(currentLine);

                elements.Add(currentLine);
            }
        }

        return Task.CompletedTask;
    }

    [Button] //Update the file contents.
    private void CreateTypes()
    {
        if (Application.isPlaying || this.elements.Count < 1)
            return;

        var path = GetPath();

        if (path == null)
            return;

        var elements = this.elements.ToHashSet();
        using (FileStream stream = File.Open(path, FileMode.Truncate))
        {
            using StreamWriter streamWriter = new(stream, Encoding.UTF8);

            streamWriter.WriteLine("namespace Events {");
            streamWriter.WriteLine("public enum EventType");
            streamWriter.WriteLine("{");

            var enumerator = elements.GetEnumerator();
            while (enumerator.MoveNext())
            {
                streamWriter.WriteLine(enumerator.Current + ",");
            }
            streamWriter.WriteLine("}");
            streamWriter.WriteLine("}");
        }

        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
    }
}
#endif