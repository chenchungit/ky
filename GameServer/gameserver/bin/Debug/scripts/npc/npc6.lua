-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font size=\"14\" color=\"#ffff00\">召唤测试:</font> <font color=\"#10d5ff\">测试玩家召唤自己的怪物\n\n      <a href=\"event:talk_101\">[召唤1个自己的怪物1]</a> \n\n    <a href=\"event:talk_102\">[召唤2个自己的怪物2]</a> \n\n    <a href=\"event:talk_103\">[召唤野外怪物]</a> \n\n"
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
