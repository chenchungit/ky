using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.cc.Defult
{
    public class GlobalDefultOccTimeManager
    {
        Dictionary<int, Object > DefultOccTimeList = new Dictionary<int, Object>();
        public void LoadConfigXml()
        {
            XElement xml = null;
            try
            {
                string Url = Global.GameResPath("Config/Timer/OccTimeStep.xml");
                xml = XElement.Load(Url);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("加载角色移动时间间隔配置文件:{0}, 失败。没有找到相关XML配置文件!", "OccTimeStep.xml"));
            }
            IEnumerable<XElement> DefultOccTimeItems = xml.Elements("Times").Elements();
            if (null == DefultOccTimeItems) return;
            foreach (var monsterItem in DefultOccTimeItems)
            {
                GlobalDefultOccTimeObject szDefultOccTimeObject = new GlobalDefultOccTimeObject();
                // IEnumerable<XElement> MoveXml = xml.Elements("Times").Elements();
                szDefultOccTimeObject.ID = (int)Global.GetSafeAttributeLong(monsterItem, "ID");
                szDefultOccTimeObject.Occ = (int)Global.GetSafeAttributeLong(monsterItem, "occ");
                szDefultOccTimeObject.MoveSpeed = (int)Global.GetSafeAttributeLong(monsterItem, "MoveSpeed");
                szDefultOccTimeObject.MoveFrameTime = (int)Global.GetSafeAttributeLong(monsterItem,"MoveFrameTime");
                szDefultOccTimeObject.MoveType = (int)Global.GetSafeAttributeLong(monsterItem,"MoveType");
                DefultOccTimeList[szDefultOccTimeObject.Occ] = szDefultOccTimeObject;
            }
        }
        /// <summary>
        /// 获取职业对应移动速度，时间的对象，主要用于角色移动的服务器客户端校对
        /// </summary>
        /// <param name="_Occ"></param>
        /// <param name="_DefultOccTime"></param>
        /// <returns></returns>
        public bool getDefultOccTime(int _Occ,out Object _DefultOccTime)
        {
            foreach(var s in DefultOccTimeList)
            {
                if(s.Key == _Occ)
                {
                    _DefultOccTime = s.Value;
                    return true;
                }
            }
            _DefultOccTime = null;
            return false;
        }
    }
}
