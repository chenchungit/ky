-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font color=\"#ff0000\">ɳ��ս����ͽ�����ȡ:<font color=\"#10d5ff\">\n\n����֮��һ��������,\n\n�������룬���ο���\n\n ֻ�а����������룬\n\nͬʱ��ֻ��ɳ�ǳ���������ȡ����\n\n      <a href=\"event:_requestcitywar\">[����ɳ������ս]</a> \n\n    <a href=\"event:_getcityaward\">[��ȡɳ��ÿ�ս���]</a> \n\n"
end

function _requestcitywar(luaMgr, client, params)
--    luaMgr:CallMonstersForGameClient(client, 15, 1002, 1)
end

function _getcityaward(luaMgr, client, params)
--    luaMgr:AddDynamicMonsters(client, 14, 1, 25, 24, 3)
end
