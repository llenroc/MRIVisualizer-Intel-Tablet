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
using GroupLab.iNetwork.Tcp;
using GroupLab.iNetwork;

namespace MRIVisualizer_Intel_Tablet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region iNetwork Code

        private Connection _connection;

        public void StartConnection()
        {
            Connection.Discover("MRIViz", new SingleConnectionDiscoveryEventHandler(OnConnectionDiscovered));
        }

        private void OnConnectionDiscovered(Connection connection)
        {
            this._connection = connection;

            if (this._connection != null)
            {
                this._connection.MessageReceived += new ConnectionMessageEventHandler(OnMessageReceived);
                this._connection.Start();
            }
            else
            {
                // Through the GUI thread, close the window
                this.Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            this.Close();
                        }
                ));
            }
        }

        private void OnMessageReceived(object sender, Message msg)
        {
            if (msg != null)
            {
                switch (msg.Name)
                {
                    default:
                        // Do nothing
                        break;

                    #region AllowToDraw Message
                    case "ShowSlice":
                        this.Dispatcher.Invoke(
                            new Action(
                                delegate()
                                {
                                    int sliceNumber = msg.GetIntField("sliceNumber");
                                    //valueLabel.Content = sliceNumber;
                                    ShowImageSlice(sliceNumber);
                                }
                        ));
                    #endregion
                        break;
                }
            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MriImageCanvas.Height = this.ActualHeight;
            MriImageCanvas.Width = this.ActualWidth;
            StartConnection();
            ToolBar.OnDrawSelectionChanged += ToolBar_OnDrawSelectionChanged;
            ToolBar.OnDeleteSelected += ToolBar_OnDeleteSelected;
            ToolBar.OnPauseSelectionChanged += ToolBar_OnPauseSelectionChanged;
        }



        public void ShowImageSlice(int sliceNumber)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(@"../../Images/MRI/" + sliceNumber + ".jpg", UriKind.RelativeOrAbsolute);
            bi.EndInit();
            MriImageCanvas.Background = new ImageBrush(bi);
        }

        #region ToolBar Functionality

        #region Drawing Code

        void ToolBar_OnDrawSelectionChanged(object sender, ToolBarEventArgs e)
        {
            Console.Out.WriteLine("Draw selected " + e.Selected);
        }

        #endregion

        #region Pausing Code

        void ToolBar_OnPauseSelectionChanged(object sender, ToolBarEventArgs e)
        {
            if (e.Selected)
            {
                Message msg = new Message("PauseScan");
                this._connection.SendMessage(msg);
            }
            else
            {
                Message msg = new Message("ContinueScan");
                this._connection.SendMessage(msg);
            }
        }

        #endregion

        #region Deletion Code

        void ToolBar_OnDeleteSelected(object sender, EventArgs e)
        {
            Console.Out.WriteLine("Delete selected");
        }

        #endregion

        #endregion

    }
}
