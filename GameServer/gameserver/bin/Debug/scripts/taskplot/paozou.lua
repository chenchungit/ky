-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function run(luaMgr, client, params)
  luaMgr:RemoveNPCForClient(client, 17)
  luaMgr:NotifySelfDeco(client, 81001, 1, -1, 61 * 64, 170 * 32, 0, 65 * 64, 156 * 32, 10000, 15000)
end


