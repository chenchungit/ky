-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 古墓秘境
function talk(luaMgr, client, params)
  return '<span class="title_center">古墓秘境说明</span>\n'

       ..'<span class="title_0">地图说明：</span>'
       ..'<span class="padding_60">秘境地图为安全地图不可PK\n'
       ..'秘境地图内死亡不掉落物品</span>\n\n'

       ..'<span class="title_0">地图级别：</span>'
       ..'<span class="padding_60">秘境每10级一个跨度,系统会自动\n'
       ..'玩家所在的等级段传入相应的地图</span>\n\n'

       ..'<span class="title_0">秘境时间：</span>'
       ..'<span class="padding_60">普通玩家每天可修炼120分钟\n'
       ..'VIP玩家每天可修炼240分钟\n'
       ..'<span class="mi">【古墓秘令】可增加修炼时间</span></span>\n\n'

       ..'<span class="title_0">经验倍率：</span>'
       ..'<span class="padding_60">白银VIP玩家,经验倍率1.1倍\n'
       ..'黄金VIP玩家,经验倍率1.2倍\n'
       ..'钻石VIP玩家,经验倍率1.3倍</span>\n\n'

       ..'<span class="title_0">等级限制：</span>'
       ..'<span class="padding_60">30级以上玩家</span>\n\n'

       ..'<span class="center"><a href="event:_gotoGuMuMiJing">进入【古墓秘境】</a></span>\n'
end

-- 进入古墓密境函数
function _gotoGuMuMiJing(luaMgr, client, params)
--    执行进入古墓地图 第一个参数是npc挂的脚本id， 第二个参数是npcid
      result = luaMgr:GotoGuMuMap(client)
      if result < 0 then
	-- 通知客户端错误
	luaMgr:Error(client, "30级以上玩家才能进入古墓地图！")
      end
end
