using HarmonyLib;

namespace pLawnModLoader
{
    /// <summary>
    /// 所有在模组加载之后必须应用的 Harmony 补丁
    /// </summary>
    public static class PostModPatches
    {
        // 示例：此处可添加植物类型数组扩容、动画注册等补丁
        // [HarmonyPatch(typeof(SomeGameClass), "SomeMethod")]
        // public static class SomePatch { ... }
    }
}