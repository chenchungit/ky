function enterArea(luaMgr, client, params)
	luaMgr:Error(client, "�������������")
	luaMgr:BroadCastGameEffect(1, -1, "xiayu1.swf", 0)
end

function leaveArea(luaMgr, client, params)
	luaMgr:Error(client, "�뿪����������")
	luaMgr:BroadCastGameEffect(1, -1, "", 0)
end