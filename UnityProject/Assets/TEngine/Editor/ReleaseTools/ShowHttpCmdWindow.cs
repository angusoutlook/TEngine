using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

namespace TEngine.Editor
{
    /// <summary>
    /// 显示启动 HTTP 资源服务器（YooAsset Host 模式）的 node / python 命令。
    /// 服务器根 = {项目根}/Builds/ServerRoot；端口取自 UpdateSetting 的 ResDownLoadPath / FallbackResDownLoadPath，
    /// 配置变化时命令自动跟随。
    /// 默认 --bind 0.0.0.0（监听所有网卡，Android 真机可经局域网访问）；附停止服务器的命令。
    /// </summary>
    internal class ShowHttpCmdWindow : EditorWindow
    {
        private string _rootPath;
        private int _mainPort;
        private int _fallbackPort;
        private string _pythonCmd;
        private string _nodeCmd;
        private string _killCmd;
        private string _killCmdCmd;
        private bool _running;
        private string[] _lanIps;

        [MenuItem("TEngine/Build/ShowHttpCmd", false, 41)]
        public static void Open()
        {
            var win = GetWindow<ShowHttpCmdWindow>(true, "HTTP Server Cmd", true);
            win.minSize = new Vector2(720, 320);
        }

        private void OnEnable()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            _rootPath = Path.Combine(projectRoot, "Builds", "ServerRoot");

            _mainPort = ParsePort(Settings.UpdateSetting.GetResDownLoadPath(), 8081);
            _fallbackPort = ParsePort(Settings.UpdateSetting.GetFallbackResDownLoadPath(), 8082);

            string rootQuoted = $"\"{_rootPath}\"";
            _pythonCmd = $"python -m http.server {_mainPort} --bind 0.0.0.0 --directory {rootQuoted}";
            _nodeCmd = $"npx http-server {rootQuoted} -p {_mainPort} -a 0.0.0.0";
            _killCmd = $"Get-NetTCPConnection -LocalPort {_mainPort} -State Listen | " +
                       $"ForEach-Object {{ Stop-Process -Id $_.OwningProcess -Force }}";
            _killCmdCmd = $"for /f \"tokens=5\" %a in ('netstat -ano ^| findstr \":{_mainPort}\" ^| findstr \"LISTENING\"') do taskkill /f /pid %a";
            _running = IsPortInUse(_mainPort);
            _lanIps = GetLanIPv4s();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                $"服务器根目录：{_rootPath}\n" +
                $"端口：{_mainPort}（主）/ {_fallbackPort}（备）　状态：{(_running ? "运行中" : "未启动")}\n" +
                "已 --bind 0.0.0.0（监听所有网卡，局域网设备可访问）",
                MessageType.Info);

            if (_lanIps is { Length: > 0 })
            {
                EditorGUILayout.LabelField("本机局域网 IP（Android 真机热更时，UpdateSetting 的 ResDownLoadPath 需填此 IP）",
                    EditorStyles.miniBoldLabel);
                foreach (var ip in _lanIps)
                {
                    EditorGUILayout.SelectableLabel(
                        $"http://{ip}:{_mainPort}/{GetProjectName()}/Android/",
                        EditorStyles.textField,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
                EditorGUILayout.Space(4);
            }

            DrawCommand("Python", _pythonCmd);
            DrawCommand("Node", _nodeCmd);
            DrawCommand("停止服务器（PowerShell 版）", _killCmd);
            DrawCommand("停止服务器（CMD 版，两种终端通用）", _killCmdCmd);
        }

        private static void DrawCommand(string label, string cmd)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(cmd, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button($"复制 {label} 命令", GUILayout.Width(220)))
            {
                GUIUtility.systemCopyBuffer = cmd;
            }
            EditorGUILayout.Space(4);
        }

        private static int ParsePort(string url, int fallback)
        {
            try
            {
                return new Uri(url).Port;
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// 从运行时下载 URL 提取 {projectName} 段（projectName 为 UpdateSetting 私有字段，跨程序集不可直接访问）。
        /// </summary>
        private static string GetProjectName()
        {
            try
            {
                string abs = new Uri(Settings.UpdateSetting.GetResDownLoadPath()).AbsolutePath.TrimStart('/');
                string first = abs.Split('/')[0];
                return string.IsNullOrEmpty(first) ? "Demo" : first;
            }
            catch
            {
                return "Demo";
            }
        }

        private static bool IsPortInUse(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                return false; // 绑定成功 → 端口空闲
            }
            catch (SocketException)
            {
                return true; // 绑定失败 → 已被占用（大概率服务器在跑）
            }
            finally
            {
                listener?.Stop();
            }
        }

        /// <summary>
        /// 获取本机所有 IPv4 局域网地址（排除 loopback），供真机访问服务器使用。
        /// </summary>
        private static string[] GetLanIPv4s()
        {
            try
            {
                return System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
                    .AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                 && !System.Net.IPAddress.Loopback.Equals(ip))
                    .Select(ip => ip.ToString())
                    .Distinct()
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
