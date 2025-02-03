using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

public class SceneGroupController
{
    VisualElement _groupHeaderContainer;
    ScrollView _groupListScrollView;
    TextField _newGroupNameField;
    Button _addGroupButton;

    SceneGroupDatabase _groupDatabase;
    // ユーザー用データの保存先として Assets/Settings/SceneGroupDatabase.asset
    const string UserDatabasePath = "Assets/Settings/SceneGroupDatabase.asset";
    // パッケージ内のデフォルトデータのパス（必要に応じて変更）
    const string PackageDatabasePath = "Packages/dev.msdd.scenephony/Editor/SceneGroupDatabase.asset";

    // 各グループの個別編集状態を管理する辞書（初期状態は false）
    Dictionary<SceneGroupData, bool> _groupEditStates = new Dictionary<SceneGroupData, bool>();
    // 各グループの折りたたみ状態を管理する辞書（初期状態は false＝展開）
    Dictionary<SceneGroupData, bool> _groupCollapsedStates = new Dictionary<SceneGroupData, bool>();

    public void Initialize(VisualElement groupHeaderContainer, ScrollView groupListScrollView)
    {
        _groupHeaderContainer = groupHeaderContainer;
        _groupListScrollView = groupListScrollView;
        LoadGroupDatabase();
        SetupUI();
    }

