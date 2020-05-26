using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 版本系统开放管理器 [XSea 2015/5/4]
    /// 1.用途：
    /// 根据功能Key获取对应该功能在本版本是否开启
    /// 2.使用：
    /// 根据Config/VersionSystemOpen.xml中SystemName制定常量
    /// 对应存放于VersionSystemOpenKey中
    /// 3.接口IsVersionSystemOpen调用：
    /// GameManager.VersionSystemOpenMgr.IsVersionSystemOpen(key);
    /// 
    /// 注：不要用配置文件中的ID列，要使用字符串功能标识
    /// </summary>
    public class VersionSystemOpenManager
    {
        /// <summary>
        /// 版本系统开放字典线程锁
        /// </summary>
        private object _VersionSystemOpenMutex = new object();

        /// <summary>
        /// 存放版本系统开放信息 key = SystemCode，value = IsOpen(0=不开，1=开放)
        /// </summary>
        private Dictionary<string, int> VersionSystemOpenDict = new Dictionary<string, int>();

        /// <summary>
        /// 加载版本系统开放配置表
        /// </summary>
        public void LoadVersionSystemOpenData()
        {
            lock (_VersionSystemOpenMutex)
            {
                string fileName = "Config/VersionSystemOpen.xml"; // 配置文件地址
                GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(fileName)); // 移除缓存
                XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(fileName)); // 获取XML
                if (null == xml)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("加载{0}时出错!!!文件异常", fileName));
                    return;
                }

                IEnumerable<XElement> xmlItems = xml.Elements();

                // 清空容器
                VersionSystemOpenDict.Clear();

                // 放入容器管理 key = SystemCode，value = IsOpen(0=不开，1=开放)
                foreach (var xmlItem in xmlItems)
                {
                    string key = Global.GetSafeAttributeStr(xmlItem, "SystemName");
                    int nValue = (int)Global.GetSafeAttributeLong(xmlItem, "IsOpen");

                    VersionSystemOpenDict[key] = nValue;
                }
            }
        }

        /// <summary>
        /// 通过VersionSystemOpenKey中的常量作为key获取该功能在本版本中是否开启
        /// </summary>
        public bool IsVersionSystemOpen(string key)
        {
            int nValue = 0; // 是否开放 0=不开，1=开放
            bool bRes = false;

            lock (_VersionSystemOpenMutex)
            {
                // 根据key找是否开放
                if (VersionSystemOpenDict.TryGetValue(key, out nValue))
                {
                    // 开放
                    if (nValue == 1)
                        bRes = true;
                    else // 不开放
                        bRes = false;
                }
            }
            return bRes;
        }
    }
}
