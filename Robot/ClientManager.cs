using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Robot
{
    class ClientManager
    {
        static int m_Key = 0;
        public static string m_Account = null;
        public static long m_MinNumber = 0;
        public static long m_MaxNumber = 0;
        public static string m_LoginIP = null;
        public static long m_LoginPort = 0;
        public static string m_GameIP = null;
        public static long m_GamePort = 0;
        public Dictionary<int, NetManager> m_ClientList = new Dictionary<int, NetManager>();
      

        public static string GetXElementNodePath(XElement element)
        {
            try
            {
                string path = element.Name.ToString();
                element = element.Parent;
                while (null != element)
                {
                    path = element.Name.ToString() + "/" + path;
                    element = element.Parent;
                }

                return path;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public static XAttribute GetSafeAttribute(XElement XML, string root, string attribute)
        {
            try
            {
                XAttribute attrib = XML.Element(root).Attribute(attribute);
                if (null == attrib)
                {
                    throw new Exception(string.Format("读取属性: {0}/{1} 失败, xml节点名: {2}", root, attribute, GetXElementNodePath(XML)));
                }

                return attrib;
            }
            catch (Exception)
            {
                throw new Exception(string.Format("读取属性: {0}/{1} 失败, xml节点名: {2}", root, attribute, GetXElementNodePath(XML)));
            }
        }
        public static string GetSafeAttributeStr(XElement XML, string root, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, root, attribute);
            return (string)attrib;
        }

        public static long GetSafeAttributeLong(XElement XML, string root, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, root, attribute);
            string str = (string)attrib;
            if (null == str || str == "") return -1;

            try
            {
                return (long)Convert.ToDouble(str);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("读取属性: {0}/{1} 失败, xml节点名: {2}", root, attribute, GetXElementNodePath(XML)));
            }
        }
        public void LoadConfig()
        {
            XElement xml = null;

            try
            {
                xml = XElement.Load(@"Robot.xml");
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", @"AppConfig.xml"));
            }
            m_Account = GetSafeAttributeStr(xml, "Robot", "Account");
            m_MinNumber = GetSafeAttributeLong(xml, "Robot", "MinNumber");
            m_MaxNumber = GetSafeAttributeLong(xml, "Robot", "MaxNumber");

            m_LoginIP = GetSafeAttributeStr(xml, "LoginService", "IP");
            m_LoginPort = GetSafeAttributeLong(xml, "LoginService", "port");
            m_GameIP = GetSafeAttributeStr(xml, "GameService", "IP");
            m_GamePort = GetSafeAttributeLong(xml, "GameService", "port");
        }
        public void InitClient(int _Count)
        {
            LoadConfig();
           for (int  i = 0;i < _Count;i++)
            {
                NetManager mNetManager = new NetManager();
                mNetManager.InitClient(mNetManager);
                if (mNetManager.ConnectSer(m_LoginIP, m_GameIP, (int)m_LoginPort, (int)m_GamePort, ++m_Key))
                {
                  //  m_ClientList.Add(m_Key, mNetManager);
                      mNetManager.SendLoginData();
                    // mNetManager.SendRoleData(); 9929,
                }

               

            }
        }
    }
}