    void SetupUI()
    {
        _groupHeaderContainer.Clear();

        // --- Add Group Row ---
        VisualElement addGroupRow = new VisualElement();
        addGroupRow.style.flexDirection = FlexDirection.Row;
        addGroupRow.style.alignItems = Align.Center;

        _newGroupNameField = new TextField();
        _newGroupNameField.value = "";
        _newGroupNameField.label = "New Group:";
        _newGroupNameField.style.minWidth = 250;
        addGroupRow.Add(_newGroupNameField);

        _addGroupButton = new Button(() => { AddNewGroup(); }) { text = "Add Group" };
        addGroupRow.Add(_addGroupButton);

        _newGroupNameField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                AddNewGroup();
                evt.StopPropagation();
            }
        });
        _groupHeaderContainer.Add(addGroupRow);

        RefreshUI();
    }

    void AddNewGroup()
    {
        var groupName = _newGroupNameField.value.Trim();
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogWarning("Group name is empty.");
            return;
        }
        if (_groupDatabase.groups.Exists(g => g.groupName == groupName))
        {
            Debug.LogWarning("Group already exists.");
            return;
        }
        try
        {
            var newGroup = new SceneGroupData();
            newGroup.groupName = groupName;
            _groupDatabase.groups.Add(newGroup);
            _newGroupNameField.value = "";
            SaveGroupDatabase();
            RefreshUI();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to add new group: " + ex.Message);
        }
    }

    void LoadGroupDatabase()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        _groupDatabase = AssetDatabase.LoadAssetAtPath<SceneGroupDatabase>(UserDatabasePath);
        if (_groupDatabase == null)
        {
            var defaultDatabase = AssetDatabase.LoadAssetAtPath<SceneGroupDatabase>(PackagePaths.SceneGroupDatabasePackagePath);
            if (defaultDatabase != null)
            {
                if (AssetDatabase.CopyAsset(PackagePaths.SceneGroupDatabasePackagePath, UserDatabasePath))
                {
                    AssetDatabase.Refresh();
                    _groupDatabase = AssetDatabase.LoadAssetAtPath<SceneGroupDatabase>(UserDatabasePath);
                }
                else
                {
                    Debug.LogError("Failed to copy SceneGroupDatabase from package to " + UserDatabasePath);
                    _groupDatabase = ScriptableObject.CreateInstance<SceneGroupDatabase>();
                    AssetDatabase.CreateAsset(_groupDatabase, UserDatabasePath);
                    AssetDatabase.SaveAssets();
                }
            }
            else
            {
                Debug.LogWarning("Default SceneGroupDatabase not found in package. Creating new asset.");
                _groupDatabase = ScriptableObject.CreateInstance<SceneGroupDatabase>();
                AssetDatabase.CreateAsset(_groupDatabase, UserDatabasePath);
                AssetDatabase.SaveAssets();
            }
        }
    }

    void SaveGroupDatabase()
    {
        try
        {
            EditorUtility.SetDirty(_groupDatabase);
            AssetDatabase.SaveAssets();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save SceneGroupDatabase: " + ex.Message);
        }
    }

    public void RefreshUI()
    {
        _groupListScrollView.Clear();

        foreach (var group in _groupDatabase.groups)
        {
            if (!_groupEditStates.ContainsKey(group))
                _groupEditStates[group] = false;
            bool isEditing = _groupEditStates[group];

            if (!_groupCollapsedStates.ContainsKey(group))
                _groupCollapsedStates[group] = false;
            bool isCollapsed = _groupCollapsedStates[group];

            var groupContainer = new VisualElement();
            groupContainer.AddToClassList("group-item");

            // --- ヘッダー部分 ---
            var header = new VisualElement();
            header.AddToClassList("group-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            // 折りたたみトグルボタン
            var collapseButton = new Button(() =>
            {
                _groupCollapsedStates[group] = !_groupCollapsedStates[group];
                RefreshUI();
            });
            collapseButton.text = isCollapsed ? "►" : "▼";
            collapseButton.style.width = 24;
            collapseButton.style.height = 24;
            collapseButton.style.marginRight = 5;
            collapseButton.AddToClassList("common-button");
            header.Add(collapseButton);

            VisualElement nameElement;
            if (isEditing)
            {
                var nameField = new TextField();
                nameField.value = group.groupName;
                nameField.style.flexGrow = 1;
                nameField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    var newName = nameField.value.Trim();
                    if (!string.IsNullOrEmpty(newName) && newName != group.groupName)
                    {
                        if (_groupDatabase.groups.Exists(g => g.groupName == newName))
                        {
                            EditorUtility.DisplayDialog("Error", "A group with this name already exists.", "OK");
                            nameField.value = group.groupName;
                        }
                        else
                        {
                            group.groupName = newName;
                            SaveGroupDatabase();
                        }
                    }
                    else
                    {
                        nameField.value = group.groupName;
                    }
                });
                nameField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        nameField.Blur();
                        evt.StopPropagation();
                    }
                });
                nameElement = nameField;
            }
            else
            {
                var label = new Label(group.groupName);
                label.AddToClassList("group-title");
                nameElement = label;
            }
            header.Add(nameElement);

            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;

            // Open All ボタン
            var openAllButton = new Button(() =>
            {
                foreach (var scenePath in group.scenePaths)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }) { text = "Open All" };
            openAllButton.AddToClassList("common-button");
            buttonContainer.Add(openAllButton);

            // 個別 Edit/Save ボタン
            var editButton = new Button(() =>
            {
                _groupEditStates[group] = !isEditing;
                RefreshUI();
            }) { text = isEditing ? "Save" : "Edit" };
            editButton.AddToClassList("common-button");
            buttonContainer.Add(editButton);

            // 削除ボタン（グループ全体削除）
            var deleteButton = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("Confirm Deletion",
                    $"Are you sure you want to delete group '{group.groupName}'?", "Delete", "Cancel"))
                {
                    _groupDatabase.groups.Remove(group);
                    _groupEditStates.Remove(group);
                    _groupCollapsedStates.Remove(group);
                    SaveGroupDatabase();
                    RefreshUI();
                }
            });
            deleteButton.text = "";
            deleteButton.style.width = 24;
            deleteButton.style.height = 24;
            deleteButton.style.marginLeft = 5;
            deleteButton.style.unityBackgroundImageTintColor = new Color(0.8f, 0f, 0f);
            deleteButton.tooltip = "Delete group";
            var trashIcon = EditorGUIUtility.IconContent("TreeEditor.Trash");
            var trashImage = new Image();
            trashImage.image = trashIcon.image;
            trashImage.style.width = 16;
            trashImage.style.height = 16;
            trashImage.style.alignSelf = Align.Center;
            deleteButton.Add(trashImage);
            deleteButton.AddToClassList("common-button");
            buttonContainer.Add(deleteButton);

            header.Add(buttonContainer);
            groupContainer.Add(header);

            if (!isCollapsed)
            {
                // --- Add Scene Dropdown Row (非編集モードのみ) ---
                if (!isEditing)
                {
                    var addSceneRow = new VisualElement();
                    addSceneRow.style.flexDirection = FlexDirection.Row;
                    addSceneRow.style.alignItems = Align.Center;
                    
                    var addSceneDropdown = new DropdownField();
                    addSceneDropdown.label = "Add Scene:";
                    addSceneDropdown.style.flexGrow = 1;
                    
                    List<string> allScenes = GetAllScenePaths();
                    List<string> availableScenes = new List<string>();
                    foreach (var s in allScenes)
                    {
                        if (!group.scenePaths.Contains(s))
                            availableScenes.Add(Path.GetFileNameWithoutExtension(s));
                    }
                    List<string> dropdownOptions = new List<string>();
                    dropdownOptions.Add("--Select Scene--");
                    dropdownOptions.AddRange(availableScenes);
                    
                    addSceneDropdown.choices = dropdownOptions;
                    addSceneDropdown.value = "--Select Scene--";
                    
                    addSceneDropdown.RegisterValueChangedCallback(evt =>
                    {
                        string selectedSceneName = evt.newValue;
                        if (selectedSceneName == "--Select Scene--")
                            return;
                        
                        string fullPath = "";
                        foreach (var s in allScenes)
                        {
                            if (Path.GetFileNameWithoutExtension(s) == selectedSceneName)
                            {
                                fullPath = s;
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            group.scenePaths.Add(fullPath);
                            SaveGroupDatabase();
                            RefreshUI();
                        }
                    });
                    addSceneRow.Add(addSceneDropdown);
                    groupContainer.Add(addSceneRow);
                }

                // --- グループ内の各シーン一覧 ---
                foreach (var scenePath in group.scenePaths)
                {
                    var sceneItem = new VisualElement();
                    sceneItem.AddToClassList("group-scene-item");

                    var sceneLabel = new Label(Path.GetFileNameWithoutExtension(scenePath));
                    sceneLabel.AddToClassList("group-scene-label");
                    sceneItem.Add(sceneLabel);

                    var openButton = new Button(() =>
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(scenePath);
                    }) { text = "Open" };
                    openButton.AddToClassList("common-button");
                    sceneItem.Add(openButton);

                    var closeButton = new Button(() =>
                    {
                        var scene = EditorSceneManager.GetSceneByPath(scenePath);
                        if (scene.isLoaded)
                        {
                            EditorSceneManager.CloseScene(scene, true);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Close Scene", "Scene is not loaded.", "OK");
                        }
                    }) { text = "Close" };
                    closeButton.AddToClassList("common-button");
                    sceneItem.Add(closeButton);

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
                    sceneItem.Add(selectButton);

                    var removeButton = new Button(() =>
                    {
                        group.scenePaths.Remove(scenePath);
                        SaveGroupDatabase();
                        RefreshUI();
                    });
                    removeButton.text = "";
                    removeButton.style.width = 24;
                    removeButton.style.height = 24;
                    removeButton.style.marginLeft = 5;
                    removeButton.style.unityBackgroundImageTintColor = new Color(0.8f, 0f, 0f);
                    removeButton.tooltip = "Delete scene from group";
                    var trashIcon2 = EditorGUIUtility.IconContent("TreeEditor.Trash");
                    var trashImage2 = new Image();
                    trashImage2.image = trashIcon2.image;
                    trashImage2.style.width = 16;
                    trashImage2.style.height = 16;
                    trashImage2.style.alignSelf = Align.Center;
                    removeButton.Add(trashImage2);
                    removeButton.AddToClassList("common-button");
                    sceneItem.Add(removeButton);

                    groupContainer.Add(sceneItem);
                }
            }

            _groupListScrollView.Add(groupContainer);
        }
    }

    List<string> GetAllScenePaths()
    {
        var guids = AssetDatabase.FindAssets("t:Scene");
        var scenePaths = new List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Packages/"))
                scenePaths.Add(path);
        }
        return scenePaths;
    }
}