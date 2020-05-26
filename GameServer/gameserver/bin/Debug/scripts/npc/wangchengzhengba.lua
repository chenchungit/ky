-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 王城争霸
function talk(luaMgr, client, params)
-- 最近王城争霸赛时间
  result, nearTime, nearReqBangHui = luaMgr:GetNextCityBattleTimeAndBangHui()
  return '<span class="title_center">王城争霸活动说明</span>\n'
	 ..'<span class="title_0">地图设定：</span>'
	 ..'<span class="padding_60">王城及皇宫地图不可原地复活不可\n'
	 ..'使用【随机传送石】\n'
	 ..'<span class="mi">死亡后掉落物品,请大家注意！</span></span>\n\n'

	 ..'<span class="title_0">占领王城：</span>'
	 ..'<span class="padding_60">王城战役结束后,皇宫内只剩一个\n'
	 ..'行会的成员\n'
	 ..'<span class="mi">活动开放时间内,随时可对王城发\n起攻击</span></span>\n\n'

	 ..'<span class="title_0">奖励说明：</span>'
	 ..'<span class="padding_60">王族帮会帮主每日可免费领取\n'
	 ..'<span class="mi">元宝*2000、铜钱*100万</span></span>\n\n'

	 ..'<span class="white">帮主需携【攻城号角】才能申请王城争霸战\n'
	 ..'攻城号角：可击杀【葬天魔君】获得\n\n</span>'

	 ..'<span class="center"><a href="event:_requestcitywar">申请王城争霸</a></span>\n'
	 ..'<span class="white">最近时间：【'..nearTime..'】\n'
	 ..'由帮会：【'..nearReqBangHui..'】申请触发开启</span>\n\n'
  
	 ..'<span class="center"><a href="event:_lookQuanBuZhanYi">查看全部战役</a></span>\n'
end

-- 申请王城争霸===_getcityaward用来领取奖励，一样由客户端自己发送指令完成
function _requestcitywar(luaMgr, client, params)
--    这儿只需要写一个空函数就行，不写也行，这个函数会被客户端解惑，并由客户端发送特殊指令完成，因为，这个功能一起有指令支持
end

-- 查看全部战役
function _lookQuanBuZhanYi(luaMgr, client, params)
  local sItems = luaMgr:GetCityBattleTimeAndBangHuiListString()
  local list = Split(sItems, ',', 20)
  local myStr = '<font size="12" color="#eefe2c">近期王城争霸时间:\n\n'

  --组装显示
  for i,s in ipairs(list) do
    if i % 2 > 0 then
	myStr = myStr..'<font color="#ff0000">'..s..'\n'
    else
	myStr = myStr..'<font color="#00ff00">由帮会['..s..']申请触发开启\n'
    end
  end

  myStr = myStr..'\n<font size="12" color="#00ff00"><a class="myunderline" href="event:_lookPrePage">返回</a>\n\n'

  return myStr
end
 
-- 返回
function _lookPrePage(luaMgr, client, params)
  return talk(luaMgr, client, params)
end

-- 字符串切分
function Split(str, delim, maxNb)    
    -- Eliminate bad cases...    
    if string.find(str, delim) == nil then   
        return { str }   
    end   
    if maxNb == nil or maxNb < 1 then   
        maxNb = 0    -- No limit    
    end   
    local result = {}   
    local pat = "(.-)" .. delim .. "()"    
    local nb = 0   
    local lastPos    
    for part, pos in string.gfind(str, pat) do   
        nb = nb + 1   
        result[nb] = part    
        lastPos = pos    
        if nb == maxNb then break end   
    end   
    -- Handle the last field    
    if nb ~= maxNb then   
        result[nb + 1] = string.sub(str, lastPos)    
    end   
    return result    
end  
