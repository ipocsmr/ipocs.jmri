using IPOCS.JMRI.Translators;
using IPOCS.Protocol;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPOCS.JMRI.ObjectExtensions
{
  public static class StringExtensions
  {
    public static BaseTranslator GetTranslator(this string[] _, string topicPart)
    {
      var allTranslators = from lAssembly in AppDomain.CurrentDomain.GetAssemblies()
                           from lType in lAssembly.GetTypes()
                           where typeof(BaseTranslator).IsAssignableFrom(lType) && !lType.IsAbstract
                           select lType;
      foreach (var translator in allTranslators)
      {
        var baseTranslator = Activator.CreateInstance(translator) as BaseTranslator;
        if (baseTranslator.SupportedTopics().Contains(topicPart))
        {
          Log.Debug("Found translator: {@translator}", baseTranslator.GetType().FullName);
          return baseTranslator;
        }
      }
      Log.Warning("No translator found for topic part {@topicPart}", topicPart);
      return null;
    }
  }
}
