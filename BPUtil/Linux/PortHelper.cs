#if NET6_0_LINUX

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Stub namespace for ease of porting.
namespace BPUtil.Forms
{
	public class ButtonDefinition
	{
		public string Text;
		public EventHandler OnClick;
		public ButtonDefinition(string Text, EventHandler OnClick)
		{
			this.Text = Text;
			this.OnClick = OnClick;
		}
	}

	public class Control
	{
		public bool IsDisposed;

		public void Invoke(Action theAction)
		{
			throw new NotImplementedException();
		}
	}
}

#endif
