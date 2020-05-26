using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.IO;

using System.Collections.Specialized;
using Neo.IronLua;

namespace GameServer.Logic
{
    /// <summary>
    /// 执行Lua脚本管理类
    /// </summary>
    public class LuaExeInfo
    {
        /// <summary>
        /// Lua脚本最后修改时间
        /// </summary>
        public DateTime dateLastWrite;

        /// <summary>
        /// 编译后的Lua脚本
        /// </summary>
        public LuaChunk luaChunk;
    }

    /// <summary>
    /// 执行Lua脚本管理类
    /// </summary>
    public class LuaExeManager
    {
        /// <summary>
        /// Lua脚本缓存字典
        /// </summary>
        private Dictionary<string, LuaExeInfo> dictLuaCache = new Dictionary<string, LuaExeInfo>();

        /// <summary>
        /// Lua对象
        /// </summary>
        private Lua lua = new Lua();

        /// <summary>
        /// Lua运行环境
        /// </summary>
        LuaGlobal gEnv = null;

        /// <summary>
        /// 字典数据检测定时器
        /// </summary>
        private static Timer timerCheckDict;

        /// <summary>
        /// Lua管理对象单件
        /// </summary>
        private static LuaExeManager instance = new LuaExeManager();
        public static LuaExeManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 初始化Lua运行环境
        /// </summary>
        public void InitLuaEnv()
        {            
            gEnv = lua.CreateEnvironment();

            // 每100秒对其中一个字典进行检测
            timerCheckDict = new Timer(100 * 1000);
            timerCheckDict.Elapsed += new ElapsedEventHandler(CheckDictLuaInfo);
            timerCheckDict.Interval = 100 * 1000;
            timerCheckDict.Enabled = true;
        }

        /// <summary>
        /// 定时对字典数组中的某个字典进行检测，更新Lua脚本
        /// </summary>
        private void CheckDictLuaInfo(object source, ElapsedEventArgs e)
        {
            lock (dictLuaCache)
            {
                foreach (KeyValuePair<string, LuaExeInfo> kvLuaInfo in dictLuaCache)
                {
                    DateTime dateNew = File.GetLastWriteTime(kvLuaInfo.Key);
                    if (dateNew > kvLuaInfo.Value.dateLastWrite)
                    {
                        Func<string> code = () => File.ReadAllText(kvLuaInfo.Key);
                        string chunkName = Path.GetFileName(kvLuaInfo.Key);

                        LuaChunk c = lua.CompileChunk(code(), chunkName, false);
                        kvLuaInfo.Value.dateLastWrite = dateNew;
                        kvLuaInfo.Value.luaChunk = c;
                    }
                }
            }
        }

        /// <summary>
        /// 运行Lua脚本
        /// </summary>
        public LuaGlobal ExeLua(String strLuaPath)
        {
            LuaExeInfo exeInfo = null;
            String strFullPath = Path.GetFullPath(strLuaPath);
            lock (dictLuaCache)
            {
                if (!dictLuaCache.TryGetValue(strFullPath, out exeInfo))
                {
                    Func<string> code = () => File.ReadAllText(strFullPath);
                    string chunkName = Path.GetFileName(strFullPath);

                    LuaChunk c = lua.CompileChunk(code(), chunkName, false);
                    exeInfo = new LuaExeInfo();
                    exeInfo.dateLastWrite = File.GetLastWriteTime(strFullPath);
                    exeInfo.luaChunk = c;
                    dictLuaCache.Add(strFullPath, exeInfo);
                }

                gEnv.DoChunk(exeInfo.luaChunk);
            }

            return gEnv;
        }

        public LuaResult ExecLuaFunction(LuaManager luaManager, LuaGlobal g, string strLuaFunction, GameClient client)
        {
            lock (dictLuaCache)
            {
                LuaResult retValue = (g as dynamic)[strLuaFunction](GameManager.LuaMgr, client, /*lua.GetTable("tab")*/null);
                return retValue;
            }
        }
    }
}
