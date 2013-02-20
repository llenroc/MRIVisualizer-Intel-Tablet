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
        private bool _isDrawing = false;
        private String _clientID;
        private Color _clientDrawingColor;
        private PathSegmentCollection _clientPathPoints;
        private Dictionary<String, Path> _drawingPaths = new Dictionary<string,Path>();

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
                    case "StartDrawing":
                        string startingClientID = msg.GetStringField("ClientID");
                        Color startingClientColor = (Color)ColorConverter.ConvertFromString(msg.GetStringField("ClientColor"));
                        Point startingPoint = new Point {X=msg.GetDoubleField("X"),Y=msg.GetDoubleField("Y")};
                        NewDrawingPath(startingClientID, startingClientColor, startingPoint);
                        break;
                    case "ContinueDrawing":
                        string continuingClientID = msg.GetStringField("ClientID");
                        Color continuedClientColor = (Color)ColorConverter.ConvertFromString(msg.GetStringField("ClientColor"));
                        Point continuedPoint = new Point { X = msg.GetDoubleField("X"), Y = msg.GetDoubleField("Y") };
                        ContinueDrawingPath(continuingClientID, continuedPoint);
                        break;

                    case "ClearDrawing":
                        ClearCanvas();
                        break;

                    #endregion

                    #region ShowingSlice Message
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
                        break;
                    #endregion

                    #region Pausing and Scanning 

                    case "PauseScan":
                        this.Dispatcher.Invoke(
                            new Action(
                                delegate()
                                {
                                  ToolBar.SetPauseButtonState(true);
                                }
                        ));
                        break;
                    case "ContinueScan":
                        this.Dispatcher.Invoke(
                            new Action(
                                delegate()
                                {
                                    ToolBar.SetPauseButtonState(false);
                                }
                        ));
                        break;
                    #endregion Pausing and Scanning
                }
            }
        }

        private void ContinueDrawingPath(string continuingClientID, Point continuedPoint)
        {
            this.Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        if (continuingClientID == null)
                            return;
                        PathGeometry pGeo = (PathGeometry)_drawingPaths[continuingClientID].Data;
                        if (_clientPathPoints.Count > 0)
                        {
                            _clientPathPoints = pGeo.Figures[0].Segments;
                            QuadraticBezierSegment previousLine = _clientPathPoints[_clientPathPoints.Count - 1] as QuadraticBezierSegment;
                            Point previousEndPoint = previousLine.Point2;

                            QuadraticBezierSegment newSeg = new QuadraticBezierSegment(previousEndPoint, continuedPoint, true);
                            newSeg.IsSmoothJoin = true;
                            pGeo.Figures[0].Segments.Add(newSeg);
                        }
                        else
                        {
                            QuadraticBezierSegment newSeg = new QuadraticBezierSegment(pGeo.Figures[0].StartPoint,continuedPoint,true);
                            pGeo.Figures[0].Segments.Add(newSeg);
                        }
                    }
                )
            );
        }

        private void NewDrawingPath(string startingClientID, Color startingClientColor, Point startingPoint)
        {
            this.Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        Path drawingPath = new Path();
                        drawingPath.Stroke = new SolidColorBrush {Color = startingClientColor};
                        drawingPath.StrokeThickness = 5;
                        drawingPath.StrokeStartLineCap = PenLineCap.Round;
                        drawingPath.StrokeEndLineCap = PenLineCap.Round;

                        PathGeometry geoPath = new PathGeometry();
                        geoPath.Figures = new PathFigureCollection();

                        PathFigure pFigure = new PathFigure {StartPoint = startingPoint};
                        geoPath.Figures.Add(pFigure);
                        drawingPath.Data = geoPath;

                        this._clientPathPoints = pFigure.Segments;
                        this._drawingPaths.Add(startingClientID, drawingPath);
                        MriImageCanvas.Children.Add(drawingPath);
                    }
                )
            );
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
            _clientDrawingColor = GenerateClientColor();
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

        private Color GenerateClientColor()
        {
            Random random = new Random();
            return Color.FromRgb((byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255));
        }

        void ToolBar_OnDrawSelectionChanged(object sender, ToolBarEventArgs e)
        {
            Console.Out.WriteLine("Draw selected " + e.Selected);
            _isDrawing = e.Selected;
        }

        private void MriImageCanvas_TouchDown(object sender, TouchEventArgs e)
        {
            //if (_isDrawing)
            //    SendDrawingMessage("StartDrawing", e.GetTouchPoint(MriImageCanvas).Position);
        }

        private void MriImageCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            //if (_isDrawing)
            //    SendDrawingMessage("ContinueDrawing", e.GetTouchPoint(MriImageCanvas).Position);
        }

        private void MriImageCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
                SendDrawingMessage("StartDrawing", e.GetPosition(MriImageCanvas));
        }

        private void MriImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_isDrawing)
                    SendDrawingMessage("ContinueDrawing", e.GetPosition(MriImageCanvas));
            }
        }

        public void SendDrawingMessage(String messageType, Point p)
        {
            switch (messageType)
            {
                case "StartDrawing":
                    this._clientID = Guid.NewGuid().ToString();
                    break;
                default:
                    break;
            }

            Message msg = new Message(messageType);
            msg.AddField("ClientID", this._clientID);
            msg.AddField("ClientColor", this._clientDrawingColor.ToString());
            msg.AddField("X",p.X);
            msg.AddField("Y",p.Y);
            this._connection.SendMessage(msg);
        }

        private void ClearCanvas()
        {
            this.Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        MriImageCanvas.Children.Clear();
                    }
                )
            );
        }

        #endregion

        #region Pausing Code

        void ToolBar_OnPauseSelectionChanged(object sender, ToolBarEventArgs e)
        {
            if (!e.Selected)
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
            Message msg = new Message("ClearDrawing");
            msg.AddField("ClientID", this._clientID);
            this._connection.SendMessage(msg);
        }

        #endregion

        #endregion ToolBar Functionality

    }
}
