using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MRIVisualizer_Intel_Tablet
{
    /// <summary>
    /// Interaction logic for MRIToolBar.xaml
    /// </summary>
    public partial class MRIToolBar : UserControl
    {
        public event EventHandler<ToolBarEventArgs> OnDrawSelectionChanged;
        public event EventHandler<EventArgs> OnDeleteSelected;
        public event EventHandler<ToolBarEventArgs> OnPauseSelectionChanged;
        private bool isDrawChecked = false;
        private bool isPauseChecked = false;


        public MRIToolBar()
        {
            InitializeComponent();
            BrushConverter bc = new BrushConverter();
            PanelGrid.Background = (Brush)bc.ConvertFrom("#FF0D1B31"); 
        }

        private void DeleteButton_TouchEnter(object sender, TouchEventArgs e)
        {
            OnDeleteSelected(this, new EventArgs());
        }

        private void DrawButton_TouchEnter(object sender, TouchEventArgs e)
        {
            if (isDrawChecked)
            {
                isDrawChecked = false;
                DrawButton.Opacity = 1.0;
            }
            else
            {
                isDrawChecked = true;
                DrawButton.Opacity = 0.6;
            }

            Console.Out.WriteLine("Pause selected? " + isDrawChecked); 

            OnDrawSelectionChanged(this, new ToolBarEventArgs(isDrawChecked));
        }

        private void PauseButton_TouchEnter(object sender, TouchEventArgs e)
        {
            if (isPauseChecked)
            {
                isPauseChecked = false;
                PauseButton.Opacity = 1.0;
            }
            else
            {
                isPauseChecked = true;
                PauseButton.Opacity = 0.6;
            }
            OnPauseSelectionChanged(this, new ToolBarEventArgs(isPauseChecked));

            Console.Out.WriteLine("Pause selected? " + isPauseChecked); 

        }
    }

    public class ToolBarEventArgs : EventArgs
    {
        private bool _selected;

        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        public ToolBarEventArgs(bool s)
        {
            _selected = s;
        }

    }
}
