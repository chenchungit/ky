-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function calcTaskAwards(luaMgr, client, params)
     local level = math.min(luaMgr:GetRoleLevel(client),70)
     local val = level * level * 316
     return val       
end
