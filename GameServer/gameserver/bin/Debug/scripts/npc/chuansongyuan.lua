-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 传送员
function talk(luaMgr, client, params)
  return '<span class="title">城市地图：</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap1">龙 隐 村</a>    <a href="event:_tomap2">龙    城</a>    <a href="event:_tomap6">传奇之野</a>\n'
       ..'<a href="event:_tomap1011">三 清 观</a>    <a href="event:_tomap4400">王    城</a>\n'
		..'</span>\n'

       ..'<span class="title">练级地图：</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap3">万 兽 谷</a>    <a href="event:_tomap4">血林禁地</a>    <a href="event:_tomap5">莫 邪 殿</a>\n'
       ..'<a href="event:_tomap7">绝迹鬼道</a>    <a href="event:_tomap8">浊世炼圣</a>    <a href="event:_tomap9">仙踪魔狱</a>\n'
       ..'<a href="event:_tomap10">中天走廊</a>    <a href="event:_tomap11">创世冰原</a>    <a href="event:_tomap12">天绝炼狱</a>\n'
       ..'<a href="event:_tomap13">轮 回 道</a>    <a href="event:_tomap14">圣影魔穴</a>    <a href="event:_tomap15">焚天古刹</a>\n'
		..'</span>\n'	

       ..'<span class="title">装备地图：</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap6000">血林魔1禁</a>    <a href="event:_tomap6001">神剑广场</a>    <a href="event:_tomap6002">浊世深渊</a>\n\n'
		..'</span>\n'

	..'<span class="title">挖矿地图：</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:tomap30">矿洞</a>\n'
		..'</span>\n'

end

function _tomap1(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 1)
end

function _tomap2(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 15, 2)
end

function _tomap6(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 30, 6)
end

function _tomap1011(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 15, 1011)
end

-- 王城 35级
function _tomap4400(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 35, 4400)
end

function _tomap3(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 20, 3)
end

function _tomap4(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 25, 4)
end

function _tomap5(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 30, 5)
end

function _tomap7(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 32, 7)
end

function _tomap8(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 38, 8)
end

function _tomap9(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 40, 9)
end

function _tomap10(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 45, 10)
end

function _tomap11(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 11)
end

function _tomap12(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 12)
end

function _tomap13(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 13)
end

function _tomap14(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 60, 14)
end


function _tomap15(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 60, 15)
end

function _tomap6000(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 35, 6000)
end

function _tomap6001(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 40, 6001)
end

function _tomap6002(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 45, 6002)
end

function tomap30(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 40, 30)
end

-- 检查和传送
function checkAndLetGo(luaMgr, client, level, mapid)
     if (not checkLevel(luaMgr, client, level)) then
        luaMgr:Error(client, '角色等级不够'..level..'，不能传送')
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