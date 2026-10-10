using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI, location : "LoginUI")]
	public partial class LoginUI
	{
		#region Override

		protected override void RegisterEvent()
		{
			AddUIEvent(ILoginUI_Event.OnLoginSuccess, OnLoginSuccess);
		}

		#endregion

		#region 事件

		private void OnLoginSuccess()
		{
			Close();
		}

		private partial void OnClickRegisterBtn()
		{
			DataCenterSys.Instance.Register("192.168.1.105", 20001, m_inputAccount.text, m_inputPassword.text).Coroutine();
		}

		private partial void OnClickLoginBtn()
		{
			DataCenterSys.Instance.Login("192.168.1.105", 20001, m_inputAccount.text, m_inputPassword.text).Coroutine();
		}

		#endregion
	}
}