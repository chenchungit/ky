using GameServer.Core.Executor;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GameServer.Logic.ActivityNew
{
    public enum EJieRiFuLiType
    {
        CallPetReplace = 1, //精灵捕获额外获得专属精灵
        SoulStoneExtFunc = 2, //魂石额外功能
        FuFeiQiFu = 3, // 付费祈福
    }

    class JieRiFuLiItem
    {
        public EJieRiFuLiType Type;
        public int Open;
        public string StartDate;
        public string EndDate;
        public object Arg;
    }

    /// <summary>
    /// 节日福利
    /// </summary>
    public class JieRiFuLiActivity : Activity
    {
        private readonly string FuLiCfgFile = "Config/JieRiGifts/JieRiFuLi.xml";
        private Dictionary<EJieRiFuLiType, JieRiFuLiItem> fuliDict = new Dictionary<EJieRiFuLiType, JieRiFuLiItem>();

        // 初始化配置文件信息
        public bool Init()
        {
            try
            {
                // 节日福利配置文件
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(FuLiCfgFile));
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(FuLiCfgFile));
                if (null == xml)
                {
                    LogManager.WriteLog(LogTypes.Fatal, string.Format("加载{0}时出错!!!文件不存在", FuLiCfgFile));
                    return false;
                }

                XElement args = xml.Element("Activities");
                if (null != args)
                {
                    FromDate = Global.GetSafeAttributeStr(args, "FromDate");
                    ToDate = Global.GetSafeAttributeStr(args, "ToDate");
                    ActivityType = (int)Global.GetSafeAttributeLong(args, "ActivityType");

                    AwardStartDate = Global.GetSafeAttributeStr(args, "AwardStartDate");
                    AwardEndDate = Global.GetSafeAttributeStr(args, "AwardEndDate");
                }

                args = xml.Element("GiftList");

                foreach (var fuliXml in args.Elements())
                {
                    JieRiFuLiItem item = new JieRiFuLiItem();
                    item.Type = (EJieRiFuLiType)Global.GetSafeAttributeLong(fuliXml, "TypeID");
                    item.Open = (int)Global.GetSafeAttributeLong(fuliXml, "Button");
                    item.StartDate = Global.GetSafeAttributeStr(fuliXml, "AwardStartDate");
                    item.EndDate = Global.GetSafeAttributeStr(fuliXml, "AwardEndDate");

                    string szArg = Global.GetSafeAttributeStr(fuliXml, "Function");
                    if (item.Type == EJieRiFuLiType.CallPetReplace)
                    {
                        item.Arg = Convert.ToInt32(szArg);
                    }
                    else if (item.Type == EJieRiFuLiType.SoulStoneExtFunc)
                    {
                        string[] fields = szArg.Split('|');
                        List<Tuple<int, int>> argList = new List<Tuple<int, int>>();
                        for (int i = 0; i < fields.Length; ++i)
                        {
                            string[] fields2 = fields[i].Split(',');
                            argList.Add(new Tuple<int, int>(Convert.ToInt32(fields2[0]), Convert.ToInt32(fields2[1])));
                        }
                        item.Arg = argList;
                    }
                    else
                    {
                        item.Arg = szArg;
                    }

                    fuliDict.Add(item.Type, item);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", FuLiCfgFile, ex.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 福利是否开启
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public bool IsOpened(EJieRiFuLiType type, out object arg)
        {
            arg = null;

            if (!InActivityTime()) return false;

            JieRiFuLiItem item = null;
            if (!fuliDict.TryGetValue(type, out item))
                return false;

            DateTime startTime = DateTime.Parse(item.StartDate);
            DateTime endTime = DateTime.Parse(item.EndDate);
            if (TimeUtil.NowDateTime() < startTime || TimeUtil.NowDateTime() > endTime)
                return false;

            if (item.Open != 1)
                return false;

            arg = item.Arg;
            return true;
        }
    }
}
