using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI, location : "BagWindowUI")]
	public partial class BagWindowUI
	{
		#region 事件

		private async partial UniTaskVoid OnClickCloseBtn()
		{
			await UniTask.Yield();
		}

		#endregion
	}
}
