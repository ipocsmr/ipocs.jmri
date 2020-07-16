using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using IPOCS_Programmer.ObjectTypes;
using Microsoft.Extensions.Configuration;
using IPOCS.JMRI.Translators;
using Serilog;

namespace IPOCS.JMRI
{
  class Program
  {
    static void Main()
    {
      var log = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File("app.log", rollingInterval: RollingInterval.Day)
        .CreateLogger();
      Log.Logger = log;

      System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      string version = fvi.FileVersion;
      Log.Information("{@ProductName} Version {@FileVersion}", fvi.ProductName, fvi.FileVersion);

      global::JMRI.Layout_Config jmriConfig = null;
      List<Concentrator> ipocsConfig = null;

      IConfiguration config = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json", false, true)
          .AddJsonFile("appsettings.dev.json", true, true)
          .Build();
      var options = config.Get<Options>();

      using (var fileStream = File.Open(options.JmriConfig, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        var serializer = new XmlSerializer(typeof(global::JMRI.Layout_Config));
        jmriConfig = serializer.Deserialize(fileStream) as global::JMRI.Layout_Config;
      }
      using (var fileStream = File.Open(options.IpocsConfig, FileMode.Open, FileAccess.Read))
      {
        var types = (from lAssembly in AppDomain.CurrentDomain.GetAssemblies()
                     from lType in lAssembly.GetTypes()
                     where typeof(BasicObject).IsAssignableFrom(lType)
                     select lType).ToList();
        var types2 = (from lAssembly in AppDomain.CurrentDomain.GetAssemblies()
                      from lType in lAssembly.GetTypes()
                      where typeof(PointsMotor).IsAssignableFrom(lType)
                      select lType).ToList();
        types.AddRange(types2);
        types.Add(typeof(BasicObject));

        XmlSerializer xsSubmit = new XmlSerializer(typeof(List<Concentrator>), types.ToArray());
        ipocsConfig = xsSubmit.Deserialize(fileStream) as List<Concentrator>;
      }

      if (jmriConfig == null || ipocsConfig == null || ipocsConfig.Count == 0)
      {
        Log.Error("Unable to read configurations!");
        return;
      }

      var objectMappings = new Dictionary<string, string>();
      var allTranslators = from lAssembly in AppDomain.CurrentDomain.GetAssemblies()
                           from lType in lAssembly.GetTypes()
                           where typeof(BaseTranslator).IsAssignableFrom(lType) && !lType.IsAbstract
                           select lType;
      foreach (var translatorType in allTranslators)
      {
        var translatorMappings = (Activator.CreateInstance(translatorType) as BaseTranslator).LoadNameMappings(jmriConfig);
        foreach (var translatorMapping in translatorMappings)
        {
          objectMappings.Add(translatorMapping.Key, translatorMapping.Value);
        }
      }

      var jmriHandler = new JmriHandler(options);
      var ipocsHandler = new IpocsHandler(ipocsConfig);
      var translator = new Translator(ipocsHandler, jmriHandler, objectMappings, options);
      ipocsHandler.Setup();
      jmriHandler.Setup();

      Log.Information("MAIN: Enter q<RETURN> to exit program");
      while (true)
      {
        string command = Console.ReadLine();
        if (command == "q")
        {
          break;
        }
        string[] parts = command.Split(':');
        if (parts.Length != 3)
        {
          continue;
        }
        if (parts[0] == "MQTT")
        {
          jmriHandler.Send(parts[1], parts[2]);
        }
      }
      Log.CloseAndFlush();
    }
  }
}
