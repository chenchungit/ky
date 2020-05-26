-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- BOSS挑战说明
function talk(luaMgr, client, params)
  lefttimesStr = luaMgr:GetBossFuBenLeftTimeString(client)
  return '<span class="title_center">BOSS挑战说明</span>\n'
       ..'<span class="title_0">地图设定：</span>'
       ..'<span class="padding_60">从15级开始开放BOSS挑战副本\n'
       ..'每5级一个BOSS,最高B0SS60级</span>\n\n'

       ..'<span class="title_0">BOSS掉落：</span>'
       ..'<span class="padding_60">挑战副本内BOSS装备掉率超高\n'
       ..'一定几率掉落<span class="red">特殊武器</span></span>\n\n'

       ..'<span class="title_0">挑战次数：</span>'
       ..'<span class="padding_60">普通玩家每日3次挑战机会\n'
       ..'会员玩家每日5次挑战机会</span>\n\n'

       ..'<span class="title_center2">商城可购买【BOSS挑战卷】增加挑战机会</span>\n\n'

       ..'<span class="title_0">剩余次数：</span>'
       ..'<span class="padding_60">'..lefttimesStr..'次</span>\n\n'
       ..'<span class="center"><a href="event:_enterbossfuben">进入BOSS挑战副本</a></span>\n'
end

function _enterbossfuben(luaMgr, client, params)
  luaMgr:EnterBossFuBen(client)
end
