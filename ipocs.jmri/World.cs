using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ipocs.jmri
{
    public class World
    {
        ConcurrentDictionary<string, Ocs> unitidToOcs = new ConcurrentDictionary<string, Ocs>();
        ConcurrentDictionary<string, Sop> urlToObj = new ConcurrentDictionary<string, Sop>();
        ConcurrentDictionary<string, Sop> nameToObj = new ConcurrentDictionary<string, Sop>();

        public World() {
        }

        public bool LoadFile(string filename) {
            try {
                LoadConfigData(filename);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        void LoadConfigData(String filename) {
            urlToObj.Clear();
            var doc = new XmlDocument();
            doc.Load(filename);
            foreach (XmlNode concentrator in doc.DocumentElement.ChildNodes) {
                String unitid = null;
                foreach (XmlNode node in concentrator.ChildNodes) {
                    if (node.Name == "UnitID") {
                        if (unitid != null) throw new Exception("multiple UnitID");
                        unitid = node.InnerText;
                    }
                }
                if (unitid == null) throw new Exception("no UnitID");
                Ocs ocs = new Ocs(unitid);
                unitidToOcs[unitid] = ocs;
                foreach (XmlNode node in concentrator.ChildNodes) {
                    if (node.Name == "Objects") {
                        foreach (XmlNode node2 in node.ChildNodes) {
                            String url = null;
                            String obj = null;
                            String sys = null; 
                            foreach (XmlNode node3 in node2.ChildNodes) {
                                if (node3.Name == "Name") {
                                    if (obj != null) throw new Exception("multiple Name");
                                    obj = node3.InnerText;
                                } else if (node3.Name == "SystemName") {
                                    if (url != null) throw new Exception("multiple SystemName");
                                    sys = node3.InnerText;
                                    if (!sys.StartsWith("MT"))
                                        throw new Exception("Expect SystemName to start with MT");
                                    url = "/trains/track/turnout/" + sys.Substring(2);
                                }
                            }
                            if (obj == null) throw new Exception("no Name");
                            if (url == null) throw new Exception("no SystemName");
                            var sop  = new Sop(ocs, obj, url, sys);
                            ocs.Add(sop);
                            if (urlToObj.ContainsKey(url)) throw new Exception("dup url " + url);
                            if (nameToObj.ContainsKey(obj)) throw new Exception("dup obj " + sop);
                            urlToObj[url] = sop;
                            nameToObj[obj] = sop;
                        }
                    }
                }
            }
        }

        public bool IsOcs(string unitid) {
            return unitidToOcs.ContainsKey(unitid);
        }

        public Ocs GetOcs(string unitid) {
            return unitidToOcs[unitid];
        }
        public Sop GetSopFromUrl(string url) {
            return urlToObj[url];
        }
        public Sop GetSopFromName(string name) {
            return nameToObj[name];
        }

        public bool IsSopFromUrl(string url) {
            return urlToObj.ContainsKey(url);
        }

        public bool IsSopFromName(string name) {
            return nameToObj.ContainsKey(name);
        }

    }
}