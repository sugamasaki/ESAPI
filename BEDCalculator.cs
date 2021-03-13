#define ESAPI_V15

using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }
        const int n = 5;
        Window window;
        IList<Label> labelsH = new List<Label>();
        IList<Label> labelsV = new List<Label>();
        IList<TextBox> textTotalDose = new List<TextBox>();
        IList<TextBox> textFraction = new List<TextBox>();
        IList<TextBox> textDose1fr = new List<TextBox>();
        IList<TextBox> textAlphaBeta = new List<TextBox>();
        IList<TextBox> textBED = new List<TextBox>();
        IList<TextBox> textEQD2 = new List<TextBox>();
        IList<double> totaldose = new List<double>();
        IList<double> alphabeta_list = new List<double>();
        Grid gridScroll = new Grid();
        ScrollBar scrollBar = new ScrollBar();
        TextBox textPercent = new TextBox();
        Label labelPercent = new Label();
        Button buttonClear = new Button();
        int min_fontsize = 24;
        int last = -1;
        int current = -1;
        bool b_changing = false;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context/*, System.Windows.Window window, ScriptEnvironment environment*/)
        {
            window = new Window();
            window.Title = "BED Calculator";
            window.MinHeight = 400;
            window.MinWidth = 800;
            window.Height = 400;
            window.Width = 800;
            window.SizeChanged += MainWindow_SizeChanged;
            int fontsize = min_fontsize;
            Grid grid = new Grid();
            double w = window.Width;
            double h = window.Height;
            alphabeta_list.Add(10);
            alphabeta_list.Add(3);
            alphabeta_list.Add(2);
            alphabeta_list.Add(1.5);
            alphabeta_list.Add(1);
            for (int i = 0; i < 7; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions[i].Width = new GridLength(1, GridUnitType.Star);
            }
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);

            gridScroll.ColumnDefinitions.Add(new ColumnDefinition());
            gridScroll.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star);
            gridScroll.ColumnDefinitions.Add(new ColumnDefinition());
            gridScroll.ColumnDefinitions[1].Width = new GridLength(2, GridUnitType.Star);
            gridScroll.ColumnDefinitions.Add(new ColumnDefinition());
            gridScroll.ColumnDefinitions[2].Width = new GridLength(30, GridUnitType.Star);
            gridScroll.Children.Add(textPercent);
            gridScroll.Children.Add(labelPercent);
            gridScroll.Children.Add(scrollBar);
            scrollBar.Maximum = 100;
            scrollBar.Value = 100;
            scrollBar.LargeChange = 10;
            scrollBar.SmallChange = 5;
            scrollBar.Orientation = Orientation.Horizontal;
            scrollBar.Height = 30;
            scrollBar.ValueChanged += Scroll_ValueChanged;
            scrollBar.MouseWheel += ScrollBar_MouseWheel;
            textPercent.Text = scrollBar.Value.ToString();
            textPercent.TextAlignment = TextAlignment.Center;
            textPercent.VerticalAlignment = VerticalAlignment.Center;
            textPercent.FontSize = min_fontsize;
            textPercent.TextChanged += TextPercent_TextChanged;
            textPercent.SetValue(Grid.ColumnProperty, 0);
            labelPercent.Content = "%";
            labelPercent.FontSize = min_fontsize;
            labelPercent.VerticalAlignment = VerticalAlignment.Center;
            labelPercent.HorizontalContentAlignment = HorizontalAlignment.Center;
            labelPercent.SetValue(Grid.ColumnProperty, 1);
            scrollBar.SetValue(Grid.ColumnProperty, 2);
            grid.Children.Add(gridScroll);
            gridScroll.SetValue(Grid.RowProperty, 0);
            gridScroll.SetValue(Grid.ColumnSpanProperty, 7);
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);

            buttonClear.Content = "Clear";
            buttonClear.Click += ButtonClear_Click;
            grid.Children.Add(buttonClear);
            buttonClear.SetValue(Grid.ColumnProperty, 0);
            buttonClear.SetValue(Grid.RowProperty, 1);
            buttonClear.FontSize = fontsize;
            for (int i = 0; i < 7; i++)
            {
                labelsH.Add(new Label());
                grid.Children.Add(labelsH[i]);
                labelsH[i].VerticalAlignment = VerticalAlignment.Center;
                labelsH[i].HorizontalAlignment = HorizontalAlignment.Center;
                labelsH[i].SetValue(Grid.ColumnProperty, i + 1);
                labelsH[i].SetValue(Grid.RowProperty, 1);
            }
            labelsH[0].Content = "Total Dose[Gy]";
            labelsH[1].Content = "Fraction";
            labelsH[2].Content = "Dose/Fraction[Gy]";
            labelsH[3].Content = "alpha/beta[Gy]";
            labelsH[4].Content = "BED[Gy]";
            labelsH[5].Content = "EQD2[Gy]";
            for (int i = 0; i < n + 1; i++)
            {
                totaldose.Add(0);
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions[i + 1].Height = new GridLength(1, GridUnitType.Star);
                labelsV.Add(new Label());
                if (i == n)
                {
                    labelsV[i].Content = "Sum";
                }
                else
                {
                    labelsV[i].Content = (i + 1).ToString();
                }
                labelsV[i].FontSize = fontsize;
                textTotalDose.Add(new TextBox());
                textFraction.Add(new TextBox());
                textDose1fr.Add(new TextBox());
                textAlphaBeta.Add(new TextBox());
                textBED.Add(new TextBox());
                textEQD2.Add(new TextBox());
                grid.Children.Add(labelsV[i]);
                grid.Children.Add(textTotalDose[i]);
                grid.Children.Add(textFraction[i]);
                grid.Children.Add(textDose1fr[i]);
                grid.Children.Add(textAlphaBeta[i]);
                grid.Children.Add(textBED[i]);
                grid.Children.Add(textEQD2[i]);
                labelsV[i].SetValue(Grid.ColumnProperty, 0);
                textTotalDose[i].SetValue(Grid.ColumnProperty, 1);
                textFraction[i].SetValue(Grid.ColumnProperty, 2);
                textDose1fr[i].SetValue(Grid.ColumnProperty, 3);
                textAlphaBeta[i].SetValue(Grid.ColumnProperty, 4);
                textBED[i].SetValue(Grid.ColumnProperty, 5);
                textEQD2[i].SetValue(Grid.ColumnProperty, 6);
                labelsV[i].SetValue(Grid.RowProperty, i + 2);
                textTotalDose[i].SetValue(Grid.RowProperty, i + 2);
                textFraction[i].SetValue(Grid.RowProperty, i + 2);
                textDose1fr[i].SetValue(Grid.RowProperty, i + 2);
                textAlphaBeta[i].SetValue(Grid.RowProperty, i + 2);
                textBED[i].SetValue(Grid.RowProperty, i + 2);
                textEQD2[i].SetValue(Grid.RowProperty, i + 2);
                labelsV[i].VerticalAlignment = VerticalAlignment.Center;
                textTotalDose[i].VerticalAlignment = VerticalAlignment.Center;
                textFraction[i].VerticalAlignment = VerticalAlignment.Center;
                textDose1fr[i].VerticalAlignment = VerticalAlignment.Center;
                textAlphaBeta[i].VerticalAlignment = VerticalAlignment.Center;
                textBED[i].VerticalAlignment = VerticalAlignment.Center;
                textEQD2[i].VerticalAlignment = VerticalAlignment.Center;
                labelsV[i].HorizontalAlignment = HorizontalAlignment.Center;
                textTotalDose[i].TextAlignment = TextAlignment.Center;
                textFraction[i].TextAlignment = TextAlignment.Center;
                textDose1fr[i].TextAlignment = TextAlignment.Center;
                textAlphaBeta[i].TextAlignment = TextAlignment.Center;
                textBED[i].TextAlignment = TextAlignment.Center;
                textEQD2[i].TextAlignment = TextAlignment.Center;
                textTotalDose[i].FontSize = fontsize;
                textFraction[i].FontSize = fontsize;
                textDose1fr[i].FontSize = fontsize;
                textAlphaBeta[i].FontSize = fontsize;
                textBED[i].FontSize = fontsize;
                textEQD2[i].FontSize = fontsize;
                textTotalDose[i].Name = string.Format("TextBox{0}{1}", i, 0);
                textFraction[i].Name = string.Format("TextBox{0}{1}", i, 1);
                textDose1fr[i].Name = string.Format("TextBox{0}{1}", i, 2);
                textAlphaBeta[i].Name = string.Format("TextBox{0}{1}", i, 3);
                textBED[i].Name = string.Format("TextBox{0}{1}", i, 4);
                textEQD2[i].Name = string.Format("TextBox{0}{1}", i, 5);
                textAlphaBeta[i].Text = "10";
            }
            textTotalDose[n].IsReadOnly = true;
            textTotalDose[n].Foreground = Brushes.Black;
            textFraction[n].IsReadOnly = true;
            textFraction[n].Foreground = Brushes.Black;
            textDose1fr[n].IsReadOnly = true;
            textDose1fr[n].Foreground = Brushes.Black;
            textAlphaBeta[n].IsReadOnly = true;
            textAlphaBeta[n].Foreground = Brushes.Black;
            textAlphaBeta[n].Text = "";
            textBED[n].IsReadOnly = true;
            textBED[n].Foreground = Brushes.Black;
            textEQD2[n].IsReadOnly = true;
            textEQD2[n].Foreground = Brushes.Black;
            for (int i = 0; i < n; i++)
            {
                textTotalDose[i].PreviewKeyDown += Text_PreviewKeyDown;
                textFraction[i].PreviewKeyDown += Text_PreviewKeyDown;
                textFraction[i].MouseWheel += TextFraction_MouseWheel;
                textDose1fr[i].PreviewKeyDown += Text_PreviewKeyDown;
                textAlphaBeta[i].PreviewKeyDown += Text_PreviewKeyDown;
                textAlphaBeta[i].MouseWheel += TextAlphaBeta_MouseWheel;
                textAlphaBeta[i].MouseDoubleClick += TextAlphaBeta_MouseDoubleClick;
                textBED[i].PreviewKeyDown += Text_PreviewKeyDown;
                textEQD2[i].PreviewKeyDown += Text_PreviewKeyDown;
                textTotalDose[i].TextChanged += TextChanged;
                textFraction[i].TextChanged += TextChanged;
                textDose1fr[i].TextChanged += TextChanged;
                textAlphaBeta[i].TextChanged += TextChanged;
                textBED[i].TextChanged += TextChanged;
                textEQD2[i].TextChanged += TextChanged;
            }
            var plan = context.PlanSetup;
            if (plan != null)
            {
#if ESAPI_V15
                var dose = plan.TotalDose;
                var fr = plan.NumberOfFractions;
#else
                var dose = plan.TotalPrescribedDose;
                var fr = UniqueFractionation.NumberOfFractions;
#endif
                double doseval = dose.Dose;
                if(dose.Unit== DoseValue.DoseUnit.cGy)
                {
                    doseval /= 100;
                }
                for(int i = 0; i < n; i++)
                {
                    textAlphaBeta[i].Text = alphabeta_list.ElementAt(i).ToString();
                    totaldose[i] = doseval;
                    textTotalDose[i].Text = doseval.ToString();
                    textFraction[i].Text = fr.ToString();
                    textDose1fr[i].Text = (doseval/fr).ToString();
                }
                CalculateALL();
            }
            window.Content = grid;
            textTotalDose[0].Focus();
            window.ShowDialog();
        }
        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            b_changing = true;
            for(int i = 0; i < n+1; i++)
            {
                totaldose[i] = 0;
                textTotalDose[i].Text = "";
                textFraction[i].Text = "";
                textDose1fr[i].Text = "";
                //textAlphaBeta[i].Text = "";
                textBED[i].Text = "";
                textEQD2[i].Text = "";
            }
            b_changing = false;
        }
        private void ScrollBar_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double val = scrollBar.Value;
            if (e.Delta > 0)
            {
                val += scrollBar.LargeChange;
            }
            else
            {
                val -= scrollBar.LargeChange;
            }
            if (val < 0)
            {
                val = 0;
            }
            if (val > 100)
            {
                val = 100;
            }
            scrollBar.Value = val;
        }
        private void TextFraction_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TextBox text = sender as TextBox;
            int i;
            int.TryParse(text.Name[text.Name.Count() - 2].ToString(), out i);
            int fr;
            if (int.TryParse(textFraction[i].Text, out fr))
            {
                int j = 1;
                current = j;
                last = 2;
                if (e.Delta > 0)
                {
                    fr++;
                }
                else
                {
                    fr--;
                }
                if (fr < 1)
                {
                    fr = 1;
                }
                textFraction[i].Text = fr.ToString();
            }
        }
        private void TextAlphaBeta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox text = sender as TextBox;
            int i;
            int.TryParse(text.Name[text.Name.Count() - 2].ToString(), out i);
            double ab;
            if (double.TryParse(textAlphaBeta[i].Text, out ab))
            {
                for (int j = 0; j < n; j++)
                {
                    textAlphaBeta[j].Text = ab.ToString();
                }
                b_changing = true;
                CalculateALL();
                b_changing = false;
            }
        }

        private void TextAlphaBeta_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TextBox text = sender as TextBox;
            int i;
            int.TryParse(text.Name[text.Name.Count() - 2].ToString(), out i);
            double ab;
            if (double.TryParse(textAlphaBeta[i].Text, out ab))
            {
                int j;
                if (ab >= alphabeta_list[0])
                {
                    j = 0;
                }
                else if (ab >= alphabeta_list[1])
                {
                    j = 1;
                }
                else if (ab >= alphabeta_list[2])
                {
                    j = 2;
                }
                else if (ab >= alphabeta_list[3])
                {
                    j = 3;
                }
                else
                {
                    j = 4;
                }
                if (e.Delta > 0)
                {
                    j--;
                }
                else
                {
                    j++;
                }
                if (j < 0)
                {
                    j = 0;
                }
                if (j > 4)
                {
                    j = 4;
                }
                ab = alphabeta_list[j];
                text.Text = ab.ToString();
            }
        }
        private void Sum()
        {
            b_changing = true;
            double TD, d, ab, bed, eqd2;
            double TDsum, dsum, absum, bedsum, eqd2sum;
            absum = -1;
            int fr, frsum;
            TDsum = dsum = bedsum = eqd2sum = 0;
            frsum = 0;
            for (int i = 0; i < n; i++)
            {
                if (i == 0)
                {
                    if (!double.TryParse(textAlphaBeta[i].Text, out absum))
                    {
                        return;
                    }
                }
                if (double.TryParse(textAlphaBeta[i].Text, out ab))
                {
                    if (ab != absum)
                    {
                        textTotalDose[n].Text = "N/A";
                        textFraction[n].Text = "N/A";
                        textDose1fr[n].Text = "N/A";
                        textAlphaBeta[n].Text = "N/A";
                        textBED[n].Text = "N/A";
                        textEQD2[n].Text = "N/A";
                        return;
                    }
                }
                if (double.TryParse(textTotalDose[i].Text, out TD))
                {
                    TDsum += TD;
                }
                if (int.TryParse(textFraction[i].Text, out fr))
                {
                    frsum += fr;
                }
                if (double.TryParse(textDose1fr[i].Text, out d))
                {
                    dsum += d;
                }
                if (double.TryParse(textBED[i].Text, out bed))
                {
                    bedsum += bed;
                }
                if (double.TryParse(textEQD2[i].Text, out eqd2))
                {
                    eqd2sum += eqd2;
                }
            }
            textTotalDose[n].Text = TDsum.ToString();
            textFraction[n].Text = frsum.ToString();
            textDose1fr[n].Text = dsum.ToString();
            textAlphaBeta[n].Text = absum.ToString();
            textBED[n].Text = bedsum.ToString();
            textEQD2[n].Text = eqd2sum.ToString();
            b_changing = false;
        }
        private void CalculateALL()
        {
            b_changing = true;
            double TD, d, ab, bed, eqd2;
            int fr;
            for (int i = 0; i < n; i++)
            {
                if (double.TryParse(textTotalDose[i].Text, out TD))
                {
                    if (int.TryParse(textFraction[i].Text, out fr))
                    {
                        if (double.TryParse(textDose1fr[i].Text, out d))
                        {
                            if (double.TryParse(textAlphaBeta[i].Text, out ab))
                            {
                                TD = totaldose[i] * scrollBar.Value / 100;
                                textTotalDose[i].Text = TD.ToString();
                                d = TD / fr;
                                textDose1fr[i].Text = d.ToString();
                                bed = TD * (1 + d / ab);
                                textBED[i].Text = bed.ToString();
                                eqd2 = TD * (d + ab) / (2 + ab);
                                textEQD2[i].Text = eqd2.ToString();
                            }
                        }
                    }
                }
            }
            Sum();
            b_changing = false;
        }
        private void Scroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textPercent.Text = scrollBar.Value.ToString();
            CalculateALL();
        }

        private void TextPercent_TextChanged(object sender, TextChangedEventArgs e)
        {
            double val;
            if (double.TryParse(textPercent.Text, out val))
            {
                scrollBar.Value = val;
                textPercent.Text = scrollBar.Value.ToString();
                CalculateALL();
            }
        }
        private void Text_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //MessageBox.Show(e.Key.ToString());
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal || e.Key == Key.OemPeriod || e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Enter || e.Key == Key.Return)
            {
                TextBox text = sender as TextBox;
                int i, j;
                int.TryParse(text.Name.Last().ToString(), out j);
                int.TryParse(text.Name[text.Name.Count() - 2].ToString(), out i);
                if (j < 3)
                {
                    if (current != j)
                    {
                        if (last != current)
                        {
                            last = current;
                        }
                        current = j;
                    }
                }
            }
            else if (e.Key != Key.Tab && e.Key != Key.LeftShift && e.Key != Key.RightShift)
            {
                e.Handled = true;
            }
        }
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!b_changing)
            {
                b_changing = true;
                TextBox text = sender as TextBox;
                int i, j;
                int.TryParse(text.Name.Last().ToString(), out j);
                int.TryParse(text.Name[text.Name.Count() - 2].ToString(), out i);
                double TD, d;
                int fr;
                double ab, bed, eqd2;
                if (j < 3)
                {
                    if (last >= 0)
                    {
                        if (current != 0 && last != 0)
                        {
                            if (int.TryParse(textFraction[i].Text, out fr))
                            {
                                if (double.TryParse(textDose1fr[i].Text, out d))
                                {
                                    if (double.TryParse(textAlphaBeta[i].Text, out ab))
                                    {
                                        TD = d * fr;
                                        totaldose[i] = TD / scrollBar.Value * 100;
                                        textTotalDose[i].Text = TD.ToString();
                                        bed = TD * (1 + d / ab);
                                        textBED[i].Text = bed.ToString();
                                        eqd2 = TD * (d + ab) / (2 + ab);
                                        textEQD2[i].Text = eqd2.ToString();
                                    }
                                }
                            }
                        }
                        else if (current != 1 && last != 1)
                        {
                            if (double.TryParse(textTotalDose[i].Text, out TD))
                            {
                                if (double.TryParse(textDose1fr[i].Text, out d))
                                {
                                    if (double.TryParse(textAlphaBeta[i].Text, out ab))
                                    {
                                        totaldose[i] = TD / scrollBar.Value * 100;
                                        fr = (int)Math.Round(TD / d);
                                        textFraction[i].Text = fr.ToString();
                                        bed = TD * (1 + d / ab);
                                        textBED[i].Text = bed.ToString();
                                        eqd2 = TD * (d + ab) / (2 + ab);
                                        textEQD2[i].Text = eqd2.ToString();
                                    }
                                }
                            }
                        }
                        else if (current != 2 && last != 2)
                        {
                            if (double.TryParse(textTotalDose[i].Text, out TD))
                            {
                                if (int.TryParse(textFraction[i].Text, out fr))
                                {
                                    if (double.TryParse(textAlphaBeta[i].Text, out ab))
                                    {
                                        totaldose[i] = TD / scrollBar.Value * 100;
                                        d = TD / fr;
                                        textDose1fr[i].Text = d.ToString();
                                        bed = TD * (1 + d / ab);
                                        textBED[i].Text = bed.ToString();
                                        eqd2 = TD * (d + ab) / (2 + ab);
                                        textEQD2[i].Text = eqd2.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                if (j == 3)
                {
                    if (double.TryParse(textTotalDose[i].Text, out TD))
                    {
                        if (int.TryParse(textFraction[i].Text, out fr))
                        {
                            if (double.TryParse(textDose1fr[i].Text, out d))
                            {
                                if (double.TryParse(textAlphaBeta[i].Text, out ab))
                                {
                                    TD = totaldose[i] * scrollBar.Value / 100;
                                    textTotalDose[i].Text = TD.ToString();
                                    d = TD / fr;
                                    textDose1fr[i].Text = d.ToString();
                                    bed = TD * (1 + d / ab);
                                    textBED[i].Text = bed.ToString();
                                    eqd2 = TD * (d + ab) / (2 + ab);
                                    textEQD2[i].Text = eqd2.ToString();
                                }
                            }
                        }
                    }
                }
                if (j > 3)
                {
                    if (int.TryParse(textFraction[i].Text, out fr))
                    {
                        if (double.TryParse(textAlphaBeta[i].Text, out ab))
                        {
                            if (j == 4)
                            {
                                if (double.TryParse(textBED[i].Text, out bed))
                                {
                                    d = (-fr + Math.Sqrt(fr * fr + 4 * fr / ab * bed)) / (2 * fr / ab);
                                    textDose1fr[i].Text = d.ToString();
                                    TD = d * fr;
                                    totaldose[i] = TD / scrollBar.Value * 100;
                                    textTotalDose[i].Text = TD.ToString();
                                    eqd2 = TD * (d + ab) / (2 + ab);
                                    textEQD2[i].Text = eqd2.ToString();
                                }
                            }
                            if (j == 5)
                            {
                                if (double.TryParse(textEQD2[i].Text, out eqd2))
                                {
                                    d = (-fr * ab + Math.Sqrt(fr * ab * fr * ab + 4 * fr * eqd2 * (2 + ab))) / (2 * fr);
                                    textDose1fr[i].Text = d.ToString();
                                    TD = d * fr;
                                    totaldose[i] = TD / scrollBar.Value * 100;
                                    textTotalDose[i].Text = TD.ToString();
                                    bed = TD * (1 + d / ab);
                                    textBED[i].Text = bed.ToString();
                                }
                            }
                        }
                    }
                }
                Sum();
                b_changing = false;
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int fontsize = (int)(min_fontsize * Math.Min(e.NewSize.Width / window.MinWidth, e.NewSize.Height / window.MinHeight));
            for (int i = 0; i < 7; i++)
            {
                labelsH[i].FontSize = fontsize / 2;
            }
            for (int i = 0; i < n + 1; i++)
            {
                labelsV[i].FontSize = fontsize;
                textTotalDose[i].FontSize = fontsize;
                textDose1fr[i].FontSize = fontsize;
                textFraction[i].FontSize = fontsize;
                textAlphaBeta[i].FontSize = fontsize;
                textBED[i].FontSize = fontsize;
                textEQD2[i].FontSize = fontsize;
            }
            textPercent.FontSize = fontsize;
            labelPercent.FontSize = fontsize;
            buttonClear.FontSize = fontsize;
        }
    }
}
