using CommandLine;
using System;

namespace ipocs.jmri
{
  public class Options
  {
    [Option('c', "configuration", Required = false, HelpText = "Specify a configuration file other than the default (ConfigData.xml in current working folder)")]
    public string Configuration { get; set; }
  }
}
