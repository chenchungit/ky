function enterArea(luaMgr, client, params)
	luaMgr:Error(client, "风云突变，天降大雨")
	luaMgr:SendGameEffect(client, "xiayu1.swf", 0, 1, "xiayu2.mp3")
end

function leaveArea(luaMgr, client, params)
	luaMgr:SendGameEffect(client, "", 0)
end