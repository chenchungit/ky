function enterArea(luaMgr, client, params)
	luaMgr:Error(client, "����ͻ�䣬�콵����")
	luaMgr:SendGameEffect(client, "xiayu1.swf", 0, 1, "xiayu2.mp3")
end

function leaveArea(luaMgr, client, params)
	luaMgr:SendGameEffect(client, "", 0)
end