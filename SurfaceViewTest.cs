using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;


[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }
        Window window = null;
        SurfaceView surfaceView = null;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context/*, System.Windows.Window window, ScriptEnvironment environment*/)
        {
            StructureSet ss = context.StructureSet;
            PlanSetup plan = context.PlanSetup;
            PlanSum planSum = null;
            Dose dose = null;
            double totalDose = -1;
            if (plan == null)
            {
                if (context.PlanSumsInScope.Count() == 0)
                {
                    MessageBox.Show("Plan and PlanSum not found");
                    return;
                }
                else if (context.PlanSumsInScope.Count() > 1)
                {
                    MessageBox.Show(string.Format("{0} PlanSums are in scope. Close all but the PlunSums that are currently open.", context.PlanSumsInScope.Count()));
                    return;
                }
                else
                {
                    planSum = context.PlanSumsInScope.First();
                    ss = planSum.StructureSet;
                    //planSum.DoseValuePresentation = DoseValuePresentation.Relative;
                    dose = planSum.Dose;
                    foreach (var ps in planSum.PlanSetups)
                    {
                        DoseValue TotalDose;
                        if(context.VersionInfo.Contains("15"))
                        {
                            TotalDose = (DoseValue)typeof(PlanSetup).GetProperty("TotalDose").GetValue(ps);
                        }
                        else
                        {
                            TotalDose = (DoseValue)typeof(PlanSetup).GetProperty("TotalPrescribedDose").GetValue(ps);
                        }
                        var val = TotalDose.Dose;
                        if (TotalDose.Unit == DoseValue.DoseUnit.cGy)
                        {
                            val /= 100;
                        }
                        totalDose += val;
                    }
                }
            }
            else
            {
                plan.DoseValuePresentation = DoseValuePresentation.Relative;
                dose = plan.Dose;
                DoseValue TotalDose;
                if (context.VersionInfo.Contains("15"))
                {
                    TotalDose = (DoseValue)typeof(PlanSetup).GetProperty("TotalDose").GetValue(plan);
                }
                else
                {
                    TotalDose = (DoseValue)typeof(PlanSetup).GetProperty("TotalPrescribedDose").GetValue(plan);
                }
                totalDose = TotalDose.Dose;
                if (TotalDose.Unit == DoseValue.DoseUnit.cGy)
                {
                    totalDose /= 100;
                }
            }
            if (dose == null)
            {
                MessageBox.Show("Dose not found");
                return;
            }
            if (ss == null)
            {
                MessageBox.Show("StructureSet not found");
                return;
            }
            Common.Model.API.Image img = ss.Image;
            if (img == null)
            {
                MessageBox.Show("Image not found");
                return;
            }
            double dz = img.ZRes;
            // Grid 0:Menu 1:SurfaceView
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions[0].Height = GridLength.Auto;
            grid.RowDefinitions[1].Height = new GridLength(1.0, GridUnitType.Star);
            // Menu
            Menu menu = new Menu();
            MenuItem menu_option = new MenuItem() { Header = "Option(_O)" };
            RadioButton radioButton1 = new RadioButton() { Content = "Outer Surface View" };
            RadioButton radioButton2 = new RadioButton() { Content = "Inner Surface View" };
            radioButton1.Checked += RadioButton1_Checked;
            radioButton2.Checked += RadioButton2_Checked;
            radioButton1.Foreground = Brushes.Black;
            radioButton2.Foreground = Brushes.Black;
            menu_option.Items.Add(radioButton1);
            menu_option.Items.Add(radioButton2);
            menu.Items.Add(menu_option);
            grid.Children.Add(menu);
            menu.SetValue(Grid.RowProperty, 0);
            // SurfaceView
            surfaceView = new SurfaceView(dose, ss, totalDose, dz);
            grid.Children.Add(surfaceView);
            surfaceView.SetValue(Grid.RowProperty, 1);
            surfaceView.SetViewMode(1);
            radioButton1.IsChecked = true;
            // Main Window
            window = new Window();
            window.Content = grid;
            window.Title = "Surface Dose on Selected ROI";
            window.ShowDialog();
        }
        private void RadioButton1_Checked(object sender, RoutedEventArgs e)
        {
            surfaceView.SetViewMode(1);
        }
        private void RadioButton2_Checked(object sender, RoutedEventArgs e)
        {
            surfaceView.SetViewMode(2);
        }
    }
    class SurfaceView : Grid
    {
        public SurfaceView()
        {
        }
        public SurfaceView(Dose dose, StructureSet ss, double totalDose, double dz)
        {
            Initialize(dose, ss, totalDose, dz);
        }
        int viewMode = 1; // ViewMode 1: Outer Surface View, 2: Inner Surface View
        Dose dose = null;
        StructureSet ss = null;
        double dz = 0;
        GeometryModel3D model = null;
        Viewport3D viewport3D = null;
        PerspectiveCamera camera = null;
        DirectionalLight directionalLight = new DirectionalLight(Colors.White, new Vector3D(0, 1, 0));
        PointLight pointLight = new PointLight(Colors.White, new Point3D(0, 0, 0));
        Quaternion quaternion = new Quaternion();
        ImageBrush imageBrush = new ImageBrush();
        bool right_click = false;
        bool left_click = false;
        Point pre_point = new Point(0, 0);
        Vector3D transform = new Vector3D(0, 0, 0);
        Vector3D pre_transform = new Vector3D(0, 0, 0);
        Vector3D structure_center = new Vector3D(0, 0, 0);
        const int ncolors = 106;
        int selected_roi = 0;
        bool IsRelativeDoseValue = true;
        double totalDose = -1;
        public void Initialize(Dose dose, StructureSet ss, double totalDose, double dz)
        {
            this.dose = dose;
            this.ss = ss;
            this.dz = dz;
            this.totalDose = totalDose;
            IsRelativeDoseValue = dose.DoseMax3D.IsRelativeDoseValue;
            viewport3D = new Viewport3D();
            this.Children.Add(viewport3D);
            MakeColorWash();
            if (viewMode == 1)
            {
                camera = new PerspectiveCamera(new Point3D(0, -1000, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), 45);
                viewport3D.Camera = camera;
                viewport3D.Children.Add(new ModelVisual3D() { Content = directionalLight });
            }
            if (viewMode == 2)
            {
                camera = new PerspectiveCamera(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), 90);
                viewport3D.Camera = camera;
                viewport3D.Children.Add(new ModelVisual3D() { Content = pointLight });
            }
            Make3DModel(selected_roi);
            viewport3D.Children.Add(new ModelVisual3D() { Content = model });
            ComboBox combobox = new ComboBox();
            foreach (var st in ss.Structures)
            {
                combobox.Items.Add(st.Id);
            }
            TextBlock textBlock = new TextBlock(new Run("ROI Id"));
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Top;
            textBlock.Foreground = Brushes.White;
            textBlock.FontSize = 24;
            this.Children.Add(textBlock);
            combobox.HorizontalAlignment = HorizontalAlignment.Left;
            combobox.VerticalAlignment = VerticalAlignment.Top;
            combobox.MinWidth = 200;
            combobox.FontSize = 24;
            combobox.SelectedIndex = selected_roi;
            combobox.Margin = new Thickness(0, 30, 0, 0);
            combobox.SelectionChanged += Combobox_SelectionChanged;
            this.Children.Add(combobox);
            TextBlock textBlock2 = new TextBlock(new Run("100% at"));
            textBlock2.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock2.VerticalAlignment = VerticalAlignment.Top;
            textBlock2.Foreground = Brushes.White;
            textBlock2.FontSize = 24;
            textBlock2.Margin = new Thickness(0, 60, 0, 0);
            this.Children.Add(textBlock2);
            TextBox textBox = new TextBox();
            textBox.HorizontalAlignment = HorizontalAlignment.Left;
            textBox.VerticalAlignment = VerticalAlignment.Top;
            textBox.Foreground = Brushes.White;
            textBox.FontSize = 24;
            textBox.Width = 100;
            textBox.Margin = new Thickness(0, 90, 0, 0);
            textBox.Text = totalDose.ToString();
            textBox.TextChanged += TextBox_TextChanged;
            this.Children.Add(textBox);
            TextBlock textBlock3 = new TextBlock(new Run("Gy"));
            textBlock3.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock3.VerticalAlignment = VerticalAlignment.Top;
            textBlock3.Foreground = Brushes.White;
            textBlock3.FontSize = 24;
            textBlock3.Margin = new Thickness(100, 90, 0, 0);
            this.Children.Add(textBlock3);
            this.Background = Brushes.Black;
            this.MouseWheel += SurfaceView_MouseWheel;
            this.MouseRightButtonDown += SurfaceView_MouseRightButtonDown;
            this.MouseRightButtonUp += SurfaceView_MouseRightButtonUp;
            this.MouseLeftButtonDown += SurfaceView_MouseLeftButtonDown;
            this.MouseLeftButtonUp += SurfaceView_MouseLeftButtonUp;
            this.MouseDown += SurfaceView_MouseDown;
            this.MouseMove += SurfaceView_MouseMove;
            this.MouseLeave += SurfaceView_MouseLeave;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsRelativeDoseValue)
            {
                TextBox text = sender as TextBox;
                double val;
                if (double.TryParse(text.Text, out val))
                {
                    totalDose = val;
                    Make3DModel(selected_roi);
                    viewport3D.Children.RemoveAt(1);
                    viewport3D.Children.Add(new ModelVisual3D() { Content = model });
                    Transform3DGroup transform3DGroup = new Transform3DGroup();
                    if (viewMode == 1)
                    {
                        transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                        transform3DGroup.Children.Add(new TranslateTransform3D(transform.X, 0, transform.Z));
                        model.Transform = transform3DGroup;
                    }
                    if (viewMode == 2)
                    {
                        var s = ss.Structures.ElementAt(selected_roi);
                        var vv = s.CenterPoint;
                        transform = new Vector3D(-vv.x, -vv.y, -vv.z);
                        transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                        transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                        model.Transform = transform3DGroup;
                    }
                }
            }
        }

        public void SetViewMode(int mode)
        {
            if (mode == 1 || mode == 2)
            {
                viewMode = mode;
                ResetView();
            }
            else
            {
                MessageBox.Show("Set viewMode 1 or 2. viewMode 1: Outer, viewMode 2: Inner");
            }
        }
        private void MakeColorWash()
        {
            byte[] bytecolor = new byte[3 * ncolors];
            double val = 0;
            for (int i = 0; i < ncolors; i++)
            {
                val = i;
                if (val >= 105)
                {
                    bytecolor[3 * i] = 255;
                    bytecolor[3 * i + 1] = 128;
                    bytecolor[3 * i + 2] = 128;
                }
                else if (val >= 100)
                {
                    val -= 100;
                    val /= 5;
                    bytecolor[3 * i] = 255;
                    bytecolor[3 * i + 1] = (byte)(128 * val);
                    bytecolor[3 * i + 2] = (byte)(128 * val);
                }
                else if (val >= 95)
                {
                    val -= 95;
                    val /= 5;
                    bytecolor[3 * i] = 255;
                    bytecolor[3 * i + 1] = (byte)(165 * (1 - val));
                    bytecolor[3 * i + 2] = 0;
                }
                else if (val >= 90)
                {
                    val -= 90;
                    val /= 5;

                    bytecolor[3 * i] = 255;
                    bytecolor[3 * i + 1] = (byte)((255 - 165) * (1 - val) + 165);
                    bytecolor[3 * i + 2] = 0;
                }
                else if (val >= 70)
                {
                    val -= 70;
                    val /= 20;
                    bytecolor[3 * i] = (byte)(255 * val);
                    bytecolor[3 * i + 1] = (byte)((255 - 128) * val + 128);
                    bytecolor[3 * i + 2] = 0;
                }
                else if (val >= 50)
                {
                    val -= 50;
                    val /= 20;
                    bytecolor[3 * i] = 0;
                    bytecolor[3 * i + 1] = (byte)((255 - 128) * (1 - val) + 128);
                    bytecolor[3 * i + 2] = (byte)(255 * (1 - val));
                }
                else if (val >= 30)
                {
                    val -= 30;
                    val /= 20;
                    bytecolor[3 * i] = 0;
                    bytecolor[3 * i + 1] = (byte)(255 * val);
                    bytecolor[3 * i + 2] = 255;
                }
                else if (val >= 10)
                {
                    val -= 10;
                    val /= 20;
                    bytecolor[3 * i] = (byte)(128 * (1 - val));
                    bytecolor[3 * i + 1] = 0;
                    bytecolor[3 * i + 2] = (byte)((255 - 128) * val + 128); ;
                }
                else
                {
                    bytecolor[3 * i] = 255;
                    bytecolor[3 * i + 1] = 255;
                    bytecolor[3 * i + 2] = 255;
                }
            }
            imageBrush.ImageSource = BitmapSource.Create(ncolors, 1, 96, 96, PixelFormats.Rgb24, null, bytecolor, (ncolors * 24 + 7) / 8);
            for (int i = 1; i <= 12; i++)
            {
                Color color = new Color();
                int j;
                if (i < 10)
                {
                    j = i * 10;
                }
                else
                {
                    j = i * 10 - (i - 9) * 5;
                }
                TextBlock textDose = new TextBlock(new Run(string.Format(" {0,3}%", j)));
                textDose.FontSize = 24;
                textDose.HorizontalAlignment = HorizontalAlignment.Left;
                textDose.VerticalAlignment = VerticalAlignment.Bottom;
                textDose.Margin = new Thickness(0, 0, 0, i * 30);
                color.R = bytecolor[3 * j];
                color.G = bytecolor[3 * j + 1];
                color.B = bytecolor[3 * j + 2];
                color.A = 255;
                textDose.Foreground = new SolidColorBrush(color);
                this.Children.Add(textDose);
            }
        }
        private void Make3DModel(int index)
        {
            int n = index;
            var meshGeometry = ss.Structures.ElementAt(n).MeshGeometry;
            double val;
            int npoints = meshGeometry.TriangleIndices.Count;
            IList<Point3D> positions = new List<Point3D>();
            IList<Point> points = new List<Point>();
            double zmin = 1000;
            double zmax = -1000;
            foreach (var point in meshGeometry.Positions)
            {
                if (point.Z < zmin)
                {
                    zmin = point.Z;
                }
                if (point.Z > zmax)
                {
                    zmax = point.Z;
                }
            }
            bool flag;
            for (int i = 0; i < npoints / 3; i++)
            {
                flag = false;
                for (int j = 0; j < 3; j++)
                {
                    int k = meshGeometry.TriangleIndices.ElementAt(i * 3 + j);
                    var point = meshGeometry.Positions.ElementAt(k);
                    if (point.Z > zmin + dz / 2 && point.Z < zmax - dz / 2)
                    {
                        flag = true;
                    }
                }
                if (flag || viewMode == 1)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int k = meshGeometry.TriangleIndices.ElementAt(i * 3 + j);
                        var point = meshGeometry.Positions.ElementAt(k);
                        positions.Add(point);
                        val = dose.GetDoseToPoint(new VVector(point.X, point.Y, point.Z)).Dose;
                        if (!IsRelativeDoseValue)
                        {
                            val /= totalDose;
                            val *= 100;
                        }
                        if (val > 105)
                        {
                            val = 105;
                        }
                        if (val < 0)
                        {
                            val = 0;
                        }
                        points.Add(new Point(val / (ncolors - 1), val / (ncolors - 1)));
                    }
                }
            }
            for (int i = 0; i < 3; i++)
            {
                positions.Add(new Point3D(0, 0, 0));
                points.Add(new Point(0, 0));
            }
            for (int i = 0; i < 3; i++)
            {
                positions.Add(new Point3D(0, 0, 0));
                points.Add(new Point(1.0, 1.0));
            }
            model = new GeometryModel3D()
            {
                Geometry = new MeshGeometry3D() { Positions = new Point3DCollection(positions), TextureCoordinates = new PointCollection(points) },
                Material = new DiffuseMaterial(imageBrush),
            };
            model.BackMaterial = model.Material;
        }
        private void Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            selected_roi = cb.SelectedIndex;
            Make3DModel(selected_roi);
            viewport3D.Children.RemoveAt(1);
            viewport3D.Children.Add(new ModelVisual3D() { Content = model });
            Transform3DGroup transform3DGroup = new Transform3DGroup();
            var s = ss.Structures.ElementAt(selected_roi);
            var vv = s.CenterPoint;
            structure_center = new Vector3D(-vv.x, -vv.y, -vv.z);
            if (viewMode == 1)
            {
                transform3DGroup.Children.Add(new TranslateTransform3D(structure_center));
                transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                transform3DGroup.Children.Add(new TranslateTransform3D(transform.X, 0, transform.Z));
                model.Transform = transform3DGroup;
            }
            if (viewMode == 2)
            {
                transform = new Vector3D(-vv.x, -vv.y, -vv.z);
                transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                model.Transform = transform3DGroup;
            }
        }
        private void ResetView()
        {
            if (viewMode == 1)
            {
                camera = new PerspectiveCamera(new Point3D(0, -1000, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), 45);
                viewport3D.Camera = camera;
                Make3DModel(selected_roi);
                viewport3D.Children.Clear();
                viewport3D.Children.Add(new ModelVisual3D() { Content = directionalLight });
                viewport3D.Children.Add(new ModelVisual3D() { Content = model });
                //quaternion = new Quaternion();
                //model.Transform = new RotateTransform3D(new QuaternionRotation3D(quaternion));
                model.Transform = new TranslateTransform3D(structure_center);
                transform = new Vector3D(0, 0, 0);
                directionalLight.Transform = new TranslateTransform3D(transform);
                camera.Transform = new TranslateTransform3D(transform);
            }
            if (viewMode == 2)
            {
                camera = new PerspectiveCamera(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), 90);
                viewport3D.Camera = camera;
                Make3DModel(selected_roi);
                viewport3D.Children.Clear();
                viewport3D.Children.Add(new ModelVisual3D() { Content = pointLight });
                viewport3D.Children.Add(new ModelVisual3D() { Content = model });
                quaternion = new Quaternion();
                model.Transform = new RotateTransform3D(new QuaternionRotation3D(quaternion));
                var s = ss.Structures.ElementAt(selected_roi);
                var vv = s.CenterPoint;
                camera.Transform = new TranslateTransform3D(0, 0, 0);
                transform = new Vector3D(-vv.x, -vv.y, -vv.z);
                model.Transform = new TranslateTransform3D(transform);
            }
        }
        private void SurfaceView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                ResetView();
            }
        }
        private void SurfaceView_MouseMove(object sender, MouseEventArgs e)
        {
            var diff = e.GetPosition(this) - pre_point;
            var size = Math.Min(this.ActualWidth, this.ActualHeight);
            if (right_click)
            {
                double x = 360 * diff.X / size;
                double y = 360 * diff.Y / size;
                var q = new Quaternion(new Vector3D(0, 0, 1), x);
                q *= new Quaternion(new Vector3D(1, 0, 0), y);
                q *= quaternion;
                Transform3DGroup transform3DGroup = new Transform3DGroup();
                if (viewMode == 1)
                {
                    transform3DGroup.Children.Add(new TranslateTransform3D(structure_center));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(q)));
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform.X, 0, transform.Z));
                    model.Transform = transform3DGroup;
                }
                if (viewMode == 2)
                {
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(q)));
                    model.Transform = transform3DGroup;
                }
            }
            if (left_click)
            {
                double x = diff.X / size * 100;
                double y = -diff.Y / size * 100;
                Transform3DGroup transform3DGroup = new Transform3DGroup();
                if (viewMode == 1)
                {
                    transform3DGroup.Children.Add(new TranslateTransform3D(structure_center));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    transform3DGroup.Children.Add(new TranslateTransform3D(x + transform.X, 0, y + transform.Z));
                    model.Transform = transform3DGroup;
                }
                if (viewMode == 2)
                {
                    var q = quaternion;
                    q.Invert();
                    x /= 5;
                    y /= 5;
                    Point3D p = (new RotateTransform3D(new QuaternionRotation3D(q))).Transform(new Point3D(x, 0, y));
                    var s = ss.Structures.ElementAt(selected_roi);
                    VVector vv = new VVector(-(p.X + transform.X), -(p.Y + transform.Y), -(p.Z + transform.Z));
                    if (s.IsPointInsideSegment(vv))
                    {
                        transform3DGroup.Children.Add(new TranslateTransform3D(p.X + transform.X, p.Y + transform.Y, p.Z + transform.Z));
                        transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                        model.Transform = transform3DGroup;
                        pre_transform = new Vector3D(p.X + transform.X, p.Y + transform.Y, p.Z + transform.Z);
                    }
                }
            }
        }
        private void SurfaceView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (left_click)
            {
                var diff = e.GetPosition(this) - pre_point;
                var size = Math.Min(this.ActualWidth, this.ActualHeight);
                double x = diff.X / size * 100;
                double y = -diff.Y / size * 100;
                Transform3DGroup transform3DGroup = new Transform3DGroup();
                if (viewMode == 1)
                {
                    transform = new Vector3D(x + transform.X, transform.Y, y + transform.Z);
                    transform3DGroup.Children.Add(new TranslateTransform3D(structure_center));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform.X, 0, transform.Z));
                    model.Transform = transform3DGroup;
                }
                if (viewMode == 2)
                {
                    var q = quaternion;
                    q.Invert();
                    x /= 5;
                    y /= 5;
                    Point3D p = (new RotateTransform3D(new QuaternionRotation3D(q))).Transform(new Point3D(x, 0, y));
                    var s = ss.Structures.ElementAt(selected_roi);
                    VVector vv = new VVector(-(p.X + transform.X), -(p.Y + transform.Y), -(p.Z + transform.Z));
                    if (s.IsPointInsideSegment(vv))
                    {
                        transform = new Vector3D(p.X + transform.X, p.Y + transform.Y, p.Z + transform.Z);
                        transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                        transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                        model.Transform = transform3DGroup;
                        pre_transform = new Vector3D(p.X + transform.X, p.Y + transform.Y, p.Z + transform.Z);
                    }
                    else
                    {
                        transform = new Vector3D(pre_transform.X, pre_transform.Y, pre_transform.Z);
                        transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                        transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                        model.Transform = transform3DGroup;
                    }
                }
                left_click = false;
            }
        }
        private void SurfaceView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            pre_point = e.GetPosition(this);
            left_click = true;
            right_click = false;
        }
        private void SurfaceView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (right_click)
            {
                var diff = e.GetPosition(this) - pre_point;
                var size = Math.Min(this.ActualWidth, this.ActualHeight);
                double x = 360 * diff.X / size;
                double y = 360 * diff.Y / size;
                var q = new Quaternion(new Vector3D(0, 0, 1), x);
                q *= new Quaternion(new Vector3D(1, 0, 0), y);
                quaternion = q * quaternion;
                Transform3DGroup transform3DGroup = new Transform3DGroup();
                if (viewMode == 1)
                {
                    transform3DGroup.Children.Add(new TranslateTransform3D(structure_center));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform.X, 0, transform.Z));
                    model.Transform = transform3DGroup;
                }
                if (viewMode == 2)
                {
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    model.Transform = transform3DGroup;
                }
                right_click = false;
            }
        }
        private void SurfaceView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            pre_point = e.GetPosition(this);
            right_click = true;
            left_click = false;
        }
        private void SurfaceView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (viewMode == 1)
            {
                if (e.Delta > 0)
                {
                    transform.Y += 10;
                }
                else
                {
                    transform.Y -= 10;
                }
                camera.Transform = new TranslateTransform3D(0, transform.Y, 0);
            }
            if (viewMode == 2)
            {
                var q = quaternion;
                q.Invert();
                double val = e.Delta > 0 ? -1 : 1;
                Point3D p = (new RotateTransform3D(new QuaternionRotation3D(q))).Transform(new Point3D(0, val, 0));
                var s = ss.Structures.ElementAt(selected_roi);
                VVector vv = new VVector(-(p.X + transform.X), -(p.Y + transform.Y), -(p.Z + transform.Z));
                if (s.IsPointInsideSegment(vv))
                {
                    transform = new Vector3D(p.X + transform.X, p.Y + transform.Y, p.Z + transform.Z);
                    Transform3DGroup transform3DGroup = new Transform3DGroup();
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    model.Transform = transform3DGroup;
                    pre_transform = transform;
                }
            }
        }
        private void SurfaceView_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var diff = e.GetPosition(this) - pre_point;
                var size = Math.Min(this.ActualWidth, this.ActualHeight);
                double x = diff.X / size * 100;
                double y = -diff.Y / size * 100;
                Transform3DGroup transform3DGroup = new Transform3DGroup();
                if (viewMode == 1)
                {
                    transform = new Vector3D(x + transform.X, transform.Y, y + transform.Z);
                    transform3DGroup.Children.Add(new TranslateTransform3D(structure_center));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform.X, 0, transform.Z));
                    model.Transform = transform3DGroup;
                }
                if (viewMode == 2)
                {
                    var q = quaternion;
                    q.Invert();
                    x /= 5;
                    y /= 5;
                    Point3D p = (new RotateTransform3D(new QuaternionRotation3D(q))).Transform(new Point3D(x, 0, y));
                    transform = new Vector3D(p.X + transform.X, p.Y + transform.Y, p.Z + transform.Z);
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    model.Transform = transform3DGroup;
                }
                left_click = false;
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                var diff = e.GetPosition(this) - pre_point;
                var size = Math.Min(this.ActualWidth, this.ActualHeight);
                double x = 360 * diff.X / size;
                double y = 360 * diff.Y / size;
                var q = new Quaternion(new Vector3D(0, 0, 1), x);
                q *= new Quaternion(new Vector3D(1, 0, 0), y);
                quaternion = q * quaternion;
                Transform3DGroup transform3DGroup = new Transform3DGroup();
                if (viewMode == 1)
                {
                    transform3DGroup.Children.Add(new TranslateTransform3D(structure_center));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform.X, 0, transform.Z));
                    model.Transform = transform3DGroup;
                }
                if (viewMode == 2)
                {
                    transform3DGroup.Children.Add(new TranslateTransform3D(transform));
                    transform3DGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quaternion)));
                    model.Transform = transform3DGroup;
                }
                right_click = false;
            }
        }
    }
}
