-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

--福袋执行函数
function run(luaMgr, client, params)
  --luaMgr:Error(client, client, "测试发送消息!")
  if luaMgr:get_param(client, "open_fudai_num") == "" then
    luaMgr:set_param(client, "open_fudai_num", 0)
  end
  if tonumber(luaMgr:get_param(client, "open_fudai_dayid")) ~= luaMgr:Today() then
    luaMgr:set_param(client, "open_fudai_dayid", luaMgr:Today())
    luaMgr:set_param(client, "open_fudai_num", 0)
  end
  local vipType = luaMgr:GetVipType(client)
  if 0 == vipType and tonumber(luaMgr:get_param(client, "open_fudai_num")) >= 10 and tonumber(luaMgr:get_param(client, "open_fudai_dayid")) == luaMgr:Today() then
    luaMgr:set_param(client, "open_fudai_num", tonumber(luaMgr:get_param(client, "open_fudai_num")) + 1)
    luaMgr:Error(client, "你今天已经开启了" .. luaMgr:get_param(client, "open_fudai_num") .. "次福袋!")
    luaMgr:Error(client, "建议您留到明天再开启!")
    luaMgr:Error(client, "非VIP玩家每天第11个福袋开始为10绑定铜钱!")
    luaMgr:AddMoney1(client, 10)
    return 
  end
  if 0 == vipType and tonumber(luaMgr:get_param(client, "open_fudai_num")) < 10 and tonumber(luaMgr:get_param(client, "open_fudai_dayid")) == luaMgr:Today() then
    luaMgr:set_param(client, "open_fudai_num", tonumber(luaMgr:get_param(client, "open_fudai_num")) + 1)
    luaMgr:Hot(client, "你今天已经开启了" .. luaMgr:get_param(client, "open_fudai_num") .. "次福袋!")
    local l = math.random(0, 7)
    if l == 0 then
      luaMgr:AddMoney1(client, tonumber(luaMgr:get_level(client)) * 460)
      luaMgr:Hot(client, "开启福袋:绑定铜钱+" .. tonumber(luaMgr:get_level(client)) * 460 .. "")
      return 
    end
    if l == 1 then
      luaMgr:AddMoney1(client, tonumber(luaMgr:get_level(client)) * 360)
      luaMgr:Hot(client, "开启福袋:绑定铜钱+" .. tonumber(luaMgr:get_level(client)) * 360 .. "")
      return 
    end
    if l == 2 then
      luaMgr:AddMoney1(client, tonumber(luaMgr:get_level(client)) * 520)
      luaMgr:Hot(client, "开启福袋:绑定铜钱+" .. tonumber(luaMgr:get_level(client)) * 520 .. "")
      return 
    end
    if l == 4 then
      luaMgr:AddExp(client, tonumber(luaMgr:get_level(client)) * 2255)
      luaMgr:Hot(client, "开启福袋:经验+" .. tonumber(luaMgr:get_level(client)) * 2225 .. "")
      return 
    end
    if l == 5 then
      luaMgr:AddUserGold(client, tonumber(luaMgr:get_level(client)) * 3)
      luaMgr:Hot(client, "开启福袋:绑定元宝+" .. tonumber(luaMgr:get_level(client)) * 3 .. "")
      return 
    end
    if l == 6 then
      luaMgr:AddUserGold(client, tonumber(luaMgr:get_level(client)) * 5)
      luaMgr:Hot(client, "开启福袋:绑定元宝+" .. tonumber(luaMgr:get_level(client)) * 5 .. "")
      return 
    end
    if l == 7 then
      luaMgr:AddUserGold(client, tonumber(luaMgr:get_level(client)) * 7)
      luaMgr:Hot(client, "开启福袋:绑定元宝+" .. tonumber(luaMgr:get_level(client)) * 7 .. "")
      return 
    end
    return 
  end
  local maxNum = 0
  if (1 == vipType) then
    maxNum = 20;
  end
  if (3 == vipType) then
    maxNum = 25;
  end
  if (6 == vipType) then
    maxNum = 30;
  end
  if vipType > 0 and tonumber(luaMgr:get_param(client, "open_fudai_num")) >= maxNum and tonumber(luaMgr:get_param(client, "open_fudai_dayid")) == luaMgr:Today() then
    luaMgr:set_param(client, "open_fudai_num", tonumber(luaMgr:get_param(client, "open_fudai_num")) + 1)
    luaMgr:Error(client, "你今天已经开启了" .. luaMgr:get_param(client, "open_fudai_num") .. "次福袋!")
    luaMgr:Error(client, "建议您留到明天再开启!")
    luaMgr:Error(client, "每天第" .. (maxNum + 1) .. "个福袋开始为10绑定铜钱!")
    luaMgr:AddMoney1(client, 10)
    return 
  end
  if vipType > 0 and tonumber(luaMgr:get_param(client, "open_fudai_num")) < maxNum and tonumber(luaMgr:get_param(client, "open_fudai_dayid")) == luaMgr:Today() then
    luaMgr:set_param(client, "open_fudai_num", tonumber(luaMgr:get_param(client, "open_fudai_num")) + 1)
    luaMgr:Hot(client, "你今天已经开启了" .. luaMgr:get_param(client, "open_fudai_num") .. "次福袋!")
    local l = math.random(0, 7)
    if l == 0 then
      luaMgr:AddMoney1(client, tonumber(luaMgr:get_level(client)) * 260)
      luaMgr:Hot(client, "开启福袋:绑定铜钱+" .. tonumber(luaMgr:get_level(client)) * 260 .. "")
      return 
    end
    if l == 1 then
      luaMgr:AddMoney1(client, tonumber(luaMgr:get_level(client)) * 360)
      luaMgr:Hot(client, "开启福袋:绑定铜钱+" .. tonumber(luaMgr:get_level(client)) * 360 .. "")
      return 
    end
    if l == 2 then
      luaMgr:AddMoney1(client, tonumber(luaMgr:get_level(client)) * 520)
      luaMgr:Hot(client, "开启福袋:绑定铜钱+" .. tonumber(luaMgr:get_level(client)) * 520 .. "")
      return 
    end
    if l == 4 then
      luaMgr:AddExp(client, tonumber(luaMgr:get_level(client)) * 2255)
      luaMgr:Hot(client, "开启福袋:经验+" .. tonumber(luaMgr:get_level(client)) * 2225 .. "")
      return 
    end
    if l == 5 then
      luaMgr:AddUserGold(client, tonumber(luaMgr:get_level(client)) * 3)
      luaMgr:Hot(client, "开启福袋:绑定元宝+" .. tonumber(luaMgr:get_level(client)) * 3 .. "")
      return 
    end
    if l == 6 then
      luaMgr:AddUserGold(client, tonumber(luaMgr:get_level(client)) * 5)
      luaMgr:Hot(client, "开启福袋:绑定元宝+" .. tonumber(luaMgr:get_level(client)) * 5 .. "")
      return 
    end
    if l == 7 then
      luaMgr:AddUserGold(client, tonumber(luaMgr:get_level(client)) * 7)
      luaMgr:Hot(client, "开启福袋:绑定元宝+" .. tonumber(luaMgr:get_level(client)) * 7 .. "")
      return
    end
  end
end
