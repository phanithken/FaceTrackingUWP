using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RealtimeFacedetection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private FaceDetectionEffect _faceDetectionEffect;
        private MediaCapture _mediaCapture;
        private IMediaEncodingProperties _previewProperties;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnCamera_Click(object sender, RoutedEventArgs e)
        {
            this._mediaCapture = new MediaCapture();
            await this._mediaCapture.InitializeAsync();
            this.cePreview.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
        }

        private async void btnDetectFaces_Click(object sender, RoutedEventArgs e)
        {
            var faceDetectionDefinition = new FaceDetectionEffectDefinition();
            faceDetectionDefinition.DetectionMode = FaceDetectionMode.HighPerformance;
            faceDetectionDefinition.SynchronousDetectionEnabled = false;
            this._faceDetectionEffect = (FaceDetectionEffect) await
                this._mediaCapture.AddVideoEffectAsync(faceDetectionDefinition, MediaStreamType.VideoPreview);
            this._faceDetectionEffect.FaceDetected += FaceDetectionEffect_FaceDetected;
            this._faceDetectionEffect.DesiredDetectionInterval = TimeSpan.FromMilliseconds(33);
            this._faceDetectionEffect.Enabled = true;
        }

        private async void FaceDetectionEffect_FaceDetected(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            var detectedFaces = args.ResultFrame.DetectedFaces;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DrawFaceBoxes(detectedFaces));
        }

        private void DrawFaceBoxes(IReadOnlyList<DetectedFace> detectedFaces)
        {
            this.cvsFaceOverlay.Children.Clear();
            for (int i=0; i<detectedFaces.Count; i++)
            {
                var face = detectedFaces[i];
                var faceBounds = face.FaceBox;
                var correctFaceHightLight = this.MapRectangleToDetectFace(faceBounds);
                Canvas.SetLeft(correctFaceHightLight, faceBounds.X * 1.9);
                Canvas.SetTop(correctFaceHightLight, faceBounds.Y * 1.5);
                correctFaceHightLight.StrokeThickness = 2;
                correctFaceHightLight.Stroke = new SolidColorBrush(Colors.Red);
                this.cvsFaceOverlay.Children.Add(correctFaceHightLight);
            }
        }

        private Rectangle MapRectangleToDetectFace(BitmapBounds detectedfaceBoxCoordinates)
        {
            var faceRectangle = new Rectangle();
            var deviceController = this._mediaCapture.VideoDeviceController;
            this._previewProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            var previewStreamProperties = this._previewProperties as VideoEncodingProperties;
            double mediaStreamWidth = previewStreamProperties.Width;
            double mediaStreamHeight = previewStreamProperties.Height;
            var faceHighlightRect = LocatePreviewStreamCoordinates(previewStreamProperties, this.cePreview);
            faceRectangle.Width = (detectedfaceBoxCoordinates.Width / mediaStreamWidth) * faceHighlightRect.Width;
            faceRectangle.Height = (detectedfaceBoxCoordinates.Height / mediaStreamHeight) * faceHighlightRect.Height;
            faceRectangle.Margin = new Thickness((uint)(detectedfaceBoxCoordinates.X / mediaStreamWidth), (uint)(detectedfaceBoxCoordinates.Y / mediaStreamHeight), 0, 0);
            return faceRectangle;
        }
        
        public Rect LocatePreviewStreamCoordinates(VideoEncodingProperties previewResolution, CaptureElement previewControl)
        {
            var uiRectangle = new Rect();
            var mediaStreamWidth = previewResolution.Width;
            var mediaStreamHeight = previewResolution.Height;
            uiRectangle.Width = previewControl.ActualWidth;
            uiRectangle.Height = previewControl.ActualHeight;
            var uiRatio = previewControl.ActualWidth / previewControl.ActualHeight;
            var mediaStreamRatio = mediaStreamWidth / mediaStreamHeight;
            if (uiRatio > mediaStreamRatio)
            {
                var scaleFactor = previewControl.ActualHeight / mediaStreamHeight;
                var scaledWidth = mediaStreamWidth * scaleFactor;
                uiRectangle.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
                uiRectangle.Width = scaledWidth;
            } else
            {
                var scaleFactor = previewControl.ActualWidth / mediaStreamWidth;
                var scaledHeight = mediaStreamHeight * scaleFactor;
                uiRectangle.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
                uiRectangle.Height = scaledHeight;
            }
            return uiRectangle;
        }
    }
}
