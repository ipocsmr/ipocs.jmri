using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;

namespace IPOCS.JMRI.CONTROL.Models
{
  public enum TurnoutState
  {
    THROWN,
    CLOSED,
    MOVING,
    UNKNOWN,
    INCONSISTENT
  }

  public enum TurnoutType
  {
    NONE,
    RH_TURNOUT,
    LH_TURNOUT,
    WYE_TURNOUT,
    DOUBLE_XOVER,
    RH_XOVER,
    LH_XOVER,
  }

  public class Turnout : YardObject
  {
    private Options Options { get; }

    private TurnoutState _State;
    public TurnoutState State
    {
      get
      {
        return _State;
      }
      set {
        if (value != _State)
        {
          switch (value)
          {
            case TurnoutState.THROWN:
              Dispatcher.UIThread.InvokeAsync(() =>
              {
                Lines.Clear();
                foreach (var t in Thrown)
                  Lines.Add(t);
              });
              break;
            case TurnoutState.CLOSED:
              Dispatcher.UIThread.InvokeAsync(() =>
              {
                Lines.Clear();
                foreach (var t in Closed)
                  Lines.Add(t);
              });
              break;
            case TurnoutState.UNKNOWN:
              Dispatcher.UIThread.InvokeAsync(() =>
              {
                Lines.Clear();
                foreach (var t in Thrown)
                  Lines.Add(t);
                foreach (var t in Closed)
                  Lines.Add(t);
              });
              break;
            case TurnoutState.MOVING:
            case TurnoutState.INCONSISTENT:
              Dispatcher.UIThread.InvokeAsync(() =>
              {
                Lines.Clear();
              });
              break;
          }
          _State = value;
        }
      }
    }
    public ObservableCollection<Shape> Lines { get; } = new ObservableCollection<Shape>();
    public Avalonia.Point PointA { get; }
    public string ConnectAName { get; set; }
    public Avalonia.Point PointB { get; }
    public string ConnectBName { get; set; }
    public Avalonia.Point PointC { get; }
    public string ConnectCName { get; set; }
    public Avalonia.Point PointD { get; }
    public string ConnectDName { get; set; }
    public TurnoutType TurnoutType { get; }

    public List<Shape> Thrown { get; } = new List<Shape>();
    public List<Shape> Closed { get; } = new List<Shape>();

    public Turnout(Options options, TurnoutType turnoutType, Avalonia.Point pointA, Avalonia.Point pointB, Avalonia.Point pointC, Avalonia.Point pointD)
    {
      Options = options;
      PointA = pointA;
      PointB = pointB;
      PointC = pointC;
      PointD = pointD;
      State = TurnoutState.UNKNOWN;
      TurnoutType = turnoutType;
      switch (turnoutType)
      {
        case TurnoutType.NONE:
          break;
        case TurnoutType.LH_TURNOUT:
        case TurnoutType.RH_TURNOUT:
          Thrown.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointA,
            EndPoint = PointC,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          Closed.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointA,
            EndPoint = PointB,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          break;
        case TurnoutType.DOUBLE_XOVER:
          Thrown.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointA,
            EndPoint = PointC,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          Closed.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointD,
            EndPoint = PointB,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          break;
        case TurnoutType.RH_XOVER:
          Thrown.Add(new Avalonia.Controls.Shapes.Polyline
          {
            Points = new List<Avalonia.Point>
            {
              PointA,
              new Avalonia.Point(PointA.X + (PointB.X - PointA.X) * 1 / 3, PointA.Y),
              new Avalonia.Point(PointD.X + (PointC.X - PointD.X) * 2 / 3, PointD.Y),
              PointC
            },
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          Closed.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointA,
            EndPoint = PointB,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          Closed.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointD,
            EndPoint = PointC,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          break;
        case TurnoutType.LH_XOVER:
          Thrown.Add(new Avalonia.Controls.Shapes.Polyline
          {
            Points = new List<Avalonia.Point>
            {
              PointC,
              new Avalonia.Point(PointD.X + (PointC.X - PointD.X) * 1 / 3, PointD.Y),
              new Avalonia.Point(PointA.X + (PointB.X - PointA.X) * 2 / 3, PointA.Y),
              PointA
            },
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          Closed.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointA,
            EndPoint = PointB,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          Closed.Add(new Avalonia.Controls.Shapes.Line
          {
            StartPoint = PointD,
            EndPoint = PointC,
            Fill = Brush.Parse("Black"),
            Stroke = Brush.Parse("Black"),
            StrokeThickness = 1
          });
          break;
      }
    }


    public ReactiveUI.ReactiveCommand<string, Unit> ThrowTurnout { get
      {
        return ReactiveUI.ReactiveCommand.Create<string>(DoThrowTurnout);
      }
    }

    private void DoThrowTurnout(string tag)
    {
      var topic = string.Join('/', Options.Channel, "command", "track", "turnout", SystemName.Substring(2));
      ViewModels.MainWindowViewModel.MqttHandler.Send(topic, tag);
      System.Diagnostics.Debug.WriteLine(tag);
    }
  }
}
