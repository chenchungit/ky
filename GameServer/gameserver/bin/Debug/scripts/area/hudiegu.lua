function enterArea(luaMgr, client, params)
	luaMgr:Error(client, "进入蝴蝶谷区域")
	luaMgr:BroadCastGameEffect(1, -1, "fireworks.swf", 0, 3, "fireworks.mp3")
end

function leaveArea(luaMgr, client, params)
	luaMgr:Error(client, "离开蝴蝶谷区域")
	luaMgr:BroadCastGameEffect(1, -1, "", 0, 1)
end