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
    UNKNOWN,
    INCONSISTENT
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
                Lines.Add(new Avalonia.Controls.Shapes.Line
                {
                  StartPoint = PointA,
                  EndPoint = PointC,
                  Fill = Brush.Parse("Black"),
                  Stroke = Brush.Parse("Black"),
                  StrokeThickness = 1
                });
              });
              break;
            case TurnoutState.CLOSED:
              Dispatcher.UIThread.InvokeAsync(() =>
              {
                Lines.Clear();
                Lines.Add(new Avalonia.Controls.Shapes.Line
                {
                  StartPoint = PointA,
                  EndPoint = PointB,
                  Fill = Brush.Parse("Black"),
                  Stroke = Brush.Parse("Black"),
                  StrokeThickness = 1
                });
              });
              break;
            case TurnoutState.UNKNOWN:
              Dispatcher.UIThread.InvokeAsync(() =>
              {
                Lines.Clear();
                Lines.Add(new Avalonia.Controls.Shapes.Line
                {
                  StartPoint = PointA,
                  EndPoint = PointB,
                  Fill = Brush.Parse("Black"),
                  Stroke = Brush.Parse("Black"),
                  StrokeThickness = 1
                });
                Lines.Add(new Avalonia.Controls.Shapes.Line
                {
                  StartPoint = PointA,
                  EndPoint = PointC,
                  Fill = Brush.Parse("Black"),
                  Stroke = Brush.Parse("Black"),
                  StrokeThickness = 1
                });
              });
              break;
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
    public ObservableCollection<Avalonia.Controls.Shapes.Line> Lines { get; } = new ObservableCollection<Avalonia.Controls.Shapes.Line>();
    public Avalonia.Point PointA { get; set; }
    public string ConnectAName { get; set; }
    public Avalonia.Point PointB { get; set; }
    public string ConnectBName { get; set; }
    public Avalonia.Point PointC { get; set; }
    public string ConnectCName { get; set; }
    public Avalonia.Point PointD { get; set; }
    public string ConnectDName { get; set; }

    public Turnout(Options options, Avalonia.Point pointA, Avalonia.Point pointB, Avalonia.Point pointC, Avalonia.Point pointD)
    {
      Options = options;
      PointA = pointA;
      PointB = pointB;
      PointC = pointC;
      PointD = pointD;
      State = TurnoutState.UNKNOWN;
    }

    public ReactiveUI.ReactiveCommand<string, Unit> ThrowTurnout { get
      {
        return ReactiveUI.ReactiveCommand.Create<string>(DoThrowTurnout);
      }
    }

    private void DoThrowTurnout(string tag)
    {
      var topic = string.Join('/', Options.Channel, "track", "turnout", SystemName.Substring(2));
      ViewModels.MainWindowViewModel.MqttHandler.Send(topic, tag);
      System.Diagnostics.Debug.WriteLine(tag);
    }
  }
}
