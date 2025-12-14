using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	public partial class m_itemTabWeapon
	{
		#region 事件

		private async partial UniTaskVoid OnClickBtn()
		{
			await UniTask.Yield();
		}

		#endregion
	}
}
