using UnityEditor;

public static class RefreshAndBuild
{
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        StudyBuild.BuildStudyApk();
    }
}
