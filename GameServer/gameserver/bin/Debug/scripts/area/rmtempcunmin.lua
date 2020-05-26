function enterArea(luaMgr, client, params)
end

function leaveArea(luaMgr, client, params)
	luaMgr:RemoveNPCForClient(client, 17)
end