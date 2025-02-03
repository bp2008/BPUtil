using BPUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.Forms
{
	public partial class MovableOverlay : Form
	{
		/// <summary>
		/// Event raised when the form is moved or resized.  The event argument indicates if the form is currently being moved or resized by the user.  It will usually be true but will be false if dragging or resizing has just ended.
		/// </summary>
		public event EventHandler<bool> BoundsChanged = delegate { };
		/// <summary>
		/// Event raised when the IsDraggable property value has been set (it may not have actually changed).  The event argument indicates the value of IsDraggable at the time the event was raised.
		/// </summary>
		public event EventHandler<bool> DraggableSet = delegate { };
		/// <summary>
		/// Event raised after form moving or resizing ends.
		/// </summary>
		public event EventHandler<MouseEventArgs> DragResizeEnd = delegate { };
		/// <summary>
		/// Gets a value indicating if this form has been closed.  This will only become true when the <see cref="Form.FormClosed"/> event is raised.
		/// </summary>
		public bool IsClosed { get; private set; } = false;
		public MovableOverlay()
		{
			InitializeComponent();
			MakeUndraggable();
			AllowTransparency = true;
		}
		private void MovableOverlay_Move(object sender, EventArgs e)
		{
			BoundsChanged(sender, IsDragging || IsResizing);
		}
		private void MovableOverlay_ResizeEnd(object sender, EventArgs e)
		{
			BoundsChanged(sender, IsDragging || IsResizing);
		}
		/// <summary>
		/// Moves and resizes the form to match the specified bounds and then ensures that the form is completely on one of the connected screens.
		/// </summary>
		/// <param name="bounds"></param>
		public void SetBoundsOnScreen(Rectangle bounds)
		{
			if (!bounds.IsEmpty)
			{
				this.StartPosition = FormStartPosition.Manual;
				this.Location = bounds.Location;
				this.Size = bounds.Size;
			}
			FormUtil.MoveOntoScreen(this);
		}
		#region Draggable
		/// <summary>
		/// True if the form is currently being moved by the user.
		/// </summary>
		public bool IsDragging { get; private set; } = false;
		private Point pWindowOffsetFromMouse;
		private void btnLockPosition_Click(object sender, EventArgs e)
		{
			MakeUndraggable();
		}
		private void Move_MouseDown(object sender, MouseEventArgs e)
		{
			if (!IsDraggable)
				return;
			Point mGlobal = Cursor.Position;
			pWindowOffsetFromMouse = new Point(this.Left - mGlobal.X, this.Top - mGlobal.Y);
			IsDragging = true;
		}
		private void DragImpl(MouseEventArgs e)
		{
			Point mGlobal = Cursor.Position;
			this.Location = new Point(mGlobal.X + pWindowOffsetFromMouse.X, mGlobal.Y + pWindowOffsetFromMouse.Y);
		}
		#endregion
		#region Resizable
		/// <summary>
		/// True if the form is currently being resized by the user.
		/// </summary>
		public bool IsResizing { get; private set; } = false;
		private Point pResizeStart_MousePos;
		private Size sResizeStart_Size;
		private void Resize_MouseDown(object sender, MouseEventArgs e)
		{
			if (!IsDraggable)
				return;
			pResizeStart_MousePos = Cursor.Position;
			sResizeStart_Size = this.Size;
			IsResizing = true;
		}
		private void ResizeImpl(MouseEventArgs e)
		{
			Point mGlobal = Cursor.Position;
			int xGrowth = mGlobal.X - pResizeStart_MousePos.X;
			int yGrowth = mGlobal.Y - pResizeStart_MousePos.Y;
			int newW = Math.Max(100, sResizeStart_Size.Width + xGrowth);
			int newH = Math.Max(100, sResizeStart_Size.Height + yGrowth);
			this.Size = new Size(newW, newH);
			this.Invalidate();
		}
		#endregion
		#region Drag / Resize Misc
		public bool IsDraggable { get; private set; } = false;
		private const int WM_NCHITTEST = 0x84;
		private const int HTTRANSPARENT = -1;
		protected override void WndProc(ref Message m)
		{
			if (!IsDraggable)
			{
				if (m.Msg == WM_NCHITTEST)
					m.Result = (IntPtr)HTTRANSPARENT;
				else
					base.WndProc(ref m);
			}
			else
				base.WndProc(ref m);
		}
		public void MakeDraggable()
		{
			IsDraggable = true;
			IsDragging = false;
			IsResizing = false;
			panelDragMe.Visible = true;
			panelResizeMe.Visible = true;
			// Due to a quirk of Windows Forms, only the "Red" color allows the form to accept user input.
			BackColor = Color.Red;
			TransparencyKey = Color.Red;
			DraggableSet(this, true);
			this.Invalidate();
		}
		public void MakeUndraggable()
		{
			IsDraggable = false;
			IsDragging = false;
			IsResizing = false;
			panelDragMe.Visible = false;
			panelResizeMe.Visible = false;
			// Due to a quirk of Windows Forms, only the "Red" color allows the form to accept user input.
			// Otherwise if you set BackColor and TransparencyKey to the same non-red, non-alpha-including color,
			// it does not allow user input.
			BackColor = TransparencyKey = Color.FromArgb(255, 1, 255);
			DraggableSet(this, false);
			this.Invalidate();
		}
		private void MoveResize_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsDragging)
				DragImpl(e);
			if (IsResizing)
				ResizeImpl(e);
		}
		private void MoveResize_MouseUp(object sender, MouseEventArgs e)
		{
			bool didEnd = IsDragging || IsResizing;
			if (IsDragging)
			{
				IsDragging = false;
				DragImpl(e);
			}
			if (IsResizing)
			{
				IsResizing = false;
				ResizeImpl(e);
			}
			if (didEnd)
				DragResizeEnd(this, e);
		}
		#endregion
		private void MovableOverlay_FormClosed(object sender, FormClosedEventArgs e)
		{
			IsClosed = true;
		}
	}
}