#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PlaytestVisualValidation
{
    const string Armed = "MIS.PlaytestVisualValidation";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static IEnumerator routine;
    static int lastFrame;
    static double deadline;
    static Report report;
    [Serializable] class Report
    {
        public string status;
        public List<string> passed = new List<string>();
        public List<string> measurements = new List<string>();
        public List<string> errors = new List<string>();
        public string failure;
    }
    static PlaytestVisualValidation() => EditorApplication.playModeStateChanged += State;

    [MenuItem("MIS/テスト/魔道鉱石のモデル参照を修復")]
    public static void RepairOre()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        const string path = "Assets/Prefab/Item/魔道鉱石.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        int repaired = 0, removed = 0;
        try
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null) continue;
                var source = PrefabUtility.GetCorrespondingObjectFromSource(filter);
                if (source == null || source.sharedMesh == null) throw new InvalidOperationException("Missing original ore mesh: " + filter.name);
                filter.sharedMesh = source.sharedMesh;
                repaired++;
            }
            // Old ProBuilder components reference a package no longer in this project.
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("[OreRepair] restored mesh references=" + repaired + "; removed missing components=" + removed);
    }

    [MenuItem("MIS/テスト/ショップ事前読み込みデータを作成")]
    public static void BuildWarmupCatalog()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        const string path = "Assets/Resources/ShopArtCache.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<ShopArtCache>(path);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<ShopArtCache>(); AssetDatabase.CreateAsset(catalog, path); }
        var layers = new[] { "HairL", "Hat", "Owner", "OwnerB", "OwnerF" };
        catalog.atlases = layers.Select(layer => AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>("Assets/UI/TakeShop/OwnerAtlases/" + layer + ".spriteatlasv2")).ToArray();
        catalog.animations = layers.Select(layer => AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/UI/TakeShop/Owner/" + layer + "/" + (layer == "Hat" ? "Hat_01_00001" : layer) + ".controller")).ToArray();
        if (catalog.atlases.Any(x => x == null) || catalog.animations.Any(x => x == null)) throw new InvalidOperationException("Missing shop art reference");
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        Debug.Log("[ShopWarmup] Catalog created with all original animation frames.");
    }

    [MenuItem("MIS/テスト/配置した実物の表示を検証")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save scene changes first.");
        SessionState.SetBool(Armed, true);
        EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity");
        EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Armed, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            report = new Report { status = "RUNNING" };
            Application.logMessageReceived += Log;
            routine = Suite(); lastFrame = -1; deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && report != null && report.status == "RUNNING")
            Finish(new Exception("Interrupted"));
    }
    static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message);
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException();
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (!routine.MoveNext()) { Check(report.errors.Count == 0, "no Unity errors"); Finish(null); }
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception ex)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        SessionState.SetBool(Armed, false);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true; Time.timeScale = 1;
        report.status = ex == null ? "PASS" : "FAIL"; report.failure = ex?.ToString();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/playtest-visual-validation.json", JsonUtility.ToJson(report, true));
        Debug.Log("[PlaytestVisualValidation] " + report.status + " / " + report.passed.Count + " checks");
        EditorApplication.isPlaying = false;
    }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); report.passed.Add(message); }
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Flags).GetValue(target);
    static IEnumerator Suite()
    {
        for (int i = 0; i < 8; i++) yield return null;
        foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
        Object.FindFirstObjectByType<RequestManager>().enabled = false;
        GameClockText.Instance.StopAllCoroutines(); GameClockText.Instance.enabled = false;
        var put = Object.FindFirstObjectByType<PutItem>(); put.enabled = false;
        Object.FindFirstObjectByType<ItemPickup>().enabled = false;
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset");
        var camera = Get<Camera>(put, "mainCamera");
        var stations = Object.FindObjectsByType<PlacementSlots>(FindObjectsSortMode.None)
            .Where(s => s.CompareTag("blacksmith") || s.CompareTag("wash") || s.CompareTag("craft")).ToArray();
        Check(stations.Length >= 3, "real arcade stations exist");
        // Check every original item asset, including all sword variants, without station scaling.
        foreach (var item in db.allItems.Where(x => x != null && x.prefab != null))
        {
            var instance = WorldItemPlacement.Spawn(item, new Vector3(1000, 1000, 1000), Quaternion.identity, null, null);
            Check(WorldItemPlacement.TryGetBounds(instance, out var bounds), item.itemName + " has a visible mesh");
            Check(bounds.size.magnitude > 0.02f && Mathf.Abs(bounds.min.y - 1000.015f) < 0.01f, item.itemName + " sits above the surface");
            if (item.itemType.Contains("武器")) Check(bounds.size.y < Mathf.Max(bounds.size.x, bounds.size.z) * 0.6f, item.itemName + " lies flat");
            report.measurements.Add(item.itemName + " size=" + bounds.size.ToString("F3"));
            Object.Destroy(instance);
            yield return null;
        }
        foreach (string stationTag in new[] { "blacksmith", "wash" })
        {
            var station = stations.First(s => s.CompareTag(stationTag));
            var anchor = station.GetSlotTransform(0);
            var ore = db.allItems.Single(x => x.itemID == 3);
            var sword = db.allItems.Single(x => x.itemID == (stationTag == "wash" ? 40 : 21));
            station.TryPlace(sword, out var first);
            station.TryPlace(ore, out var second);
            var blade = WorldItemPlacement.Spawn(sword, first.position, first.rotation, first, station.gameObject);
            var stone = WorldItemPlacement.Spawn(ore, second.position, second.rotation, second, station.gameObject);
            WorldItemPlacement.TryGetBounds(blade, out var bladeBounds);
            WorldItemPlacement.TryGetBounds(stone, out var stoneBounds);
            Check(bladeBounds.size.y < Mathf.Max(bladeBounds.size.x, bladeBounds.size.z) * 0.6f, stationTag + " actual sword stays horizontal");
            Check(stoneBounds.size.magnitude > 0.05f, stationTag + " actual ore visible");
            report.measurements.Add(stationTag + " slot=" + anchor.position + " scale=" + anchor.lossyScale + " sword=" + bladeBounds + " ore=" + stoneBounds);
            var center = (bladeBounds.center + stoneBounds.center) * 0.5f;
            // Face the workstation from the room, retaining the real scene artwork.
            Vector3 roomCenter = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c => c.CompareTag("put") && c.GetComponentInParent<PlacementSlots>() == null)
                .OrderByDescending(c => c.bounds.size.x * c.bounds.size.z).First().bounds.center;
            var approach = roomCenter - center; approach.y = 0; approach.Normalize();
            camera.transform.position = center + approach * 2.5f + Vector3.up * 1.25f;
            camera.transform.LookAt(center);
            for (int i = 0; i < 5; i++) yield return null;
            ScreenCapture.CaptureScreenshot("Logs/placement-" + stationTag + ".png");
            for (int i = 0; i < 4; i++) yield return null;
            station.ClearAllAndDestroyChildren();
            yield return null;
            var recipes = Get<RecipeDatabase>(put, stationTag == "wash" ? "washRecipeDatabase" : "weaponRecipeDatabase");
            var recipe = recipes.allRecipes.First(r => r != null && r.resultItem == sword);
            foreach (var material in recipe.requiredItems.Where(x => x != null))
            {
                Check(station.TryPlace(material, out var materialSlot), "craft material slot available");
                WorldItemPlacement.Spawn(material, materialSlot.position, materialSlot.rotation, materialSlot, station.gameObject);
            }
            typeof(PutItem).GetField("isCraftingInProgress", Flags).SetValue(put, true);
            put.StartCoroutine((IEnumerator)typeof(PutItem).GetMethod("ProcessCrafting", Flags).Invoke(put,
                new object[] { station, recipes, station.gameObject, new RaycastHit(), station.gameObject }));
            for (int i = 0; i < 3; i++) yield return null;
            // Supply only QTE completion; the real material consumption and result spawn execute.
            typeof(PutItem).GetField("isWashCompleted", Flags).SetValue(put, true);
            foreach (var target in Get<Image[]>(put, "blacksmithImagesB")) if (target != null) target.gameObject.SetActive(false);
            int remaining = 300;
            while (put.IsCraftingInProgress && remaining-- > 0) yield return null;
            Check(!put.IsCraftingInProgress, stationTag + " production crafting finishes");
            foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
            var result = station.GetComponentsInChildren<ItemBehaviour>().Single(x => x.ItemData == sword);
            Check(WorldItemPlacement.TryGetBounds(result.gameObject, out var resultBounds), stationTag + " crafted result has visible mesh");
            report.measurements.Add(stationTag + " result=" + resultBounds + " anchor=" + station.GetResultAnchor().position + " materialSlot=" + station.GetSlotTransform(0).position);
            Check(resultBounds.size.y < 0.1f, stationTag + " crafted sword is horizontal");
            Check(Vector3.Distance(resultBounds.center, station.GetSlotTransform(0).position) < 1f,
                stationTag + " crafted result remains on its station");
            Get<Slider>(put, "washSlider").gameObject.SetActive(false);
            camera.transform.position = resultBounds.center + approach * 2.5f + Vector3.up * 1.25f;
            camera.transform.LookAt(resultBounds.center);
            for (int i = 0; i < 5; i++) yield return null;
            ScreenCapture.CaptureScreenshot("Logs/result-" + stationTag + ".png");
            for (int i = 0; i < 4; i++) yield return null;
            station.ClearAllAndDestroyChildren();
            yield return null;
        }
        // The center recipe table has no PlacementSlots: exercise the real no-slot placement path.
        var collider = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c => c.CompareTag("put") && c.GetComponentInParent<PlacementSlots>() == null)
            .OrderByDescending(c => c.bounds.size.x * c.bounds.size.z).First();
        var table = collider.gameObject;
        report.measurements.Add("center table: " + table.name + " " + collider.bounds);
        Check(collider != null, "center table collider exists");
        var point = collider.bounds.center; point.y = collider.bounds.max.y;
        Check(collider.Raycast(new Ray(point + Vector3.up, Vector3.down), out var tableHit, 3f), "center table top can be targeted");
        point = tableHit.point; // The rim is higher than the usable tray surface.
        var tableOre = WorldItemPlacement.Spawn(db.allItems.Single(x => x.itemID == 3), point, Quaternion.identity, null, table);
        Check(WorldItemPlacement.TryGetBounds(tableOre, out var tableBounds) && Mathf.Abs(tableBounds.min.y - point.y - 0.015f) < 0.01f, "ore sits on center table");
        camera.transform.position = point + new Vector3(0, 1.2f, -2.5f); camera.transform.LookAt(point);
        for (int i = 0; i < 5; i++) yield return null;
        ScreenCapture.CaptureScreenshot("Logs/placement-table.png");
        for (int i = 0; i < 4; i++) yield return null;
    }
}
#endif
