﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace DND.Gui.Zen
{
    public abstract class ZenControlBase : IDisposable
    {
        private ZenControlBase parent;
        private readonly List<ZenControlBase> zenChildren = new List<ZenControlBase>();
        private readonly List<Control> winFormsControls = new List<Control>();
        private Rectangle absRect = new Rectangle(0, 0, 0, 0);

        protected ZenControlBase(ZenControlBase parent)
        {
            this.parent = parent;
            if (Parent != null) Parent.zenChildren.Add(this);
        }

        public virtual void Dispose()
        {
            foreach (ZenControlBase ctrl in zenChildren) ctrl.Dispose();
        }

        public virtual float Scale
        {
            get { return Parent.Scale; }
        }

        public Size Size
        {
            get { return absRect.Size; }
            set { absRect.Size = value; OnSizeChanged(); }
        }

        public virtual Rectangle AbsRect
        {
            get { return absRect; }
        }

        public Rectangle RelRect
        {
            get { return new Rectangle(absRect.X - Parent.AbsRect.X, absRect.Y - Parent.AbsRect.Y, absRect.Width, absRect.Height); }
        }

        public int AbsLeft
        {
            get { return absRect.X; }
            set
            {
                int diff = value - absRect.X;
                absRect.Location = new Point(value, absRect.Location.Y);
                foreach (ZenControl ctrl in zenChildren) ctrl.AbsLeft += diff;
            }
        }

        public int AbsRight
        {
            get { return absRect.X + absRect.Width - 1; }
        }

        public int AbsTop
        {
            get { return absRect.Y; }
            set
            {
                int diff = value - absRect.Y;
                absRect.Location = new Point(absRect.X, value);
                foreach (ZenControl ctrl in zenChildren) ctrl.AbsTop += diff;
            }
        }

        public int AbsBottom
        {
            get { return absRect.Y + absRect.Height - 1; }
        }

        public int Width
        {
            get { return absRect.Width; }
            set
            {
                absRect.Size = new Size(value, absRect.Height);
                OnSizeChanged();
            }
        }

        public int Height
        {
            get { return absRect.Height; }
            set
            {
                absRect.Size = new Size(absRect.Width, value);
                OnSizeChanged();
            }
        }

        public virtual Size LogicalSize
        {
            set
            {
                float w = ((float)value.Width) * Scale;
                float h = ((float)value.Height) * Scale;
                Size newSize = new Size((int)w, (int)h);
                absRect.Size = newSize;
                OnSizeChanged();
            }
            get { return new Size((int)(absRect.Width / Scale), (int)(absRect.Height / Scale)); }
        }

        public Point AbsLocation
        {
            get { return absRect.Location; }
            set
            {
                Point newLoc = value;
                int diffX = newLoc.X - absRect.X;
                int diffY = newLoc.Y - absRect.Y;
                absRect.Location = newLoc;
                foreach (ZenControl ctrl in zenChildren)
                {
                    Point childNewLoc = new Point(ctrl.AbsLocation.X + diffX, ctrl.AbsLocation.Y + diffY);
                    ctrl.AbsLocation = childNewLoc;
                }
            }
        }

        public Point RelLocation
        {
            get { return RelRect.Location; }
            set
            {
                Point newLoc = value;
                int diffX = newLoc.X - RelRect.X;
                int diffY = newLoc.Y - RelRect.Y;
                absRect.Location = new Point(absRect.X + diffX, absRect.Y + diffY);
                foreach (ZenControl ctrl in zenChildren)
                {
                    Point childNewLoc = new Point(ctrl.AbsLocation.X + diffX, ctrl.AbsLocation.Y + diffY);
                    ctrl.AbsLocation = childNewLoc;
                }
            }
        }

        public Point AbsLogicalLocation
        {
            set
            {
                float x = ((float)value.X) * Scale;
                float y = ((float)value.Y) * Scale;
                AbsLocation = new Point((int)x, (int)y);
            }
            get { return new Point((int)(absRect.X / Scale), (int)(absRect.Y / Scale)); }
        }

        public Point RelLogicalLocation
        {
            set
            {
                float x = ((float)value.X) * Scale;
                float y = ((float)value.Y) * Scale;
                RelLocation = new Point((int)x, (int)y);
            }
            get { return new Point((int)(RelRect.X / Scale), (int)(RelRect.Y / Scale)); }
        }

        protected virtual void OnSizeChanged()
        {
        }

        public abstract void DoPaint(Graphics g);

        protected SizeF MeasureText(string text, Font font, StringFormat fmt)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.MeasureString(text, font, int.MaxValue, fmt);
            }
        }

        protected void DoPaintChildren(Graphics g)
        {
            foreach (ZenControl ctrl in zenChildren)
                ctrl.DoPaint(g);
        }

        protected void MakeMePaint(bool needBackground, RenderMode rm)
        {
            Parent.MakeCtrlPaint(this, needBackground, rm);
        }

        internal virtual void MakeCtrlPaint(ZenControlBase ctrl, bool needBackground, RenderMode rm)
        {
            Parent.MakeCtrlPaint(ctrl, needBackground, rm);
        }

        protected virtual Point MousePositionAbs
        {
            get { return Parent.MousePositionAbs; }
        }

        protected virtual void RegisterWinFormsControl(Control c)
        {
            winFormsControls.Add(c);
            Parent.RegisterWinFormsControl(c);
        }

        protected virtual void InvokeOnForm(Delegate method)
        {
            Parent.InvokeOnForm(method);
        }


        public ZenControlBase Parent
        {
            get { return parent; }
        }

        protected ReadOnlyCollection<ZenControlBase> ZenChildren
        {
            get { return new ReadOnlyCollection<ZenControlBase>(zenChildren); }
        }

        protected void RemoveChild(ZenControlBase ctrl)
        {
            IEnumerable<Control> containedWinFormsControls = ctrl.GetWinFormsControlsRecursive();
            foreach (Control c in containedWinFormsControls)
                RemoveWinFormsControl(c);
            zenChildren.Remove(ctrl);
            ctrl.parent = null;
        }

        protected void AddChild(ZenControlBase ctrl)
        {
            if (zenChildren.Contains(ctrl))
            {
                if (ctrl.Parent != this)
                    throw new InvalidOperationException("Control is already a child of a different parent.");
                return;
            }
            ctrl.parent = this;
            zenChildren.Add(ctrl);
            IEnumerable<Control> containedWinFormsControls = ctrl.GetWinFormsControlsRecursive();
            foreach (Control c in containedWinFormsControls)
                AddWinFormsControl(c);
        }

        internal virtual void AddWinFormsControl(Control c)
        {
            Parent.AddWinFormsControl(c);
        }

        internal virtual void RemoveWinFormsControl(Control c)
        {
            Parent.RemoveWinFormsControl(c);
        }

        internal IEnumerable<Control> GetWinFormsControlsRecursive()
        {
            List<Control> res = new List<Control>(winFormsControls);
            foreach (ZenControlBase child in zenChildren)
                res.AddRange(child.GetWinFormsControlsRecursive());
            return res;
        }

        protected ZenControl GetControl(Point pParent)
        {
            foreach (ZenControl ctrl in zenChildren)
                if (ctrl.Contains(pParent)) return ctrl;
            return null;
        }

        protected Point TranslateToControl(ZenControl ctrl, Point pParent)
        {
            int x = pParent.X - ctrl.RelRect.X;
            int y = pParent.Y - ctrl.RelRect.Y;
            return new Point(x, y);
        }
    }
}