using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TEngine.Editor
{
    /// <summary>
    /// 创建 YooAsset Host 模式的 HTTP 发布视图目录（Builds/ServerRoot/{projectName}/{platform}/）。
    /// 目录结构与运行时请求 URL（UpdateSetting.GetResDownLoadPath → {host}/{projectName}/{platform}/{fileName}）强同源：
    /// projectName 或平台配置变化时，本菜单的创建路径自动跟随。
    /// 目录已存在时菜单项置灰。菜单名中的平台名随当前 Build Target 编译期自动切换。
    /// </summary>
    internal static class ServerRootMenu
    {
#if UNITY_ANDROID
        private const string PlatformName = "Android";
#elif UNITY_IOS
        private const string PlatformName = "IOS";
#elif UNITY_WEBGL
        private const string PlatformName = "WebGL";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string PlatformName = "MacOS";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        private const string PlatformName = "Linux";
#else
        private const string PlatformName = "Windows64";
#endif

        private const string MenuItemPath = "TEngine/Build/CreateServerRoot {" + PlatformName + "}";

        /// <summary>
        /// 发布视图根目录：{项目根}/Builds/ServerRoot/{projectName}/{platform}/
        /// </summary>
        private static string ServerRootPath
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                // 复用运行时的 URL 规则，从中提取 {projectName}/{platform} 相对路径
                string relative = new Uri(Settings.UpdateSetting.GetResDownLoadPath()).AbsolutePath.TrimStart('/');
                return Path.Combine(projectRoot, "Builds", "ServerRoot", relative.Replace('/', Path.DirectorySeparatorChar));
            }
        }

        [MenuItem(MenuItemPath, false, 40)]
        public static void CreateServerRoot()
        {
            Directory.CreateDirectory(ServerRootPath);
            Debug.Log($"[CreateServerRoot] 发布视图目录已就绪：{ServerRootPath}");
            EditorUtility.RevealInFinder(ServerRootPath);
        }

        [MenuItem(MenuItemPath, true)]
        public static bool ValidateServerRootExists()
        {
            return !Directory.Exists(ServerRootPath);
        }
    }
}
