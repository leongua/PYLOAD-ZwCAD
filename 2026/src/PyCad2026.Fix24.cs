using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public int GetCommandActiveFlags()
        {
            try
            {
                MethodInfo mi = typeof(ZwSoft.ZwCAD.ApplicationServices.Application).GetMethod("GetSystemVariable", BindingFlags.Public | BindingFlags.Static);
                if (mi == null) return -1;
                object value = mi.Invoke(null, new object[] { "CMDACTIVE" });
                return value == null ? -1 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return -1;
            }
        }

        public Hashtable GetCommandChannelState()
        {
            Hashtable info = NewInfo();
            info["cmdactive"] = GetCommandActiveFlags();
            info["transcript_lines"] = _shellTranscript.Count;
            info["last_line"] = GetLastShellLine();
            return info;
        }

        public void FlushCommandChannelHard()
        {
            FlushCommandChannelHard(4, 3);
        }

        public void FlushCommandChannelHard(int cancelCount, int enterCount)
        {
            int c = Math.Max(0, cancelCount);
            int e = Math.Max(0, enterCount);
            for (int i = 0; i < c; i++)
            {
                _doc.SendStringToExecute("\x03", true, false, false);
            }
            for (int i = 0; i < e; i++)
            {
                _doc.SendStringToExecute(" ", true, false, false);
            }
        }

        public void RunCommandSafe(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText)) return;
            FlushCommandChannelHard(1, 0);
            RunCommandNoiseFree(commandText);
            FlushCommandChannelHard(0, 1);
        }

        public void RunCommandsSafe(IList commandTexts)
        {
            if (commandTexts == null) return;
            FlushCommandChannelHard(1, 0);
            RunCommandsNoiseFree(commandTexts);
            FlushCommandChannelHard(0, 1);
        }

        public Hashtable RunCommandMacro(IList commandTexts, bool hardFlushBefore, bool hardFlushAfter, int trailingEnters)
        {
            Hashtable info = NewInfo();
            if (hardFlushBefore) FlushCommandChannelHard();
            RunCommandsNoiseFree(commandTexts);
            int enterCount = Math.Max(0, trailingEnters);
            for (int i = 0; i < enterCount; i++)
            {
                _doc.SendStringToExecute(" ", true, false, false);
            }
            if (hardFlushAfter) FlushCommandChannelHard();
            info["sent"] = commandTexts == null ? 0 : commandTexts.Count;
            info["hard_flush_before"] = hardFlushBefore;
            info["hard_flush_after"] = hardFlushAfter;
            info["trailing_enters"] = enterCount;
            info["cmdactive"] = GetCommandActiveFlags();
            return info;
        }

        public void ZoomExtentsSafe()
        {
            if (!TryZoomExtentsNoCmd())
            {
                RunCommandSafe("_.ZOOM _E");
            }
        }

        public void ZoomPreviousSafe()
        {
            RunCommandSafe("_.ZOOM _P");
        }

        public void ZoomCenterSafe(double x, double y, double z, double height)
        {
            if (!TrySetViewCenterNoCmd(x, y, height))
            {
                RunCommandSafe(string.Format(CultureInfo.InvariantCulture, "_.ZOOM _C {0},{1},{2} {3}", x, y, z, height));
            }
        }

        public void ZoomWindowSafe(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            if (!TrySetViewWindowNoCmd(x1, y1, x2, y2))
            {
                RunCommandSafe(string.Format(
                    CultureInfo.InvariantCulture,
                    "_.ZOOM _W {0},{1},{2} {3},{4},{5}",
                    x1, y1, z1, x2, y2, z2));
            }
        }

        public void RegenSafe()
        {
            RunCommandSafe("_.REGEN");
        }

        public void AuditSafe(bool fixErrors)
        {
            RunCommandSafe("_.AUDIT " + (fixErrors ? "_Y" : "_N"));
        }

        public void PrincQuiet(string text)
        {
            string safe = (text ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
            string expr = "(progn (princ \"" + safe + "\") (princ))";
            RunLispQuiet(expr);
        }

        public void RunLispMacro(IList expressions, bool hardFlushBefore, bool hardFlushAfter)
        {
            if (hardFlushBefore) FlushCommandChannelHard();
            if (expressions != null)
            {
                foreach (object raw in expressions)
                {
                    string expr = raw == null ? string.Empty : Convert.ToString(raw, CultureInfo.InvariantCulture);
                    if (string.IsNullOrWhiteSpace(expr)) continue;
                    RunLispQuiet(expr);
                }
            }
            if (hardFlushAfter) FlushCommandChannelHard();
        }

        public Hashtable ValidateCommandPipeline(IList smokeCommands)
        {
            Hashtable info = NewInfo();
            int before = GetCommandActiveFlags();
            try
            {
                RunCommandMacro(smokeCommands, true, false, 1);
                info["ok"] = true;
            }
            catch (Exception ex)
            {
                info["ok"] = false;
                info["error"] = ex.Message;
            }
            int after = GetCommandActiveFlags();
            info["cmdactive_before"] = before;
            info["cmdactive_after"] = after;
            return info;
        }

        public bool TrySetViewCenterNoCmd(double x, double y, double height)
        {
            if (height <= 0.0) return false;
            try
            {
                object view = GetCurrentViewObjectFix24();
                if (view == null) return false;
                Type vt = view.GetType();
                PropertyInfo centerPi = vt.GetProperty("CenterPoint");
                PropertyInfo widthPi = vt.GetProperty("Width");
                PropertyInfo heightPi = vt.GetProperty("Height");
                if (centerPi == null || widthPi == null || heightPi == null) return false;

                double oldWidth = Convert.ToDouble(widthPi.GetValue(view, null), CultureInfo.InvariantCulture);
                if (oldWidth <= 0.0) oldWidth = height;
                double oldHeight = Convert.ToDouble(heightPi.GetValue(view, null), CultureInfo.InvariantCulture);
                if (oldHeight <= 0.0) oldHeight = height;
                double aspect = oldWidth / oldHeight;
                if (aspect <= 0.0) aspect = 1.0;

                centerPi.SetValue(view, new Point2d(x, y), null);
                heightPi.SetValue(view, height, null);
                widthPi.SetValue(view, height * aspect, null);
                return SetCurrentViewObjectFix24(view);
            }
            catch
            {
                return false;
            }
        }

        public bool TrySetViewWindowNoCmd(double x1, double y1, double x2, double y2)
        {
            try
            {
                double minX = Math.Min(x1, x2);
                double maxX = Math.Max(x1, x2);
                double minY = Math.Min(y1, y2);
                double maxY = Math.Max(y1, y2);
                double w = maxX - minX;
                double h = maxY - minY;
                if (w <= 1e-9 || h <= 1e-9) return false;

                object view = GetCurrentViewObjectFix24();
                if (view == null) return false;
                Type vt = view.GetType();
                PropertyInfo centerPi = vt.GetProperty("CenterPoint");
                PropertyInfo widthPi = vt.GetProperty("Width");
                PropertyInfo heightPi = vt.GetProperty("Height");
                if (centerPi == null || widthPi == null || heightPi == null) return false;

                centerPi.SetValue(view, new Point2d((minX + maxX) * 0.5, (minY + maxY) * 0.5), null);
                widthPi.SetValue(view, w, null);
                heightPi.SetValue(view, h, null);
                return SetCurrentViewObjectFix24(view);
            }
            catch
            {
                return false;
            }
        }

        public bool TryZoomExtentsNoCmd()
        {
            try
            {
                Point3d min = _db.Extmin;
                Point3d max = _db.Extmax;
                double w = max.X - min.X;
                double h = max.Y - min.Y;
                if (w <= 1e-9 || h <= 1e-9) return false;
                return TrySetViewWindowNoCmd(min.X, min.Y, max.X, max.Y);
            }
            catch
            {
                return false;
            }
        }

        private object GetCurrentViewObjectFix24()
        {
            MethodInfo mi = _ed.GetType().GetMethod("GetCurrentView", BindingFlags.Public | BindingFlags.Instance);
            if (mi == null) return null;
            return mi.Invoke(_ed, null);
        }

        private bool SetCurrentViewObjectFix24(object view)
        {
            if (view == null) return false;
            MethodInfo mi = _ed.GetType().GetMethod("SetCurrentView", BindingFlags.Public | BindingFlags.Instance);
            if (mi == null) return false;
            mi.Invoke(_ed, new[] { view });
            return true;
        }
    }
}
