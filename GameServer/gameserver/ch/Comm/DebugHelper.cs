using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.ch.Comm
{
    public static class DebugHelper
    {

        /// <summary>
        /// 逻辑枚举名字字符
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemValue"></param>
        /// <returns></returns>
        public static string EnumShowStr<T>(int itemValue)
        {
            if (itemValue > 30000 //cc编号
                ||
                (itemValue >= 15000 && itemValue < 20000)//ch编号
                )
                return string.IsNullOrEmpty(ConvertEnumToString<CC.CommandID>(itemValue)) ?
                    ConvertEnumToString<CC.CommandID>(itemValue) :
                    "不存在";
            else//自带编号
                return string.IsNullOrEmpty(ConvertEnumToString<Server.TCPGameServerCmds>(itemValue))?
                    ConvertEnumToString<Server.TCPGameServerCmds>(itemValue) :
                    "不存在";
        }

        private static string ConvertEnumToString<T>(int itemValue)
        {
            return Enum.Parse(typeof(T), itemValue.ToString()).ToString();
        }

    }
}
