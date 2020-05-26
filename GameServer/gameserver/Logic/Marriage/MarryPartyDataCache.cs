using System.Collections.Generic;
using Server.Data;
using Server.Protocol;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 帮会领地管理
    /// </summary>
    /// 
    
    public class MarryPartyDataCache
    {  
        #region 基础数据       
        public Dictionary<int, MarryPartyData> MarryPartyList// = new Dictionary<int, MarryPartyData>();
        {
            private get;
            set;
        }
        #endregion 基础数据

        #region 基础方法
        public MarryPartyData AddParty(int roleID, int partyType, long startTime, int husbandRoleID, int wifeRoleID, string husbandName, string wifeName)
        {
            MarryPartyData data = null;

            lock (MarryPartyList)
            {
                if (MarryPartyList.ContainsKey(husbandRoleID) == false &&
                    MarryPartyList.ContainsKey(wifeRoleID) == false)
                {
                    data = new MarryPartyData()
                    {
                        RoleID = roleID,
                        PartyType = partyType,
                        JoinCount = 0,
                        StartTime = startTime,
                        HusbandRoleID = husbandRoleID,
                        WifeRoleID = wifeRoleID,
                        HusbandName = husbandName,
                        WifeName = wifeName,
                    };
                    MarryPartyList.Add(roleID, data);
                }
            }

            return data;
        }

        public void SetPartyTime(MarryPartyData data, long startTime)
        {
            lock (MarryPartyList)
            {
                data.StartTime = startTime;
            }
        }

        public bool RemoveParty(int roleid)
        {
            lock (MarryPartyList)
            {
                return MarryPartyList.Remove(roleid);
            }
        }

        public void RemovePartyCancel(MarryPartyData partyData)
        {
            lock (MarryPartyList)
            {
                try
                {
                    MarryPartyList.Add(partyData.RoleID, partyData);
                }
                catch
                {
                }
            }
        }

        public bool IncPartyJoin(int roleid, int maxJoin, out bool remove)
        {
            remove = false;

            MarryPartyData data = null;
            lock (MarryPartyList)
            {
                bool ret = MarryPartyList.TryGetValue(roleid, out data);
                if (ret == true)
                {
                    if (data.JoinCount < maxJoin)
                    {
                        ++data.JoinCount;
                        if (data.JoinCount == maxJoin)
                        {
                            remove = true;
                        }
                    }
                    else
                    {
                        ret = false;
                    }
                }
                return ret;
            }
        }
        public void IncPartyJoinCancel(int roleid)
        {
            MarryPartyData data = null;
            lock (MarryPartyList)
            {
                if (MarryPartyList.TryGetValue(roleid, out data) == true)
                {
                    --data.JoinCount;
                }
            }
        }

        public MarryPartyData GetParty(int roleid)
        {
            MarryPartyData data = null;
            lock (MarryPartyList)
            {
                MarryPartyList.TryGetValue(roleid, out data);
                return data;
            }
        }

        public int GetPartyCount()
        {
            lock (MarryPartyList)
            {
                return MarryPartyList.Count;
            }
        }

        public TCPOutPacket GetPartyList(TCPOutPacketPool pool, int cmdID)
        {
            lock (MarryPartyList)
            {
                return DataHelper.ObjectToTCPOutPacket<Dictionary<int, MarryPartyData>>(MarryPartyList, pool, cmdID);
            }
        }

        public bool HasPartyStarted(long ticks)
        {
            bool showNPC = false;

            lock (MarryPartyList)
            {
                foreach (KeyValuePair<int, MarryPartyData> kv in MarryPartyList)
                {
                    if (ticks > kv.Value.StartTime)
                    {
                        showNPC = true;
                        break;
                    }
                }
            }

            return showNPC;
        }
        #endregion

        #region 角色改名，修改婚宴缓存角色名
        public void OnChangeName(int roleId, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                return;
            }

            // 未婚，应该没有婚宴数据，如果申请了婚宴，然后离婚，需要确认是否删除婚宴数据
            SafeClientData clientData = Global.GetSafeClientDataFromLocalOrDB(roleId);

            if (clientData == null
                || clientData.MyMarriageData == null
                || clientData.MyMarriageData.nSpouseID == -1)
            {
                return;
            }

            // 修改我或者我的配偶举办的婚宴
            lock (MarryPartyList)
            {
                MarryPartyData data = null;

                // 我举办的婚宴
                MarryPartyList.TryGetValue(clientData.RoleID, out data);
                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.HusbandName) && data.HusbandName == oldName)
                    {
                        data.HusbandName = newName;
                    }
                    else if (!string.IsNullOrEmpty(data.WifeName) && data.WifeName == oldName)
                    {
                        data.WifeName = newName;
                    }
                }

                // 我的配偶举办的婚宴
                data = null;
                MarryPartyList.TryGetValue(clientData.MyMarriageData.nSpouseID, out data);
                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.HusbandName) && data.HusbandName == oldName)
                    {
                        data.HusbandName = newName;
                    }
                    else if (!string.IsNullOrEmpty(data.WifeName) && data.WifeName == oldName)
                    {
                        data.WifeName = newName;
                    }
                }
            }
        }
        #endregion
    }
}
