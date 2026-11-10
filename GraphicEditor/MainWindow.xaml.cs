using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace GraphicEditor
{
    public partial class MainWindow : Window
    {
        private DrawingTool _currentTool;
        private SolidColorBrush _currentColor = Brushes.Black;

        public MainWindow()
        {
            InitializeComponent();
            _currentTool = new PencilTool(DrawCanvas);
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(DrawCanvas);
            _currentTool.OnPress(pos, _currentColor, ThicknessSlider.Value);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = e.GetPosition(DrawCanvas);
                _currentTool.OnDrag(pos, _currentColor, ThicknessSlider.Value);
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(DrawCanvas);
            _currentTool.OnRelease(pos, _currentColor, ThicknessSlider.Value);
        }

        private void BtnPencil_Click(object sender, RoutedEventArgs e) => _currentTool = new PencilTool(DrawCanvas);
        private void BtnLine_Click(object sender, RoutedEventArgs e) => _currentTool = new LineTool(DrawCanvas);
        private void BtnRect_Click(object sender, RoutedEventArgs e) => _currentTool = new RectTool(DrawCanvas);
        private void BtnEllipse_Click(object sender, RoutedEventArgs e) => _currentTool = new EllipseTool(DrawCanvas);

        private void BtnClear_Click(object sender, RoutedEventArgs e) => DrawCanvas.Children.Clear();

        private void ColorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColorPicker != null && ColorPicker.SelectedItem is ComboBoxItem item)
            {
                var colorStr = item.Content.ToString();
                _currentColor = (SolidColorBrush)new BrushConverter().ConvertFromString(colorStr);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                FileName = "Image",
                DefaultExt = ".png",
                Filter = "PNG Image (.png)|*.png|JPEG Image (.jpg)|*.jpg"
            };

            if (dlg.ShowDialog() == true)
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)DrawCanvas.ActualWidth, (int)DrawCanvas.ActualHeight, 96d, 96d, PixelFormats.Default);
                rtb.Render(DrawCanvas);

                BitmapEncoder encoder = dlg.FileName.EndsWith(".png") ? new PngBitmapEncoder() : new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var fs = File.OpenWrite(dlg.FileName))
                {
                    encoder.Save(fs);
                }
            }
        }
    }

    public abstract class DrawingTool
    {
        protected Canvas _canvas;
        protected Point _startPoint;

        public DrawingTool(Canvas canvas)
        {
            _canvas = canvas;
        }

        public abstract void OnPress(Point p, Brush color, double thickness);
        public abstract void OnDrag(Point p, Brush color, double thickness);
        public abstract void OnRelease(Point p, Brush color, double thickness);
    }

    public class PencilTool : DrawingTool
    {
        private Point _lastPoint;

        public PencilTool(Canvas canvas) : base(canvas) { }

        public override void OnPress(Point p, Brush color, double thickness)
        {
            _lastPoint = p;
        }

        public override void OnDrag(Point p, Brush color, double thickness)
        {
            Line line = new Line
            {
                X1 = _lastPoint.X,
                Y1 = _lastPoint.Y,
                X2 = p.X,
                Y2 = p.Y,
                Stroke = color,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            _canvas.Children.Add(line);
            _lastPoint = p;
        }

        public override void OnRelease(Point p, Brush color, double thickness) { }
    }

    public abstract class ShapeTool<T> : DrawingTool where T : Shape, new()
    {
        protected T _tempShape;

        public ShapeTool(Canvas canvas) : base(canvas) { }

        public override void OnPress(Point p, Brush color, double thickness)
        {
            _startPoint = p;
            _tempShape = CreateShape(color, thickness);
            _canvas.Children.Add(_tempShape);
        }

        public override void OnRelease(Point p, Brush color, double thickness)
        {
            _tempShape = null; 
        }

        protected abstract T CreateShape(Brush color, double thickness);
    }


    public class RectTool : ShapeTool<Rectangle>
    {
        public RectTool(Canvas canvas) : base(canvas) { }

        protected override Rectangle CreateShape(Brush color, double thickness)
        {
            return new Rectangle { Stroke = color, StrokeThickness = thickness };
        }

        public override void OnDrag(Point p, Brush color, double thickness)
        {
            if (_tempShape == null) return;

            var x = Math.Min(p.X, _startPoint.X);
            var y = Math.Min(p.Y, _startPoint.Y);
            var width = Math.Abs(p.X - _startPoint.X);
            var height = Math.Abs(p.Y - _startPoint.Y);

            Canvas.SetLeft(_tempShape, x);
            Canvas.SetTop(_tempShape, y);
            _tempShape.Width = width;
            _tempShape.Height = height;
        }
    }

    public class EllipseTool : ShapeTool<Ellipse>
    {
        public EllipseTool(Canvas canvas) : base(canvas) { }

        protected override Ellipse CreateShape(Brush color, double thickness)
        {
            return new Ellipse { Stroke = color, StrokeThickness = thickness };
        }

        public override void OnDrag(Point p, Brush color, double thickness)
        {
            if (_tempShape == null) return;

            var x = Math.Min(p.X, _startPoint.X);
            var y = Math.Min(p.Y, _startPoint.Y);
            var width = Math.Abs(p.X - _startPoint.X);
            var height = Math.Abs(p.Y - _startPoint.Y);

            Canvas.SetLeft(_tempShape, x);
            Canvas.SetTop(_tempShape, y);
            _tempShape.Width = width;
            _tempShape.Height = height;
        }
    }

    public class LineTool : ShapeTool<Line>
    {
        public LineTool(Canvas canvas) : base(canvas) { }

        protected override Line CreateShape(Brush color, double thickness)
        {
            return new Line { 
                Stroke = color, 
                StrokeThickness = thickness,
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = _startPoint.X,
                Y2 = _startPoint.Y
            };
        }

        public override void OnDrag(Point p, Brush color, double thickness)
        {
            if (_tempShape == null) return;
            _tempShape.X2 = p.X;
            _tempShape.Y2 = p.Y;
        }
    }
}