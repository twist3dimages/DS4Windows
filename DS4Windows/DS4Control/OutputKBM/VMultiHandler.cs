﻿using System;
using System.Collections.Generic;
using System.Threading;
using VMultiDllWrapper;

namespace DS4Windows.DS4Control
{
    public class VMultiHandler : VirtualKBMBase
    {
        public const string DISPLAY_NAME = "VMulti";
        public const string IDENTIFIER = "vmulti";
        public const int MODIFIER_MASK = 1 << 9;
        public const int MODIFIER_MULTIMEDIA = 1 << 10;
        public const int MODIFIER_ENHANCED = 1 << 11;

        private VMulti vMulti = null;
        private RelativeMouseReport mouseReport = new RelativeMouseReport();
        private KeyboardReport keyReport = new KeyboardReport();
        private KeyboardEnhancedReport mediaKeyReport = new KeyboardEnhancedReport();
        private HashSet<KeyboardModifier> modifiers = new HashSet<KeyboardModifier>();
        private HashSet<KeyboardKey> pressedKeys = new HashSet<KeyboardKey>();

        // Used to guard reports and attempt to keep methods thread safe
        private ReaderWriterLockSlim eventLock = new ReaderWriterLockSlim();

        public VMultiHandler()
        {
            vMulti = new VMulti();
        }

        public override bool Connect()
        {
            return vMulti.connect();
        }

        public override bool Disconnect()
        {
            Release();
            vMulti.disconnect();
            return !vMulti.isConnected();
        }

        private void Release()
        {
            eventLock.EnterWriteLock();

            mouseReport.ResetMousePos();
            vMulti.updateMouse(mouseReport);

            foreach(KeyboardModifier mod in modifiers)
            {
                keyReport.keyUp(mod);
            }
            modifiers.Clear();

            foreach(KeyboardKey key in pressedKeys)
            {
                keyReport.keyUp(key);
            }
            pressedKeys.Clear();

            vMulti.updateKeyboard(keyReport);

#if VMULTI_CUSTOM
            mediaKeyReport.EnhancedKeys = 0;
            mediaKeyReport.MediaKeys = 0;
            vMulti.updateKeyboardEnhanced(mediaKeyReport);
#endif

            eventLock.ExitWriteLock();
        }

        public override void MoveRelativeMouse(int x, int y)
        {
#if VMULTI_CUSTOM
            const int MOUSE_MIN = -32767;
            const int MOUSE_MAX = 32767;
#else
            const int MOUSE_MIN = -127;
            const int MOUSE_MAX = 127;
#endif
            //Console.WriteLine("RAW MOUSE {0} {1}", x, y);
            eventLock.EnterWriteLock();

            mouseReport.ResetMousePos();

#if VMULTI_CUSTOM
            mouseReport.MouseX = (ushort)(x < MOUSE_MIN ? MOUSE_MIN : (x > MOUSE_MAX) ? MOUSE_MAX : x);
            mouseReport.MouseY = (ushort)(y < MOUSE_MIN ? MOUSE_MIN : (y > MOUSE_MAX) ? MOUSE_MAX : y);
#else
            mouseReport.MouseX = (byte)(x < MOUSE_MIN ? MOUSE_MIN : (x > MOUSE_MAX) ? MOUSE_MAX : x);
            mouseReport.MouseY = (byte)(y < MOUSE_MIN ? MOUSE_MIN : (y > MOUSE_MAX) ? MOUSE_MAX : y);
#endif
            //Console.WriteLine("LKJDFSLKJDFSLKJS {0} {1}", mouseReport.MouseX, mouseReport.MouseY);

            vMulti.updateMouse(mouseReport);

            eventLock.ExitWriteLock();
        }

