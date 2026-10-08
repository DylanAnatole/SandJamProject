using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // Converts the original game's serialized data (copied to OriginalReference/, outside Assets)
    // into runtime data: OriginalConfig JSON and SpriteSequence flip-books for the tutorial "gifs".
    public static class OriginalDataImporter
    {
        const string Reference = "OriginalReference";
        const string ConfigOut = "Assets/SandJam/Resources/OriginalConfig";
        const string GifOut = "Assets/SandJam/Resources/OriginalGifs";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [Serializable] sealed class Colors { public OriginalConfig.ColorEntry[] colors; }
        [Serializable] sealed class Intros { public OriginalConfig.Intro[] features, boosters; }
        [Serializable] sealed class Difficulty { public int[] easy, hard; }

        [MenuItem("Sand Jam/Original data/Import configs and tutorial gifs")]
        public static string Import()
        {
            Directory.CreateDirectory(ConfigOut); Directory.CreateDirectory(GifOut);
            var clipNames = ImportGifs(out int gifCount);
            var controllerToClip = ControllerClips(clipNames);
            Write("colors", new Colors { colors = ParseColors() });
            Write("intros", new Intros
            {
                features = ParseIntros("NewFeatureConfig.asset", "NewFeatureList", "NewFeatureIndex", "NewFeatureShowLevel", "FeatureInfo", "NewFeatureGifAnimation", null, controllerToClip),
                boosters = ParseIntros("NewBoosterConfig.asset", "NewBoosterList", "NewBoosterIndex", "NewBoosterShowLevel", "BoosterInfo", "NewBoosterGifAnimation", "BoosterType", controllerToClip)
            });
            var levels = File.ReadAllText(Path.Combine(Reference, "MonoBehaviour/EasyHardLevelConfig.asset"));
            Write("difficulty", new Difficulty { easy = HexInts(levels, "EasyLevels"), hard = HexInts(levels, "HardLevels") });
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            string summary = "Imported " + gifCount + " gif sequences, colours, feature/booster intros and difficulty tags";
            Debug.Log(summary);
            return summary;
        }

        static void Write(string name, object data)
        {
            string path = ConfigOut + "/" + name + ".json";
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset(path);
        }

        static string Guid(string metaPath) { var m = Regex.Match(File.ReadAllText(metaPath), @"guid: (\w+)"); return m.Success ? m.Groups[1].Value : null; }

        // clip guid -> clip name, creating a SpriteSequence for every clip that swaps sprites.
        static Dictionary<string, string> ImportGifs(out int count)
        {
            count = 0;
            var names = new Dictionary<string, string>();
            foreach (var file in Directory.GetFiles(Path.Combine(Reference, "AnimationClip"), "*.anim"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                names[Guid(file + ".meta")] = name;
                string text = File.ReadAllText(file);
                var keys = Regex.Matches(text, @"- time: ([\d.eE+-]+)\s+value: \{fileID: 21300000, guid: (\w+)");
                if (keys.Count == 0) continue;
                var frames = new List<Sprite>(); var times = new List<float>();
                foreach (Match k in keys)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(k.Groups[2].Value));
                    if (!sprite) continue;
                    frames.Add(sprite); times.Add(float.Parse(k.Groups[1].Value, Inv));
                }
                if (frames.Count == 0) { Debug.LogWarning("No sprites resolved for " + name); continue; }
                var stop = Regex.Match(text, @"m_StopTime: ([\d.eE+-]+)");
                var seq = ScriptableObject.CreateInstance<SpriteSequence>();
                seq.Frames = frames.ToArray(); seq.Times = times.ToArray();
                seq.Length = stop.Success ? float.Parse(stop.Groups[1].Value, Inv) : times.Last() + .05f;
                seq.Loop = !Regex.IsMatch(text, @"m_LoopTime: 0");
                string path = GifOut + "/" + name + ".asset";
                AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(seq, path);
                count++;
            }
            return names;
        }

        // Configs point at AnimatorControllers (fileID 9100000); each wraps one clip.
        static Dictionary<string, string> ControllerClips(Dictionary<string, string> clipNames)
        {
            var map = new Dictionary<string, string>();
            foreach (var file in Directory.GetFiles(Path.Combine(Reference, "AnimatorController"), "*.controller"))
            {
                var motion = Regex.Match(File.ReadAllText(file), @"m_Motion: \{fileID: 7400000, guid: (\w+)");
                string clip;
                if (motion.Success && clipNames.TryGetValue(motion.Groups[1].Value, out clip)) map[Guid(file + ".meta")] = clip;
            }
            return map;
        }

        static OriginalConfig.ColorEntry[] ParseColors()
        {
            var text = File.ReadAllText(Path.Combine(Reference, "MonoBehaviour/ColorDataSO.asset"));
            var list = new List<OriginalConfig.ColorEntry>();
            foreach (var block in Regex.Split(text, @"\n  - ColorType: ").Skip(1))
            {
                var entry = new OriginalConfig.ColorEntry { type = int.Parse(Regex.Match(block, @"^-?\d+").Value, Inv) };
                entry.init = ColorField(block, "InitColor"); entry.final = ColorField(block, "FinalColor");
                entry.dark = ColorField(block, "FinalColorDark"); entry.render = ColorField(block, "RenderColor");
                list.Add(entry);
            }
            return list.ToArray();
        }

        static Color ColorField(string block, string name)
        {
            var m = Regex.Match(block, name + @": \{r: ([\d.eE+-]+), g: ([\d.eE+-]+), b: ([\d.eE+-]+), a: ([\d.eE+-]+)\}");
            return m.Success ? new Color(F(m, 1), F(m, 2), F(m, 3), F(m, 4)) : Color.clear;
        }
        static float F(Match m, int i) { return float.Parse(m.Groups[i].Value, Inv); }

        static OriginalConfig.Intro[] ParseIntros(string file, string list, string indexKey, string levelKey, string infoKey, string gifKey, string typeKey, Dictionary<string, string> controllers)
        {
            var text = File.ReadAllText(Path.Combine(Reference, "MonoBehaviour", file));
            var result = new List<OriginalConfig.Intro>();
            foreach (var block in Regex.Split(text, @"\n  - " + indexKey + ": ").Skip(1))
            {
                var intro = new OriginalConfig.Intro { index = int.Parse(Regex.Match(block, @"^\d+").Value, Inv) };
                intro.showLevel = int.Parse(Regex.Match(block, levelKey + @": (\d+)").Groups[1].Value, Inv);
                intro.info = Regex.Match(block, infoKey + @": (.*)").Groups[1].Value.Trim();
                if (typeKey != null) intro.type = int.Parse(Regex.Match(block, typeKey + @": (\d+)").Groups[1].Value, Inv);
                var gif = Regex.Match(block, gifKey + @": \{fileID: \d+, guid: (\w+)");
                string clip; intro.gif = gif.Success && controllers.TryGetValue(gif.Groups[1].Value, out clip) ? clip : "";
                result.Add(intro);
            }
            return result.ToArray();
        }

        // Unity serialises int lists in this asset as little-endian hex.
        static int[] HexInts(string text, string key)
        {
            var hex = Regex.Match(text, key + @": ([0-9a-fA-F]*)").Groups[1].Value;
            var values = new List<int>();
            for (int i = 0; i + 8 <= hex.Length; i += 8)
                values.Add(BitConverter.ToInt32(Enumerable.Range(0, 4).Select(b => Convert.ToByte(hex.Substring(i + b * 2, 2), 16)).ToArray(), 0));
            return values.ToArray();
        }
    }
}
