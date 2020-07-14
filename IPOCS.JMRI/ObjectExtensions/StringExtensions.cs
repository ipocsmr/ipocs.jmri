using IPOCS.JMRI.Translators;
using IPOCS.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPOCS.JMRI.ObjectExtensions
{
  public static class StringExtensions
  {
    public static BaseTranslator GetTranslator(this string[] array, string topicPart)
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
          Console.WriteLine($"Found translator: {baseTranslator.GetType().Name}");
          return baseTranslator;
        }
      }
      Console.WriteLine($"GetTranslator: Unexpected topic part ({ topicPart })");
      return null;
    }
  }
}
