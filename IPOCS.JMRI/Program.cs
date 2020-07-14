using IPOCS;
using IPOCS.Protocol.Packets.Orders;
using IPOCS.Protocol.Packets.Status;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using IPOCS_Programmer.ObjectTypes;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;

namespace IPOCS.JMRI
{
  class Program
  {
    static void Main()
    {
      global::JMRI.Layout_Config jmriConfig = null;

      IConfiguration config = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json", false, true)
          .AddJsonFile("appsettings.dev.json", true, true)
          .Build();
      var options = config.Get<Options>();

      var jmriHandler = new JmriHandler(options);
      var ipocsHandler = new IpocsHandler();
      var translator = new Translator(ipocsHandler, jmriHandler, options);

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

        XmlSerializer xsSubmit = new XmlSerializer(translator.IpocsConfig.GetType(), types.ToArray());
        translator.IpocsConfig.AddRange(xsSubmit.Deserialize(fileStream) as List<Concentrator>);
      }

      if (jmriConfig == null || translator.IpocsConfig.Count == 0)
      {
        Console.WriteLine("Unable to read configurations!");
        return;
      }

      System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      string version = fvi.FileVersion;
      Console.WriteLine($"{fvi.ProductName} Version {version}");

      Console.WriteLine("Mapping JMRI and IPOCS objects");


      var turnoutManager = jmriConfig.Turnouts.First(tmt => tmt.Class == "jmri.jmrix.mqtt.configurexml.MqttTurnoutManagerXml");
      foreach (var turnout in turnoutManager.Turnout)
      {
        Console.WriteLine(turnout.SystemName + " = " + turnout.UserName);
        translator.TurnoutMapping.Add(turnout.UserName, turnout.SystemName);
      }

      ipocsHandler.Setup();
      jmriHandler.Setup();

      Console.WriteLine("Enter q<RETURN> to exit program");
      while (true)
      {
        Console.Write("> ");
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
    }
  }
}
