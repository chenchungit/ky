function enterArea(luaMgr, client, params)
	luaMgr:NotifySelfDeco(client, 60000, 1, -1, 6112, 2660 - 140, 0, -1, -1, 0, 0)
end

function leaveArea(luaMgr, client, params)
	luaMgr:NotifySelfDeco(client, 60000, -1, -1, 0, 0, 0, -1, -1, 0, 0)
end