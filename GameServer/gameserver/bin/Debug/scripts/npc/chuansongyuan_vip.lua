-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ����Ա
function talk(luaMgr, client, params)
  return '<span class="title">BOSS��ͼ��</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap303">���ٹ��Ĳ�[21��]</a>    <a href="event:_tomap402">Ѫ �� �� ��[26��]</a>\n'
       ..'<a href="event:_tomap502">��  ��  ڣ[32��]</a>    <a href="event:_tomap702">�� �� �� ��[32��]</a>\n'
       ..'<a href="event:_tomap802">����������[38��]</a>    <a href="event:_tomap905">��ħ�������[44��]</a>\n'
		..'</span>\n'

       ..'<span class="title">VIPר����</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap4250">BOSS���[40��]</a>\n'
		..'</span>\n'	

end

function _tomap303(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 21, 303)
end

function _tomap402(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 26, 402)
end

function _tomap502(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 32, 502)
end

function _tomap702(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 32, 702)
end

function _tomap802(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 38, 802)
end

function _tomap905(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 44, 905)
end

function _tomap4250(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 40, 4250)
end

-- ���ʹ���
function checkAndLetGo(luaMgr, client, viptype, level, mapid)
     if (not checkLevel(luaMgr, client, level)) then
        luaMgr:Error(client, '��ɫ�ȼ�����'..level..'�����ܴ���')
	return 'keepopen'
     end

     if (not checkVipType(luaMgr, client, viptype)) then
        luaMgr:Error(client, '����VIP�����ܴ���')
	return 'keepopen'
     end

     luaMgr:GotoMap(client, mapid)
     return 'closewindow'
end

-- ����ɫ�㼶
function checkLevel(luaMgr, client, level)
     if (luaMgr:GetRoleLevel(client) < level) then
        return false
     end

     return true
end

-- ����ɫvip�ȼ�
function checkVipType(luaMgr, client, viptype)
     if (luaMgr:GetVipType(client) < viptype) then
        return false
     end

     return true
end