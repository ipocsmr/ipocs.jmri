using Avalonia;
using System;
using System.Collections.Generic;
using System.Text;

namespace IPOCS.JMRI.CONTROL.Models
{
  public class Line: YardObject
  {
    public double X2 { get; set; }
    public double Y2 { get; set; }

    public Avalonia.Point StartPoint => new Avalonia.Point(X, Y);
    public Avalonia.Point EndPoint => new Avalonia.Point(X2, Y2);
  }
}
