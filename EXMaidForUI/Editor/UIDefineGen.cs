#if UNITY_EDITOR
namespace EXMaidForUI.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using EXMaidForUI.Runtime.EXMaid;
    using FairyGUI;
    using FairyGUIEditor;
    using UnityEditor;
    using UnityEngine;
    
    public static class UIDefineGen
    {
        private static HashSet<PackageItem> _itemsMap;

        [MenuItem("EXTool/EX Maid For UI/Generate FairyGUI Define Code")]
        private static void Gen()
        {
            EditorToolSet.ReloadPackages();
            _itemsMap = new HashSet<PackageItem>(UIPackage.GetPackages().SelectMany(p => p.GetItems()));
            var code = $@"
using FairyGUI;
namespace UIGen
{{
    internal static class U
    {{
        internal static GComponent G(object o) => o is Window w ? w.contentPane : (GComponent)o;
        internal static GObject G(object o, string name) => G(o).GetChild(name);
    }}
{string.Join("", UIPackage.GetPackages().Select(GenPackageClass))}
}}";
            File.WriteAllText($"{Application.dataPath}{EXMaidUIAsset.Asset.fguiGenUIDefinePath}UIDefine.gen.cs", code);
        }

        private static string GenPackageClass(UIPackage package)
        {
            var groups = package.GetItems()
                .Where(i => i.type == PackageItemType.Component)
                .GroupBy(i => i.name);
            foreach (var g in groups.Where(g => g.Count() > 1))
                Debug.LogWarning(
                    $"{nameof(UIDefineGen)}: there are {g.Count()} items named \"{g.Key}\" in {package.name} package, only id \"{g.First().id}\" will be generated.");
            var code = $@"
    namespace {SafeName(package.name)}
    {{
        {string.Join("", groups.Select(g => GenClass(package, g.First())))}
    }}
";
            return code;
        }

        private static string GenClass(UIPackage package, PackageItem item)
        {
            var comp = (GComponent)package.CreateObject(item.name);

            //由于FGUI的组合不会生成实际的节点，收集第一层即可
            static IEnumerable<GObject> Children(GComponent o)
            {
                for (var i = 0; i < o.numChildren; i++) yield return o.GetChildAt(i);
            }

            var groups = Children(comp)
                .Where(o => IsVaildName(o.name))
                .Where(o => !Regex.IsMatch(o.name, "^n\\d+$"))
                .Where(o => !(comp is GButton) || (o.name != "icon" && o.name != "title"))
                .GroupBy(g => g.name);
            foreach (var g in groups.Where(g => g.Count() > 1))
                Debug.LogWarning(
                    $"{nameof(UIDefineGen)}: there are {g.Count()} object named \"{g.Key}\" in {package.name}.{item.name}, only index of \"{comp.GetChildIndex(g.First())}\" will be generated.");
            var safeName = SafeName(item.name);
            var controllers = comp.Controllers
                .Where(c => c.name != "button");
            var pages = controllers
                .SelectMany(c => Enumerable.Range(0, c.pageCount).Select(i => (c, PageName: c.GetPageName(i))))
                .Where(p => IsVaildName(p.PageName));
            var code = $@"
        internal interface {GenItemInterfaceTypeName(item)} {{ }}
        internal struct {GenItemProxyTypeName(item)}
        {{
            internal readonly {comp.GetType().Name} Target {{ get; }}
            internal {GenItemProxyTypeName(item)}({comp.GetType().Name} o) => Target = o;
            {string.Join(@"", controllers.Select(c => $@"
            internal readonly Controller Controller_{c.name} => U.G(Target).GetController(""{c.name}"");"))}
            {string.Join(@"", groups.Select(o => GenGetChildProperty(o.First())))}
        }}
        internal static class {safeName}_Extensions
        {{
            internal static string GetUIPackageName(this {GenItemInterfaceTypeName(item)} _) => ""{package.name}"";
            internal static string GetUIPackageItemName(this {GenItemInterfaceTypeName(item)} _) => ""{item.name}"";
            internal static {GenItemProxyTypeName(item)} GetUIDefine(this {GenItemInterfaceTypeName(item)} @this) => new {GenItemProxyTypeName(item)}(({comp.GetType().Name})U.G(@this));
        }}{(pages.Any() ? $@"
        internal static class {safeName}_Pages
        {{
            {string.Join(@"", pages.Select(p => $@"
            internal static readonly string {p.c.name}_{SafeName(p.PageName)} = ""{p.PageName}"";"))}
        }}" : "")}
";
            comp.Dispose();
            return code;
        }

        private static string GenItemInterfaceTypeName(PackageItem item)
        {
            return $"I_{SafeName(item.name)}";
        }

        private static string GenItemProxyTypeName(PackageItem item)
        {
            return $"{SafeName(item.name)}_Proxy";
        }

        private static string SafeName(string name)
        {
            return name.Replace(' ', '_');
        }

        private static bool IsVaildName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, "^[\\w ]+$");
        }

        private static string GenGetChildProperty(GObject o)
        {
            var safeName = SafeName(o.name);
            safeName = IsValidIdentifier(safeName) ? safeName : $"_{safeName}";
            if (o is GComponent
                && o.packageItem != null
                && _itemsMap.Contains(o.packageItem))
                return $@"
            internal readonly {GenItemProxyTypeName(o.packageItem)} {safeName} => new {GenItemProxyTypeName(o.packageItem)}(({o.GetType().Name})U.G(Target, ""{o.name}""));";
            return $@"
            internal readonly {o.GetType().Name} {safeName} => ({o.GetType().Name})U.G(Target, ""{o.name}"");";
        }

        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;

            for (var i = 1; i < identifier.Length; i++)
                if (!char.IsLetterOrDigit(identifier[i]) && identifier[i] != '_')
                    return false;

            return true;
        }
    }
}
#endif