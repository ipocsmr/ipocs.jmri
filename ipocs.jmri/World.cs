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

        Dictionary<string, Sop.Straight> sops = new Dictionary<string, Sop.Straight>() {
            { "Aa60", Sop.Straight.Unknown},
            { "Aa61", Sop.Straight.Unknown},
            { "Aa62", Sop.Straight.Unknown},
            { "Aa63", Sop.Straight.Unknown},
            { "Aa64", Sop.Straight.Unknown},
            { "Aa65", Sop.Straight.Unknown},
            { "Aa66", Sop.Straight.Unknown},
            { "Aa68", Sop.Straight.Unknown},
            { "Aa69", Sop.Straight.Unknown},
            { "Aa70", Sop.Straight.Unknown},
            { "Aa71", Sop.Straight.Unknown},
            { "Ba100", Sop.Straight.Unknown},
            { "Ba101", Sop.Straight.Unknown},
            { "Ba102", Sop.Straight.Unknown},
            { "Ba103", Sop.Straight.Unknown},
            { "Ba104", Sop.Straight.Unknown},
            { "Ba105", Sop.Straight.Unknown},
            { "Ba106", Sop.Straight.Unknown},
            { "Ba107", Sop.Straight.Unknown},
            { "Ba108", Sop.Straight.Unknown},
            { "Ba110", Sop.Straight.Unknown},
            { "Ba111", Sop.Straight.Unknown},
            { "Ba113", Sop.Straight.Unknown},
            { "Ba114", Sop.Straight.Unknown},
            { "Ba115", Sop.Straight.Right},
            { "Ba116", Sop.Straight.Unknown},
            { "Ba117", Sop.Straight.Right},
            { "Ba118", Sop.Straight.Unknown},
            { "Ba119", Sop.Straight.Unknown},
            { "Ba120", Sop.Straight.Unknown},
            { "Ba121", Sop.Straight.Unknown},
            { "Ha60", Sop.Straight.Unknown},
            { "Ha61", Sop.Straight.Unknown},
            { "Ha62", Sop.Straight.Unknown},
            { "Ha63", Sop.Straight.Unknown},
            { "Ha64", Sop.Straight.Unknown},
            { "Ha65", Sop.Straight.Unknown},
            { "Ha66", Sop.Straight.Unknown},
            { "Ha67", Sop.Straight.Unknown},
            { "Ha68", Sop.Straight.Unknown},
            { "Ha69", Sop.Straight.Unknown},
            { "Mk60", Sop.Straight.Unknown},
            { "Mk61", Sop.Straight.Unknown},
            { "Mk62", Sop.Straight.Unknown},
            { "Mk63", Sop.Straight.Unknown},
            { "Mk64", Sop.Straight.Unknown},
            { "Mk65", Sop.Straight.Unknown},
            { "Mk66", Sop.Straight.Unknown},
            { "Mk67", Sop.Straight.Unknown},
            { "Mk68", Sop.Straight.Unknown},
            { "Mk69", Sop.Straight.Unknown},
            { "Sn60", Sop.Straight.Unknown},
            { "Sn61", Sop.Straight.Unknown},
            { "Sn62", Sop.Straight.Unknown},
            { "Sn63", Sop.Straight.Unknown},
            { "Sn64", Sop.Straight.Unknown},
            { "Sn65", Sop.Straight.Unknown},
            { "Sn66", Sop.Straight.Unknown},
            { "Sn67", Sop.Straight.Unknown},
            { "Sn69", Sop.Straight.Unknown},
            { "Vd60", Sop.Straight.Unknown},
            { "Vd61", Sop.Straight.Unknown},
            { "Vd62", Sop.Straight.Unknown},
            { "Vd63", Sop.Straight.Unknown},
            { "Vd64", Sop.Straight.Unknown},
            { "Vd65", Sop.Straight.Unknown},
            { "Vd66", Sop.Straight.Unknown}
        };


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
                            //int id = Int32.Parse(unitid);
                            var sop  = new Sop(ocs, obj, url, sys, sops[obj]);
                            ocs.Add(sop);
                            if (urlToObj.ContainsKey(url)) throw new Exception("dup url " + url);
                            //if (objToUrl.ContainsKey(obj)) throw new Exception("dup obj " + sop);
                            urlToObj[url] = sop;
                            //objToUrl[obj] = url;
                        }
                    }
                }
            }
        }

        public Ocs GetOcs(string unitid) {
            return unitidToOcs[unitid];
        }
        public Sop GetSop(string url) {
            return urlToObj[url];
        }
        public bool IsSop(string url) {
            return urlToObj.ContainsKey(url);
        }

    }
}