using Project.Player.Car;
using Project.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelGSetupTool
{
    [MenuItem("Zombie Rush/Level G/Setup HUD (CAR_TEST style)")]
    public static void SetupHud()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        GameObject player = GameObject.Find("PlayerCar");
        if (player == null)
        {
            Debug.LogError("Setup HUD: PlayerCar not found.");
            return;
        }

        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();

        GameObject hudRoot = GetOrCreateUi("HUD", canvasRt);
        RectTransform hudRt = hudRoot.GetComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero;
        hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero;
        hudRt.offsetMax = Vector2.zero;

        TextMeshProUGUI speedText = CreateTmpText(
            hudRt,
            "TextSpeed",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 58f),
            new Vector2(460f, 88f),
            54,
            TextAlignmentOptions.Center);
        speedText.text = "<color=#FF2D2D><size=120%>0</size> KM/H</color>";

        GameObject fuelPanel = GetOrCreateUi("Fuel Panel", hudRt);
        RectTransform fuelRt = fuelPanel.GetComponent<RectTransform>();
        fuelRt.anchorMin = new Vector2(1f, 0f);
        fuelRt.anchorMax = new Vector2(1f, 0f);
        fuelRt.pivot = new Vector2(1f, 0f);
        fuelRt.anchoredPosition = new Vector2(-24f, 24f);
        fuelRt.sizeDelta = new Vector2(290f, 64f);
        Image fuelBg = EnsureComponent<Image>(fuelPanel);
        fuelBg.color = new Color(0f, 0f, 0f, 0.42f);

        Slider fuelSlider = CreateSlider(fuelRt, "Fuel Slider", new Vector2(14f, -34f), new Vector2(190f, 22f), new Color(0.18f, 0.18f, 0.18f, 0.9f), new Color(0.97f, 0.71f, 0.09f, 0.95f));
        TextMeshProUGUI fuelText = CreateTmpText(fuelRt, "Fuel Text", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, -2f), new Vector2(92f, 40f), 28, TextAlignmentOptions.Center);
        fuelText.text = "F:200/200";

        GameObject hpPanel = GetOrCreateUi("HP Panel", hudRt);
        RectTransform hpRt = hpPanel.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(1f, 0f);
        hpRt.anchorMax = new Vector2(1f, 0f);
        hpRt.pivot = new Vector2(1f, 0f);
        hpRt.anchoredPosition = new Vector2(-24f, 98f);
        hpRt.sizeDelta = new Vector2(290f, 64f);
        Image hpBg = EnsureComponent<Image>(hpPanel);
        hpBg.color = new Color(0f, 0f, 0f, 0.42f);

        Slider hpSlider = CreateSlider(hpRt, "HP Slider", new Vector2(14f, -34f), new Vector2(190f, 22f), new Color(0.18f, 0.18f, 0.18f, 0.9f), new Color(0.80f, 0.18f, 0.18f, 0.95f));
        TextMeshProUGUI hpText = CreateTmpText(hpRt, "HP Text", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, -2f), new Vector2(92f, 40f), 28, TextAlignmentOptions.Center);
        hpText.text = "HP:100/100";

        var speed = EnsureComponent<VehicleSpeedDisplay>(hudRoot);
        SetSerializedField(speed, "vehicleBody", player.GetComponent<Rigidbody>());
        SetSerializedField(speed, "speedTextTMP", speedText);
        SetSerializedField(speed, "logToConsole", false);

        var fuel = EnsureComponent<FuelUI>(fuelPanel);
        SetSerializedField(fuel, "fuelSystem", player.GetComponent<FuelSystem>());
        SetSerializedField(fuel, "fuelSlider", fuelSlider);
        SetSerializedField(fuel, "fuelTextTMP", fuelText);
        SetSerializedField(fuel, "fuelLabel", "Fuel");

        var health = EnsureComponent<VehicleHealthUI>(hpPanel);
        SetSerializedField(health, "vehicleHealth", player.GetComponent<VehicleHealth>());
        SetSerializedField(health, "healthSlider", hpSlider);
        SetSerializedField(health, "healthTextTMP", hpText);
        SetSerializedField(health, "healthLabel", "HP");

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Level G HUD setup complete.");
    }

    [MenuItem("Zombie Rush/Level G/Setup Colliders (Buildings + Small Props)")]
    public static void SetupColliders()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        PhysicsMaterial buildingMat = GetOrCreatePhysicsMaterial("Assets/_Project/Physics/BuildingImpact.physicMaterial", 0.045f);
        PhysicsMaterial propMat = GetOrCreatePhysicsMaterial("Assets/_Project/Physics/PropImpact.physicMaterial", 0.06f);

        foreach (GameObject root in scene.GetRootGameObjects())
            ProcessTransformRecursive(root.transform, buildingMat, propMat);

        GameObject player = GameObject.Find("PlayerCar");
        if (player != null)
        {
            BoxCollider carCol = player.GetComponent<BoxCollider>();
            if (carCol != null)
                carCol.material = propMat;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Level G colliders setup complete.");
    }

    private static void ProcessTransformRecursive(Transform t, PhysicsMaterial buildingMat, PhysicsMaterial propMat)
    {
        string n = t.name;

        if (n.StartsWith("Bld_"))
        {
            BoxCollider col = EnsureComponent<BoxCollider>(t.gameObject);
            col.material = buildingMat;
            t.gameObject.isStatic = true;
        }
        else if (n.StartsWith("Bush_") || n.StartsWith("Fence_") || n.StartsWith("Light_") || n.StartsWith("Tree_") || n.StartsWith("FinishBarrier"))
        {
            BoxCollider col = EnsureComponent<BoxCollider>(t.gameObject);
            col.material = propMat;
            Rigidbody rb = EnsureComponent<Rigidbody>(t.gameObject);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 28f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 1.2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            t.gameObject.isStatic = false;
        }

        for (int i = 0; i < t.childCount; i++)
            ProcessTransformRecursive(t.GetChild(i), buildingMat, propMat);
    }

    private static PhysicsMaterial GetOrCreatePhysicsMaterial(string path, float bounciness)
    {
        PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat == null)
        {
            string dir = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                string parent = "Assets";
                foreach (string part in dir.Replace("Assets/", "").Split('/'))
                {
                    string candidate = $"{parent}/{part}";
                    if (!AssetDatabase.IsValidFolder(candidate))
                        AssetDatabase.CreateFolder(parent, part);
                    parent = candidate;
                }
            }

            mat = new PhysicsMaterial();
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.dynamicFriction = 0.55f;
        mat.staticFriction = 0.65f;
        mat.bounciness = bounciness;
        mat.frictionCombine = PhysicsMaterialCombine.Average;
        mat.bounceCombine = PhysicsMaterialCombine.Maximum;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
            return canvas;

        GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject GetOrCreateUi(string name, RectTransform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateTmpText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        GameObject go = GetOrCreateUi(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(go);
        text.fontSize = fontSize;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = Color.white;
        return text;
    }

    private static Slider CreateSlider(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color bgColor, Color fillColor)
    {
        GameObject root = GetOrCreateUi(name, parent);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        GameObject bg = GetOrCreateUi("Background", rt);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImage = EnsureComponent<Image>(bg);
        bgImage.color = bgColor;

        GameObject fillArea = GetOrCreateUi("Fill Area", rt);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0f);
        fillAreaRt.anchorMax = new Vector2(1f, 1f);
        fillAreaRt.offsetMin = new Vector2(3f, 3f);
        fillAreaRt.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = GetOrCreateUi("Fill", fillAreaRt);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImage = EnsureComponent<Image>(fill);
        fillImage.color = fillColor;

        Slider slider = EnsureComponent<Slider>(root);
        slider.fillRect = fillRt;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        return slider;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void SetSerializedField(Object target, string fieldName, object value)
    {
        if (target == null)
            return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            return;

        switch (value)
        {
            case bool b:
                prop.boolValue = b;
                break;
            case int i:
                prop.intValue = i;
                break;
            case float f:
                prop.floatValue = f;
                break;
            case string s:
                prop.stringValue = s;
                break;
            case Object o:
                prop.objectReferenceValue = o;
                break;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
