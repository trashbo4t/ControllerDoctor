﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace ControllerDoctorUWP
{
    public sealed partial class ControllerDeadzonePage : Page
    {
        public class Painter
        {
            public Ellipse Ellipse;
            public Canvas Canvas;
            public bool LeftOrRight;

            public Painter(Ellipse e, Canvas c, bool lor)
            {
                Ellipse = e;
                Canvas = c;
                LeftOrRight = lor;
                Canvas.Children.Add(Ellipse);
            }
        }

        readonly bool LEFT = true;
        readonly bool RIGHT = false;

        Painter LeftPainter;
        Painter RightPainter;

        GenericController controller;
        Task LeftTask;
        Task RightTask;
        CancellationTokenSource CancellationToken;

        public ControllerDeadzonePage()
        {
            this.InitializeComponent();
            
            LeftPainter = new Painter(CreateAnEllipse(5, 5), LeftCanvas, LEFT);
            RightPainter = new Painter(CreateAnEllipse(5, 5), RightCanvas, RIGHT);

            this.Loaded += (s1, e1) =>
            {
                controller = new GenericController();

                controller.Connected += (s2, e2) =>
                {
                    SetConnectedAsync();
                };

                controller.Disconnected += (s2, e2) =>
                {
                    SetDisconnectedAsync();
                };

                controller.Refresh();
            };
        }

        public async void SetConnectedAsync()
        {
            CancellationToken = new CancellationTokenSource();

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {
                Connected_TextBlock.Text = "Connected";
                Connected_TextBlock.Foreground = new SolidColorBrush(Colors.LawnGreen);

                LeftTask = new Task(Diagnos, LeftPainter, CancellationToken.Token, TaskCreationOptions.LongRunning);
                RightTask = new Task(Diagnos, RightPainter, CancellationToken.Token, TaskCreationOptions.LongRunning);
                
                LeftTask.Start();
                RightTask.Start();
            });
        }

        public async void SetDisconnectedAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                CancellationToken.Cancel();

                Connected_TextBlock.Text = "Disconnected";
                Connected_TextBlock.Foreground = new SolidColorBrush(Colors.Red);
            });
        }

        public void Diagnos(object obj)
        {
            while (true)
            {
                try
                {
                    CancellationToken.Token.ThrowIfCancellationRequested();

                    if (controller.IsConnected())
                    {
                        DrawAsync((Painter)obj);
                    }
                }
                catch 
                {
                    return;
                }

                Thread.Sleep(Settings.DelayMilliseconds);
            }
        }

        public async void DrawAsync(Painter p)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {
                Draw(p);
            });
        }

        public void Draw(Painter p)
        {
            double CenterY = ((p.Canvas.ActualHeight - p.Ellipse.ActualHeight) / 2);
            double X;
            double Y;

            if (p.LeftOrRight == LEFT)
            {
                X = controller.LeftStickXPosition() / 100;
                Y = controller.LeftStickYPosition() / 100;
              
                X_Left_Text.Text = String.Format("X: {0:0.00}", X);
                Y_Left_Text.Text = String.Format("Y: {0:0.00}", Y);
            }
            else
            {
                X = controller.RightStickXPosition() / 100;
                Y = controller.RightStickYPosition() / 100;

                X_Right_Text.Text = String.Format("X: {0:0.00}", X);
                Y_Right_Text.Text = String.Format("Y: {0:0.00}", Y);
            }

            Canvas.SetLeft(p.Ellipse, (X * (p.Canvas.ActualWidth - 5)));
            Canvas.SetTop(p.Ellipse, (Y * (-(p.Canvas.ActualHeight + 5)) + p.Canvas.ActualHeight));
        }

        Ellipse CreateAnEllipse(int height, int width)
        {
            Color c = new Color();
            c.R = 0x2F; //"2F2F4F"
            c.G = 0x2F;
            c.B = 0x4F;
            c.A = 0xFF;

            SolidColorBrush fillBrush = new SolidColorBrush() { Color = c };
            SolidColorBrush borderBrush = new SolidColorBrush() { Color = c};

            return new Ellipse()
            {
                Height = height,
                Width = width,
                StrokeThickness = 1,
                Stroke = borderBrush,
                Fill = fillBrush
            };
        }

        public async Task AlertAndWaitAsync(string msg, string title)
        {
            MessageDialog messageDialog = new MessageDialog(msg, title);
            await messageDialog.ShowAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CancellationToken.Cancel();
            base.OnNavigatedFrom(e);
        }

        private void Left_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Left_Rectangle_1 == null || Left_Rectangle_2 == null)
            {
                return;
            }

            
            Left_Rectangle_1.Width = e.NewValue;

            double CenterX = ((LeftCanvas.ActualWidth - Left_Rectangle_1.ActualWidth) / 2);
            double CenterY = ((LeftCanvas.ActualHeight - Left_Rectangle_1.ActualHeight) / 2);

            Canvas.SetLeft(Left_Rectangle_1, CenterX);
            Canvas.SetTop(Left_Rectangle_1, CenterY);

            Left_Rectangle_2.Height = e.NewValue;

            CenterX = ((LeftCanvas.ActualWidth - Left_Rectangle_2.ActualWidth) / 2);
            CenterY = ((LeftCanvas.ActualHeight - Left_Rectangle_2.ActualHeight) / 2);

            Canvas.SetLeft(Left_Rectangle_2, CenterX);
            Canvas.SetTop(Left_Rectangle_2, CenterY);
        }

        private void Right_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Right_Rectangle_1 == null || Right_Rectangle_2 == null)
            {
                return;
            }

            Right_Rectangle_1.Width = e.NewValue;

            double CenterX = ((RightCanvas.ActualWidth - Right_Rectangle_1.ActualWidth) / 2);
            double CenterY = ((RightCanvas.ActualHeight - Right_Rectangle_1.ActualHeight) / 2);

            Canvas.SetLeft(Right_Rectangle_1, CenterX);
            Canvas.SetTop(Right_Rectangle_1, CenterY);

            Right_Rectangle_2.Height = e.NewValue;

            CenterX = ((RightCanvas.ActualWidth - Right_Rectangle_2.ActualWidth) / 2);
            CenterY = ((RightCanvas.ActualHeight - Right_Rectangle_2.ActualHeight) / 2);

            Canvas.SetLeft(Right_Rectangle_2, CenterX);
            Canvas.SetTop(Right_Rectangle_2, CenterY);
        }
    }
}
