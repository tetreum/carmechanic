using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Unity.Screenshots
{
    public class ScreenshotRecorder
    {
        #region Static Fields

        private static int nextIdentifier;

        #endregion

        #region Fields

        private readonly List<ScreenshotOperation> operationPool;

        #endregion

        #region Constructors

        public ScreenshotRecorder()
        {
            operationPool = new List<ScreenshotOperation>();
        }

        #endregion

        #region Nested Types

        private class ScreenshotOperation
        {
            #region Constructors

            public ScreenshotOperation()
            {
                ScreenshotCallbackDelegate = ScreenshotCallback;
                EncodeCallbackDelegate = EncodeCallback;
            }

            #endregion

            #region Fields

            public readonly WaitCallback EncodeCallbackDelegate;

            public readonly Action<AsyncGPUReadbackRequest> ScreenshotCallbackDelegate;

            #endregion

            #region Properties

            public Action<byte[], object> Callback { get; set; }

            public int Height { get; set; }

            public int Identifier { get; set; }

            public bool IsInUse { get; set; }

            public int MaximumHeight { get; set; }

            public int MaximumWidth { get; set; }

            public NativeArray<byte> NativeData { get; set; }

            public Texture Source { get; set; }

            public object State { get; set; }

            public ScreenshotType Type { get; set; }

            public int Width { get; set; }

            #endregion

            #region Methods

            private void EncodeCallback(object state)
            {
                var byteData = NativeData.ToArray();
                int downsampledStride;
                byteData = Downsampler.Downsample(byteData, Width * 4, MaximumWidth, MaximumHeight,
                    out downsampledStride);
                if (Type == ScreenshotType.Png) byteData = PngEncoder.Encode(byteData, downsampledStride);
                if (Callback != null) Callback(byteData, State);
                NativeData.Dispose();
                IsInUse = false;
            }

            private void SavePngToDisk(byte[] byteData)
            {
                if (!Directory.Exists("Screenshots")) Directory.CreateDirectory("Screenshots");
                File.WriteAllBytes(string.Format("Screenshots/{0}.png", Identifier % 60), byteData);
            }

            private void ScreenshotCallback(AsyncGPUReadbackRequest request)
            {
                if (!request.hasError)
                {
                    NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
                    var data = request.GetData<byte>();
                    var persistentData = new NativeArray<byte>(data, Allocator.Persistent);
                    Width = request.width;
                    Height = request.height;
                    NativeData = persistentData;
                    ThreadPool.QueueUserWorkItem(EncodeCallbackDelegate, null);
                }
                else
                {
                    if (Callback != null) Callback(null, State);
                }

                if (Source != null) Object.Destroy(Source);
            }

            #endregion
        }

        #endregion

        #region Methods

        private ScreenshotOperation GetOperation()
        {
            foreach (var operation in operationPool)
                if (!operation.IsInUse)
                {
                    operation.IsInUse = true;
                    return operation;
                }

            var newOperation = new ScreenshotOperation();
            newOperation.IsInUse = true;
            operationPool.Add(newOperation);
            return newOperation;
        }

        public void Screenshot(int maximumWidth, int maximumHeight, ScreenshotType type,
            Action<byte[], object> callback, object state)
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            Screenshot(texture, maximumWidth, maximumHeight, type, callback, state);
        }

        public void Screenshot(Camera source, int maximumWidth, int maximumHeight, ScreenshotType type,
            Action<byte[], object> callback, object state)
        {
            var renderTexture = new RenderTexture(maximumWidth, maximumHeight, 24);
            var originalTargetTexture = source.targetTexture;
            source.targetTexture = renderTexture;
            source.Render();
            source.targetTexture = originalTargetTexture;
            Screenshot(renderTexture, maximumWidth, maximumHeight, type, callback, state);
        }

        public void Screenshot(RenderTexture source, int maximumWidth, int maximumHeight, ScreenshotType type,
            Action<byte[], object> callback, object state)
        {
            ScreenshotInternal(source, maximumWidth, maximumHeight, type, callback, state);
        }

        public void Screenshot(Texture2D source, int maximumWidth, int maximumHeight, ScreenshotType type,
            Action<byte[], object> callback, object state)
        {
            ScreenshotInternal(source, maximumWidth, maximumHeight, type, callback, state);
        }

        private void ScreenshotInternal(Texture source, int maximumWidth, int maximumHeight, ScreenshotType type,
            Action<byte[], object> callback, object state)
        {
            var operation = GetOperation();
            operation.Identifier = nextIdentifier++;
            operation.Source = source;
            operation.MaximumWidth = maximumWidth;
            operation.MaximumHeight = maximumHeight;
            operation.Type = type;
            operation.Callback = callback;
            operation.State = state;
            AsyncGPUReadback.Request(source, 0, TextureFormat.RGBA32, operation.ScreenshotCallbackDelegate);
        }

        #endregion
    }
}