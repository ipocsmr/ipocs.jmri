using System;
using System.Collections.Generic;
using System.Text;

namespace IPOCS.JMRI.CONTROL.Models
{
  public class TurnTable : YardObject
  {
    public float Width { get; set; }
    public float Height { get; set; }
    public Avalonia.Thickness Margin { get; set; }
    public float Radius
    {
      set
      {
        Width = value * 2;
        Height = value * 2;
        Margin = new Avalonia.Thickness(-value, -value, 0, 0);
      }
    }
  }
}
