using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 系统参数配置列表
    /// </summary>
    public class SystemParamsList
    {
        /// <summary>
        /// 配置参数字典
        /// </summary>
        private Dictionary<string, string> _ParamsDict = null;

        /// <summary>
        /// 根据参数名称获取参数值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetParamValueByName(string name)
        {
            if (null == _ParamsDict) return "";

            string value = null;
            Dictionary<string, string> paramsDict = _ParamsDict;
            paramsDict.TryGetValue(name, out value);
            return value;
        }

        /// <summary>
        /// 获取系统配置参数中的整型参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public long GetParamValueIntByName(string name, int defvalue = -1)
        {
            string value = GetParamValueByName(name);
            if (string.IsNullOrEmpty(value))
            {
                return defvalue;
            }

            try
            {
                return Convert.ToInt64(value);
            }
            catch (Exception)
            {
            }

            return defvalue;
        }

        /// <summary>
        /// 获取系统配置参数中的整型参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public long GetParamValueIntByName(string name, long defvalue)
        {
            string value = GetParamValueByName(name);
            if (string.IsNullOrEmpty(value))
            {
                return defvalue;
            }

            try
            {
                return Convert.ToInt64(value);
            }
            catch (Exception)
            {
            }

            return defvalue;
        }

        /// <summary>
        /// 获取系统配置参数中的整型数组参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int[] GetParamValueIntArrayByName(string name)
        {
            string value = GetParamValueByName(name);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                return Global.String2IntArray(value);
            }
            catch (Exception)
            {
            }

            return null;
        }

        /// <summary>
        /// 获取系统配置参数中的整型数组参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<String> GetParamValueStringListByName(string name, char spliteChar = ',')
        {
            string value = GetParamValueByName(name);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                return Global.StringToList(value, spliteChar);
            }
            catch (Exception)
            {
            }

            return null;
        }

        /// <summary>
        /// 获取系统配置参数中的浮点型参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public double GetParamValueDoubleByName(string name)
        {
            string value = GetParamValueByName(name);
            if (string.IsNullOrEmpty(value))
            {
                return 0.0;
            }

            try
            {
                return Convert.ToDouble(value);
            }
            catch (Exception)
            {
            }

            return 0.0;
        }

        /// <summary>
        /// 获取系统配置参数中的浮点数组参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public double[] GetParamValueDoubleArrayByName(string name)
        {
            string value = GetParamValueByName(name);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                return Global.String2DoubleArray(value);
            }
            catch (Exception)
            {
            }

            return null;
        }

        /// <summary>
        /// 加载字典列表
        /// </summary>
        /// <param name="fileName"></param>
        public void LoadParamsList()
        {
            string fileName = string.Format("Config/SystemParams.xml");
            XElement xml = XElement.Load(Global.GameResPath(fileName));
            if (null == xml)
            {
                throw new Exception(string.Format("加载系统配置参数配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
            }

            IEnumerable<XElement> paramNodes = xml.Elements("Params").Elements();
            if (null == paramNodes) return;

            Dictionary<string, string> paramsDict = new Dictionary<string, string>();
            foreach (var xmlNode in paramNodes)
            {
                string paramName = (string)Global.GetSafeAttributeStr(xmlNode, "Name");
                string paramValue = (string)Global.GetSafeAttributeStr(xmlNode, "Value");
                paramsDict[paramName] = paramValue;
            }

            _ParamsDict = paramsDict;

            double[] nArray = GameManager.systemParamsList.GetParamValueDoubleArrayByName("ZhuanShengExpXiShu");
            if (null != nArray)
            {
                for (int i = 0; i < nArray.Length; i++)
                {
                    Data.ChangeLifeEverydayExpRate.Add(i, nArray[i]);
                }
            }
        }

        /// <summary>
        /// 重新加载Xml
        /// </summary>
        /// <param name="fileName"></param>
        public int ReloadLoadParamsList()
        {
            try
            {
                //从Xml中加载(原子访问)
                LoadParamsList();
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }
    }
}
