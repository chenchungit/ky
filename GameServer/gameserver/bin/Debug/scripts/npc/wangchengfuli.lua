-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '<font color="#10d5ff">夺得王城的帮会帮主，每日可在我处领取\n\n2000元宝,100000铜钱奖励！\n\n<a href="event:_getcityaward">[领取王城福利]</a> \n\n'
end

function _getcityaward(luaMgr, client, params)
--    luaMgr:AddDynamicMonsters(client, 14, 1, 25, 24, 3)
end
