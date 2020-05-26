-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return "<font color=\"#ff0000\">篝火堆:<font color=\"#10d5ff\">曾几何时，你是不是感到寂寞、\n\n空虚、感到冷？\n\n就在我身边待着吧！\n\n<font color=\"#ff0000\">让我给你几分温馨和温暖！\n\n <a href=\"event:talk_101\">[龙  城]</a>    <a href=\"event:onMyNpcMenuClick\">[龙城城外]</a>    <a href=\"event:onMyNpcMenuClick1\">[至尊王城]</a>\n\n"
end

function onMyNpcMenuClick(luaMgr, client, params)
     luaMgr:GotoMap(client, 4)
--   return string.format("角色 ID = %d, 角色名称 %s", client:GetObjectID(), luaMgr:GetUserName("ggk"))
end

function onMyNpcMenuClick1(luaMgr, client, params)
    luaMgr:GotoMap(client, 3)
end