        public override void PerformKeyPress(uint key)
        {
            //Console.WriteLine("PerformKeyPress {0}", key);
            bool sync = false;
            bool syncEnhanced = false;
            eventLock.EnterWriteLock();

            if (key < MODIFIER_MASK)
            {
                KeyboardKey temp = (KeyboardKey)key;
                if (!pressedKeys.Contains(temp))
                {
                    keyReport.keyDown(temp);
                    pressedKeys.Add(temp);
                    sync = true;
                }
            }
            else if (key < MODIFIER_MULTIMEDIA)
            {
                KeyboardModifier modifier = (KeyboardModifier)(key & ~MODIFIER_MASK);
                if (!modifiers.Contains(modifier))
                {
                    keyReport.keyDown(modifier);
                    modifiers.Add(modifier);
                    sync = true;
                }
            }
#if VMULTI_CUSTOM
            else if (key < MODIFIER_ENHANCED)
            {
                MultimediaKey temp = (MultimediaKey)(key & ~MODIFIER_MULTIMEDIA);
                mediaKeyReport.KeyDown(temp);
                syncEnhanced = true;
            }
            else
            {
                EnhancedKey temp = (EnhancedKey)(key & ~MODIFIER_ENHANCED);
                mediaKeyReport.KeyDown(temp);
                syncEnhanced = true;
            }
#endif

            if (sync)
            {
                vMulti.updateKeyboard(keyReport);
            }

            if (syncEnhanced)
            {
                vMulti.updateKeyboardEnhanced(mediaKeyReport);
            }

            eventLock.ExitWriteLock();
        }

        /// <summary>
        /// Just use normal routine
        /// </summary>
        /// <param name="key"></param>
        public override void PerformKeyPressAlt(uint key)
        {
            //Console.WriteLine("PerformKeyPressAlt {0}", key);
            bool sync = false;
            bool syncEnhanced = false;
            eventLock.EnterWriteLock();

            if (key < MODIFIER_MASK)
            {
                KeyboardKey temp = (KeyboardKey)key;
                if (!pressedKeys.Contains(temp))
                {
                    keyReport.keyDown(temp);
                    pressedKeys.Add(temp);
                    sync = true;
                }
            }
            else if (key < MODIFIER_MULTIMEDIA)
            {
                KeyboardModifier modifier = (KeyboardModifier)(key & ~MODIFIER_MASK);
                if (!modifiers.Contains(modifier))
                {
                    keyReport.keyDown(modifier);
                    modifiers.Add(modifier);
                    sync = true;
                }
            }
#if VMULTI_CUSTOM
            else if (key < MODIFIER_ENHANCED)
            {
                MultimediaKey temp = (MultimediaKey)(key & ~MODIFIER_MULTIMEDIA);
                mediaKeyReport.KeyDown(temp);
                syncEnhanced = true;
            }
            else
            {
                EnhancedKey temp = (EnhancedKey)(key & ~MODIFIER_ENHANCED);
                mediaKeyReport.KeyDown(temp);
                syncEnhanced = true;
            }
#endif

            if (sync)
            {
                vMulti.updateKeyboard(keyReport);
            }

            if (syncEnhanced)
            {
                vMulti.updateKeyboardEnhanced(mediaKeyReport);
            }

            eventLock.ExitWriteLock();
        }

        public override void PerformKeyRelease(uint key)
        {
            //Console.WriteLine("PerformKeyRelease {0}", key);
            bool sync = false;
            bool syncEnhanced = false;
            eventLock.EnterWriteLock();

            if (key < MODIFIER_MASK)
            {
                KeyboardKey temp = (KeyboardKey)key;
                if (pressedKeys.Contains(temp))
                {
                    keyReport.keyUp(temp);
                    pressedKeys.Remove(temp);
                    sync = true;
                }
            }
            else if (key < MODIFIER_MULTIMEDIA)
            {
                KeyboardModifier modifier = (KeyboardModifier)(key & ~MODIFIER_MASK);
                if (modifiers.Contains(modifier))
                {
                    keyReport.keyUp(modifier);
                    modifiers.Remove(modifier);
                    sync = true;
                }
            }
#if VMULTI_CUSTOM
            else if (key < MODIFIER_ENHANCED)
            {
                MultimediaKey temp = (MultimediaKey)(key & ~MODIFIER_MULTIMEDIA);
                mediaKeyReport.KeyUp(temp);
                syncEnhanced = true;
            }
            else
            {
                EnhancedKey temp = (EnhancedKey)(key & ~MODIFIER_ENHANCED);
                mediaKeyReport.KeyUp(temp);
                syncEnhanced = true;
            }
#endif

            if (sync)
            {
                vMulti.updateKeyboard(keyReport);
            }

            if (syncEnhanced)
            {
                vMulti.updateKeyboardEnhanced(mediaKeyReport);
            }

            eventLock.ExitWriteLock();
        }

