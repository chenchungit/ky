using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.cc.Defult
{
   public class GlobalDefultManager
    {
        private const string DefultPathName = "Config/OccDefult.xml";
        private const string DefultRootName = "OccDefult";
        private Dictionary<int, GlobalDefultObject> _SystemGlobalDefultList = new Dictionary<int, GlobalDefultObject>();
        public void InitlDefult()
        {

            XElement szxXml = null;
            try
            {
                string fullPathFileName = Global.GameResPath(DefultPathName);
                szxXml = XElement.Load(fullPathFileName);
                if (null == szxXml)
                {
                    throw new Exception(string.Format("加载技能xml配置文件:{0}, 失败。没有找到相关XML配置文件!", DefultPathName));
                }
                SystemXmlItem systemXmlItem = new SystemXmlItem();
                IEnumerable<XElement> nodes = szxXml.Elements(DefultRootName).Elements();
                foreach (var node in nodes)
                {
                    systemXmlItem.XMLNode = node;

                    GlobalDefultObject globalDefultObject = new GlobalDefultObject()
                    {
                        ID = systemXmlItem.GetIntValue("ID"),
                        Occupation = systemXmlItem.GetIntValue("Occ"),
                        SkillID = systemXmlItem.GetIntValue("SkillID"),

                    };
                    _SystemGlobalDefultList.Add(globalDefultObject.ID, globalDefultObject);
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载技能xxml配置文件:{0}, 失败。没有找到相关XML配置文件!", DefultPathName));
            }


        }
        public Dictionary<int, GlobalDefultObject> SystemGlobalDefultList
        {
            get { return _SystemGlobalDefultList; }
        }
        public void GetOccDefultSkillList(int _occ,out List<GlobalDefultObject> _SkillList)
        {
            _SkillList = new List<GlobalDefultObject>();
           foreach (var s in _SystemGlobalDefultList)
            {
                if (_occ == s.Value.Occupation)
                    _SkillList.Add(s.Value);
            }
           
        }
    }
}
