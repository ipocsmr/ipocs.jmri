using Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace IPOCS.JMRI.CONTROL.Models
{
  public class YardObject: ReactiveObject
  {
    public string Ident { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }

    public List<ConnectionPoint> Connections { get; } = new List<ConnectionPoint>();
  }
}
