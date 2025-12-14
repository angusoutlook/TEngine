using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI, location : "TestUI")]
	public partial class TestUI
	{
		#region 事件

		private async partial UniTaskVoid OnClickOKBtn()
		{
			await UniTask.Yield();
		}

		private async partial UniTaskVoid OnClickCancelBtn()
		{
			await UniTask.Yield();
		}

		#endregion
	}
}
