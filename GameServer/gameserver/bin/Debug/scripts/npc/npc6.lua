-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font size=\"14\" color=\"#ffff00\">�ٻ�����:</font> <font color=\"#10d5ff\">��������ٻ��Լ��Ĺ���\n\n      <a href=\"event:talk_101\">[�ٻ�1���Լ��Ĺ���1]</a> \n\n    <a href=\"event:talk_102\">[�ٻ�2���Լ��Ĺ���2]</a> \n\n    <a href=\"event:talk_103\">[�ٻ�Ұ�����]</a> \n\n"
end

function talk_101(luaMgr, client, params)
    luaMgr:CallMonstersForGameClient(client, 14, 1001, 1)
end

function talk_102(luaMgr, client, params)
    luaMgr:CallMonstersForGameClient(client, 15, 1002, 1)
end

function talk_103(luaMgr, client, params)
    luaMgr:AddDynamicMonsters(client, 14, 1, 25, 24, 3)
end
