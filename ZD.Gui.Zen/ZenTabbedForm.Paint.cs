﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ZD.Gui.Zen
{
    partial class ZenTabbedForm
    {
        private void doPaintBackground(Graphics g)
        {
            // Header: solid color in top right, full area behind system buttons
            using (Brush b = new SolidBrush(ZenParams.HeaderBackColorR))
            {
                g.FillRectangle(b, btnMinimize.RelLeft, 0, canvas.Width - btnMinimize.RelLeft, headerHeight);
            }
            // Gradient transition to left edge from there
            Rectangle grRect = new Rectangle(0, 0, btnMinimize.RelLeft, headerHeight);
            using (LinearGradientBrush lgb = new LinearGradientBrush(grRect, ZenParams.HeaderBackColorL, ZenParams.HeaderBackColorR, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(lgb, grRect);
            }

            // For content tab and main tab, pad with different color
            Color colPad = ZenParams.PaddingBackColor;
            if (activeTabIdx == -1) colPad = Color.White;
            using (Brush b = new SolidBrush(colPad))
            {
                g.FillRectangle(b, innerPadding, headerHeight, canvas.Width - 2 * innerPadding, innerPadding);
                g.FillRectangle(b, 0, headerHeight, innerPadding, canvas.Height - headerHeight);
                g.FillRectangle(b, canvas.Width - innerPadding, headerHeight, innerPadding, canvas.Height - headerHeight);
                g.FillRectangle(b, innerPadding, canvas.Height - innerPadding - 1, canvas.Width - 2 * innerPadding, innerPadding);
            }
            using (Pen p = new Pen(ZenParams.BorderColor))
            {
                p.Width = 1;
                g.DrawRectangle(p, 0, 0, canvas.Width - 1, canvas.Height - 1);
            }
        }

        private void doPaintHeaderText(Graphics g)
        {
            // Text in header: my window title
            float x;
            if (contentTabControls.Count == 0) x = mainTabCtrl.AbsRight;
            else x = contentTabControls[contentTabControls.Count - 1].AbsRight;
            x += ZenParams.HeaderTabPadding * 3.0F;
            float y = 7.0F * Scale;
            float w = btnClose.AbsLeft - x;
            RectangleF rectHeader = new RectangleF(x, y, btnClose.AbsLeft - w, headerHeight - y);
            using (Brush b = new SolidBrush(ZenParams.HeaderFontColor))
            using (Font f = SystemFontProvider.Instance.GetSystemFont(FontStyle.Regular, ZenParams.HeaderFontSize))
            {
                SizeF hsz;
                StringFormat sf = StringFormat.GenericTypographic;
                // Header is not ellipsed yet. Measure full text. Maybe it just fits.
                if (headerEllipsed == null)
                {
                    hsz = g.MeasureString(header, f, 65535, sf);
                    if (hsz.Width < w) headerEllipsed = header;
                }
                // Our manual ellipsis - or not
                if (headerEllipsed == null)
                {
                    headerEllipsed = header.Substring(0, header.Length - 1) + "…";
                    while (true)
                    {
                        if (headerEllipsed.Length == 1) break;
                        hsz = g.MeasureString(headerEllipsed, f, 65535, sf);
                        if (hsz.Width < w) break;
                        headerEllipsed = headerEllipsed.Substring(0, headerEllipsed.Length - 2) + "…";
                    }
                }
                // Draw ellipsed text - centered
                hsz = g.MeasureString(headerEllipsed, f, 65535, sf);
                rectHeader.Width = hsz.Width + 1.0F;
                if (headerEllipsed == header)
                    rectHeader.X = x + (w - rectHeader.Width) / 2.0F;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.DrawString(headerEllipsed, f, b, rectHeader, sf);
            }
        }

        /// <summary>
        /// Paints tooltip for a single control.
        /// </summary>
        private void doPaintTooltip(Graphics g, Font f, TooltipToPaint ttp)
        {
            int width = AbsRect.Width;
            int height = AbsRect.Height;
            // Painting children will have set all sorts of clip and transform
            g.ResetClip();
            g.ResetTransform();

            // This will be bubble's rectangle, and the polygon representing the needle.
            Rectangle brect;
            Point[] points;
            // This, the text's inside the bubble
            Rectangle trect;
            // Measure tooltip text as if we had all the width in the world (well, within canvas at least)
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            StringFormat sf = StringFormat.GenericDefault;
            SizeF szF = g.MeasureString(ttp.TTI.Tooltip.Text, f, width - 2 * tooltipPadding);
            Size sz = new Size((int)Math.Ceiling(szF.Width), (int)Math.Ceiling(szF.Height));

            // Tooltip is at the north
            if (ttp.TTI.Tooltip.TooltipLocation == TooltipLocation.North)
            {
                // Look at tooltip's left and right bounds: what control wishes, and how much space we have in window.
                // We assume control's smart enough to request a bubble that fits vertically. We control for breadth.
                int left, right;
                // Center aligned over needle
                if (ttp.TTI.Tooltip.TopOrSide == int.MinValue)
                {
                    left = ttp.TTI.Tooltip.NeedlePos + ttp.Ctrl.AbsLeft - sz.Width / 2 - tooltipPadding;
                    right = left + sz.Width + 2 * tooltipPadding;
                }
                // Right aligned
                else if (ttp.TTI.Tooltip.TopOrSide < 0)
                {
                    int x = -ttp.TTI.Tooltip.TopOrSide;
                    right = ttp.Ctrl.AbsLeft + x;
                    left = right - sz.Width - 2 * tooltipPadding;
                }
                // Left aligned
                else
                {
                    left = ttp.Ctrl.AbsLeft + ttp.TTI.Tooltip.TopOrSide;
                    right = left + sz.Width + 2 * tooltipPadding;
                }
                // Fix left and right so they're never outside canvas
                if (right > width) { left -= (right - width); right = width; }
                if (left < 0) { right += -left; left = 0; }
                // If this is less space than text's desired width, re-measure
                // May need more lines
                if (right - left - 2 * tooltipPadding < sz.Width)
                {
                    szF = g.MeasureString(ttp.TTI.Tooltip.Text, f, right - left);
                    sz = new Size((int)Math.Ceiling(szF.Width), (int)Math.Ceiling(szF.Height));
                }
                // So, we got our rectangles
                brect = new Rectangle(
                    left,
                    ttp.Ctrl.AbsTop - ttp.TTI.Tooltip.NeedleHeight - sz.Height - tooltipPadding,
                    sz.Width + 2 * tooltipPadding, sz.Height + tooltipPadding);
                trect = brect;
                trect.X += tooltipPadding;
                trect.Y += tooltipPadding / 2;
                trect.Width -= 2 * tooltipPadding;
                trect.Height -= tooltipPadding / 2;
                // Bubble with needle
                int npos = ttp.TTI.Tooltip.NeedlePos;
                int nheight = ttp.TTI.Tooltip.NeedleHeight;
                points = new Point[]
                {
                    new Point(ttp.Ctrl.AbsLeft + npos, ttp.Ctrl.AbsTop),
                    new Point(ttp.Ctrl.AbsLeft + npos + nheight, ttp.Ctrl.AbsTop - nheight),
                    new Point(brect.Right, brect.Bottom),
                    new Point(brect.Right, brect.Top),
                    new Point(brect.Left, brect.Top),
                    new Point(brect.Left, brect.Bottom),
                    new Point(ttp.Ctrl.AbsLeft + npos - nheight, ttp.Ctrl.AbsTop - nheight),
                    new Point(ttp.Ctrl.AbsLeft + npos, ttp.Ctrl.AbsTop),
                };
            }
            // Tooltip is at the west
            else if (ttp.TTI.Tooltip.TooltipLocation == TooltipLocation.West)
            {
                // Look at tooltip's top and bottom bounds: what control wishes, and how much space we have in window.
                // We assume control's smart enough to request a bubble that fits horizontally. We control for height.
                int top = 0;
                int bottom = 0;
                // Center aligned next to needle
                if (ttp.TTI.Tooltip.TopOrSide == int.MinValue)
                {
                    top = ttp.TTI.Tooltip.NeedlePos + ttp.Ctrl.AbsTop - sz.Height / 2 - tooltipPadding / 2;
                    bottom = top + sz.Height + tooltipPadding;
                }
                // Top aligned
                else if (ttp.TTI.Tooltip.TopOrSide > 0)
                {
                    int y = ttp.TTI.Tooltip.TopOrSide;
                    top = ttp.Ctrl.AbsTop + y;
                    bottom = top + sz.Height + tooltipPadding;
                }
                // Fix top and bottom so they're never outside canvas
                if (bottom > height) { bottom = height; top = bottom - sz.Height; }
                if (top < 0) { top = 0; bottom = top + height; }
                // So, we got our rectangles
                brect = new Rectangle(
                    ttp.Ctrl.AbsLeft - ttp.TTI.Tooltip.NeedleHeight - sz.Width - 2 * tooltipPadding, top,
                    sz.Width + 2 * tooltipPadding, sz.Height + tooltipPadding);
                trect = brect;
                trect.X += tooltipPadding;
                trect.Y += tooltipPadding / 2;
                trect.Width -= 2 * tooltipPadding;
                trect.Height -= tooltipPadding / 2;
                // Bubble with needle
                int npos = ttp.TTI.Tooltip.NeedlePos;
                int nwidth = ttp.TTI.Tooltip.NeedleHeight;
                points = new Point[]
                {
                    new Point(ttp.Ctrl.AbsLeft, ttp.Ctrl.AbsTop + npos),
                    new Point(ttp.Ctrl.AbsLeft - nwidth, ttp.Ctrl.AbsTop + npos - nwidth),
                    new Point(brect.Right, brect.Top),
                    new Point(brect.Left, brect.Top),
                    new Point(brect.Left, brect.Bottom),
                    new Point(brect.Right, brect.Bottom),
                    new Point(brect.Right, ttp.Ctrl.AbsTop + npos + nwidth),
                    new Point(ttp.Ctrl.AbsLeft, ttp.Ctrl.AbsTop + npos),
                };
            }
            // TO-DO: tooltip at east. Will do when needed.
            else
            {
                // These rectangles will crash FillPolygon below. Goed zo.
                brect = new Rectangle(0, 0, 0, 0);
                trect = new Rectangle(0, 0, 0, 0);
                points = new Point[0];
            }

            // Draw bubble
            g.SmoothingMode = SmoothingMode.HighQuality;
            float alfa = ((float)ZenParams.TooltipMaxAlfa) * ttp.Strength;
            using (Brush b = new SolidBrush(Color.FromArgb((int)alfa, ZenParams.TooltipBackColor)))
            {
                g.FillPolygon(b, points);
            }
            // Write text
            alfa = 255F * ttp.Strength;
            using (Brush b = new SolidBrush(Color.FromArgb((int)alfa, ZenParams.TooltipTextColor)))
            {
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.DrawString(ttp.TTI.Tooltip.Text, f, b, trect);
            }
        }

        public override sealed void DoPaint(Graphics g)
        {
            // If form is not yet loaded, don't bother with painting.
            // That happens during initialization
            if (!isLoaded) return;

            // Header, frame, content background...
            doPaintBackground(g);
            // Header text
            doPaintHeaderText(g);
            // All children
            DoPaintChildren(g);
            // Paint tooltips
            List<TooltipToPaint> ttpList = getTooltipsToPaint();
            if (ttpList.Count != 0)
            {
                using (Font f = SystemFontProvider.Instance.GetSystemFont(FontStyle.Regular, ZenParams.TooltipFontSize))
                {
                    foreach (TooltipToPaint ttp in ttpList) doPaintTooltip(g, f, ttp);
                }
            }
        }
    }
}
