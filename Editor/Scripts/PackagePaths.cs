public static class PackagePaths
{
    public static string PackageRootPath = "Packages/dev.msdd.scenephony";

    // UXML のパス
    public static string SceneListWindowUxmlPath => $"{PackageRootPath}/Editor/UI/SceneListWindowUI.uxml";
    // USS　のパス
    public static string SceneListWindowStyleSheetPath => $"{PackageRootPath}/Editor/UI/SceneListWindowUI.uss";

    

    // デフォルトの SceneGroupDatabase のパス（パッケージ内）
    public static string SceneGroupDatabasePackagePath => $"{PackageRootPath}/Editor/SceneGroupDatabase.asset";

}