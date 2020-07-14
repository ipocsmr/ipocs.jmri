using Avalonia.Media;
using Avalonia.Threading;
using IPOCS.JMRI.CONTROL.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace IPOCS.JMRI.CONTROL.ViewModels
{
  public class MainWindowViewModel : ViewModelBase
  {
    public static MqttHandler MqttHandler { get; private set; }

    public ObservableCollection<YardObject> Points { get; } = new ObservableCollection<YardObject>();

    public ObservableCollection<string> LogItems { get; } = new ObservableCollection<string>();

    public MainWindowViewModel()
    {
      IConfiguration config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false, true)
        .AddJsonFile("appsettings.dev.json", true, true)
        .Build();
      var options = config.Get<Options>();
      MqttHandler = new MqttHandler(options);
      MqttHandler.OnMqttMessage += MqttHandler_OnMqttMessage;

      var serializer = new XmlSerializer(typeof(global::JMRI.Layout_Config));
      var jmriConfig = serializer.Deserialize(new FileStream(options.JmriConfig, FileMode.Open, FileAccess.Read, FileShare.Read)) as global::JMRI.Layout_Config;

      var turnoutManager = jmriConfig.Turnouts.First(tmt => tmt.Class == "jmri.jmrix.mqtt.configurexml.MqttTurnoutManagerXml");

      var layoutEditor = jmriConfig.LayoutEditor.First(le => le.Name == options.LayoutName);
      foreach (var turnout in layoutEditor.Layoutturnout)
      {
        var PointA_X = turnout.Xa;
        var PointA_Y = turnout.Ya;
        var PointB_X = turnout.Xb;
        var PointB_Y = turnout.Yb;
        var PointC_X = turnout.Xc;
        var PointC_Y = turnout.Yc;
        var PointD_X = turnout.Xd;
        var PointD_Y = turnout.Yd;

        var turnoutManagerTurnout = turnoutManager.Turnout.FirstOrDefault(tmt => tmt.UserName == turnout.Turnoutname);
        var tt = new Turnout(options, Enum.Parse<TurnoutType>(turnout.Type),
          new Avalonia.Point(PointA_X, PointA_Y),
          new Avalonia.Point(PointB_X, PointB_Y),
          new Avalonia.Point(PointC_X, PointC_Y),
          new Avalonia.Point(PointD_X, PointD_Y))
        {
          Ident = turnout.Ident,
          UserName = turnoutManagerTurnout?.UserName ?? string.Empty,
          SystemName = turnoutManagerTurnout?.SystemName ?? string.Empty,
          X = turnout.Xcen,
          Y = turnout.Ycen,
          ConnectAName = turnout.Connectaname,
          ConnectBName = turnout.Connectbname,
          ConnectCName = turnout.Connectcname,
          ConnectDName = turnout.Connectdname
        };

        Points.Add(tt);
      }
      foreach (var point in layoutEditor.Positionablepoint)
      {
        Points.Add(new Point
        {
          Ident = point.Ident,
          X = point.X,
          Y = point.Y
        });
      }
      foreach (var yardO in layoutEditor.Levelxing)
      {
        Points.Add(new Point
        {
          Ident = yardO.Ident,
          X = yardO.Xcen,
          Y = yardO.Ycen
        });
      }
      foreach (var yardO in layoutEditor.Layoutturntable)
      {
        Points.Add(new TurnTable
        {
          Ident = yardO.Ident,
          X = yardO.Xcen,
          Y = yardO.Ycen,
          Radius = yardO.Radius
        });
      }
      foreach (var tracksegment in layoutEditor.Tracksegment)
      {
        var connect1 = Points.First(p => p.Ident == tracksegment.Connect1Name);
        var point1 = new Avalonia.Point(connect1.X, connect1.Y);
        if (connect1 is Turnout)
        {
          var turnout1 = connect1 as Turnout;
          if (tracksegment.Ident == turnout1.ConnectAName)
          {
            point1 = turnout1.PointA;
          }
          if (tracksegment.Ident == turnout1.ConnectBName)
          {
            point1 = turnout1.PointB;
          }
          if (tracksegment.Ident == turnout1.ConnectCName)
          {
            point1 = turnout1.PointC;
          }
          if (tracksegment.Ident == turnout1.ConnectDName)
          {
            point1 = turnout1.PointD;
          }
        }
        var connect2 = Points.First(p => p.Ident == tracksegment.Connect2Name);
        var point2 = new Avalonia.Point(connect2.X, connect2.Y);
        if (connect2 is Turnout)
        {
          var turnout2 = connect2 as Turnout;
          if (tracksegment.Ident == turnout2.ConnectAName)
          {
            point2 = turnout2.PointA;
          }
          if (tracksegment.Ident == turnout2.ConnectBName)
          {
            point2 = turnout2.PointB;
          }
          if (tracksegment.Ident == turnout2.ConnectCName)
          {
            point2 = turnout2.PointC;
          }
          if (tracksegment.Ident == turnout2.ConnectDName)
          {
            point2 = turnout2.PointD;
          }
        }
        Points.Add(new Line
        {
          Ident = tracksegment.Ident,
          X = point1.X,
          Y = point1.Y,
          X2 = point2.X,
          Y2 = point2.Y
        });
      }
      foreach (var layoutLabel in layoutEditor.Positionablelabel)
      {

        var brush = new BrushConverter().ConvertFromString(Color.FromRgb(layoutLabel.Red, layoutLabel.Green, layoutLabel.Blue).ToString()) as IBrush;
        Points.Add(new PositionLabel
        {
          X = layoutLabel.X,
          Y = layoutLabel.Y,
          Text = layoutLabel.Text,
          FontName = FontFamily.Parse(layoutLabel.Fontname),
          FontSize = int.Parse(layoutLabel.Size),
          FontColor = brush
        });
      }
      Points = new ObservableCollection<YardObject>(Points.Reverse());
      MqttHandler.Setup();
    }

    private void MqttHandler_OnMqttMessage(string systemName, string payload)
    {
      if (Points.FirstOrDefault(yo => yo.SystemName == systemName) is Turnout p)
      {
        TurnoutState result = TurnoutState.UNKNOWN;
        Enum.TryParse<TurnoutState>(payload, out result);
        p.State = result;
      }
      else
      {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
          LogItems.Add($"Unknown yard object: {systemName}");
        });
      }
    }
  }
}
