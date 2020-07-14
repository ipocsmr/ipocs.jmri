using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace IPOCS.JMRI.CONTROL.Models
{
  public class PositionLabel: YardObject
  {
    public string Text { get; set; }
    public FontFamily FontName { get; set; }
    public int FontSize { get; set; }
    public IBrush FontColor { get; set; }
  }
}
