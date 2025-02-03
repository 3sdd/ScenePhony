using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

public class SceneListController
{
    Button _refreshButton;
    TextField _searchField;
    DropdownField _sortField;
    ScrollView _sceneListScrollView;
    List<string> _scenePaths = new List<string>();

    public void Initialize(Button refreshButton, TextField searchField, DropdownField sortField, ScrollView sceneListScrollView)
    {
        _refreshButton = refreshButton;
        _searchField = searchField;
        _sortField = sortField;
        _sceneListScrollView = sceneListScrollView;

        SetRefreshButtonContent();

        _sortField.choices = new List<string> { "Name Ascending", "Name Descending" };
        _sortField.value = "Name Ascending";
        _sortField.RegisterValueChangedCallback(evt => PopulateSceneList());

        _refreshButton.clicked += () =>
        {
            RefreshSceneList();
            PopulateSceneList();
        };
        _searchField.RegisterValueChangedCallback(evt => PopulateSceneList());

        RefreshSceneList();
        PopulateSceneList();
    }

    void SetRefreshButtonContent()
    {
        _refreshButton.Clear();
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;

        var iconContent = EditorGUIUtility.IconContent("Refresh");
        var iconImage = new Image { image = iconContent.image };
        iconImage.style.width = 16;
        iconImage.style.height = 16;
        iconImage.style.marginRight = 4;
        container.Add(iconImage);

        var label = new Label("Refresh");
        container.Add(label);

        _refreshButton.Add(container);
    }

    public void RefreshSceneList()
    {
        var guids = AssetDatabase.FindAssets("t:Scene");
        _scenePaths.Clear();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Packages/"))
                _scenePaths.Add(path);
        }
    }

    public void PopulateSceneList()
    {
        _sceneListScrollView.Clear();
        var searchQuery = _searchField.value.Trim().ToLower();
        var sortedScenes = new List<string>(_scenePaths);
        if (_sortField.value == "Name Descending")
            sortedScenes.Sort((a, b) => string.Compare(b, a));
        else
            sortedScenes.Sort();

        foreach (var scenePath in sortedScenes)
        {
            if (!string.IsNullOrEmpty(searchQuery) && !scenePath.ToLower().Contains(searchQuery))
                continue;

            var sceneItem = new VisualElement();
            sceneItem.style.flexDirection = FlexDirection.Row;
            sceneItem.style.alignItems = Align.Center;
            sceneItem.AddToClassList("scene-item");

            var textContainer = new VisualElement();
            textContainer.style.flexGrow = 1;
            textContainer.style.flexDirection = FlexDirection.Column;
            textContainer.AddToClassList("scene-text-container");

            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var sceneNameLabel = new Label(sceneName);
            sceneNameLabel.AddToClassList("scene-item-name");
            textContainer.Add(sceneNameLabel);

            var scenePathLabel = new Label(scenePath);
            scenePathLabel.AddToClassList("scene-item-path");
            textContainer.Add(scenePathLabel);
            sceneItem.Add(textContainer);

            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.alignItems = Align.Center;

            var openButton = new Button(() =>
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(scenePath);
            }) { text = "Open" };
            openButton.AddToClassList("common-button");
            buttonContainer.Add(openButton);

            var selectButton = new Button(() =>
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    EditorGUIUtility.PingObject(sceneAsset);
                    Selection.activeObject = sceneAsset;
                }
                else
                {
                    EditorUtility.DisplayDialog("Select Scene", "Scene asset not found.", "OK");
                }
            }) { text = "Select" };
            selectButton.AddToClassList("common-button");
            buttonContainer.Add(selectButton);

            sceneItem.Add(buttonContainer);
            _sceneListScrollView.Add(sceneItem);
        }
    }
}