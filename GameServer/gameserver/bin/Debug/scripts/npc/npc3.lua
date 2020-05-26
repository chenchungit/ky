-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font color=\"#ff0000\">你好:<font color=\"#10d5ff\">你需要什么?\n\n"
  .." <a href=\"event:_mendequipment\">[修理装备]</a>\n\n"
  .." <a href=\"event:_canclemendequipment\">[取消修理]</a>\n\n"
  .." <a href=\"event:_huizhangexchange\">[徽章兑 换]</a>\n\n"
  .." <a href=\"event:_jingyuanexchange\">[精元兑换]</a>\n\n"
  .." <a href=\"event:onMyNpcMenuClick\">[召唤一个怪]</a>\n\n"
  .." <a href=\"event:onMyNpcMenuClick1\">[召唤6个怪]</a>\n\n"
end

function onMyNpcMenuClick(luaMgr, client, params)
     luaMgr:GotoMap(client, 1)
--   return string.format("角色 ID = %d, 角色名称 %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function onMyNpcMenuClick1(luaMgr, client, params)
--    luaMgr:GotoMap(client, 6)
    luaMgr:AddDynamicMonsters(client, 14, 1, 25, 24, 3)
    luaMgr:AddDynamicMonsters(client, 15, 2, 26, 24, 3)
end

function _mendequipment(luaMgr, client, params)
--     luaMgr:GotoMap(client, 1)
--   return string.format("角色 ID = %d, 角色名称 %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function _canclemendequipment(luaMgr, client, params)
--     luaMgr:GotoMap(client, 1)
--   return string.format("角色 ID = %d, 角色名称 %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function _jingyuanexchange(luaMgr, client, params)
--     luaMgr:GotoMap(client, 1)
--   return string.format("角色 ID = %d, 角色名称 %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end
