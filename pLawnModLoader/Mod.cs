using System;

namespace pLawnModLoader
{
    public enum ModTypeEnum
    {
        Pre,    // 前置补丁
        Post,   // 后置补丁
        Normal  // 普通模组（通常指外部DLL）
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ModPatchAttribute : Attribute
    {
        public ModTypeEnum Type { get; set; }
        public string Name { get; set; }

        public ModPatchAttribute(ModTypeEnum type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}