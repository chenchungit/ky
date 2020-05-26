function enterArea(luaMgr, client, params)
	luaMgr:Error(client, "进入风流谷区域")
	luaMgr:BroadCastGameEffect(1, -1, "xiayu1.swf", 0)
end

function leaveArea(luaMgr, client, params)
	luaMgr:Error(client, "离开风流谷区域")
	luaMgr:BroadCastGameEffect(1, -1, "", 0)
end