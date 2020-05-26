-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">村民: </span>\n'
	 ..'<span class="chuansong"><span class="white">海盗猖獗，行径恶劣，捕鱼是我们唯一的\n生计，如今这也难维持下去，这以后的日\n子可怎么过啊！</span></span>\n'
	 ..'<span class="title_0">玩家: </span>\n'
	 ..'<span class="chuansong"><span class="white">这海盗已被我教训一番，相信以后会有所\n收敛，还是先让在下为你疗伤，以免留下\n后患。</span></span>\n\n'
	 ..'<span class="right"><a href="event:jiuzhishangyuan\">立刻救助</a></span>\n'
end

function jiuzhishangyuan(luaMgr, client, params)
  --usingBinding, usedTimeLimited
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100002, 1, true)
	--luaMgr:Error(client, "执行结果"..tostring(result))
	--result = true
	if result then
		luaMgr:RemoveNPCForClient(client, 18)
		luaMgr:AddNPCForClient(client, 17, 3953, 5490)
		
		--设置完成了任务
		luaMgr:HandleTask(client, 0, 18, 0, 9)

		--luaMgr:NotifySelfDeco(client, 81001, 1, -1, 5366, 3721, 0, 5599 - 500, 3733 - 500, 10000, 10000)
	else
	  -- 通知客户端错误
	  luaMgr:Error(client, "没有找到疗伤药，无法治疗")
	end
end
