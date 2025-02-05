using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SceneListWindowUI : EditorWindow
{
    SceneListController _sceneListController;
    SceneGroupController _sceneGroupController;

    [MenuItem("Window/ScenePhony")]
    [MenuItem("Tools/ScenePhony")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<SceneListWindowUI>();
        wnd.titleContent = new GUIContent("Scene List UI");
    }

    public void CreateGUI()
    {
        // UXML の読み込みとルートへの追加
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PackagePaths.SceneListWindowUxmlPath);
        if (visualTree == null)
        {
            Debug.LogError($"Failed to load UXML at path: {PackagePaths.SceneListWindowUxmlPath}");
            return;
        }
        var rootFromUXML = visualTree.CloneTree();
        rootVisualElement.Add(rootFromUXML);
        Debug.Log(visualTree);

        // USS の読み込み（ここではハードコーディングしていますが、必要に応じて PackagePaths からも取得可能）
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackagePaths.SceneListWindowStyleSheetPath);
        Debug.Log(PackagePaths.SceneListWindowStyleSheetPath);
        Debug.Log(styleSheet);
        rootVisualElement.styleSheets.Add(styleSheet);

        // タブ関連の UI 要素取得（BEM 命名に合わせて変更）
        var tabButtonList = rootVisualElement.Q<Button>("scene-manager__tab--list");
        var tabButtonGroups = rootVisualElement.Q<Button>("scene-manager__tab--groups");
        var containerList = rootVisualElement.Q<VisualElement>("scene-manager__container--list");
        var containerGroups = rootVisualElement.Q<VisualElement>("scene-manager__container--groups");

        // タブ切り替え処理
        tabButtonList.clicked += () =>
        {
            containerList.style.display = DisplayStyle.Flex;
            containerGroups.style.display = DisplayStyle.None;
            tabButtonList.AddToClassList("scene-manager__tab--active");
            tabButtonGroups.RemoveFromClassList("scene-manager__tab--active");
        };
        tabButtonGroups.clicked += () =>
        {
            containerList.style.display = DisplayStyle.None;
            containerGroups.style.display = DisplayStyle.Flex;
            tabButtonGroups.AddToClassList("scene-manager__tab--active");
            tabButtonList.RemoveFromClassList("scene-manager__tab--active");
            _sceneGroupController?.RefreshUI();
        };

        // コントローラーの初期化
        _sceneListController = new SceneListController();
        _sceneListController.Initialize(
            rootVisualElement.Q<Button>("scene-manager__refresh-button"),
            rootVisualElement.Q<TextField>("scene-manager__search-field"),
            rootVisualElement.Q<DropdownField>("scene-manager__sort-field"),
            rootVisualElement.Q<ScrollView>("scene-manager__scrollview--list")
        );

        _sceneGroupController = new SceneGroupController();
        _sceneGroupController.Initialize(
            rootVisualElement.Q<VisualElement>("scene-manager__group-header"),
            rootVisualElement.Q<ScrollView>("scene-manager__scrollview--groups")
        );
    }
}