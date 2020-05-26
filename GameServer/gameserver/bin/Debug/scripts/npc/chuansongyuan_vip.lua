-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 传送员
function talk(luaMgr, client, params)
  return '<span class="title">BOSS地图：</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap303">万寿谷四层[21级]</a>    <a href="event:_tomap402">血 林 仙 禁[26级]</a>\n'
       ..'<a href="event:_tomap502">神  剑  冢[32级]</a>    <a href="event:_tomap702">异 域 神 殿[32级]</a>\n'
       ..'<a href="event:_tomap802">浊世三重天[38级]</a>    <a href="event:_tomap905">仙魔走廊五层[44级]</a>\n'
		..'</span>\n'

       ..'<span class="title">VIP专属：</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap4250">BOSS神殿[40级]</a>\n'
		..'</span>\n'	

end

function _tomap303(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 21, 303)
end

function _tomap402(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 26, 402)
end

function _tomap502(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 32, 502)
end

function _tomap702(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 32, 702)
end

function _tomap802(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 38, 802)
end

function _tomap905(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 44, 905)
end

function _tomap4250(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 40, 4250)
end

-- 检查和传送
function checkAndLetGo(luaMgr, client, viptype, level, mapid)
     if (not checkLevel(luaMgr, client, level)) then
        luaMgr:Error(client, '角色等级不够'..level..'，不能传送')
	return 'keepopen'
     end

     if (not checkVipType(luaMgr, client, viptype)) then
        luaMgr:Error(client, '不是VIP，不能传送')
	return 'keepopen'
     end

     luaMgr:GotoMap(client, mapid)
     return 'closewindow'
end

-- 检查角色层级
function checkLevel(luaMgr, client, level)
     if (luaMgr:GetRoleLevel(client) < level) then
        return false
     end

     return true
end

-- 检查角色vip等级
function checkVipType(luaMgr, client, viptype)
     if (luaMgr:GetVipType(client) < viptype) then
        return false
     end

     return true
end