-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font color=\"#ff0000\">���:<font color=\"#10d5ff\">����Ҫʲô?\n\n"
  .." <a href=\"event:_mendequipment\">[����װ��]</a>\n\n"
  .." <a href=\"event:_canclemendequipment\">[ȡ������]</a>\n\n"
  .." <a href=\"event:_huizhangexchange\">[���¶� ��]</a>\n\n"
  .." <a href=\"event:_jingyuanexchange\">[��Ԫ�һ�]</a>\n\n"
  .." <a href=\"event:onMyNpcMenuClick\">[�ٻ�һ����]</a>\n\n"
  .." <a href=\"event:onMyNpcMenuClick1\">[�ٻ�6����]</a>\n\n"
end

function onMyNpcMenuClick(luaMgr, client, params)
     luaMgr:GotoMap(client, 1)
--   return string.format("��ɫ ID = %d, ��ɫ���� %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function onMyNpcMenuClick1(luaMgr, client, params)
--    luaMgr:GotoMap(client, 6)
    luaMgr:AddDynamicMonsters(client, 14, 1, 25, 24, 3)
    luaMgr:AddDynamicMonsters(client, 15, 2, 26, 24, 3)
end

function _mendequipment(luaMgr, client, params)
--     luaMgr:GotoMap(client, 1)
--   return string.format("��ɫ ID = %d, ��ɫ���� %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function _canclemendequipment(luaMgr, client, params)
--     luaMgr:GotoMap(client, 1)
--   return string.format("��ɫ ID = %d, ��ɫ���� %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function _jingyuanexchange(luaMgr, client, params)
--     luaMgr:GotoMap(client, 1)
--   return string.format("��ɫ ID = %d, ��ɫ���� %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end