        /// <summary>
        /// Just use normal routine
        /// </summary>
        /// <param name="key"></param>
        public override void PerformKeyReleaseAlt(uint key)
        {
            //Console.WriteLine("PerformKeyReleaseAlt {0}", key);
            bool sync = false;
            bool syncEnhanced = false;
            eventLock.EnterWriteLock();

            if (key < MODIFIER_MASK)
            {
                KeyboardKey temp = (KeyboardKey)key;
                if (pressedKeys.Contains(temp))
                {
                    keyReport.keyUp(temp);
                    pressedKeys.Remove(temp);
                    sync = true;
                }
            }
            else if (key < MODIFIER_MULTIMEDIA)
            {
                KeyboardModifier modifier = (KeyboardModifier)(key & ~MODIFIER_MASK);
                if (modifiers.Contains(modifier))
                {
                    keyReport.keyUp(modifier);
                    modifiers.Remove(modifier);
                    sync = true;
                }
            }
#if VMULTI_CUSTOM
            else if (key < MODIFIER_ENHANCED)
            {
                MultimediaKey temp = (MultimediaKey)(key & ~MODIFIER_MULTIMEDIA);
                mediaKeyReport.KeyUp(temp);
                syncEnhanced = true;
            }
            else
            {
                EnhancedKey temp = (EnhancedKey)(key & ~MODIFIER_ENHANCED);
                mediaKeyReport.KeyUp(temp);
                syncEnhanced = true;
            }
#endif

            if (sync)
            {
                vMulti.updateKeyboard(keyReport);
            }

            if (syncEnhanced)
            {
                vMulti.updateKeyboardEnhanced(mediaKeyReport);
            }

            eventLock.ExitWriteLock();
        }

        public override void PerformMouseButtonEvent(uint mouseButton)
        {
            bool sync = false;
            MouseButton temp = (MouseButton)mouseButton;
            eventLock.EnterWriteLock();

            mouseReport.ResetMousePos();

            if (!mouseReport.HeldButtons.Contains(temp))
            {
                mouseReport.ButtonDown(temp);
                sync = true;
            }
            else
            {
                mouseReport.ButtonUp(temp);
                sync = true;
            }

            if (sync)
            {
                vMulti.updateMouse(mouseReport);
            }

            eventLock.ExitWriteLock();
        }

        /// <summary>
        /// Just use normal routine
        /// </summary>
        /// <param name="mouseButton"></param>
        /// <param name="type"></param>
        public override void PerformMouseButtonEventAlt(uint mouseButton, int type)
        {
            bool sync = false;
            MouseButton temp = (MouseButton)mouseButton;
            eventLock.EnterWriteLock();

            mouseReport.ResetMousePos();

            if (!mouseReport.HeldButtons.Contains(temp))
            {
                mouseReport.ButtonDown(temp);
                sync = true;
            }
            else
            {
                mouseReport.ButtonUp(temp);
                sync = true;
            }

            if (sync)
            {
                vMulti.updateMouse(mouseReport);
            }

            eventLock.ExitWriteLock();
        }

        /// <summary>
        /// No support for horizontal mouse wheel in vmulti
        /// </summary>
        /// <param name="vertical"></param>
        /// <param name="horizontal"></param>
        public override void PerformMouseWheelEvent(int vertical, int horizontal)
        {
            eventLock.EnterWriteLock();
            mouseReport.ResetMousePos();
            mouseReport.WheelPosition = (byte)vertical;
            mouseReport.HWheelPosition = (byte)horizontal;
            vMulti.updateMouse(mouseReport);
            eventLock.ExitWriteLock();
        }

        public override string GetDisplayName()
        {
            return DISPLAY_NAME;
        }

        public override string GetIdentifier()
        {
            return IDENTIFIER;
        }

        public override void PerformMouseButtonPress(uint mouseButton)
        {
            bool sync = false;
            eventLock.EnterWriteLock();

            MouseButton tempButton = (MouseButton)mouseButton;
            if (!mouseReport.HeldButtons.Contains(tempButton))
            {
                mouseReport.ResetMousePos();
                mouseReport.ButtonDown(tempButton);
                sync = true;
            }

            if (sync)
            {
                vMulti.updateMouse(mouseReport);
            }

            eventLock.ExitWriteLock();
        }

        public override void PerformMouseButtonRelease(uint mouseButton)
        {
            bool sync = false;
            eventLock.EnterWriteLock();

            MouseButton tempButton = (MouseButton)mouseButton;
            if (mouseReport.HeldButtons.Contains(tempButton))
            {
                mouseReport.ResetMousePos();
                mouseReport.ButtonUp(tempButton);
                sync = true;
            }

            if (sync)
            {
                vMulti.updateMouse(mouseReport);
            }

            eventLock.ExitWriteLock();
        }
    }
}
