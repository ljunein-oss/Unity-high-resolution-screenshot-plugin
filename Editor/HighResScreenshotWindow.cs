// High-Res Screenshot — 类似 UE 的 HighResShot（独立小插件，全渲染管线通用，窗口内可切中英文）
//
//   工具 ▸ 高分辨率截图
//   Tools ▸ High-Res Screenshot
//
// 做什么：
//   * 按倍数或指定分辨率出图（1x ~ 8x，或直接填 3840x2160 / 7680x4320）
//   * 两种取景：游戏相机（和 Game 视图一致）或场景视图机位
//   * PNG 或 EXR（EXR 保留 HDR 亮度，方便后期）
//   * 文件名前缀 + 三位递增，存到你指定的文件夹，拍完能在窗口里预览
//   * 窗口右上角可以切中文 / English
//
// 注意：
//   * 相机离屏渲染不含 Screen Space Overlay 的 UI。要连 UI 一起截，用 Unity 自带的截图，
//     或者把 UI 改成 Screen Space Camera。
//   * 分辨率上限受 SystemInfo.maxTextureSize 限制，超了会被夹住并提示。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class HighResScreenshotWindow : EditorWindow
{
    const string KSource = "HighResShot.Source";
    const string KCamera = "HighResShot.Camera";
    const string KMode = "HighResShot.Mode";
    const string KMultiplier = "HighResShot.Multiplier";
    const string KWidth = "HighResShot.Width";
    const string KHeight = "HighResShot.Height";
    const string KFolder = "HighResShot.Folder";
    const string KPrefix = "HighResShot.Prefix";
    const string KFormat = "HighResShot.Format";
    const string KPost = "HighResShot.Post";
    const string KOpen = "HighResShot.OpenAfter";
    const string KLang = "HighResShot.Chinese";

    static readonly int[] Multipliers = { 1, 2, 3, 4, 6, 8 };
    static readonly string[] MultiplierLabels = { "1x", "2x", "3x", "4x", "6x", "8x" };

    // ------------------------------------------------------------------ 语言
    static bool Chinese
    {
        get { return EditorPrefs.GetBool(KLang, true); }
        set { EditorPrefs.SetBool(KLang, value); }
    }

    static string L(string cn, string en) { return Chinese ? cn : en; }
    static string WindowTitle { get { return L("高分辨率截图", "High-Res Screenshot"); } }

    // ------------------------------------------------------------------ 设置
    static string DefaultFolder
    {
        get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots"); }
    }

    static string Folder
    {
        get { return EditorPrefs.GetString(KFolder, DefaultFolder); }
        set { EditorPrefs.SetString(KFolder, value); }
    }
    static string Prefix
    {
        get { return EditorPrefs.GetString(KPrefix, "shot_"); }
        set { EditorPrefs.SetString(KPrefix, value); }
    }
    static int SourceIndex { get { return EditorPrefs.GetInt(KSource, 0); } set { EditorPrefs.SetInt(KSource, value); } }
    static int ModeIndex { get { return EditorPrefs.GetInt(KMode, 0); } set { EditorPrefs.SetInt(KMode, value); } }
    static int MultiplierIndex { get { return Mathf.Clamp(EditorPrefs.GetInt(KMultiplier, 1), 0, Multipliers.Length - 1); } set { EditorPrefs.SetInt(KMultiplier, value); } }
    static int Width { get { return Mathf.Max(16, EditorPrefs.GetInt(KWidth, 3840)); } set { EditorPrefs.SetInt(KWidth, value); } }
    static int Height { get { return Mathf.Max(16, EditorPrefs.GetInt(KHeight, 2160)); } set { EditorPrefs.SetInt(KHeight, value); } }
    static int FormatIndex { get { return EditorPrefs.GetInt(KFormat, 0); } set { EditorPrefs.SetInt(KFormat, value); } }
    static bool IncludePost { get { return EditorPrefs.GetBool(KPost, true); } set { EditorPrefs.SetBool(KPost, value); } }
    static bool OpenAfter { get { return EditorPrefs.GetBool(KOpen, false); } set { EditorPrefs.SetBool(KOpen, value); } }
    static string CameraName { get { return EditorPrefs.GetString(KCamera, ""); } set { EditorPrefs.SetString(KCamera, value); } }

    List<Camera> _cameras = new List<Camera>();
    Vector2 _scroll;
    string _status = "";
    Texture2D _preview;
    string _lastPath = "";

    [MenuItem("Tools/High-Res Screenshot", false, 47)]
    [MenuItem("工具/高分辨率截图", false, 47)]
    static void Open()
    {
        var w = GetWindow<HighResScreenshotWindow>(WindowTitle);
        w.titleContent = new GUIContent(WindowTitle);
        w.minSize = new Vector2(470f, 540f);
    }

    void OnEnable() { RefreshCameras(); titleContent = new GUIContent(WindowTitle); }
    void OnDisable() { if (_preview != null) { DestroyImmediate(_preview); _preview = null; } }

    void RefreshCameras()
    {
        _cameras = UnityEngine.Object.FindObjectsOfType<Camera>()
            .Where(c => c != null && c.gameObject.activeInHierarchy && c.targetTexture == null
                        && c.cameraType != CameraType.Preview && c.cameraType != CameraType.Reflection)
            .OrderBy(c => c.CompareTag("MainCamera") ? 0 : 1).ThenBy(c => c.name)
            .ToList();
    }

    // ------------------------------------------------------------------ 界面
    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            int lang = GUILayout.Toolbar(Chinese ? 0 : 1, new[] { "中文", "English" },
                EditorStyles.miniButton, GUILayout.Width(130));
            if ((lang == 0) != Chinese)
            {
                Chinese = lang == 0;
                titleContent = new GUIContent(WindowTitle);
                Repaint();
            }
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(L("① 拍哪里", "① What to shoot"), EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            SourceIndex = EditorGUILayout.Popup(L("取景来源", "Source"), SourceIndex, new[]
            {
                L("游戏相机（和 Game 视图一致）", "Game camera (matches the Game view)"),
                L("场景视图机位（当前 Scene 窗口角度）", "Scene view camera (current angle)")
            });

            if (SourceIndex == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(L("刷新相机", "Refresh"), EditorStyles.miniButton, GUILayout.Width(80))) RefreshCameras();
                    var names = _cameras.Select(c => c.name + (c.CompareTag("MainCamera") ? "  (MainCamera)" : "")).ToArray();
                    if (names.Length == 0)
                    {
                        EditorGUILayout.LabelField(L("场景里没有可用相机", "No usable camera in the scene"));
                    }
                    else
                    {
                        int cur = Mathf.Max(0, _cameras.FindIndex(c => c.name == CameraName));
                        int ni = EditorGUILayout.Popup(cur, names);
                        if (ni != cur || string.IsNullOrEmpty(CameraName)) CameraName = _cameras[ni].name;
                    }
                }
                EditorGUILayout.LabelField(L("后处理跟随这台相机自己的设置（相机上没勾 Post Processing 就不会有泛光/雾）。",
                                             "Post processing follows this camera's own setting."), EditorStyles.miniLabel);
            }
            else
            {
                var sv = SceneView.lastActiveSceneView;
                EditorGUILayout.LabelField(sv != null && sv.camera != null
                    ? L(string.Format("当前场景视图视口 {0} × {1}", sv.camera.pixelWidth, sv.camera.pixelHeight),
                        string.Format("Scene view viewport {0} × {1}", sv.camera.pixelWidth, sv.camera.pixelHeight))
                    : L("没有打开的 Scene 窗口", "No Scene window open"), EditorStyles.miniLabel);
                IncludePost = EditorGUILayout.ToggleLeft(
                    L("包含后处理（给临时相机补一个 URP 相机数据并打开）",
                      "Include post processing (adds URP camera data to the temporary camera)"), IncludePost);
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(L("② 多大", "② Resolution"), EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            ModeIndex = EditorGUILayout.Popup(L("分辨率方式", "Mode"), ModeIndex, new[]
            {
                L("按视图倍数（UE HighResShot 那种）", "Multiplier of the view (UE HighResShot style)"),
                L("直接指定宽高", "Explicit width and height")
            });

            int vw = 1920, vh = 1080;
            if (SourceIndex == 1 && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                vw = Mathf.Max(16, SceneView.lastActiveSceneView.camera.pixelWidth);
                vh = Mathf.Max(16, SceneView.lastActiveSceneView.camera.pixelHeight);
            }
            else if (_cameras.Count > 0 && _cameras[0].pixelWidth > 0)
            {
                vw = _cameras[0].pixelWidth; vh = _cameras[0].pixelHeight;
            }

            int w, h;
            if (ModeIndex == 0)
            {
                MultiplierIndex = EditorGUILayout.IntPopup(L("倍数", "Multiplier"), MultiplierIndex, MultiplierLabels, Multipliers);
                w = vw * Multipliers[MultiplierIndex];
                h = vh * Multipliers[MultiplierIndex];
                EditorGUILayout.LabelField(L(string.Format("基准 {0} × {1}  →  出图 {2} × {3}", vw, vh, w, h),
                                             string.Format("Base {0} × {1}  →  output {2} × {3}", vw, vh, w, h)), EditorStyles.miniLabel);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(L("宽 × 高", "Width × Height"), GUILayout.Width(EditorGUIUtility.labelWidth));
                    Width = EditorGUILayout.IntField(Width, GUILayout.Width(80));
                    EditorGUILayout.LabelField("×", GUILayout.Width(12));
                    Height = EditorGUILayout.IntField(Height, GUILayout.Width(80));
                    if (GUILayout.Button("1920×1080", EditorStyles.miniButton, GUILayout.Width(86))) { Width = 1920; Height = 1080; }
                    if (GUILayout.Button("4K", EditorStyles.miniButton, GUILayout.Width(50))) { Width = 3840; Height = 2160; }
                    if (GUILayout.Button("8K", EditorStyles.miniButton, GUILayout.Width(50))) { Width = 7680; Height = 4320; }
                }
                w = Width; h = Height;
            }

            FormatIndex = EditorGUILayout.Popup(L("格式", "Format"), FormatIndex, new[]
            {
                L("PNG（普通截图）", "PNG"),
                L("EXR（保留 HDR 亮度，做后期用）", "EXR (keeps HDR values)")
            });

            int max = Mathf.Max(1024, SystemInfo.maxTextureSize);
            if (w > max || h > max)
                EditorGUILayout.HelpBox(L(
                    string.Format("这张卡最大纹理边长是 {0}，比它大的分辨率会被夹到 {0}。（要真正超过就得做分块渲染，暂时没做）", max),
                    string.Format("Max texture size on this GPU is {0}. Anything larger gets clamped (no tiled rendering yet).", max)),
                    MessageType.Warning);

            long pixels = (long)w * h;
            if (pixels > 33000000L)
                EditorGUILayout.HelpBox(L(
                    string.Format("约 {0:0.#} 亿像素，显存吃紧的卡可能失败或很慢。", pixels / 1e8),
                    string.Format("About {0:0.#} hundred million pixels. Weak GPUs may fail or take a while.", pixels / 1e8)),
                    MessageType.Info);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(L("③ 存哪", "③ Output"), EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(L("文件夹", "Folder"), GUILayout.Width(52));
                EditorGUILayout.SelectableLabel(Folder, EditorStyles.textField, GUILayout.Height(18));
                if (GUILayout.Button(L("选择…", "Browse"), GUILayout.Width(60)))
                {
                    string picked = EditorUtility.OpenFolderPanel(L("选择截图输出文件夹", "Choose output folder"), Folder, "");
                    if (!string.IsNullOrEmpty(picked)) Folder = picked;
                }
                if (GUILayout.Button(L("打开", "Open"), GUILayout.Width(50)))
                {
                    Directory.CreateDirectory(Folder);
                    EditorUtility.RevealInFinder(Folder);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(L("文件名", "Filename"), GUILayout.Width(52));
                Prefix = EditorGUILayout.TextField(Prefix);
                int next; NextPath(Folder, Prefix, out next);
                EditorGUILayout.LabelField(string.Format("{0}{1:D3}.{2}", Prefix, next, FormatIndex == 0 ? "png" : "exr"),
                    EditorStyles.miniLabel, GUILayout.Width(150));
            }
            OpenAfter = EditorGUILayout.ToggleLeft(L("拍完自动打开文件夹", "Reveal in explorer after shooting"), OpenAfter);
        }

        EditorGUILayout.Space(8);
        var old = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.65f, 0.9f, 0.65f);
        if (GUILayout.Button(L("拍摄高分辨率截图", "Take high-res screenshot"), GUILayout.Height(42))) Capture();
        GUI.backgroundColor = old;

        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);

        if (_preview != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(L("上一张：", "Last shot: ") + Path.GetFileName(_lastPath), EditorStyles.miniLabel);
            float pw = Mathf.Min(position.width - 40f, 420f);
            var rect = GUILayoutUtility.GetRect(pw, pw * _preview.height / Mathf.Max(1f, _preview.width));
            EditorGUI.DrawPreviewTexture(rect, _preview, null, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------------ 拍摄
    void Capture()
    {
        int max = Mathf.Max(1024, SystemInfo.maxTextureSize);
        int w, h;
        Camera cam = null;

        if (SourceIndex == 0)
        {
            cam = _cameras.FirstOrDefault(c => c.name == CameraName) ?? _cameras.FirstOrDefault();
            if (cam == null) { _status = L("没有可用相机，先打开一个场景并放一台相机。", "No usable camera. Open a scene with a camera first."); return; }
            int vw = cam.pixelWidth > 0 ? cam.pixelWidth : 1920;
            int vh = cam.pixelHeight > 0 ? cam.pixelHeight : 1080;
            if (ModeIndex == 0) { int m = Multipliers[MultiplierIndex]; w = vw * m; h = vh * m; }
            else { w = Width; h = Height; }
        }
        else
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.camera == null) { _status = L("没有打开的 Scene 窗口。", "No Scene window open."); return; }
            int vw = Mathf.Max(16, sv.camera.pixelWidth), vh = Mathf.Max(16, sv.camera.pixelHeight);
            if (ModeIndex == 0) { int m = Multipliers[MultiplierIndex]; w = vw * m; h = vh * m; }
            else { w = Width; h = Height; }
        }

        bool clamped = false;
        if (w > max) { w = max; clamped = true; }
        if (h > max) { h = max; clamped = true; }
        w = Mathf.Max(16, w); h = Mathf.Max(16, h);

        try { Directory.CreateDirectory(Folder); }
        catch (Exception e) { _status = L("创建输出文件夹失败：", "Could not create the output folder: ") + e.Message; return; }

        int index;
        string path = NextPath(Folder, Prefix, out index);
        bool exr = FormatIndex == 1;

        var rt = new RenderTexture(w, h, 24,
            exr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32,
            exr ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
        rt.antiAliasing = 4;
        rt.Create();

        GameObject tempGo = null;
        RenderTexture prevTarget = null;
        Camera prevCam = null;
        bool prevEnabled = true;

        try
        {
            if (SourceIndex == 0)
            {
                // 直接用游戏相机拍：构图和 Game 视图完全一致
                prevCam = cam;
                prevTarget = cam.targetTexture;
                prevEnabled = cam.enabled;
                cam.targetTexture = rt;
                cam.Render();
            }
            else
            {
                // 场景视图：临时相机复制它的机位与投影
                var sv = SceneView.lastActiveSceneView;
                tempGo = new GameObject("~HighResShotEditorCamera") { hideFlags = HideFlags.HideAndDontSave };
                var tmp = tempGo.AddComponent<Camera>();
                tmp.CopyFrom(sv.camera);
                tmp.cameraType = CameraType.Game;
                tmp.targetTexture = rt;
                tmp.enabled = false;
                SetCameraPostProcessing(tmp, IncludePost);
                tmp.Render();
            }

            Save(rt, path, exr);
        }
        catch (Exception e)
        {
            _status = L("渲染失败：", "Render failed: ") + e.Message;
            Debug.LogException(e);
            return;
        }
        finally
        {
            if (prevCam != null) { prevCam.targetTexture = prevTarget; prevCam.enabled = prevEnabled; }
            if (tempGo != null) DestroyImmediate(tempGo);
            rt.Release();
            DestroyImmediate(rt);
        }

        if (_preview != null) DestroyImmediate(_preview);
        _preview = new Texture2D(w, h, TextureFormat.RGB24, false);
        try { _preview.LoadImage(File.ReadAllBytes(path)); } catch { }
        _lastPath = path;

        _status = L(string.Format("已保存 {0} × {1} 到 {2}{3}", w, h, path, clamped ? "（分辨率被硬件上限夹过）" : ""),
                    string.Format("Saved {0} × {1} to {2}{3}", w, h, path, clamped ? " (clamped to the GPU limit)" : ""));
        Debug.Log("[HighResShot] " + path);
        if (OpenAfter) EditorUtility.RevealInFinder(path);
        Repaint();
    }

    // 通过反射设置 URP 的相机后处理开关：这样这个包在没有 URP 的工程里也能编译。
    static void SetCameraPostProcessing(Camera cam, bool on)
    {
        var type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData"))
                    .FirstOrDefault(t => t != null);
        if (type == null) return;

        var comp = cam.GetComponent(type);
        if (comp == null)
        {
            if (!on) return;
            comp = cam.gameObject.AddComponent(type);
        }

        var so = new SerializedObject(comp);
        var p = so.FindProperty("m_RenderPostProcessing");
        if (p == null) return;
        p.boolValue = on;
        so.ApplyModifiedProperties();
    }

    static void Save(RenderTexture rt, string path, bool exr)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = exr
            ? new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false, true)
            : new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false, false);
        try
        {
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            byte[] bytes = exr
                ? ImageConversion.EncodeToEXR(tex, Texture2D.EXRFlags.OutputAsFloat)
                : ImageConversion.EncodeToPNG(tex);
            File.WriteAllBytes(path, bytes);
        }
        finally
        {
            RenderTexture.active = prev;
            UnityEngine.Object.DestroyImmediate(tex);
        }

        string full = Path.GetFullPath(path).Replace('\\', '/');
        string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
        if (full.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
            AssetDatabase.Refresh();
    }

    static string NextPath(string folder, string prefix, out int index)
    {
        if (string.IsNullOrEmpty(prefix)) prefix = "shot_";
        index = 1;
        if (Directory.Exists(folder))
        {
            foreach (var f in Directory.GetFiles(folder, prefix + "*"))
            {
                string name = Path.GetFileNameWithoutExtension(f);
                if (name.Length <= prefix.Length) continue;
                if (int.TryParse(name.Substring(prefix.Length), out int n) && n >= index) index = n + 1;
            }
        }
        string ext = FormatIndex == 1 ? ".exr" : ".png";
        return Path.Combine(folder, prefix + index.ToString("D3") + ext);
    }
}
