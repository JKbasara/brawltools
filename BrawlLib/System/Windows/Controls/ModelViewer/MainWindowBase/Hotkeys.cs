﻿using BrawlLib.Modeling;
using BrawlLib.OpenGL;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.SSBBTypes;
using Gif.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Windows.Forms
{
    public partial class ModelEditorBase : UserControl
    {
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Ctrl { get { return ModifierKeys.HasFlag(Keys.Control); } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Alt { get { return ModifierKeys.HasFlag(Keys.Alt); } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Shift { get { return ModifierKeys.HasFlag(Keys.Shift); } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CtrlAlt { get { return Ctrl && Alt; } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool NotCtrlAlt { get { return !Ctrl && !Alt; } }

        private Dictionary<Keys, Func<bool>> _hotKeysDown;
        private Dictionary<Keys, Func<bool>> _hotKeysUp;

        /// <summary>
        /// This handles all key input in the main control instead of through child controls.
        /// If you want a hotkey to work only when a specific control is focused,
        /// use the control's 'Focused' bool in the hotkey's function.
        /// </summary>
        protected override bool ProcessKeyPreview(ref Message m)
        {
            const int WM_KEYDOWN = 0x100;
            const int WM_KEYUP = 0x101;
            //const int WM_CHAR = 0x102;
            //const int WM_SYSCHAR = 0x106;
            const int WM_SYSKEYDOWN = 0x104;
            const int WM_SYSKEYUP = 0x105;
            //const int WM_IME_CHAR = 0x286;

            bool down = m.Msg == WM_KEYDOWN || m.Msg == WM_SYSKEYDOWN;
            bool up = m.Msg == WM_KEYUP || m.Msg == WM_SYSKEYUP;

            if (down || up)
            {
                Keys key;
                if (m.Msg == WM_SYSKEYDOWN || m.Msg == WM_SYSKEYUP)
                    key = ModifierKeys;
                else
                {
                    key = (Keys)m.WParam;
                    if (Ctrl)
                        key |= Keys.Control;
                    if (Alt)
                        key |= Keys.Alt;
                    if (Shift)
                        key |= Keys.Shift;
                }

                if (down && _hotKeysDown.ContainsKey(key))
                {
                    if ((bool)_hotKeysDown[key].DynamicInvoke())
                        return true;
                }
                else if (_hotKeysUp.ContainsKey(key))
                {
                    if ((bool)_hotKeysUp[key].DynamicInvoke())
                        return true;
                }
            }

            return base.ProcessKeyPreview(ref m);
        }

        public class HotKeyInfo
        {
            public Keys _baseKey;
            public bool _ctrl, _alt, _shift, _keyDown, _keyUp;
            public Func<bool> _function;

            public Keys KeyCode
            {
                get
                {
                    Keys key = _baseKey;
                    if (_ctrl)
                        key |= Keys.Control;
                    if (_shift)
                        key |= Keys.Shift;
                    if (_alt)
                        key |= Keys.Alt;
                    return key;
                }
            }

            public HotKeyInfo(Keys baseKey, bool ctrl, bool alt, bool shift, Func<bool> function, bool keydown = true, bool keyup = false)
            {
                _baseKey = baseKey;
                _ctrl = ctrl;
                _alt = alt;
                _shift = shift;
                _function = function;
                _keyDown = keydown;
                _keyUp = keyup;
            }
        }

        public virtual void InitHotkeyList()
        {
            _hotkeyList = new List<HotKeyInfo>()
            {
                new HotKeyInfo(Keys.G, false, false, false, HotkeyRefreshReferences),
                new HotKeyInfo(Keys.U, false, false, false, HotkeyResetCamera),
                new HotKeyInfo(Keys.B, false, false, false, HotkeyRenderBones),
                new HotKeyInfo(Keys.F, false, false, false, HotkeyRenderFloor),
                new HotKeyInfo(Keys.P, false, false, false, HotkeyRenderPolygons),
                new HotKeyInfo(Keys.C, true, false, true, HotkeyCopyWholeFrame),
                new HotKeyInfo(Keys.C, true, false, false, HotkeyCopyEntryFrame),
                new HotKeyInfo(Keys.Space, false, false, false, HotkeyPlayAnim),
                new HotKeyInfo(Keys.Back, true, false, true, HotkeyClearWholeFrame),
                new HotKeyInfo(Keys.Back, true, false, false, HotkeyClearEntryFrame),
                new HotKeyInfo(Keys.Back, false, false, true, HotkeyDeleteFrame),
                new HotKeyInfo(Keys.Escape, false, false, false, HotkeyCancelChange),
                new HotKeyInfo(Keys.PageUp, true, false, false, HotkeyLastFrame),
                new HotKeyInfo(Keys.PageUp, false, false, false, HotkeyNextFrame),
                new HotKeyInfo(Keys.PageDown, true, false, false, HotkeyFirstFrame),
                new HotKeyInfo(Keys.PageDown, false, false, false, HotkeyPrevFrame),
                new HotKeyInfo(Keys.V, true, true, true, HotkeyPasteWholeFrameKeyframesOnly),
                new HotKeyInfo(Keys.V, true, false, true, HotkeyPasteWholeFrame),
                new HotKeyInfo(Keys.V, true, true, false, HotkeyPasteEntryFrameKeyframesOnly),
                new HotKeyInfo(Keys.V, true, false, false, HotkeyPasteEntryFrame),
                new HotKeyInfo(Keys.V, false, false, false, HotkeyRenderVertices),
                new HotKeyInfo(Keys.I, true, true, false, HotkeyCaptureScreenshotTransparent),
                new HotKeyInfo(Keys.I, true, false, true, HotkeyCaptureScreenshot),
                new HotKeyInfo(Keys.Z, true, false, false, HotkeyUndo),
                new HotKeyInfo(Keys.Y, true, false, false, HotkeyRedo),

#if DEBUG
                new HotKeyInfo(Keys.M, false, false, false, HotkeyRenderDepthPressed),
                new HotKeyInfo(Keys.M, false, false, false, HotkeyRenderDepthReleased, false, true),
#endif
            };
        }

#if DEBUG
        private bool HotkeyRenderDepthPressed()
        {
            if (ModelPanel.Focused && !_renderDepth)
            {
                _renderDepth = true;
                ModelPanel.Invalidate();
                return true;
            }
            return false; 
        }
        private bool HotkeyRenderDepthReleased()
        {
            if (ModelPanel.Focused && _renderDepth)
            {
                _renderDepth = false;
                ModelPanel.Invalidate();
                return true;
            }
            return false; 
        }
#endif

        private bool HotkeyCaptureScreenshotTransparent()
        {
            if (ModelPanel.Focused)
            {
                SaveScreenCapture(ModelPanel.GetScreenshot(ModelPanel.ClientRectangle, true), ScreenCaptureFolder, "." + ScreenCaptureType.ToString(), this);
                return true;
            }
            return false;
        }
        private bool HotkeyCaptureScreenshot()
        {
            if (ModelPanel.Focused)
            {
                SaveScreenCapture(ModelPanel.GetScreenshot(ModelPanel.ClientRectangle, false), ScreenCaptureFolder, "." + ScreenCaptureType.ToString(), this);
                return true;
            }
            return false;
        }
        private bool HotkeyUndo()
        {
            if (ModelPanel.Focused)
            {
                Undo();
                return true;
            }
            return false;
        }
        private bool HotkeyRedo()
        {
            if (ModelPanel.Focused)
            {
                Redo();
                return true;
            }
            return false;
        }
        private bool HotkeyCopyWholeFrame()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor.btnCopyAll.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyCopyEntryFrame()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor.btnCopy.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyPasteWholeFrameKeyframesOnly()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor._onlyKeys = true;
                CHR0Editor.btnPasteAll.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyPasteWholeFrame()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor._onlyKeys = false;
                CHR0Editor.btnPasteAll.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyPasteEntryFrameKeyframesOnly()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor._onlyKeys = true;
                CHR0Editor.btnPaste.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyPasteEntryFrame()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor._onlyKeys = false;
                CHR0Editor.btnPaste.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyResetCamera()
        {
            if (Ctrl)
            {
                ModelPanel.ResetCamera();
                return true;
            }
            return false;
        }
        private bool HotkeyRefreshReferences()
        {
            if (ModelPanel.Focused)
            {
                ModelPanel.RefreshReferences();
                return true;
            }
            return false;
        }
        private bool HotkeyRenderVertices()
        {
            if (ModelPanel.Focused)
            {
                RenderVertices = !RenderVertices;
                return true;
            }
            return false;
        }
        private bool HotkeyPlayAnim()
        {
            if (ModelPanel.Focused)
            {
                TogglePlay();
                return true;
            }
            return false;
        }
        private bool HotkeyRenderFloor()
        {
            if (ModelPanel.Focused)
            {
                RenderFloor = !RenderFloor;
                return true;
            }
            return false;
        }
        private bool HotkeyRenderBones()
        {
            if (ModelPanel.Focused)
            {
                RenderBones = !RenderBones;
                return true;
            }
            return false;
        }
        private bool HotkeyRenderPolygons()
        {
            if (ModelPanel.Focused)
            {
                RenderPolygons = !RenderPolygons;
                return true;
            }
            return false;
        }
        private bool HotkeyClearWholeFrame()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor.btnClearAll.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyClearEntryFrame()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor.ClearEntry();
                return true;
            }
            return false;
        }
        private bool HotkeyDeleteFrame()
        {
            if (ModelPanel.Focused && _currentControl is CHR0Editor)
            {
                CHR0Editor.btnDelete.PerformClick();
                return true;
            }
            return false;
        }
        private bool HotkeyCancelChange()
        {
            //Undo transformations, make sure to reset keyframes
            if (_rotating)
            {
                _rotating = false;
                CHR0Editor.numRotX.Value = _oldAngles._x;
                CHR0Editor.numRotY.Value = _oldAngles._y;
                CHR0Editor.numRotZ.Value = _oldAngles._z;
                CHR0Editor.BoxChanged(CHR0Editor.numRotX, null);
                CHR0Editor.BoxChanged(CHR0Editor.numRotY, null);
                CHR0Editor.BoxChanged(CHR0Editor.numRotZ, null);
            }
            if (_translating)
            {
                _translating = false;
                CHR0Editor.numTransX.Value = _oldPosition._x;
                CHR0Editor.numTransY.Value = _oldPosition._y;
                CHR0Editor.numTransZ.Value = _oldPosition._z;
                CHR0Editor.BoxChanged(CHR0Editor.numTransX, null);
                CHR0Editor.BoxChanged(CHR0Editor.numTransY, null);
                CHR0Editor.BoxChanged(CHR0Editor.numTransZ, null);
            }
            if (_scaling)
            {
                _scaling = false;
                CHR0Editor.numScaleX.Value = _oldScale._x;
                CHR0Editor.numScaleY.Value = _oldScale._y;
                CHR0Editor.numScaleZ.Value = _oldScale._z;
                CHR0Editor.BoxChanged(CHR0Editor.numScaleX, null);
                CHR0Editor.BoxChanged(CHR0Editor.numScaleY, null);
                CHR0Editor.BoxChanged(CHR0Editor.numScaleZ, null);
            }
            ModelPanel.CurrentViewport.AllowSelection = true;
            return false;
        }
        private bool HotkeyLastFrame()
        {
            if (PlaybackPanel != null)
                PlaybackPanel.btnLast_Click(this, null);
            return true;
        }
        private bool HotkeyNextFrame()
        {
            if (PlaybackPanel != null)
                PlaybackPanel.btnNextFrame_Click(this, null);
            return true;
        }
        private bool HotkeyFirstFrame()
        {
            if (PlaybackPanel != null)
                PlaybackPanel.btnFirst_Click(this, null);
            return true;
        }
        private bool HotkeyPrevFrame()
        {
            if (PlaybackPanel != null)
                PlaybackPanel.btnPrevFrame_Click(this, null);
            return true;
        }
    }
}