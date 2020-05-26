-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font color=\"#ff0000\">沙城战申请和奖励领取:<font color=\"#10d5ff\">\n\n申请之后，一般隔天举行,\n\n多人申请，依次靠后\n\n 只有帮主才能申请，\n\n同时，只有沙城城主才能领取奖励\n\n      <a href=\"event:_requestcitywar\">[申请沙城争夺战]</a> \n\n    <a href=\"event:_getcityaward\">[领取沙城每日奖励]</a> \n\n"
end

function _requestcitywar(luaMgr, client, params)
--    luaMgr:CallMonstersForGameClient(client, 15, 1002, 1)
end

function _getcityaward(luaMgr, client, params)
--    luaMgr:AddDynamicMonsters(client, 14, 1, 25, 24, 3)
end
