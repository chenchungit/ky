-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font color=\"#ff0000\">�����:<font color=\"#10d5ff\">������ʱ�����ǲ��Ǹе���į��\n\n���顢�е��䣿\n\n��������ߴ��Űɣ�\n\n<font color=\"#ff0000\">���Ҹ��㼸����ܰ����ů��\n\n <a href=\"event:talk_101\">[��  ��]</a>    <a href=\"event:onMyNpcMenuClick\">[���ǳ���]</a>    <a href=\"event:onMyNpcMenuClick1\">[��������]</a>\n\n"
end

function onMyNpcMenuClick(luaMgr, client, params)
     luaMgr:GotoMap(client, 4)
--   return string.format("��ɫ ID = %d, ��ɫ���� %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function onMyNpcMenuClick1(luaMgr, client, params)
    luaMgr:GotoMap(client, 3)
end
