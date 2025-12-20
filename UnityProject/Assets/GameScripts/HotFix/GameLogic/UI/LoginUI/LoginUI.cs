using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;
using Log = TEngine.Log;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class UILogin : UIWindow
    {
        #region 脚本工具生成的代码
        private InputField _inputFieldName;
        private InputField _inputFieldPwd;
        private Button _btnOK;
        private Button _btnCancel;
        protected override void ScriptGenerator()
        {
            _inputFieldName = FindChildComponent<InputField>("m_inputFieldName");
            _inputFieldPwd = FindChildComponent<InputField>("m_inputFieldPwd");
            _btnOK = FindChildComponent<Button>("m_btnOK");
            _btnCancel = FindChildComponent<Button>("m_btnCancel");
            _btnOK.onClick.AddListener(UniTask.UnityAction(OnClickOKBtn));
            _btnCancel.onClick.AddListener(UniTask.UnityAction(OnClickCancelBtn));
        }
        #endregion

        protected override void RegisterEvent()
        {
            // 监听游戏失败事件，当游戏失败时显示登录窗口
            AddUIEvent(ActorEventDefine.GameOver, OnGameOver);
            
            // 监听 ILoginUI_Event 事件（如果使用事件接口方式）
            AddUIEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
            AddUIEvent(ILoginUI_Event.CloseLoginUI, OnCloseLoginUI);
        }

        protected override void OnRefresh()
        {
            // 初始化窗口数据
            if (_inputFieldName != null)
                _inputFieldName.text = string.Empty;
            if (_inputFieldPwd != null)
                _inputFieldPwd.text = string.Empty;
            //this.Visible = false;
        }

        #region 事件处理

        /// <summary>
        /// 游戏失败时触发，显示登录窗口
        /// </summary>
        private void OnGameOver()
        {
            Log.Info("游戏失败，显示登录窗口");
            GameModule.UI.ShowUIAsync<UILogin>();  // 显示登录窗口
        }

        /// <summary>
        /// 响应 ILoginUI_Event.ShowLoginUI 事件
        /// </summary>
        private void OnShowLoginUI()
        {
            Log.Info("通过事件接口显示登录窗口");
            GameModule.UI.ShowUIAsync<UILogin>();  // 显示登录窗口
        }

        /// <summary>
        /// 响应 ILoginUI_Event.CloseLoginUI 事件
        /// </summary>
        private void OnCloseLoginUI()
        {
            Log.Info("通过事件接口关闭登录窗口");
            GameModule.UI.CloseUI<UILogin>();  // 关闭登录窗口
        }

        #endregion

        #region 按钮事件
        private async UniTaskVoid OnClickOKBtn()
        {
            await UniTask.Yield();
            Log.Info("点击登录按钮");
            // TODO: 实现登录逻辑
            // 例如：验证用户名和密码
            // 登录成功后关闭窗口
            GameModule.UI.CloseUI<UILogin>();  // 关闭登录窗口
        }

        private async UniTaskVoid OnClickCancelBtn()
        {
            await UniTask.Yield();
            Log.Info("点击取消按钮");
            //GameModule.UI.CloseUI<UILogin>();  // 关闭登录窗口
            this.Visible = false;
        }
        #endregion
    }
}
