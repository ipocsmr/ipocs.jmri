using CommandLine;
using System;

namespace IPOCS.JMRI
{
  public class Options
  {
    [Option('c', "configuration", Required = false, HelpText = "Specify a configuration file other than the default (ConfigData.xml in current working folder)")]
    public string Configuration { get; set; }
  }
}
