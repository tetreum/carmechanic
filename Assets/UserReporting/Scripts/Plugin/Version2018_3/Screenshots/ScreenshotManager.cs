using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Screenshots
{
    public class ScreenshotManager
    {
        #region Constructors

        public ScreenshotManager()
        {
            screenshotRecorder = new ScreenshotRecorder();
            screenshotCallbackDelegate = ScreenshotCallback;
            screenshotOperations = new List<ScreenshotOperation>();
        }

        #endregion

        #region Nested Types

        private class ScreenshotOperation
        {
            #region Methods

            public void Use()
            {
                Callback = null;
                Data = null;
                FrameNumber = 0;
                IsAwaiting = true;
                IsComplete = false;
                IsInUse = true;
                MaximumHeight = 0;
                MaximumWidth = 0;
                Source = null;
            }

            #endregion

            #region Properties

            public Action<int, byte[]> Callback { get; set; }

            public byte[] Data { get; set; }

            public int FrameNumber { get; set; }

            public bool IsAwaiting { get; set; }

            public bool IsComplete { get; set; }

            public bool IsInUse { get; set; }

            public int MaximumHeight { get; set; }

            public int MaximumWidth { get; set; }

            public object Source { get; set; }

            #endregion
        }

        #endregion

        #region Fields

        private readonly Action<byte[], object> screenshotCallbackDelegate;

        private readonly List<ScreenshotOperation> screenshotOperations;

        private readonly ScreenshotRecorder screenshotRecorder;

        #endregion

        #region Methods

        private ScreenshotOperation GetScreenshotOperation()
        {
            foreach (var screenshotOperation in screenshotOperations)
                if (!screenshotOperation.IsInUse)
                {
                    screenshotOperation.Use();
                    return screenshotOperation;
                }

            var newScreenshotOperation = new ScreenshotOperation();
            newScreenshotOperation.Use();
            screenshotOperations.Add(newScreenshotOperation);
            return newScreenshotOperation;
        }

        public void OnEndOfFrame()
        {
            foreach (var screenshotOperation in screenshotOperations)
                if (screenshotOperation.IsInUse)
                {
                    if (screenshotOperation.IsAwaiting)
                    {
                        screenshotOperation.IsAwaiting = false;
                        if (screenshotOperation.Source == null)
                            screenshotRecorder.Screenshot(screenshotOperation.MaximumWidth,
                                screenshotOperation.MaximumHeight, ScreenshotType.Png, screenshotCallbackDelegate,
                                screenshotOperation);
                        else if (screenshotOperation.Source is Camera)
                            screenshotRecorder.Screenshot(screenshotOperation.Source as Camera,
                                screenshotOperation.MaximumWidth, screenshotOperation.MaximumHeight, ScreenshotType.Png,
                                screenshotCallbackDelegate, screenshotOperation);
                        else if (screenshotOperation.Source is RenderTexture)
                            screenshotRecorder.Screenshot(screenshotOperation.Source as RenderTexture,
                                screenshotOperation.MaximumWidth, screenshotOperation.MaximumHeight, ScreenshotType.Png,
                                screenshotCallbackDelegate, screenshotOperation);
                        else if (screenshotOperation.Source is Texture2D)
                            screenshotRecorder.Screenshot(screenshotOperation.Source as Texture2D,
                                screenshotOperation.MaximumWidth, screenshotOperation.MaximumHeight, ScreenshotType.Png,
                                screenshotCallbackDelegate, screenshotOperation);
                        else
                            ScreenshotCallback(null, screenshotOperation);
                    }
                    else if (screenshotOperation.IsComplete)
                    {
                        screenshotOperation.IsInUse = false;
                        try
                        {
                            if (screenshotOperation != null && screenshotOperation.Callback != null)
                                screenshotOperation.Callback(screenshotOperation.FrameNumber, screenshotOperation.Data);
                        }
                        catch
                        {
                            // Do Nothing
                        }
                    }
                }
        }

        private void ScreenshotCallback(byte[] data, object state)
        {
            var screenshotOperation = state as ScreenshotOperation;
            if (screenshotOperation != null)
            {
                screenshotOperation.Data = data;
                screenshotOperation.IsComplete = true;
            }
        }

        public void TakeScreenshot(object source, int frameNumber, int maximumWidth, int maximumHeight,
            Action<int, byte[]> callback)
        {
            var screenshotOperation = GetScreenshotOperation();
            screenshotOperation.FrameNumber = frameNumber;
            screenshotOperation.MaximumWidth = maximumWidth;
            screenshotOperation.MaximumHeight = maximumHeight;
            screenshotOperation.Source = source;
            screenshotOperation.Callback = callback;
        }

        #endregion
    }
}